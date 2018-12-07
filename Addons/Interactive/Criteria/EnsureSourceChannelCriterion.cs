namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using PoE.Bot.Contexts;
    using System.Threading.Tasks;

    public class EnsureSourceChannelCriterion : ICriterion<IMessage>
    {
        public Task<bool> JudgeAsync(GuildContext context, IMessage parameter)
        {
            var ok = context.Channel.Id == parameter.Channel.Id;
            return Task.FromResult(ok);
        }
    }
}