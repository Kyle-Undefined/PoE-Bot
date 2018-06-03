namespace PoE.Bot.Addons.Interactive.Criterias
{
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class Criteria<T> : ICriteria<T>
    {
        readonly List<ICriteria<T>> Criterias = new List<ICriteria<T>>();

        public Criteria<T> AddCriteria(ICriteria<T> criteria)
        {
            Criterias.Add(criteria);
            return this;
        }

        public async Task<bool> JudgeAsync(IContext Context, T Param)
        {
            foreach (var Crit in Criterias)
            {
                var result = await Crit.JudgeAsync(Context, Param).ConfigureAwait(false);
                if (!result) return false;
            }
            return true;
        }
    }
}
