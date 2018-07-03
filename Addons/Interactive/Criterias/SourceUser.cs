namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;

    public class SourceUser : ICriteria<IMessage>
    {
        public Task<bool> JudgeAsync(Context context, IMessage param)
            => Task.FromResult(context.User.Id == param.Author.Id);
    }
}