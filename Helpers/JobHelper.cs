namespace PoE.Bot.Helpers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Discord.WebSocket;
    using PoE.Bot.Objects;

    public class JobHelper
    {
        public static async Task UnmuteUser(ulong UserId, SocketGuild Guild, GuildObject Server)
        {
            var User = Guild.GetUser(UserId);
            var MuteRole = Guild.GetRole(Server.MuteRole) ?? Guild.Roles.FirstOrDefault(x => x.Name is "Muted");
            var TradeMuteRole = Guild.GetRole(Server.TradeMuteRole) ?? Guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute");
            if (!(User as SocketGuildUser).Roles.Contains(MuteRole) && !(User as SocketGuildUser).Roles.Contains(TradeMuteRole))
                return;
            if ((User as SocketGuildUser).Roles.Contains(MuteRole))
                await User.RemoveRoleAsync(MuteRole);
            else if ((User as SocketGuildUser).Roles.Contains(TradeMuteRole))
                await User.RemoveRoleAsync(TradeMuteRole);
        }
    }
}
