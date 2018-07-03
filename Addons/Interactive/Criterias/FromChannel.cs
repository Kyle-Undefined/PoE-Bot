namespace PoE.Bot.Addons.Interactive.Criterias
{
    using Discord;
    using System.Threading.Tasks;

    public class FromChannel : ICriteria<IMessage>
    {
        public FromChannel(IMessageChannel channel)
            => ChannelID = channel.Id;

        private ulong ChannelID { get; }

        public Task<bool> JudgeAsync(Context context, IMessage param)
            => Task.FromResult(ChannelID == param.Channel.Id);
    }
}