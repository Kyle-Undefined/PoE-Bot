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
	using PoE.Bot.Checks;
	using Qmmands;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Administrator Module")]
	[Description("Administrator Commands")]
	[Group("Set")]
	[RequireAdmin]
	public class AdministratorModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }

		[Command("BotChangeChannel")]
		[Name("Set BotChangeChannel")]
		[Description("Set the channel for bot changes to be posted in")]
		[Usage("set botchangechannel #channel")]
		public async Task BotChangeChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the bot change channel")]
			SocketGuildChannel channel)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.BotChangeChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("XboxRole")]
		[Name("Set XboxRole")]
		[Description("Set the role for the Xbox category")]
		[Usage("set xboxrole @role")]
		public async Task XboxRoleAsync(
			[Name("role")]
			[Description("The role you want to set as the Xbox role")]
			SocketRole role)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.XboxRole = role.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("PlaystationRole")]
		[Name("Set PlaystationRole")]
		[Description("Set the role for the Playstation category")]
		[Usage("set playstationrole @role")]
		public async Task PlaystationRoleAsync(
			[Name("role")]
			[Description("The role you want to set as the Playstation role")]
			SocketRole role)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.PlaystationRole = role.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("AnnouncementRole")]
		[Name("Set AnnouncementRole")]
		[Description("Set the role for the Announcement notifications")]
		[Usage("set announcementrole @role")]
		public async Task AnnouncementRoleAsync(
			[Name("role")]
			[Description("The role you want to set as the Announcement role")]
			SocketRole role)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.AnnouncementRole = role.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("LotteryRole")]
		[Name("Set LotteryRole")]
		[Description("Set the role for the Lottery notifications")]
		[Usage("set lotteryrole @role")]
		public async Task LotteryRoleAsync(
			[Name("role")]
			[Description("The role you want to set as the Lottery role")]
			SocketRole role)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.LotteryRole = role.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("CaseLogChannel")]
		[Name("Set CaseLogChannel")]
		[Description("Set the channel for cases to be logged in")]
		[Usage("set caselogchannel #channel")]
		public async Task CaseLogChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the case log channel")]
			SocketGuildChannel channel)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.CaseLogChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("EnableAntiProfanity")]
		[Name("Set EnableAntiProfanity")]
		[Description("Choose if Anti Profanity filtering searches each message sent")]
		[Usage("set enableantiprofanity false")]
		public async Task EnableAntiProfanityAsync(
			[Name("Enabled")]
			[Description("True or False")]
			bool enabled)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.EnableAntiProfanity = enabled;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("EnableDeletionLog")]
		[Name("Set EnableDeletionLog")]
		[Description("Choose if Message Deletion will be logged")]
		[Usage("set enabledeletionlog true")]
		public async Task EnableDeletionLogAsync(
			[Name("Enabled")]
			[Description("True or False")]
			bool enabled)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.EnableDeletionLog = enabled;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("EnableLeaderboardFeed")]
		[Name("Set EnableLeaderboardFeed")]
		[Description("Choose if Leaderboard Feeds will be posted")]
		[Usage("set enableleaderboardfeed false")]
		public async Task EnableLeaderboardFeedAsync(
			[Name("Enabled")]
			[Description("True or False")]
			bool enabled)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.EnableLeaderboardFeed = enabled;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("EnableMixerFeed")]
		[Name("Set EnableMixerFeed")]
		[Description("Choose if Mixer Feeds will be posted")]
		[Usage("set enablemixerfeed true")]
		public async Task EnableMixerFeedAsync(
			[Name("Enabled")]
			[Description("True or False")]
			bool enabled)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.EnableMixerFeed = enabled;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("EnableRssFeed")]
		[Name("Set EnableRssFeed")]
		[Description("Choose if Rss Feeds will be posted")]
		[Usage("set enablerssfeed true")]
		public async Task EnableRssFeedAsync(
			[Name("Enabled")]
			[Description("True or False")]
			bool enabled)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.EnableRssFeed = enabled;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("EnableTwitchFeed")]
		[Name("Set EnableTwitchFeed")]
		[Description("Choose if Twitch Feeds will be posted")]
		[Usage("set enabletwitchfeed false")]
		public async Task EnableTwitchFeedAsync(
			[Name("Enabled")]
			[Description("True or False")]
			bool enabled)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.EnableTwitchFeed = enabled;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("MaxWarnings")]
		[Name("Set MaxWarnings")]
		[Description("Set the max warnings a user can receive before users are muted")]
		[Usage("set maxwarnings 3")]
		public async Task MaxWarningsAsync(
			[Name("Number of Warnings")]
			[Description("The number of warnings to allow users to receive before being muted")]
			int numberOfWarnings)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.MaxWarnings = numberOfWarnings;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("MessageLogChannel")]
		[Name("Set MessageLogChannel")]
		[Description("Set the channel for deleted messages to be logged in")]
		[Usage("set messagelogchannel #channel")]
		public async Task MessageLogChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the message log channel")]
			SocketGuildChannel channel)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.MessageLogChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("MuteRole")]
		[Name("Set MuteRole")]
		[Description("Set the role users are assigned when muted")]
		[Usage("set muterole role")]
		public async Task MuteRoleAsync(
			[Name("Role")]
			[Description("The role to assign to users who get muted")]
			SocketRole role)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.MuteRole = role.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("ReportLogChannel")]
		[Name("Set ReportLogChannel")]
		[Description("Set the channel you want user reports to show up in")]
		[Usage("set reportlogchannel #channel")]
		public async Task ReportLogChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the report log channel")]
			SocketGuildChannel channel)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.ReportLogChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("RulesChannel")]
		[Name("Set RulesChannel")]
		[Description("Set the channel you want rules to be posted in")]
		[Usage("set ruleschannel #channel")]
		public async Task RulesChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the rule channel")]
			SocketGuildChannel channel)
		{
			var guild = await Database.Guilds.FirstAsync(x => x.GuildId == Context.Guild.Id);
			guild.RulesChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Settings")]
		[Name("Set Settings")]
		[Description("Displays the current guild settings")]
		[Usage("set settings")]
		public async Task SettingsAsync()
		{
			var guild = await Database.Guilds.AsNoTracking()
				.Include(x => x.Tags)
				.Include(x => x.CurrencyItems)
				.Include(x => x.Shops)
				.Include(x => x.Streams)
				.Include(x => x.Leaderboards)
				.Include(x => x.RssFeeds)
				.Include(x => x.Cases)
				.FirstAsync(x => x.GuildId == Context.Guild.Id);
			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithTitle(Context.Guild.Name + " Settings")
				.WithDescription("Guild settings and data for the Administrators")
				.WithCurrentTimestamp()
				.AddField("General Information",
					"```"
					+ "Prefix                : " + (await Database.BotConfigs.AsNoTracking().FirstAsync()).Prefix + "\n"
					+ "Bot Change channel    : " + Context.Guild.GetChannelName(guild.BotChangeChannel) + "\n"
					+ "Case Log channel      : " + Context.Guild.GetChannelName(guild.CaseLogChannel) + "\n"
					+ "Message Log channel   : " + Context.Guild.GetChannelName(guild.MessageLogChannel) + "\n"
					+ "Report channel        : " + Context.Guild.GetChannelName(guild.ReportLogChannel) + "\n"
					+ "Rules channel         : " + Context.Guild.GetChannelName(guild.RulesChannel) + "\n"
					+ "Announcement role     : " + Context.Guild.GetRoleName(guild.AnnouncementRole) + "\n"
					+ "Lottery role          : " + Context.Guild.GetRoleName(guild.LotteryRole) + "\n"
					+ "Mute role             : " + Context.Guild.GetRoleName(guild.MuteRole) + "\n"
					+ "Playstation role      : " + Context.Guild.GetRoleName(guild.PlaystationRole) + "\n"
					+ "Xbox role             : " + Context.Guild.GetRoleName(guild.XboxRole) + "\n"
					+ "```")
				.AddField("Mod Information",
					"```"
					+ "Log Deleted Messages    : " + (guild.EnableDeletionLog ? "Enabled" : "Disabled") + "\n"
					+ "Profanity Check         : " + (guild.EnableAntiProfanity ? "Enabled" : "Disabled") + "\n"
					+ "Rss Feed                : " + (guild.EnableRssFeed ? "Enabled" : "Disabled") + "\n"
					+ "Mixer                   : " + (guild.EnableMixerFeed ? "Enabled" : "Disabled") + "\n"
					+ "Twitch                  : " + (guild.EnableTwitchFeed ? "Enabled" : "Disabled") + "\n"
					+ "Leaderboard             : " + (guild.EnableLeaderboardFeed ? "Enabled" : "Disabled") + "\n"
					+ "Rss Feeds               : " + guild.RssFeeds.Count + "\n"
					+ "Mixer Streams           : " + guild.Streams.Count(x => x.StreamType is StreamType.Mixer) + "\n"
					+ "Twitch Stream           : " + guild.Streams.Count(x => x.StreamType is StreamType.Twitch) + "\n"
					+ "Leaderboard Variants    : " + guild.Leaderboards.Count + "\n"
					+ "Max Warnings            : " + guild.MaxWarnings + "\n"
					+ "```")
				.AddField("Guild Statistics",
					"```"
					+ "Users Banned           : " + guild.Cases.Count(x => x.CaseType is CaseType.Ban) + "\n"
					+ "Users Soft Banned      : " + guild.Cases.Count(x => x.CaseType is CaseType.Softban) + "\n"
					+ "Users Kicked           : " + guild.Cases.Count(x => x.CaseType is CaseType.Kick) + "\n"
					+ "Users Warned           : " + guild.Cases.Count(x => x.CaseType is CaseType.Warning) + "\n"
					+ "Users Muted            : " + guild.Cases.Count(x => x.CaseType is CaseType.Mute) + "\n"
					+ "Auto Mutes             : " + guild.Cases.Count(x => x.CaseType is CaseType.AutoMute) + "\n"
					+ "Total Currency Items   : " + guild.CurrencyItems.Count + "\n"
					+ "Total Shop Items       : " + guild.Shops.Count + "\n"
					+ "Total Tags             : " + guild.Tags.Count + "\n"
					+ "Total Mod Cases        : " + guild.Cases.Count + "\n"
					+ "```");

			await ReplyAsync(embed: embed.Build());
		}
	}
}