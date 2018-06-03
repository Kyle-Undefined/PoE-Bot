namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;

    public class SourceUser : ICriteria<IMessage>
    {
        public Task<bool> JudgeAsync(IContext Context, IMessage Param)
            => Task.FromResult(Context.User.Id == Param.Author.Id);
    }
}
