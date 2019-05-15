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
	public class EventService
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly DatabaseContext _database;
		private readonly LogService _log;
		private readonly CommandHandlerService _commandHandler;

		public EventService(DiscordSocketClient client, CommandService commands, DatabaseContext database, LogService log, CommandHandlerService commandHandler)
		{
			_client = client;
			_commands = commands;
			_database = database;
			_log = log;
			_commandHandler = commandHandler;
		}

		public void Initialize()
		{
			_client.JoinedGuild += JoinedGuild;
			_client.LeftGuild += LeftGuild;
			_client.Log += _log.LogMessage;
			_client.MessageDeleted += MessageDeletedAsync;
			_client.MessageReceived += _commandHandler.HandleMessageAsync;
			_client.MessageUpdated += async (_, message, __) => await _commandHandler.HandleMessageAsync(message);
			_client.Ready += Ready;

			_commands.CommandErrored += _commandHandler.CommandErroredAsync;
			_commands.CommandExecuted += _commandHandler.CommandExecutedAsync;
		}

		private async Task JoinedGuild(SocketGuild guild)
		{
			await _database.Guilds.AddAsync(new Guild
			{
				GuildId = guild.Id
			});
			await _database.SaveChangesAsync();
			return;
		}

		private async Task LeftGuild(SocketGuild guild)
		{
			_database.Guilds.Remove(await _database.Guilds.FirstAsync(x => x.GuildId == guild.Id));
			await _database.SaveChangesAsync();
			return;
		}

		private async Task MessageDeletedAsync(Cacheable<IMessage, ulong> cache, ISocketMessageChannel channel)
		{
			var guild = await _database.Guilds.AsNoTracking().FirstAsync(x => x.GuildId == (channel as SocketGuildChannel).Guild.Id);

			if (!guild.EnableDeletionLog)
				return;

			var message = cache.Value ?? await cache.GetOrDownloadAsync();

			if ((string.IsNullOrWhiteSpace(message.Content) && message.Attachments.Count is 0) || message.Author.IsBot)
				return;

			if (guild.MessageLogChannel is 0)
				return;

			var embed = EmbedHelper.Embed(EmbedHelper.Deleted)
				.WithAuthor(message.Author)
				.WithThumbnailUrl((message.Author as SocketUser)?.GetAvatar())
				.WithTitle("Message Deleted")
				.AddField("**Channel**:", "#" + message.Channel.Name)
				.AddField("**Content**:", message.Attachments.FirstOrDefault()?.Url ?? message.Content)
				.WithCurrentTimestamp()
				.Build();

			var socketGuild = (message.Author as SocketGuildUser)?.Guild;
			var logChannel = socketGuild.GetTextChannel(guild.MessageLogChannel);
			await logChannel.SendMessageAsync(embed: embed);
		}

		private async Task Ready()
		{
			var botConfig = await _database.BotConfigs.AsNoTracking().FirstAsync();
			var guilds = await _database.Guilds.AsNoTracking().ToListAsync();
			var guildIds = guilds.Select(x => Convert.ToUInt64(x.GuildId)).ToList();

			await _client.SetActivityAsync(new Game("Use " + botConfig.Prefix + "Help", ActivityType.Playing));
			await _log.LogMessage(new LogMessage(LogSeverity.Info, "Presence", "Activity has been set to: [" + ActivityType.Playing + "] Use " + botConfig.Prefix + "Help"));

			foreach (var guild in _client.Guilds.Select(x => x.Id))
			{
				if (!guildIds.Contains(guild))
				{
					await _database.Guilds.AddAsync(new Guild
					{
						GuildId = guild
					});
				}
			}

			foreach (var guild in guilds)
			{
				if (!_client.Guilds.Select(x => x.Id).Contains(guild.GuildId))
					_database.Guilds.Remove(guilds.First(x => x.GuildId == guild.GuildId));
			}

			await _database.SaveChangesAsync();
		}
	}
}