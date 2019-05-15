namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	[Service]
	public class CommandHandlerService
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly DatabaseContext _database;
		private readonly LogService _log;
		private readonly IServiceProvider _services;

		public CommandHandlerService(DiscordSocketClient client, CommandService command, DatabaseContext database, LogService log, IServiceProvider services)
		{
			_client = client;
			_commands = command;
			_database = database;
			_log = log;
			_services = services;
		}

		public async Task HandleMessageAsync(SocketMessage socketMessage)
		{
			if (!(socketMessage is SocketUserMessage message) || message.Channel is IDMChannel)
				return;

			var context = new GuildContext(_client, message);
			var blacklist = await _database.BlacklistedUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == message.Author.Id);
			var botConfig = await _database.BotConfigs.AsNoTracking().FirstAsync();
			var guild = await _database.Guilds.AsNoTracking().Include(x => x.Users).Include(x => x.Profanities).FirstAsync(x => x.GuildId == context.Guild.Id);

			if (!(blacklist is null) || message.Source != MessageSource.User)
				return;

			await ProfanityHandler(message, guild);

			// Inline Wiki command, just because the users want it so bad
			if (message.Content.Contains("[["))
			{
				var item = message.Content.Split('[', ']')[2];
				var result = await _commands.ExecuteAsync("Wiki " + item, context, _services);
			}
			else
			{
				if (!CommandUtilities.HasPrefix(message.Content, botConfig.Prefix, out string output))
					return;

				var result = await _commands.ExecuteAsync(output, context, _services);
				await CommandExecutedResultAsync(result, context);
			}
		}

		public async Task CommandErroredAsync(ExecutionFailedResult result, ICommandContext originalContext, IServiceProvider _)
		{
			if (!(originalContext is GuildContext context))
				return;

			if (!string.IsNullOrWhiteSpace(result.Exception.ToString()))
			{
				await _log.LogMessage(new LogMessage(LogSeverity.Error, "Command", string.Empty, result.Exception));

				(_client.GetChannel((await _database.BotConfigs.AsNoTracking().FirstAsync()).SupportChannel) as SocketTextChannel)?.SendMessageAsync("**Guild**: " + context.Guild.Name + " ("
					+ context.Guild.Id + ")\n" + "**Channel**: " + context.Channel.Name + "(" + context.Channel.Id + ")\n**Command**: " + context.Message + " \n**Reason**: " + result.Reason + "\n"
					+ "**Exception**: ```" + result.Exception.Message + "\n" + result.Exception.TargetSite + "```");

				await context.Channel.SendMessageAsync(EmoteHelper.Cross + " I'm no beast of burden. *There was an error running the command and has been logged*");
			}
		}

		public async Task CommandExecutedAsync(Command command, CommandResult _, ICommandContext originalContext, IServiceProvider __)
		{
			if (!(originalContext is GuildContext context))
				return;

			await _log.LogMessage(new LogMessage(LogSeverity.Info, "Command", "Executed \"" + command.Name + "\" for " + context.User.GetDisplayName() + "#" + context.User.Discriminator
				+ " in " + context.Guild.Name + "/" + context.Channel.Name));
		}

		private async Task CommandExecutedResultAsync(IResult result, GuildContext context)
		{
			if (result.IsSuccessful)
				return;

			switch (result)
			{
				case ExecutionFailedResult _:
				case CommandResult _:
					return;

				case ChecksFailedResult checks:
					await context.Channel.SendMessageAsync(string.Join("\n", checks.FailedChecks.Select(x => x.Result.Reason)));
					break;

				case CommandNotFoundResult notfound:
					await context.Channel.SendMessageAsync(EmoteHelper.Cross + " I'm no beast of burden. *Command not found*");
					break;

				case ArgumentParseFailedResult args:
					await context.Channel.SendMessageAsync(EmoteHelper.Cross + " I'm no beast of burden. *Bad command arguments. Example Usage: `"
						+ (await _database.BotConfigs.AsNoTracking().FirstAsync()).Prefix + (args.Command.Attributes.FirstOrDefault(x => x is UsageAttribute) as UsageAttribute)?.ExampleUsage + "`*");
					break;

				case TypeParseFailedResult parser:
					await context.Channel.SendMessageAsync(EmoteHelper.Cross + " I'm no beast of burden. *Type Parser failed* : `" + parser.Value + "` is not a " + parser.Parameter);
					break;

				default:
					await context.Channel.SendMessageAsync(EmoteHelper.Cross + " When one defiles the effigy, one defiles the emperor. *The command was unsuccessful, "
						+ "if this continues please use the `" + (await _database.BotConfigs.AsNoTracking().FirstAsync()).Prefix + "support bug` command to submit an issue*");
					break;
			}
		}

		private Task ProfanityHandler(SocketMessage message, Guild guild)
		{
			var socketGuild = (message.Author as SocketGuildUser)?.Guild;
			var user = message.Author as SocketGuildUser;

			if (guild.MaxWarnings is 0 || user.Id == socketGuild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels
				|| user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
			{
				return Task.CompletedTask;
			}

			if (message.Content.ProfanityMatch(guild.Profanities) && guild.EnableAntiProfanity)
				return GuildHelper.WarnUserAsync(message, guild, _database, message.Author.Mention + ", Refrain from using profanity. You've been warned.");

			return Task.CompletedTask;
		}
	}
}