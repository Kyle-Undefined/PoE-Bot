namespace PoE.Bot.Handlers
{
    using Discord;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;

    public class MainHandler
    {
        ConfigObject Config { get; }
        EventHandler Event { get; }
        DiscordSocketClient Client { get; }

        public MainHandler(DiscordSocketClient client, EventHandler events, ConfigObject config)
        {
            Client = client;
            Event = events;
            Config = config;
        }

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
