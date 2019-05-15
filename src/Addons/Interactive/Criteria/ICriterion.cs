namespace PoE.Bot.Addons.Interactive
{
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    public interface ICriterion<in T>
    {
        Task<bool> JudgeAsync(GuildContext context, T parameter);
    }
}