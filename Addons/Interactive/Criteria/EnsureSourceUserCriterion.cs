namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    public class EnsureSourceUserCriterion : ICriterion<IMessage>
    {
        public Task<bool> JudgeAsync(GuildContext context, IMessage parameter)
        {
            var ok = context.User.Id == parameter.Author.Id;
            return Task.FromResult(ok);
        }
    }
}