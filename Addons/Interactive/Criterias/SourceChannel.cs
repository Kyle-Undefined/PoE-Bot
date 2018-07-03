namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;

    public class SourceChannel : ICriteria<IMessage>
    {
        public Task<bool> JudgeAsync(Context context, IMessage param)
            => Task.FromResult(context.Channel.Id == param.Channel.Id);
    }
}