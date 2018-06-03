namespace PoE.Bot.Helpers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Discord.WebSocket;
    using PoE.Bot.Handlers.Objects;

    public class JobHelper
    {
        public static Task UnmuteUser(ulong UserId, SocketGuild Guild, GuildObject Server)
        {
            var Role = Guild.GetRole(Server.MuteRole) ?? Guild.Roles.FirstOrDefault(x => x.Name == "Muted");
            var User = Guild.GetUser(UserId);
            if (!(User as SocketGuildUser).Roles.Contains(Role) || Role == null) return Task.CompletedTask;
            return User.RemoveRoleAsync(Role);
        }
    }
}
