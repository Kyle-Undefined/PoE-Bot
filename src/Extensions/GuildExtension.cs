namespace PoE.Bot.Extensions
{
	using Discord.WebSocket;
	using System.Linq;

	public static class GuildExtension
	{
		public static string GetChannelName(this SocketGuild guild, ulong id)
		{
			if (id is 0)
				return "Not Set.";

			var channel = guild.GetTextChannel(id);
			return channel?.Name ?? "Unknown (" + id + ")";
		}

		public static string GetRoleName(this SocketGuild guild, ulong id)
		{
			if (id is 0)
				return "Not Set";

			var role = guild.GetRole(id);
			return role?.Name ?? "Unknown (" + id + ")";
		}

		public static bool HierarchyCheck(this SocketGuild guild, SocketGuildUser user)
		{
			int highestRole = guild.CurrentUser.Roles.OrderByDescending(x => x.Position).FirstOrDefault()?.Position ?? default;
			return user.Roles.Any(x => x.Position >= highestRole);
		}
	}
}