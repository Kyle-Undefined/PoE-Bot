namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public class FromUser : ICriteria<IMessage>
    {
        public FromUser(IUser user)
            => UserID = user.Id;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public FromUser(ulong id)
            => UserID = id;

        private ulong UserID { get; }

        public Task<bool> JudgeAsync(Context context, IMessage param)
            => Task.FromResult(UserID == param.Author.Id);
    }
}