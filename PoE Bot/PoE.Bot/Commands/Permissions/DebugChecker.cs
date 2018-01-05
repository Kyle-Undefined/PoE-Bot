using Discord;

namespace PoE.Bot.Commands.Permissions
{
    public class DebugChecker : IPermissionChecker
    {
        public string Id { get { return "CoreDebugChecker"; } }

        public bool CanRun(Command command, IGuildUser user, IMessage message, IMessageChannel channel, IGuild guild, out string error)
        {
            error = "This is a debug command. It can be only ran by Kyle Undefined.";
            if (user.Id == 164306034331090945u && user.Username == "Kyle Undefined" && user.Discriminator == "1745")
                return true;
            return false;
        }
    }
}
