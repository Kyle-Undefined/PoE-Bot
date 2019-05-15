namespace PoE.Bot.Modules
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Addons.Interactive;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using PoE.Bot.ModuleBases;
	using PoE.Bot.Checks;
	using PoE.Bot.Services;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Http;
	using System.Text;
	using System.Threading.Tasks;
	using TwitchLib.Api;

	[Name("Moderator Module")]
	[Description("Moderator Commands")]
	[RequireModerator]
	public class ModeratorModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }
		public HttpClient HttpClient { get; set; }

		[Command("Ban")]
		[Name("Ban")]
		[Description("Bans a user from the guild")]
		[Usage("ban @user for being toxic")]
		public async Task BanAsync(
			[Name("User")]
			[Description("The user to ban, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Reason")]
			[Description("Reason for the ban")]
			[Remainder] string reason = null)
		{
			if (Context.Guild.HierarchyCheck(user))
			{
				await ReplyAsync(EmoteHelper.Cross + " Oops, clumsy me! *`" + user + "` is higher than I.*");
				return;
			}

			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(Context.User)
				.WithTitle("Mod Action")
				.WithDescription("You were banned in the " + Context.Guild.Name + " guild.")
				.WithThumbnailUrl(Context.User.GetAvatar())
				.AddField("Reason", reason)
				.Build();

			await user.TrySendDirectMessageAsync(embed: embed);
			await Context.Guild.AddBanAsync(user, 7, reason);
			await LogCaseAsync(user, CaseType.Ban, reason);

			await ReplyAsync("You are remembered only for the mess you leave behind. *`" + user + "` was banned.* " + EmoteHelper.Hammer);
		}

		[Command("CaseReason")]
		[Name("CaseReason")]
		[Description("Specifies reason for a case")]
		[Usage("casereason 10 discussed with staff, reversing")]
		public async Task CaseReasonAsync(
			[Name("Number")]
			[Description("The case number to update a reason for")]
			int number,
			[Name("Reason")]
			[Description("The new reason for the case")]
			[Remainder] string reason)
		{
			var userCase = await Database.Cases.Include(x => x.Guild).FirstOrDefaultAsync(x => x.Number == number && x.Guild.GuildId == Context.Guild.Id);
			var user = Context.Guild.GetUser(userCase.UserId);
			var moderator = Context.Guild.GetUser(userCase.ModeratorId);
			var channel = Context.Guild.GetTextChannel(userCase.Guild.CaseLogChannel);

			userCase.Reason = reason;

			if (!(channel is null) && await channel.GetMessageAsync(userCase.MessageId) is IUserMessage message)
			{
				var cases = await Database.Cases.AsNoTracking().Include(x => x.Guild).Where(x => x.UserId == user.Id && x.Guild.GuildId == Context.Guild.Id).ToListAsync();
				var embed = EmbedHelper.Embed(EmbedHelper.Case)
					.WithAuthor("Case Number: " + userCase.Number)
					.WithTitle(userCase.CaseType.ToString())
					.AddField("User", user.Mention + " `" + user + "` (" + user.Id + ")")
					.AddField("History",
						"Cases: " + cases.Count() + "\n"
						+ "Warnings: " + cases.Count(x => x.CaseType == CaseType.Warning) + "\n"
						+ "Mutes: " + cases.Count(x => x.CaseType == CaseType.Mute) + "\n"
						+ "Auto Mutes: " + cases.Count(x => x.CaseType == CaseType.AutoMute) + "\n")
					.AddField("Reason", userCase.Reason)
					.AddField("Moderator", $"{moderator}")
					.WithCurrentTimestamp()
					.Build();

				await message.ModifyAsync(x => x.Embed = embed);
			}

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Cases")]
		[Name("Cases")]
		[Description("Lists all of the cases for a specified user in the guild")]
		[Usage("cases @user")]
		public async Task CasesForUserAsync(
			[Name("User")]
			[Description("The user to get the cases of, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user)
		{
			var pages = new List<string>();
			var cases = await Database.Cases.AsNoTracking().Include(x => x.Guild).Where(x => x.UserId == user.Id && x.Guild.GuildId == Context.Guild.Id).Select(x => "Case Number: " + x.Number + "\nDate: "
				+ x.CaseDate.ToString("f") + "\nType: " + x.CaseType + "\nReason: " + x.Reason + "\nModerator: " + Context.Guild.GetUser(x.ModeratorId).GetDisplayName() + "\n").ToListAsync();

			foreach (var item in cases.SplitList())
				pages.Add(string.Join("\n", item));

			if (pages.Count > 0)
			{
				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = user.GetDisplayName() + "'s Cases",
					Author = new EmbedAuthorBuilder
					{
						Name = "Cases",
						IconUrl = user.GetAvatar()
					}
				});

				return;
			}

			await ReplyAsync(EmoteHelper.Cross + " Oops, clumsy me! *`" + user + "` doesn't have any cases.*");
		}

		[Command("Kick")]
		[Name("Kick")]
		[Description("Kicks a user out of the guild")]
		[Usage("kick @user get out")]
		public async Task KickAsync(
			[Name("User")]
			[Description("The user to kick, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Reason")]
			[Description("Reason for the kick")]
			[Remainder] string reason = null)
		{
			if (Context.Guild.HierarchyCheck(user))
			{
				await ReplyAsync(EmoteHelper.Cross + " Oops, clumsy me! `" + user + "` is higher than I.");
				return;
			}

			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(Context.User)
				.WithTitle("Mod Action")
				.WithDescription("You were kicked in the " + Context.Guild.Name + " guild.")
				.WithThumbnailUrl(Context.User.GetAvatar())
				.AddField("Reason", reason)
				.Build();

			await user.TrySendDirectMessageAsync(embed: embed);
			await user.KickAsync(reason);
			await LogCaseAsync(user, CaseType.Kick, reason);

			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("LeaderboardAdd")]
		[Name("LeaderboardAdd")]
		[Description("Add a Leaderboard to the list")]
		[Usage("leaderboardadd #channel true Xbox Flashback")]
		public async Task LeaderboardAddAsync(
			[Name("Channel")]
			[Description("The channel to post the leaderboard to")]
			SocketGuildChannel channel,
			[Name("Enabled")]
			[Description("True to allow posting, False to disable")]
			bool enabled,
			[Name("Console")]
			[Description("The Console that the leaderboard is for")]
			string console,
			[Name("Variant")]
			[Description("The Leaderboard name to pull")]
			[Remainder] string variant)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Leaderboards).FirstAsync(x => x.GuildId == Context.Guild.Id);
			variant = variant.Replace(" ", "_");

			if (guild.Leaderboards.Any(x => x.Variant == variant && x.Console == console && x.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " My spirit is spent. *`" + variant + "` is already in the list.*");
				return;
			}

			await Database.Leaderboards.AddAsync(new Leaderboard
			{
				ChannelId = channel.Id,
				Enabled = enabled,
				Console = console,
				Variant = variant,
				GuildId = guild.Id
			});

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("LeaderboardDelete")]
		[Name("LeaderboardDelete")]
		[Description("Delete a Leaderboard from the list")]
		[Usage("leaderboarddelete #channel Xbox Flashback")]
		public async Task LeaderboardDeleteAsync(
			[Name("Channel")]
			[Description("The channel the leaderboard is posted to")]
			SocketGuildChannel channel,
			[Name("Console")]
			[Description("The Console that the leaderboard is for")]
			string console,
			[Name("Variant")]
			[Description("The Leaderboard name to delete")]
			[Remainder] string variant)
		{
			var leaderboards = await Database.Leaderboards.Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			variant = variant.Replace(" ", "_");

			if (!leaderboards.Any(x => x.Variant == variant && x.Console == console && x.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " Poor, corrupted creature. *Can't find the Variant: `" + variant + "`*");
				return;
			}

			var leaderboard = leaderboards.FirstOrDefault(x => x.Variant == variant && x.Console == console && x.ChannelId == channel.Id);
			Database.Leaderboards.Remove(leaderboard);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("LeaderboardList")]
		[Name("LeaderboardsList")]
		[Description("List all Leaderboards")]
		[Usage("leaderboardlist")]
		public async Task LeaderboardListAsync()
		{
			var leaderboards = await Database.Leaderboards.Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			var sb = new StringBuilder();

			if (leaderboards.Count > 0)
			{
				foreach (var leaderboard in leaderboards)
				{
					sb.Append("Variant: ").Append(leaderboard.Variant)
						.Append(" | Console: ").Append(leaderboard.Console)
						.Append(" | Channel: ").Append((Context.Guild.GetTextChannel(leaderboard.ChannelId)).Mention)
						.Append(" | Enabled: ").AppendLine(leaderboard.Enabled.ToString());
				}

				await ReplyAsync("**Leaderboard Variants**:\n" + sb);
			}
			else
			{
				await ReplyAsync(EmoteHelper.Cross + " Return to Kitava! *Wraeclast doesn't have any leaderboards.*");
			}
		}

		[Command("Mute")]
		[Name("Mute")]
		[Description("Mutes a user for a given time")]
		[Usage("mute @user 1d read the rules")]
		public Task Mute(
			[Name("User")]
			[Description("The user to mute, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Time")]
			[Description("The time to mute a user for. Defaults to 5 minutes. Time is formatted like: 1-31d / 1-11h / 1-59m / 1-59s")]
			TimeSpan? time,
			[Name("Reason")]
			[Description("The reason for the mute")]
			[Remainder] string reason = null) => Context.Message.DeleteAsync().ContinueWith(async _ =>
			{
				var guild = await Database.Guilds.Include(x => x.Users).FirstAsync(x => x.GuildId == Context.Guild.Id);
				reason = reason ?? "No reason specified.";

				if (guild.MuteRole is 0)
				{
					await ReplyAsync(EmoteHelper.Cross + " I'm baffled by this at the moment. *No Mute Role Configured*");
					return;
				}
				if ((user as IGuildUser)?.RoleIds.Contains(guild.MuteRole) is true)
				{
					await ReplyAsync(EmoteHelper.Cross + " I'm no fool, but this one's got me beat. *`" + user + "` is already muted.*");
					return;
				}
				if (Context.Guild.HierarchyCheck(user))
				{
					await ReplyAsync(EmoteHelper.Cross + " Oops, clumsy me! *`" + user + "` is higher than I.*");
					return;
				}

				await user.AddRoleAsync(Context.Guild.GetRole(guild.MuteRole));
				var mutedUser = guild.Users.FirstOrDefault(x => x.UserId == user.Id);

				if (!(mutedUser is null))
				{
					mutedUser.Muted = true;
					mutedUser.MutedUntil = DateTime.Now.Add((TimeSpan)time);
				}
				else
				{
					await Database.Users.AddAsync(new User
					{
						UserId = user.Id,
						Muted = true,
						MutedUntil = DateTime.Now.Add((TimeSpan)time),
						GuildId = guild.Id
					});
				}

				await LogCaseAsync(user, CaseType.Mute, reason + " (" + ((TimeSpan)time).FormatTimeSpan() + ")");
				await ReplyAsync("Rest now, tormented soul. *`" + user + "` has been muted for " + ((TimeSpan)time).FormatTimeSpan() + "* " + EmoteHelper.OkHand);

				var embed = EmbedHelper.Embed(EmbedHelper.Info)
					.WithAuthor(Context.User)
					.WithTitle("Mod Action")
					.WithDescription("You were muted in the " + Context.Guild.Name + " guild.")
					.WithThumbnailUrl(Context.User.GetDisplayName())
					.WithFooter("You can PM " + Context.User.GetDisplayName() + " directly to resolve the issue.")
					.AddField("Reason", reason)
					.AddField("Duration", ((TimeSpan)time).FormatTimeSpan())
					.Build();

				await user.TrySendDirectMessageAsync(embed: embed);
			});

		[Command("ProfanityList")]
		[Name("ProfanityList")]
		[Description("Lists all words in the profanity list")]
		[Usage("profanitylist")]
		public async Task ProfanitiesAsync()
		{
			var profanities = await Database.Profanities.AsNoTracking().Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			await ReplyAsync(string.Join(", ", profanities.Select(x => x.Word)));
		}

		[Command("ProfanityAdd")]
		[Name("ProfanityAdd")]
		[Description("Add a word to the profanity list")]
		[Usage("profanityadd nerd")]
		public async Task ProfanityAddAsync(
			[Name("Word")]
			[Description("The word to add to the profanity list")]
			[Remainder] string word)
		{
			var guild = await Database.Guilds.AsNoTracking().FirstAsync(x => x.GuildId == Context.Guild.Id);
			await Database.Profanities.AddAsync(new Profanity
			{
				Word = word.ToLower(),
				GuildId = guild.Id
			});
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("ProfanityDelete")]
		[Name("ProfanityDelete")]
		[Description("Delete a word from the profanity list")]
		[Usage("profanitydelete nerd")]
		public async Task ProfanityDeleteAsync(
			[Name("Word")]
			[Description("The word to delete from the profanity list")]
			[Remainder] string word)
		{
			var profanity = await Database.Profanities.AsNoTracking().Include(x => x.Guild).FirstAsync(x => x.Guild.GuildId == Context.Guild.Id && x.Word == word.ToLower());
			Database.Profanities.Remove(profanity);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Purge", "Prune")]
		[Name("Purge")]
		[Description("Deletes Messages, and can specify a user")]
		[Usage("purge 5 @user")]
		public async Task PurgeAsync(
			[Name("Amount")]
			[Description("The amount of messages to purge")]
			int amount = 20,
			[Name("User")]
			[Description("The user whose messages to delete, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user = null)
		{
			IUserMessage message = null;

			if (user is null)
			{
				await Context.Channel.DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync());
				await LogCaseAsync(Context.User, CaseType.Purge, "Purged " + amount + " Messages in #" + Context.Channel.Name);
				message = await ReplyWithEmoteAsync(EmoteHelper.OkHand);
			}
			else
			{
				await Context.Channel.DeleteMessagesAsync((await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync()).Where(x => x.Author.Id == user.Id));
				await LogCaseAsync(Context.User, CaseType.Purge, "Purged " + amount + " of " + user + "'s Messages #" + Context.Channel.Name);
				message = await ReplyWithEmoteAsync(EmoteHelper.OkHand);
			}

			_ = Task.Delay(3000).ContinueWith(_ => message.DeleteAsync());
		}

		[Command("RssAdd")]
		[Name("RssAdd")]
		[Description("Add a Rss feed to the Rss list")]
		[Usage("rssadd feedUrl #channel tag")]
		public async Task RssAddAsync(
			[Name("Feed URL")]
			[Description("URL of the Rss feed")]
			string feedUrl,
			[Name("Channel")]
			[Description("The channel the Rss feed will be posted to")]
			SocketGuildChannel channel,
			[Name("Tag")]
			[Description("The tag to look for from the Rss feed so the proper roles are tagged")]
			string tag)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.RssFeeds).FirstAsync(x => x.GuildId == Context.Guild.Id);
			if (guild.RssFeeds.Any(x => x.FeedUrl == feedUrl && x.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " `" + feedUrl + "` for `" + channel.Name + "` already exists.");
				return;
			}

			await Database.RssFeeds.AddAsync(new RssFeed
			{
				ChannelId = channel.Id,
				FeedUrl = feedUrl,
				Tag = tag,
				GuildId = guild.Id
			});

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("RssDelete")]
		[Name("RssDelete")]
		[Description("Delete a Rss feed from the Rss list")]
		[Usage("rssdelete feedUrl #channel")]
		public async Task RssDeleteAsync(
			[Name("Feed URL")]
			[Description("URL of the Rss feed")]
			string feedUrl,
			[Name("Channel")]
			[Description("The channel the Rss feed is posted to")]
			SocketGuildChannel channel)
		{
			var rssFeeds = await Database.RssFeeds.Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			if (!rssFeeds.Any(x => x.FeedUrl != feedUrl && x.ChannelId != channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " `" + feedUrl + "` for `" + channel.Name + "` doesn't exist.");
				return;
			}

			var rssFeed = rssFeeds.Find(x => x.FeedUrl == feedUrl && x.ChannelId == channel.Id);
			Database.RssFeeds.Remove(rssFeed);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("RssList")]
		[Name("RssList")]
		[Description("Lists all of the Rss feeds set in the guild")]
		[Usage("rsslist")]
		public async Task RssListAsync()
		{
			var rssFeeds = await Database.RssFeeds.AsNoTracking().Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			if (rssFeeds.Count is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " No Rss feeds set in this guild");
				return;
			}

			var sb = new StringBuilder();
			foreach (var feed in rssFeeds)
			{
				sb.Append("Feed: ").Append(feed.FeedUrl)
					.Append(" | Channel: #").Append(Context.Guild.GetChannelName(feed.ChannelId));
				if (!string.IsNullOrEmpty(feed.Tag))
					sb.Append(" | Tag: ").Append(feed.Tag);

				var rssRoles = await Database.RssRoles.AsNoTracking().Include(x => x.Guild).Include(x => x.RssFeed).Where(x => x.Guild.GuildId == Context.Guild.Id && x.RssFeedId == feed.Id).ToListAsync();
				if (rssRoles.Count > 0)
					sb.Append(" | Role(s): `").Append(string.Join(", ", Context.Guild.Roles.OrderByDescending(r => r.Position).Where(x => rssRoles.Select(y => y.RoleId).Contains(x.Id)).Select(x => x.Name))).Append("`");
				sb.AppendLine();
			}

			await ReplyAsync("**Subbed To Following RSS Feeds**:\n" + sb);
		}

		[Command("RssRolesAdd")]
		[Name("RssRolesAdd")]
		[Description("Add role(s) to a Rss feed in the Rss list")]
		[Usage("rssrolesadd feedUrl #channel roles")]
		public async Task RssRolesAddAsync(
			[Name("Feed URL")]
			[Description("URL of the Rss feed")]
			string feedUrl,
			[Name("Channel")]
			[Description("The channel the Rss feed is posted to")]
			SocketGuildChannel channel,
			[Name("Roles")]
			[Description("The roles to mention when Rss feed is posted")]
			params SocketRole[] roles)
		{
			var rssFeeds = await Database.RssFeeds.Include(x => x.Guild).ThenInclude(x => x.RssRoles).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();

			if (!rssFeeds.Any(x => x.FeedUrl == feedUrl && x.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " `" + feedUrl + "` for `" + channel.Name + "` doesn't exist.");
				return;
			}

			var rssFeed = rssFeeds.Find(x => x.FeedUrl == feedUrl && x.ChannelId == channel.Id);

			foreach (var role in roles)
			{
				await Database.RssRoles.AddAsync(new RssRole
				{
					RoleId = role.Id,
					RssFeedId = rssFeed.Id,
					GuildId = rssFeed.Guild.Id
				});
			}

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("RssRolesDelete")]
		[Name("RssRolesDelete")]
		[Description("Delete role(s) from a Rss feed in the Rss list")]
		[Usage("rssrolesdelete feedUrl #channel")]
		public async Task RssRolesDeleteAsync(
			[Name("Feed URL")]
			[Description("URL of the Rss feed")]
			string feedUrl,
			[Name("Channel")]
			[Description("The channel the Rss feed is posted to")]
			SocketGuildChannel channel,
			[Name("Roles")]
			[Description("The roles to delete from being mentioned")]
			params SocketRole[] roles)
		{
			var rssRoles = await Database.RssRoles.Include(x => x.Guild).ThenInclude(x => x.RssFeeds).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();

			if (!rssRoles.Any(x => x.RssFeed.FeedUrl == feedUrl && x.RssFeed.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " `" + feedUrl + "` for `" + channel.Name + "` doesn't exist.");
				return;
			}

			foreach(var role in rssRoles.Where(x => x.RssFeed.FeedUrl == feedUrl && x.RssFeed.ChannelId == channel.Id))
			{
				if (roles.Select(x => x.Id).Contains(role.RoleId))
					Database.RssRoles.Remove(role);
			}

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("RulesPost")]
		[Name("RulesPost")]
		[Description("Posts the rules you've configured to the rules channel you setup in the Guild Config. Only done once, if you want to edit the rules, use Rules Configure followed by Rules Update")]
		[Usage("rulespost")]
		public async Task RulesPostAsync()
		{
			var guild = await Database.Guilds.Include(x => x.Rules).Include(x => x.RuleFields).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (string.IsNullOrEmpty(guild.Rules.Description))
			{
				await ReplyAsync(EmoteHelper.Cross + " *You have no rules to post, please use rules setup to set them up.*");
				return;
			}

			if (guild.RulesChannel is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " *You have not configured a rules channel.*");
				return;
			}

			var channel = Context.Guild.GetChannel(guild.RulesChannel) as SocketTextChannel;
			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithTitle($"{Context.Guild.Name} Rules")
				.WithDescription(guild.Rules.Description);

			foreach (var field in guild.RuleFields.OrderBy(x => x.Order))
				embed.AddField(field.Title, field.Content, false);

			var message = await channel.SendMessageAsync(embed: embed.Build());
			guild.Rules.MessageId = message.Id;
			await Database.SaveChangesAsync();

			await message.AddReactionsAsync(new[]
			{
				EmoteHelper.Announcement,
				EmoteHelper.Lottery
			});

			await message.AddReactionsAsync(new[]
			{
				EmoteHelper.Xbox,
				EmoteHelper.Playstation
			});

			await ReplyAsync("*Rules have been posted.* " + EmoteHelper.OkHand);
		}

		[Command("RulesSetup")]
		[Name("RulesSetup")]
		[Description("Sets the rules for the guild, each section has 3 minutes to setup")]
		[Usage("rulessetup")]
		[RunMode(RunMode.Parallel)]
		public async Task RulesSetupAsync()
		{
			var guild = await Database.Guilds.Include(x => x.Rules).Include(x => x.RuleFields).FirstAsync(x => x.GuildId == Context.Guild.Id);
			var maxChars = 6000 - (Context.Guild.Name + " Rules").Length;
			var rule = new Rule();

			await ReplyAsync("What should the description be? *You have " + maxChars + " left*");
			var description = await NextMessageAsync(timeout: TimeSpan.FromMinutes(3));

			if (description is null)
			{
				await ReplyAsync(EmoteHelper.Cross + " Timed out.");
				return;
			}

			maxChars -= description.Content.Length;
			rule.Description = description.Content;

			await ReplyAsync("How many sections should there be? *You can have 25 sections at most*");
			var totalFields = await NextMessageAsync(timeout: TimeSpan.FromMinutes(3));

			if (totalFields is null)
			{
				await ReplyAsync(EmoteHelper.Cross + " Timed out.");
				return;
			}

			if (!int.TryParse(totalFields.Content, out var fieldCount))
			{
				await ReplyAsync(EmoteHelper.Cross + " Input is not a number, canceling rule setup.");
				return;
			}

			rule.TotalFields = fieldCount > 25 ? 25 : fieldCount;

			for (int i = 0; i < rule.TotalFields; i++)
			{
				await ReplyAsync("What should the section be called?  *You have " + maxChars + " left*");
				var fieldTitle = await NextMessageAsync(timeout: TimeSpan.FromMinutes(3));

				if (fieldTitle is null)
				{
					await ReplyAsync(EmoteHelper.Cross + " Timed out.");
					break;
				}

				maxChars -= fieldTitle.Content.Length;

				await ReplyAsync("What should the section contain? (You have 5 minutes for this section) *You have " + maxChars + " left. You can use Discord Markup*");
				var fieldContent = await NextMessageAsync(timeout: TimeSpan.FromMinutes(5));

				if (fieldContent is null)
				{
					await ReplyAsync(EmoteHelper.Cross + " Timed out.");
					break;
				}

				maxChars -= fieldContent.Content.Length;
				await Database.RuleFields.AddAsync(new RuleField
				{
					Title = fieldTitle.Content,
					Content = fieldContent.Content,
					Order = i,
					GuildId = guild.Id
				});
			}

			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithTitle(Context.Guild.Name + " Rules")
				.WithDescription(rule.Description);

			foreach (var field in guild.RuleFields.OrderBy(x => x.Order))
				embed.AddField(field.Title, field.Content, false);

			var message = await ReplyAsync(embed: embed.Build());
			rule.MessageId = message.Id;
			guild.Rules = rule;

			await Database.SaveChangesAsync();
		}

		[Command("RulesUpdate")]
		[Name("RulesUpdate")]
		[Description("Updates the rules you've configured and posted to the rules channel.")]
		[Usage("rulesupdate")]
		public async Task RulesUpdateAsync()
		{
			var guild = await Database.Guilds.Include(x => x.Rules).Include(x => x.RuleFields).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (string.IsNullOrEmpty(guild.Rules.Description))
			{
				await ReplyAsync(EmoteHelper.Cross + " *You have no rules to post, please use rules setup to set them up.*");
				return;
			}

			if (guild.RulesChannel is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " *You have not configured a rules channel.*");
				return;
			}

			var channel = Context.Guild.GetChannel(guild.RulesChannel) as SocketTextChannel;
			var message = await channel.GetMessageAsync(guild.Rules.MessageId);

			if (message is null)
			{
				await ReplyAsync(EmoteHelper.Cross + " *No messages found to edit, please make sure you've posted the rules to the channel.*");
				return;
			}

			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithTitle(Context.Guild.Name + " Rules")
				.WithDescription(guild.Rules.Description);

			foreach (var field in guild.RuleFields.OrderBy(x => x.Order))
				embed.AddField(field.Title, field.Content, false);

			await (message as IUserMessage)?.ModifyAsync(x => x.Embed = embed.Build());
			await ReplyAsync("*Rules have been edited.* " + EmoteHelper.OkHand);
		}

		[Command("SoftBan")]
		[Name("SoftBan")]
		[Description("Bans a user then unbans them")]
		[Usage("softban @user stop it")]
		public Task SoftBan(
			[Name("User")]
			[Description("The user to soft ban, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Reason")]
			[Description("Reason for the soft ban")]
			[Remainder] string reason = null)
		{
			if (Context.Guild.HierarchyCheck(user))
				return ReplyAsync(EmoteHelper.Cross + " Oops, clumsy me! `" + user + "` is higher than I.");

			Context.Guild.AddBanAsync(user, 7, reason);
			Context.Guild.RemoveBanAsync(user);
			_ = LogCaseAsync(user, CaseType.Softban, reason);
			return ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("StreamerAdd")]
		[Name("StreamerAdd")]
		[Description("Add a streamer to the Stream list")]
		[Usage("streameradd twitch ziggydlive #channel")]
		public async Task StreamerAddAsync(
			[Name("Stream Type")]
			[Description("The stream type, Mixer or Twitch")]
			StreamType streamType,
			[Name("Username")]
			[Description("The username of the streamer")]
			string userName,
			[Name("Channel")]
			[Description("The channel to post live notifications to")]
			SocketTextChannel channel)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Streams).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (guild.Streams.Any(x => x.StreamType == streamType && x.Username == userName && x.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " My spirit is spent. *`" + userName + "` is already on the `" + streamType + "` list.*");
				return;
			}

			switch (streamType)
			{
				case StreamType.Mixer:
					MixerAPI mixer = new MixerAPI(HttpClient);
					var userId = await mixer.GetUserId(userName);
					var chanId = await mixer.GetChannelId(userName);

					if (userId is 0 || chanId is 0)
					{
						await ReplyAsync(EmoteHelper.Cross + " I don't think I need to be doing that right now. *No user/channel found.*");
						return;
					}

					await Database.Streams.AddAsync(new Stream
					{
						ChannelId = channel.Id,
						MixerChannelId = chanId,
						MixerUserId = userId,
						StreamType = streamType,
						Username = userName,
						GuildId = guild.Id
					});
					break;

				case StreamType.Twitch:
					var config = await Database.BotConfigs.AsNoTracking().FirstAsync();
					var twitch = new TwitchAPI();
					twitch.Settings.ClientId = config.TwitchClientId;
					var user = await twitch.V5.Users.GetUserByNameAsync(userName);

					if (user.Matches.Length == 0)
					{
						await ReplyAsync(EmoteHelper.Cross + " I don't think I need to be doing that right now. *Twitch user not found.*");
						return;
					}

					await Database.Streams.AddAsync(new Stream
					{
						ChannelId = channel.Id,
						StreamType = streamType,
						TwitchUserId = Convert.ToUInt64(user.Matches[0].Id),
						Username = userName,
						GuildId = guild.Id
					});
					break;
			}

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("StreamerDelete")]
		[Name("StreamerDelete")]
		[Description("Delete a streamer from the Stream list")]
		[Usage("streamerdelete twitch ziggydlive #channel")]
		public async Task StreamerDeleteAsync(
			[Name("Stream Type")]
			[Description("The stream type, Mixer or Twitch")]
			StreamType streamType,
			[Name("Username")]
			[Description("The username of the streamer")]
			string userName,
			[Name("Channel")]
			[Description("The channel the live notifications are posted to")]
			SocketTextChannel channel)
		{
			var streams = await Database.Streams.Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			if (!streams.Any(x => x.StreamType == streamType && x.Username == userName && x.ChannelId == channel.Id))
			{
				await ReplyAsync(EmoteHelper.Cross + " My spirit is spent. *`" + userName + "` isn't on the `" + streamType + "` list.*");
				return;
			}

			var streamer = streams.FirstOrDefault(x => x.StreamType == streamType && x.Username == userName && x.ChannelId == channel.Id);
			Database.Streams.Remove(streamer);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("StreamerList")]
		[Name("StreamerList")]
		[Description("Lists all streamers in the guild")]
		[Usage("streamerlist")]
		public async Task StreamerListAsync()
		{
			var streams = await Database.Streams.AsNoTracking().Include(x => x.Guild).Where(x => x.Guild.GuildId == Context.Guild.Id).ToListAsync();
			var pages = new List<string>();
			var mixer = streams.Where(x => x.StreamType == StreamType.Mixer);
			var twitch = streams.Where(x => x.StreamType == StreamType.Twitch);

			if (mixer.Any())
				pages.Add("\u200b\n**Mixer**\n\u200b\n" + string.Join("\n", mixer.Select(x => x.Username)));

			if (twitch.Any())
				pages.Add("\u200b\n**Twitch**\n\u200b\n" + string.Join("\n", twitch.Select(x => x.Username)));

			await PagedReplyAsync(new PaginatedMessage
			{
				Pages = pages,
				Color = new Color(0, 255, 255),
				Title = "Current list of streamers in the guild",
				Author = new EmbedAuthorBuilder
				{
					Name = "Streamer List",
					IconUrl = Context.Client.CurrentUser.GetAvatar()
				}
			});
		}

		[Command("Unban")]
		[Name("Unban")]
		[Description("Unbans a user from the guild")]
		[Usage("unban 123")]
		public async Task UnbanAsync(
			[Name("ID")]
			[Description("User ID to unban")]
			ulong id)
		{
			if ((await Context.Guild.GetBansAsync()).All(x => x.User.Id != id))
			{
				await ReplyAsync(EmoteHelper.Cross + " I have nothing more to give. *No user with `" + id + "` found.*");
				return;
			}

			await Context.Guild.RemoveBanAsync(id);
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Unmute")]
		[Name("Umute")]
		[Description("Umutes a user")]
		[Usage("unmute user")]
		public async Task UnmuteAsync(
			[Name("User")]
			[Description("The user to unmute, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user)
		{
			var guild = await Database.Guilds.Include(x => x.Users).FirstAsync(x => x.GuildId == Context.Guild.Id);
			var role = Context.Guild.GetRole(guild.MuteRole) ?? Context.Guild.Roles.FirstOrDefault(x => x.Name is "Muted");

			if (!user.Roles.Contains(role))
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm no fool, but this one's got me beat. *`" + user + "` doesn't have any mute role.*");
				return;
			}

			await user.RemoveRoleAsync(role);

			var mutedUser = guild.Users.FirstOrDefault(x => x.UserId == user.Id);
			mutedUser.Muted = false;

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Warn")]
		[Name("Warn")]
		[Description("Warns a user with a specified reason")]
		[Usage("warn @user read the rules")]
		public async Task WarnAsync(
			[Name("User")]
			[Description("The user to warn, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Reason")]
			[Description("The reason for the warning")]
			[Remainder] string reason)
		{
			var guild = await Database.Guilds.Include(x => x.Users).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (guild.MaxWarnings is 0 || user.Id == Context.Guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels
				|| user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
			{
				return;
			}

			var profile = await guild.GetUserProfile(user.Id, Database);
			profile.Warnings++;
			Database.Users.Update(profile);
			await Database.SaveChangesAsync();

			if (profile.Warnings >= guild.MaxWarnings)
			{
				await Mute(user, TimeSpan.FromDays(1), "Muted for 1 day due to reaching max number of warnings. " + reason);
				return;
			}
			else
			{
				var embed = EmbedHelper.Embed(EmbedHelper.Info)
					.WithAuthor(Context.User)
					.WithTitle("Mod Action")
					.WithDescription("You were Warned in the " + Context.Guild.Name + " guild.")
					.WithThumbnailUrl(Context.User.GetAvatar())
					.WithFooter("You can PM " + Context.User.GetDisplayName() + " directly to resolve the issue.")
					.AddField("Reason", reason)
					.Build();

				await user.TrySendDirectMessageAsync(embed: embed);
				await ReplyWithEmoteAsync(EmoteHelper.Warning);
			}

			await LogCaseAsync(user, CaseType.Warning, reason);
		}

		[Command("WarnDelete")]
		[Name("WarnDelete")]
		[Description("Deletes a number of users warnings")]
		[Usage("warndelete @user 1")]
		public async Task WarnDeleteAsync(
			[Name("User")]
			[Description("The user to remove warnings, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Amount")]
			[Description("The amount of warnings to remove")]
			int amount = 1)
		{
			var guild = await Database.Guilds.Include(x => x.Users).FirstAsync(x => x.GuildId == Context.Guild.Id);
			var profile = await guild.GetUserProfile(user.Id, Database);

			if (amount > profile.Warnings)
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm no fool, but this one's got me beat. *`" + user + "` doesn't have `" + amount + "` warnings to remove.*");
				return;
			}

			profile.Warnings -= amount;
			Database.Users.Update(profile);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("WarnReset")]
		[Name("WarnReset")]
		[Description("Resets warnings for a user")]
		[Usage("warnreset @user")]
		public async Task WarnResetAsync(
			[Name("User")]
			[Description("The user to reset warnings, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user)
		{
			var guild = await Database.Guilds.Include(x => x.Users).FirstAsync(x => x.GuildId == Context.Guild.Id);
			var profile = await guild.GetUserProfile(user.Id, Database);
			profile.Warnings = 0;
			Database.Users.Update(profile);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		private async Task LogCaseAsync(SocketUser user, CaseType caseType, string reason = null)
		{
			reason = reason ?? "No Reason Specified";
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Cases).FirstAsync(x => x.GuildId == Context.Guild.Id);
			var channel = Context.Guild.GetTextChannel(guild.CaseLogChannel);
			IUserMessage message = null;

			if (!(channel is null))
			{
				var cases = guild.Cases.Where(x => x.UserId == user.Id);
				var embed = EmbedHelper.Embed(EmbedHelper.Case)
					.WithAuthor("Case Number: " + cases.Count() + 1)
					.WithTitle(caseType.ToString())
					.AddField("User", user.Mention + " `" + user + "` (" + user.Id + ")")
					.AddField("History",
						"Cases: " + cases.Count() + "\n"
						+ "Warnings: " + cases.Count(x => x.CaseType == CaseType.Warning) + "\n"
						+ "Mutes: " + cases.Count(x => x.CaseType == CaseType.Mute) + "\n"
						+ "Auto Mutes: " + cases.Count(x => x.CaseType == CaseType.AutoMute) + "\n")
					.AddField("Reason", reason)
					.AddField("Moderator", Context.User)
					.WithCurrentTimestamp()
					.Build();

				message = await channel.SendMessageAsync(embed: embed);
			}

			await Database.Cases.AddAsync(new Case
			{
				CaseDate = DateTime.Now,
				CaseType = caseType,
				MessageId = message?.Id ?? 0,
				ModeratorId = Context.User.Id,
				Number = guild.Cases.Count + 1,
				Reason = reason,
				UserId = user.Id,
				GuildId = guild.Id
			});

			await Database.SaveChangesAsync();
		}
	}
}