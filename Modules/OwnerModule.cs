namespace PoE.Bot.Modules
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using PoE.Bot.ModuleBases;
	using PoE.Bot.Checks;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using PoE.Bot.Services;
	using System.Collections;
	using System.Text;
	using PoE.Bot.Extensions;
	using Microsoft.CodeAnalysis;
	using PoE.Bot.Addons.Interactive;

	[Name("Owner Module")]
	[Description("Bot Owner Commands")]
	[Group("Bot")]
	[RequireOwner]
	public class OwnerModule : PoEBotBase
	{
		public ScriptingService Scripting { get; set; }
		public DatabaseContext Database { get; set; }
		public IServiceProvider Services { get; set; }

		[Command("Activity")]
		[Name("Bot Activity")]
		[Description("Updates the Bots presence")]
		[Usage("bot activity playing Use !commands")]
		public async Task ActivityAsync(
			[Name("Activity")]
			[Description("Listening, Streaming, Playing, Watching")]
			ActivityType activity,
			[Name("Message")]
			[Description("The activity message")]
			[Remainder] string message)
		{
			await Context.Client.SetActivityAsync(new Game(message, activity));
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("BlacklistAdd")]
		[Name("Bot BlacklistAdd")]
		[Description("Adds a user to the blacklist")]
		[Usage("bot blacklistadd @user being lame")]
		public async Task BlaclistAddAsync(
			[Name("User")]
			[Description("The user to blacklist, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user,
			[Name("Reason")]
			[Description("Reason for the blacklist")]
			[Remainder] string reason)
		{
			var blacklist = await Database.BlacklistedUsers.FirstOrDefaultAsync(x => x.UserId == user.Id);

			if (!(blacklist is null))
			{
				await ReplyAsync(EmoteHelper.Cross + " " + user + " is already in blacklisted.");
				return;
			}

			await Database.BlacklistedUsers.AddAsync(new BlacklistedUser
			{
				UserId = user.Id,
				BlacklistedWhen = DateTime.Now,
				Reason = reason
			});
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("BlacklistDelete")]
		[Name("Bot BlacklistDelete")]
		[Description("Deletes a user from the blacklist")]
		[Usage("bot blacklistdelete @user")]
		public async Task BlaclistDeleteAsync(
			[Name("User")]
			[Description("The user to de-blacklist, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user)
		{
			var blacklist = await Database.BlacklistedUsers.FirstOrDefaultAsync(x => x.UserId == user.Id);

			if (blacklist is null)
			{
				await ReplyAsync(EmoteHelper.Cross + " " + user + " isn't blacklisted.");
				return;
			}

			Database.BlacklistedUsers.Remove(blacklist);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Blacklist")]
		[Name("Bot Blacklist")]
		[Description("Lists all users who are blacklisted")]
		[Usage("bot blacklist")]
		public async Task BlacklistedAsync()
		{
			var blacklisted = await Database.BlacklistedUsers.ToListAsync();
			var pages = new List<string>();

			if (blacklisted.Count > 0)
			{
				foreach (var item in blacklisted.SplitList())
					pages.Add(string.Join("\n", item.Select(x => "**User**: " + Context.Client.GetUser(x.UserId) + "\n**Reason**: " + x.Reason + "\n**Date**: " + x.BlacklistedWhen + "\n")));

				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = "Blacklisted Users",
					Author = new EmbedAuthorBuilder
					{
						Name = "Blacklisted",
						IconUrl = Context.Client.CurrentUser.GetAvatar()
					}
				});

				return;
			}

			await ReplyAsync(EmoteHelper.Cross + " No users are blacklisted.");
		}

		[Command("Eval")]
		[Name("Bot Eval")]
		[Description("Evaluate C# code")]
		[Usage("bot eval 2+2")]
		[RunMode(RunMode.Parallel)]
		public async Task EvalAsync(
			[Name("Code")]
			[Description("The code you want to evaluate")]
			[Remainder] string script)
		{
			var props = new EvaluationHelper(Context);
			var result = await Scripting.EvaluateScriptAsync(script, props);

			var canUseEmbed = true;
			string stringRep;

			if (result.IsSuccess)
			{
				if (result.ReturnValue != null)
				{
					var special = false;

					switch (result.ReturnValue)
					{
						case string str:
							stringRep = str;
							break;

						case IDictionary dictionary:
							var asb = new StringBuilder();
							asb.Append("Dictionary of type ``").Append(dictionary.GetType().Name).AppendLine("``");
							foreach (var ent in dictionary.Keys)
								asb.Append("- ``").Append(ent).Append("``: ``").Append(dictionary[ent]).AppendLine("``");

							stringRep = asb.ToString();
							special = true;
							canUseEmbed = false;
							break;

						case IEnumerable enumerable:
							var asb0 = new StringBuilder();
							asb0.Append("Enumerable of type ``").Append(enumerable.GetType().Name).AppendLine("``");
							foreach (var ent in enumerable)
								asb0.Append("- ``").Append(ent).AppendLine("``");

							stringRep = asb0.ToString();
							special = true;
							canUseEmbed = false;
							break;

						default:
							stringRep = result.ReturnValue.ToString();
							break;
					}

					if ((stringRep.StartsWith("```") && stringRep.EndsWith("```")) || special)
					{
						canUseEmbed = false;
					}
					else
					{
						stringRep = $"```cs\n{stringRep}```";
					}
				}
				else
				{
					stringRep = "No results returned.";
				}

				if (canUseEmbed)
				{
					var footerString = $"{(result.CompilationTime != -1 ? $"Compilation time: {result.CompilationTime}ms" : "")} {(result.ExecutionTime != -1 ? $"| Execution time: {result.ExecutionTime}ms" : "")}";

					await ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Added)
						.WithTitle("Scripting Result")
						.WithDescription(result.ReturnValue != null ? "Type: `" + result.ReturnValue.GetType().Name + "`" : "")
						.AddField("Input", $"```cs\n{script}```")
						.AddField("Output", stringRep)
						.WithFooter(footerString, Context.Client.CurrentUser.GetAvatar())
						.Build());

					return;
				}

				await ReplyAsync(stringRep);
				return;
			}

			var embed = EmbedHelper.Embed(EmbedHelper.Deleted)
				.WithTitle("Scripting Result")
				.WithDescription("Scripting failed during stage **" + FormatEnumMember(result.FailedStage) + "**");

			embed.AddField("Input", $"```cs\n{script}```");

			if (result.CompilationDiagnostics?.Count > 0)
			{
				var field = new EmbedFieldBuilder { Name = "Compilation Errors" };
				var sb = new StringBuilder();
				foreach (var compilationDiagnostic in result.CompilationDiagnostics.OrderBy(a => a.Location.SourceSpan.Start))
				{
					var start = compilationDiagnostic.Location.SourceSpan.Start;
					var end = compilationDiagnostic.Location.SourceSpan.End;

					var bottomRow = script.Substring(start, end - start);

					if (!string.IsNullOrEmpty(bottomRow))
						sb.Append("`").Append(bottomRow).AppendLine("`");

					sb.Append(" - ``").Append(compilationDiagnostic.Id).Append("`` (").Append(FormatDiagnosticLocation(compilationDiagnostic.Location)).Append("): **")
						.Append(compilationDiagnostic.GetMessage()).AppendLine("**");
					sb.AppendLine();
				}
				field.Value = sb.ToString();

				if (result.Exception != null)
					sb.AppendLine();

				embed.AddField(field);
			}

			if (result.Exception != null)
				embed.AddField("Exception", $"``{result.Exception.GetType().Name}``: ``{result.Exception.Message}``");

			await ReplyAsync(embed: embed.Build());
			return;
		}

		[Command("Ping")]
		[Name("Bot Ping")]
		[Description("Returns the estimated connection time to the Discord servers")]
		[Usage("bot ping")]
		public async Task PingAsync()
		{
			var latency = Context.Client.Latency;
			var content = "Latency: " + latency + "ms\nPing: ";
			var sw = new Stopwatch();

			sw.Start();

			var message = await ReplyAsync(content);

			sw.Stop();

			content = content + sw.ElapsedMilliseconds + "ms";
			await message.ModifyAsync(x => x.Content = content);
		}

		[Command("Prefix")]
		[Name("Bot Prefix")]
		[Description("Sets the prefix used globally and what the start up Activity uses")]
		[Usage("bot prefix >>")]
		public async Task PrefixAsync(
			[Name("Prefix")]
			[Description("The new global prefix you want to set")]
			[Remainder] string prefix)
		{
			var config = await Database.BotConfigs.FirstAsync();
			config.Prefix = prefix;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("ProjectChannel")]
		[Name("Bot ProjectChannel")]
		[Description("Sets the project channel for posting bot changes from")]
		[Usage("bot projectchannel #channel")]
		public async Task ProjectChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the project channel")]
			SocketGuildChannel channel)
		{
			var config = await Database.BotConfigs.FirstAsync();
			config.ProjectChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("SupportChannel")]
		[Name("Bot SupportChannel")]
		[Description("Set the channel for bot support (bug/feature requests/etc) to be posted in")]
		[Usage("bot supportchannel #channel")]
		public async Task SupportChannelAsync(
			[Name("Channel")]
			[Description("The channel you want to set as the support channel")]
			SocketGuildChannel channel)
		{
			var config = await Database.BotConfigs.FirstAsync();
			config.SupportChannel = channel.Id;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Token")]
		[Name("Bot Token")]
		[Description("Update the Bot Token through the bot, in case it somehow gets leaked, requires a restart")]
		[Usage("bot token 123abc")]
		public async Task TokenAsync(
			[Name("Token")]
			[Description("The new Token for the bot to use")]
			[Remainder] string token)
		{
			var config = await Database.BotConfigs.FirstAsync();
			config.BotToken = token;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("TwitchClientId")]
		[Name("Bot TwitchClientId")]
		[Description("Update the Twitch Client Id for the Twitch stream integration")]
		[Usage("bot twitchclientid 123abc")]
		public async Task TwitchClientIdAsync(
			[Name("Client Id")]
			[Description("The updated Twitch Client Id")]
			[Remainder] string clientId)
		{
			var config = await Database.BotConfigs.FirstAsync();
			config.TwitchClientId = clientId;
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Username")]
		[Name("Bot Username")]
		[Description("Changes the bots username")]
		[Usage("bot username poopy face")]
		public async Task UsernameAsync(
			[Name("Username")]
			[Description("The new username to set as the bots username")]
			[Remainder] string username)
		{
			await Context.Client.CurrentUser.ModifyAsync(x => x.Username = username);
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		private static string FormatEnumMember(Enum value)
		{
			return value.ToString().Replace(value.GetType().Name + ".", "");
		}

		private static string FormatDiagnosticLocation(Location loc)
		{
			if (!loc.IsInSource)
				return "Metadata";
			if (loc.SourceSpan.Start == loc.SourceSpan.End)
				return "Ch " + loc.SourceSpan.Start;

			return $"Ch {loc.SourceSpan.Start}-{loc.SourceSpan.End}";
		}
	}
}