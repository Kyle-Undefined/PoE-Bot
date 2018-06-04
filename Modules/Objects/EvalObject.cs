namespace PoE.Bot.Modules.Objects
{
    using PoE.Bot.Addons;
    using Discord.WebSocket;

    public class EvalObject
    {
        public IContext Context { get; set; }
        public SocketGuild Guild { get; set; }
        public SocketGuildUser User { get; set; }
        public DiscordSocketClient Client { get; set; }
        public SocketGuildChannel Channel { get; set; }
    }
}
