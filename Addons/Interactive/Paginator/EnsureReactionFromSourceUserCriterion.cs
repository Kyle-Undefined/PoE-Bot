namespace PoE.Bot.Addons.Interactive
{
    using Discord.WebSocket;
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    internal class EnsureReactionFromSourceUserCriterion : ICriterion<SocketReaction>
    {
        public Task<bool> JudgeAsync(GuildContext context, SocketReaction parameter)
        {
            bool ok = parameter.UserId == context.User.Id;
            return Task.FromResult(ok);
        }
    }
}