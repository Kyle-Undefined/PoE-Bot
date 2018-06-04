namespace PoE.Bot.Addons.Interactive.Criterias
{
    using System.Threading.Tasks;

    public class EmptyCriteria<T> : ICriteria<T>
    {
        public Task<bool> JudgeAsync(IContext Context, T Param) => Task.FromResult(true);
    }
}
