namespace PoE.Bot.Handlers
{
    using Discord;
    using Discord.WebSocket;
    using Objects;
    using System.Threading.Tasks;

    public class MainHandler
    {
        public MainHandler(DiscordSocketClient client, EventHandler events, ConfigObject config)
        {
            Client = client;
            Event = events;
            Config = config;
        }

        private DiscordSocketClient Client { get; }
        private ConfigObject Config { get; }
        private EventHandler Event { get; }

        public async Task InitializeAsync()
        {
            Client.Log += Event.Log;
            Client.Ready += Event.Ready;
            Client.LeftGuild += Event.LeftGuild;
            Client.Connected += Event.Connected;
            Client.JoinedGuild += Event.JoinedGuild;
            Client.Disconnected += Event.Disconnected;
            Client.ReactionAdded += Event.ReactionAddedAsync;
            Client.MessageDeleted += Event.MessageDeletedAsync;
            Client.MessageReceived += Event.MessageReceivedAsync;
            Client.ReactionRemoved += Event.ReactionRemovedAsync;

            await Client.LoginAsync(TokenType.Bot, Config.APIKeys["BT"]).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
        }
    }
}