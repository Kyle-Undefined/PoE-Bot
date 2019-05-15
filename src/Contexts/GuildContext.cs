namespace PoE.Bot.Contexts
{
	using Discord.WebSocket;
	using Qmmands;

	public class GuildContext : ICommandContext
	{
		public GuildContext(DiscordSocketClient client, SocketUserMessage message)
		{
			Channel = message.Channel as SocketTextChannel;
			Client = client;
			Guild = (message.Channel as SocketGuildChannel)?.Guild;
			Message = message;
			User = message.Author as SocketGuildUser;
		}

		public SocketTextChannel Channel { get; }
		public DiscordSocketClient Client { get; }
		public SocketGuild Guild { get; }
		public SocketUserMessage Message { get; }
		public SocketGuildUser User { get; }
	}
}