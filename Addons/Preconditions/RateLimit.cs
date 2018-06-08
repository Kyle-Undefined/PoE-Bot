namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using System.Linq;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using PoE.Bot.Helpers;

    public class Ratelimit : PreconditionAttribute
    {
        readonly ConcurrentDictionary<ulong, DateTime> Timeout = new ConcurrentDictionary<ulong, DateTime>();

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            PreconditionResult Result;

            if (Context.User.Id == MethodHelper.RunSync(Context.Client.GetApplicationInfoAsync()).Owner.Id || Context.User.Id == Context.Guild.OwnerId ||
                (Context.User as SocketGuildUser).GuildPermissions.Administrator || (Context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (!Timeout.ContainsKey(Context.User.Id))
            {
                Timeout.TryAdd(Context.User.Id, DateTime.Now);
                Result = PreconditionResult.FromSuccess();
            }
            else if (Timeout[Context.User.Id].AddSeconds(3) > DateTime.Now)
                Result = PreconditionResult.FromError($"{Extras.Cross} Too. Much. Clutter. *3 Second Cooldown*");
            else
            {
                Timeout.AddOrUpdate(Context.User.Id, DateTime.Now, (key, value) => value = DateTime.Now);
                Result = PreconditionResult.FromSuccess();
            }
            Task.Run(() =>
            {
                foreach (var user in Timeout.OrderByDescending(x => x.Value).Where(x => !(x.Value.AddSeconds(3) > DateTime.Now)))
                    Timeout.TryRemove(user.Key, out _);
            });
            return Task.FromResult(Result);
        }
    }
}
