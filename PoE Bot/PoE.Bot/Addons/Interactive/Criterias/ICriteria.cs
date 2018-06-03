namespace PoE.Bot.Addons.Interactive.Criterias
{
    using System.Threading.Tasks;

    public interface ICriteria<in T>
    {
        Task<bool> JudgeAsync(IContext Context, T Param);
    }
}
