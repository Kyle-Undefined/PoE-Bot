namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;
    using System.ComponentModel;
    public class FromUser : ICriteria<IMessage>
    {
        ulong UserID { get; }
        public FromUser(IUser User) => UserID = User.Id;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FromUser(ulong Id) => UserID = Id;

        public Task<bool> JudgeAsync(IContext Context, IMessage Param)
            => Task.FromResult(UserID == Param.Author.Id);
    }
}
