namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;

    public class SourceChannel : ICriteria<IMessage>
    {
        public Task<bool> JudgeAsync(IContext Context, IMessage Param)
            => Task.FromResult(Context.Channel.Id == Param.Channel.Id);
    }
}
