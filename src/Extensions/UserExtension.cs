namespace PoE.Bot.Extensions
{
	using Discord;
	using Discord.Net;
	using Discord.WebSocket;
	using PoE.Bot.Contexts;
	using PoE.Bot.Models;
	using System.Linq;
	using System.Threading.Tasks;

	public static class UserExtension
	{
		public static string GetAvatar(this SocketUser user) => user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();

		public static string GetDisplayName(this SocketGuildUser user) => user.Nickname ?? user.Username;

		public static string GetDisplayName(this IGuildUser user) => GetDisplayName(user);

		public static async Task<User> GetUserProfile(this Guild guild, ulong userId, DatabaseContext database)
		{
			var user = guild.Users.FirstOrDefault(x => x.UserId == userId);

			if (user is null)
			{
				user = new User
				{
					UserId = userId,
					GuildId = guild.Id
				};

				await database.Users.AddAsync(user);
				await database.SaveChangesAsync();
			}

			return user;
		}

		public static async Task<bool> TrySendDirectMessageAsync(this IUser user, string content = null, Embed embed = null)
		{
			try
			{
				await user.SendMessageAsync(content, embed: embed);
			}
			catch (HttpException)
			{
				return false;
			}

			return true;
		}
	}
}