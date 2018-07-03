namespace PoE.Bot.Addons.Interactive.Criterias
{
    using System.Threading.Tasks;

    public class EmptyCriteria<T> : ICriteria<T>
    {
        public Task<bool> JudgeAsync(Context context, T param)
            => Task.FromResult(true);
    }
}