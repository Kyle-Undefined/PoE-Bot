namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using System.Linq;
	using System.Threading.Tasks;

	[Service]
	public class ReactionService
	{
		private readonly DiscordSocketClient _client;
		private readonly DatabaseContext _database;

		public ReactionService(DiscordSocketClient client, DatabaseContext database)
		{
			_client = client;
			_database = database;
		}

		public void Initialize()
		{
			_client.ReactionAdded += ReactionAdded;
			_client.ReactionRemoved += ReactionRemoved;
		}

		private Task ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) => ReactionHandlerAsync(cache, reaction, true);

		private async Task ReactionHandlerAsync(Cacheable<IUserMessage, ulong> cache, SocketReaction reaction, bool reactionAdded)
		{
			var guild = await _database.Guilds.AsNoTracking().FirstAsync(x => x.GuildId == (reaction.Channel as SocketGuildChannel).Guild.Id);

			if ((await _database.BotConfigs.AsNoTracking().FirstAsync()).ProjectChannel == reaction.Channel.Id && reaction.Emote.Name == EmoteHelper.Check.Name)
			{
				var botOwner = (await _client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
				if (reaction.UserId == botOwner)
				{
					foreach (var activeGuild in (await _database.Guilds.AsNoTracking().Where(x => x.BotChangeChannel != 0).ToListAsync()))
					{
						if (!(_client.GetChannel(activeGuild.BotChangeChannel) is SocketChannel))
							continue;

						var channel = _client.GetChannel(activeGuild.BotChangeChannel) as SocketTextChannel;
						var cachedMessage = cache.Value ?? await cache.GetOrDownloadAsync();

						if (reactionAdded)
						{
							await channel.SendMessageAsync(cachedMessage.Content);
						}
						else
						{
							var messages = await channel.GetMessagesAsync().FlattenAsync();

							if (!messages.All(y => y.Content != cachedMessage.Content))
							{
								var message = await channel.GetMessageAsync(messages.First(y => y.Content == cachedMessage.Content).Id) as IUserMessage;
								await message.DeleteAsync();
							}
						}
					}
				}
			}
			else if (reaction.Channel.Id == guild.RulesChannel)
			{
				var socketGuild = (reaction.Channel as SocketGuildChannel).Guild;
				var user = socketGuild.GetUser(reaction.UserId);
				SocketRole role = null;

				if (reaction.Emote.Name == EmoteHelper.Announcement.Name)
					role = socketGuild.GetRole(guild.AnnouncementRole);
				else if (reaction.Emote.Name == EmoteHelper.Lottery.Name)
					role = socketGuild.GetRole(guild.LotteryRole);
				else if (reaction.Emote.Name == EmoteHelper.Xbox.Name)
					role = socketGuild.GetRole(guild.XboxRole);
				else if (reaction.Emote.Name == EmoteHelper.Playstation.Name)
					role = socketGuild.GetRole(guild.PlaystationRole);

				if (!(role is null))
				{
					if (reactionAdded)
						await user.AddRoleAsync(role);
					else
						await user.RemoveRoleAsync(role);
				}
			}
		}

		private Task ReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) => ReactionHandlerAsync(cache, reaction, false);
	}
}