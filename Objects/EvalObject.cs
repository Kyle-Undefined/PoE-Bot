namespace PoE.Bot.Objects
{
    using Addons;
    using Discord.WebSocket;

    public class EvalObject
    {
        public Context Context { get; set; }
        public SocketGuild Guild { get; set; }
        public SocketGuildUser User { get; set; }
        public DiscordSocketClient Client { get; set; }
        public SocketGuildChannel Channel { get; set; }
    }
}
