namespace PoE.Bot.Addons
{
    using System;
    using Discord;
    using PoE.Bot.Helpers;
    using System.Net.Http;
    using PoE.Bot.Handlers;
    using Discord.Commands;
    using PoE.Bot.Handlers.Objects;
    using Microsoft.Extensions.DependencyInjection;

    public class IContext : ICommandContext
    {
        public IUser User { get; }
        public IGuild Guild { get; }
        public Random Random { get; }
        public GuildObject Server { get; }
        public ConfigObject Config { get; }
        public IUserMessage Message { get; }
        public IDiscordClient Client { get; }
        public HttpClient HttpClient { get; }
        public DBHandler DBHandler { get; }
        public GuildHelper GuildHelper { get; }
        public IMessageChannel Channel { get; }

        public IContext(IDiscordClient client, IUserMessage message, IServiceProvider provider)
        {
            Client = client;
            Message = message;
            User = message.Author;
            Channel = message.Channel;
            Random = provider.GetRequiredService<Random>();
            Guild = (message.Channel as IGuildChannel).Guild;
            Config = provider.GetRequiredService<ConfigObject>();
            HttpClient = provider.GetRequiredService<HttpClient>();
            DBHandler = provider.GetRequiredService<DBHandler>();
            GuildHelper = provider.GetRequiredService<GuildHelper>();
            Server = provider.GetRequiredService<DBHandler>().Execute<GuildObject>(Operation.LOAD, Id: $"{Guild.Id}");
        }
    }
}
