using Discord;

namespace PoE.Bot.Commands.Permissions
{
    public class PermissionChecker : IPermissionChecker
    {
        public string Id { get { return "CoreAdminChecker"; } }

        public bool CanRun(Command command, IGuildUser user, IMessage message, IMessageChannel channel, IGuild guild, out string error)
        {
            error = "";
            var prm = command.RequiredPermission;
            var chn = channel as IGuildChannel;
            if (chn == null)
                return false;
            var chp = user.GetPermissions(chn);
            if (prm == Permission.None && user.GuildPermissions.Administrator)
                return true;
            if ((chp.RawValue & (ulong)prm) == (ulong)prm || (user.GuildPermissions.RawValue & (ulong)prm) == (ulong)prm)
                return true;
            error = "Insufficient Permissions";
            return false;
        }
    }
}
