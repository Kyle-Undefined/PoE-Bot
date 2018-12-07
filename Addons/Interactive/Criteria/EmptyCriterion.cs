namespace PoE.Bot.Addons.Interactive
{
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    public class EmptyCriterion<T> : ICriterion<T>
    {
        public Task<bool> JudgeAsync(GuildContext context, T parameter) => Task.FromResult(true);
    }
}