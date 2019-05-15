namespace PoE.Bot.Helpers
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Models;
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	public static class GuildHelper
	{
		public static async Task LogCaseAsync(DatabaseContext database, SocketGuild socketGuild, SocketUser user, SocketUser mod, CaseType caseType, string reason = null)
		{
			var guild = await database.Guilds.AsNoTracking().Include(x => x.Cases).FirstAsync(x => x.GuildId == socketGuild.Id);
			reason = reason ?? "No Reason Specified";
			var modChannel = socketGuild.GetTextChannel(guild.CaseLogChannel);
			IUserMessage message = null;

			if (!(modChannel is null))
			{
				var cases = guild.Cases.Where(x => x.UserId == user.Id);
				var embed = EmbedHelper.Embed(EmbedHelper.Case)
					.WithAuthor("Case Number: " + guild.Cases.Count + 1)
					.WithTitle(caseType.ToString())
					.AddField("User", user.Mention + " `" + user + "` (" + user.Id + ")")
					.AddField("History",
						"Cases: " + cases.Count() + "\n"
						+ "Warnings: " + cases.Count(x => x.CaseType == CaseType.Warning) + "\n"
						+ "Mutes: " + cases.Count(x => x.CaseType == CaseType.Mute) + "\n"
						+ "Auto Mutes: " + cases.Count(x => x.CaseType == CaseType.AutoMute) + "\n")
					.AddField("Reason", reason)
					.AddField("Moderator", mod)
					.WithCurrentTimestamp()
					.Build();
				message = await modChannel.SendMessageAsync(embed: embed);
			}

			await database.Cases.AddAsync(new Case
			{
				UserId = user.Id,
				Reason = reason,
				CaseType = caseType,
				ModeratorId = mod.Id,
				CaseDate = DateTime.Now,
				Number = guild.Cases.Count + 1,
				MessageId = message?.Id ?? 0,
				GuildId = guild.Id
			});

			await database.SaveChangesAsync();
		}

		public static async Task MuteUserAsync(DatabaseContext database, SocketMessage message, Guild guild, SocketGuildUser user, CaseType caseType, TimeSpan time, string reason)
		{
			var socketGuild = (message.Author as SocketGuildUser)?.Guild;
			await user.AddRoleAsync(socketGuild.GetRole(guild.MuteRole));
			var mutedUser = guild.Users.FirstOrDefault(x => x.UserId == user.Id);

			if (!(mutedUser is null))
			{
				mutedUser.Muted = true;
				mutedUser.MutedUntil = DateTime.Now.Add(time);
			}
			else
			{
				guild.Users.Add(new User
				{
					UserId = user.Id,
					Muted = true,
					MutedUntil = DateTime.Now.Add(time),
					GuildId = guild.Id
				});
			}

			await LogCaseAsync(database, socketGuild, user, socketGuild.CurrentUser, caseType, reason + " (" + time.FormatTimeSpan() + ")");

			var embed = EmbedHelper.Embed(EmbedHelper.Info)
				.WithAuthor(socketGuild.CurrentUser)
				.WithTitle("Mod Action")
				.WithDescription("You were muted in the " + socketGuild.Name + " guild.")
				.WithThumbnailUrl(socketGuild.CurrentUser.GetAvatar())
				.WithFooter("You can PM any Moderator directly to resolve the issue.")
				.AddField("Reason", reason)
				.AddField("Duration", time.FormatTimeSpan())
				.Build();

			await user.TrySendDirectMessageAsync(embed: embed);
		}

		public static async Task WarnUserAsync(SocketMessage message, Guild guild, DatabaseContext database, string warning)
		{
			await message.DeleteAsync().ContinueWith(async _ =>
			{
				var socketGuild = (message.Author as SocketGuildUser)?.Guild;
				var user = message.Author as SocketGuildUser;
				if (guild.MaxWarnings is 0 || user.Id == socketGuild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels
					|| user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
				{
					return;
				}

				var profile = await guild.GetUserProfile(user.Id, database);
				profile.Warnings++;
				database.Users.Update(profile);
				await database.SaveChangesAsync();

				if (profile.Warnings >= guild.MaxWarnings)
					await MuteUserAsync(database, message, guild, user, CaseType.AutoMute, TimeSpan.FromDays(1), "Auto Muted. " + warning + " For saying: `" + message.Content + "`");
				else
					await LogCaseAsync(database, socketGuild, user, socketGuild.CurrentUser, CaseType.Warning, warning + " For saying: `" + message.Content + "`");

				await message.Channel.SendMessageAsync(warning);
			});
		}
	}
}