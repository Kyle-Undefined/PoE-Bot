namespace PoE.Bot.Addons.Preconditions
{
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    public class Ratelimit : PreconditionAttribute
    {
        private readonly ConcurrentDictionary<ulong, DateTime> timeout = new ConcurrentDictionary<ulong, DateTime>();

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            PreconditionResult result;

            if (context.User.Id == MethodHelper.RunSync(context.Client.GetApplicationInfoAsync()).Owner.Id || context.User.Id == context.Guild.OwnerId ||
                (context.User as SocketGuildUser).GuildPermissions.Administrator || (context.User as SocketGuildUser).GuildPermissions.ManageGuild)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (!timeout.ContainsKey(context.User.Id))
            {
                timeout.TryAdd(context.User.Id, DateTime.Now);
                result = PreconditionResult.FromSuccess();
            }
            else if (timeout[context.User.Id].AddSeconds(3) > DateTime.Now)
                result = PreconditionResult.FromError($"{Extras.Cross} Too. Much. Clutter. *3 Second Cooldown*");
            else
            {
                timeout.AddOrUpdate(context.User.Id, DateTime.Now, (key, value) => value = DateTime.Now);
                result = PreconditionResult.FromSuccess();
            }

            Task.Run(() =>
            {
                foreach (var user in timeout.OrderByDescending(x => x.Value).Where(x => !(x.Value.AddSeconds(3) > DateTime.Now)))
                    timeout.TryRemove(user.Key, out _);
            });

            return Task.FromResult(result);
        }
    }
}