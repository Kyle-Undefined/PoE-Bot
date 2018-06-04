namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class ReactionCriteria : ICriteria<SocketReaction>
    {
        public Task<bool> JudgeAsync(IContext Context, SocketReaction Param)
            => Task.FromResult(Param.UserId == Context.User.Id);
    }
}
