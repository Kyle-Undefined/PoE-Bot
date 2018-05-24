using Discord;

namespace PoE.Bot.Commands.Permissions
{
    public class Moderator : IPermissionChecker
    {
        public string Id { get { return "CoreModerator"; } }

        public bool CanRun(Command command, IGuildUser user, IMessage message, IMessageChannel channel, IGuild guild, out string error)
        {
            error = "This is a Moderator only command.";

            if (user.GuildPermissions.Administrator)
                return true;

            foreach (var role in user.RoleIds)
            {
                if (role == 349954936261312512u || role == 439500547075080203u)
                    return true;
            }

            return false;
        }
    }
}