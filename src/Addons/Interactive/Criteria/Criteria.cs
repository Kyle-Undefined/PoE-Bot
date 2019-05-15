namespace PoE.Bot.Addons.Interactive
{
    using PoE.Bot.Contexts;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Criteria<T> : ICriterion<T>
    {
        private readonly List<ICriterion<T>> _critiera = new List<ICriterion<T>>();

        public Criteria<T> AddCriterion(ICriterion<T> criterion)
        {
            _critiera.Add(criterion);
            return this;
        }

        public async Task<bool> JudgeAsync(GuildContext context, T parameter)
        {
            foreach (var criterion in _critiera)
            {
                var result = await criterion.JudgeAsync(context, parameter).ConfigureAwait(false);
                if (!result)
                    return false;
            }
            return true;
        }
    }
}