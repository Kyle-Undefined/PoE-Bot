namespace PoE.Bot.Addons.Interactive.Criterias
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Criteria<T> : ICriteria<T>
    {
        private readonly List<ICriteria<T>> criterias = new List<ICriteria<T>>();

        public void AddCriteria(ICriteria<T> criteria)
            => criterias.Add(criteria);

        public async Task<bool> JudgeAsync(Context context, T param)
        {
            foreach (var criteria in criterias)
            {
                bool result = await criteria.JudgeAsync(context, param).ConfigureAwait(false);
                if (!result)
                    return false;
            }
            return true;
        }
    }
}