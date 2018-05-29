using Discord;

namespace PoE.Bot.Commands.Permissions
{
    public class PricerChecker : IPermissionChecker
    {
        public string Id { get { return "CorePriceChecker"; } }

        public bool CanRun(Command command, IGuildUser user, IMessage message, IMessageChannel channel, IGuild guild, out string error)
        {
            error = "This is a @Price Checker only command.";

            foreach(var role in user.RoleIds)
            {
                if (role == 396941708958629900u || role == 396926575259418626u)
                    return true;
            }

            return false;
        }
    }
}
