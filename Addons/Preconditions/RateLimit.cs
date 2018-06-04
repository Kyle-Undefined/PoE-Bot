namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using System.Linq;
    using Discord.Commands;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    public class Ratelimit : PreconditionAttribute
    {
        readonly ConcurrentDictionary<ulong, DateTime> Timeout = new ConcurrentDictionary<ulong, DateTime>();
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            PreconditionResult Result;
            if (!Timeout.ContainsKey(context.User.Id))
            {
                Timeout.TryAdd(context.User.Id, DateTime.UtcNow);
                Result = PreconditionResult.FromSuccess();
            }
            else if (Timeout[context.User.Id].AddSeconds(3) > DateTime.UtcNow)
                Result = PreconditionResult.FromError(string.Empty);
            else
            {
                Timeout.AddOrUpdate(context.User.Id, DateTime.UtcNow, (key, value) => value = DateTime.UtcNow);
                Result = PreconditionResult.FromSuccess();
            }
            Task.Run(() =>
            {
                foreach (var User in Timeout.OrderByDescending(x => x.Value).Where(x => !(x.Value.AddSeconds(3) > DateTime.UtcNow)))
                    Timeout.TryRemove(User.Key, out _);
            });
            return Task.FromResult(Result);
        }
    }
}
