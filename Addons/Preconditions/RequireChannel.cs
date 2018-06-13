namespace PoE.Bot.Addons.Preconditions
{
    using System;
    using System.Linq;
    using Discord.Commands;
    using System.Threading.Tasks;

    public class RequireChannel : PreconditionAttribute
    {
        private readonly string _name;

        public RequireChannel(string name)
        {
            _name = name;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            if (Context.Channel.Name == _name || Context.Channel.Name.StartsWith(_name + "-"))
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} The ancestors watch over me, use command in `{_name}` channels."));
        }
    }

    public class RequireChannels : PreconditionAttribute
    {
        private readonly string[] _channels;

        public RequireChannels(string[] channels)
        {
            _channels = channels;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            if (_channels.Contains(Context.Channel.Name))
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} The ancestors watch over me, use command in `{string.Join(", ", _channels)}` channels."));
        }
    }

    public class BanChannel : PreconditionAttribute
    {
        private readonly string _name;

        public BanChannel(string name)
        {
            _name = name;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services)
        {
            if (Context.Channel.Name == _name || Context.Channel.Name.StartsWith(_name + "-"))
                return Task.FromResult(PreconditionResult.FromError($"{Extras.Cross} The ancestors watch over me, can not use command in `{_name}` channels."));
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
