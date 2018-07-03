namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class ReactionCriteria : ICriteria<SocketReaction>
    {
        public Task<bool> JudgeAsync(Context context, SocketReaction param)
            => Task.FromResult(param.UserId == context.User.Id);
    }
}