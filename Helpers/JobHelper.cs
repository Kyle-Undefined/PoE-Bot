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
            var Role = Guild.GetRole(Server.MuteRole) ?? Guild.Roles.FirstOrDefault(x => x.Name == "Muted");
            var User = Guild.GetUser(UserId);
            if (!(User as SocketGuildUser).Roles.Contains(Role) || Role is null)
                return;
            if ((User as SocketGuildUser).Roles.Contains(Role) || !(Role is null))
                await User.RemoveRoleAsync(Role);
        }
    }
}
