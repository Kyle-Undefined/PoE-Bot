using Discord;

namespace PoE.Bot.Commands.Permissions
{
    public interface IPermissionChecker
    {
        string Id { get; }
        bool CanRun(Command cmd, IGuildUser user, IMessage message, IMessageChannel channel, IGuild guild, out string error);
    }
}
