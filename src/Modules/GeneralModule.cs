namespace PoE.Bot.Modules
{
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using PoE.Bot.ModuleBases;
	using Qmmands;
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("General Module")]
	[Description("General Guild Commands")]
	public class GeneralModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }

		[Command("GuildInfo")]
		[Name("GuildInfo")]
		[Description("Displays information about the Guild")]
		[Usage("guildinfo")]
		public Task GuildInfo() => ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(Context.Guild.Name + "'s Information | " + Context.Guild.Id, Context.Guild.IconUrl)
				.WithFooter("Created On: " + Context.Guild.CreatedAt.DateTime.ToLongDateString() + " @ " + Context.Guild.CreatedAt.DateTime.ToLongTimeString())
				.WithThumbnailUrl(Context.Guild.IconUrl)
				.AddField("Kitava", Context.Guild.Owner, true)
				.AddField("Text Channels", Context.Guild.TextChannels.Count, true)
				.AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
				.AddField("Characters", Context.Guild.MemberCount, true)
				.AddField("Lieutenants", Context.Guild.Users.Count(x => x.IsBot), true)
				.AddField("Exiles", Context.Guild.Users.Count(x => x.IsBot is false), true)
				.AddField("Classes", string.Join(", ", Context.Guild.Roles.OrderByDescending(x => x.Position).Select(x => x.Name)))
				.Build());

		[Command("Profile")]
		[Name("Profile")]
		[Description("Shows a users profile")]
		[Usage("profile @user")]
		public async Task ProfileAsync(
			[Name("User")]
			[Description("The user whose profile you want to get, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user = null)
		{
			user = user ?? Context.User;
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Cases).Include(x => x.Tags).Include(x => x.Shops).Include(x => x.Users).FirstAsync(x => x.GuildId == Context.Guild.Id);
			var profile = await guild.GetUserProfile(user.Id, Database);
			await ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(user.GetDisplayName() + "'s Profile", user.GetAvatar())
				.WithThumbnailUrl(user.GetAvatar())
				.AddField("Warnings", profile.Warnings, true)
				.AddField("Mod Cases", guild.Cases.Count(x => x.UserId == user.Id), true)
				.AddField("Tags", guild.Tags.Count(x => x.UserId == user.Id), true)
				.AddField("Shop Items", guild.Shops.Count(x => x.UserId == user.Id), true)
				.Build());
		}

		[Command("Report")]
		[Name("Report")]
		[Description("Reports a user to the moderators")]
		[Usage("report @user being toxic")]
		public Task Report(
			[Name("User")]
			[Description("The user you want to report, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Reason")]
			[Description("The reason why you are reporting the user")]
			[Remainder] string reason) => Context.Message.DeleteAsync().ContinueWith(async _ =>
		{
			var guild = await Database.Guilds.AsNoTracking().FirstAsync(x => x.GuildId == Context.Guild.Id);
			var channel = Context.Guild.GetTextChannel(guild.ReportLogChannel);
			await channel.SendMessageAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(Context.User.GetDisplayName(), Context.User.GetAvatar())
				.WithThumbnailUrl(user.GetAvatar())
				.WithTitle("Report for " + user.GetDisplayName())
				.WithDescription("**Reason:**\n" + reason)
				.Build());
		});

		[Command("RoleInfo")]
		[Name("RoleInfo")]
		[Description("Displays information about a role")]
		[Usage("roleinfo role")]
		public Task RoleInfo(
			[Name("Role")]
			[Description("The role you want to get info on, can either be @role, role id, or role name (wrapped in quotes if it contains a space)")]
			SocketRole role) => ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithTitle(role.Name + " Information")
				.WithFooter("Created On: " + role.CreatedAt)
				.AddField("ID", role.Id, true)
				.AddField("Rarity", role.Color, true)
				.AddField("Level", role.Position, true)
				.AddField("Shows Separately?", role.IsHoisted ? "Yep" : "Nope", true)
				.AddField("Managed By Discord?", role.IsManaged ? "Yep" : "Nope", true)
				.AddField("Can Mention?", role.IsMentionable ? "Yep" : "Nope", true)
				.AddField("Skills", string.Join(", ", role.Permissions))
				.Build());

		[Command("Stats")]
		[Name("Stats")]
		[Description("Displays information about PoE Bot and its stats")]
		[Usage("stats")]
		[RunMode(RunMode.Parallel)]
		public async Task StatsAsync()
		{
			var guilds = await Database.Guilds.AsNoTracking()
				.Include(x => x.Tags)
				.Include(x => x.CurrencyItems)
				.Include(x => x.Shops)
				.Include(x => x.Streams)
				.Include(x => x.Leaderboards)
				.Include(x => x.RssFeeds)
				.Include(x => x.Users)
				.Include(x => x.Cases)
				.Include(x => x.Profanities)
				.ToListAsync();
			var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(Context.Client.CurrentUser.Username + " Statistics 📊", Context.Client.CurrentUser.GetAvatar())
				.WithDescription(app.Description)
				.AddField("Channels",
					"Categories: " + Context.Client.Guilds.Sum(x => x.CategoryChannels.Count) + "\n"
					+ "Text: " + Context.Client.Guilds.Sum(x => x.TextChannels.Count - x.CategoryChannels.Count) + "\n"
					+ "Voice: " + Context.Client.Guilds.Sum(x => x.VoiceChannels.Count) + "\n"
					+ "Total: " + Context.Client.Guilds.Sum(x => x.Channels.Count), true)
				.AddField("Members",
					"Bot: " + Context.Client.Guilds.Sum(x => x.Users.Count(y => y.IsBot)) + "\n"
					+ "Human: " + Context.Client.Guilds.Sum(x => x.Users.Count(y => y.IsBot is false)) + "\n"
					+ "Total: " + Context.Client.Guilds.Sum(x => x.Users.Count), true)
				.AddField("Database",
					"Tags: " + guilds.Sum(x => x.Tags.Count) + "\n"
					+ "Currencies: " + guilds.Sum(x => x.CurrencyItems.Count) + "\n"
					+ "Shop Items: " + guilds.Sum(x => x.Shops.Count) + "\n"
					+ "Mixer Streams: " + guilds.Sum(x => x.Streams.Count(y => y.StreamType == StreamType.Mixer)) + "\n"
					+ "Twitch Streams: " + guilds.Sum(x => x.Streams.Count(y => y.StreamType == StreamType.Twitch)) + "\n"
					+ "Leaderboards: " + guilds.Sum(x => x.Leaderboards.Count) + "\n"
					+ "Rss Feeds: " + guilds.Sum(x => x.RssFeeds.Count) + "\n"
					+ "Users: " + guilds.Sum(x => x.Users.Count) + "\n"
					+ "Cases: " + guilds.Sum(x => x.Cases.Count) + "\n"
					+ "Profanities: " + guilds.Sum(x => x.Profanities.Count) + "\n"
					, true);
			embed.AddEmptyField();
			embed.AddField("Uptime", (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss"), true)
				.AddField("Memory", "Heap Size: " + Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2) + " MB", true)
				.AddField("Programmer", "[" + app.Owner + "](https://github.com/Kyle-Undefined)", true);

			await ReplyAsync(embed: embed.Build());
		}

		[Command("UserInfo")]
		[Name("UserInfo")]
		[Description("Displays information about a user")]
		[Usage("userinfo @user")]
		public Task UserInfo(
			[Name("User")]
			[Description("The user you want to get info on, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user = null)
		{
			user = user ?? Context.User;
			return ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(user.GetDisplayName() + " Information | " + user.Id, user.GetAvatar())
				.WithThumbnailUrl(user.GetAvatar())
				.AddField("Muted?", user.IsMuted ? "Yep" : "Nope", true)
				.AddField("Is Lieutenant?", user.IsBot ? "Yep" : "Nope", true)
				.AddField("Creation Date", user.CreatedAt, true)
				.AddField("Join Date", user.JoinedAt, true)
				.AddField("Status", user.Status, true)
				.AddField("Skills", string.Join(", ", user.GuildPermissions.ToList()), true)
				.AddField("Classes", string.Join(", ", user.Roles.OrderBy(x => x.Position).Select(x => x.Name)), true)
				.Build());
		}
	}
}