namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;

    public class FromChannel : ICriteria<IMessage>
    {
        ulong ChannelID { get; }
        public FromChannel(IMessageChannel Channel) => ChannelID = Channel.Id;

        public Task<bool> JudgeAsync(IContext Context, IMessage Param)
            => Task.FromResult(ChannelID == Param.Channel.Id);
    }
}
