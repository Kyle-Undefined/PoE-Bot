namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using PoE.Bot.Contexts;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public class EnsureFromUserCriterion : ICriterion<IMessage>
    {
        private readonly ulong _id;

        public EnsureFromUserCriterion(IUser user) => _id = user.Id;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public EnsureFromUserCriterion(ulong id) => _id = id;

        public Task<bool> JudgeAsync(GuildContext context, IMessage parameter)
        {
            bool ok = _id == parameter.Author.Id;
            return Task.FromResult(ok);
        }
    }
}