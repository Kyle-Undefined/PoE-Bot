namespace PoE.Bot.Addons.Preconditions
{
    using Discord.Commands;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class BanChannel : PreconditionAttribute
    {
        private readonly string channelName;

        public BanChannel(string name)
            => channelName = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel.Name == channelName || context.Channel.Name.StartsWith(channelName + "-"))
                return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} The ancestors watch over me, can not use command in `{channelName}` channels."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }

    public partial class RequireChannel : PreconditionAttribute
    {
        private readonly string channelName;

        public RequireChannel(string name)
            => channelName = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel.Name == channelName || context.Channel.Name.StartsWith(channelName + "-"))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} The ancestors watch over me, use command in `{channelName}` channels."));
        }
    }

    public partial class RequireChannels : PreconditionAttribute
    {
        private readonly string[] channelNames;

        public RequireChannels(string[] channels)
            => channelNames = channels;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (channelNames.Contains(context.Channel.Name))
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} The ancestors watch over me, use command in `{string.Join(", ", channelNames)}` channels."));
        }
    }
}