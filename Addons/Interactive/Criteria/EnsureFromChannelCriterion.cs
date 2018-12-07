namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    public class EnsureFromChannelCriterion : ICriterion<IMessage>
    {
        private readonly ulong _channelId;

        public EnsureFromChannelCriterion(IMessageChannel channel) => _channelId = channel.Id;

        public Task<bool> JudgeAsync(GuildContext context, IMessage parameter)
        {
            bool ok = _channelId == parameter.Channel.Id;
            return Task.FromResult(ok);
        }
    }
}