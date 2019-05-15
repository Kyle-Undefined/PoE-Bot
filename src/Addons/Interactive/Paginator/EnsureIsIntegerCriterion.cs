namespace PoE.Bot.Addons.Interactive
{
    using Discord.WebSocket;
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    internal class EnsureIsIntegerCriterion : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(GuildContext context, SocketMessage parameter)
        {
            bool ok = int.TryParse(parameter.Content, out _);
            return Task.FromResult(ok);
        }
    }
}