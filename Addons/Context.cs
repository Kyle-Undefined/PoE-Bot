namespace PoE.Bot.Addons
{
    using Discord;
    using Discord.Commands;
    using Handlers;
    using Microsoft.Extensions.DependencyInjection;
    using Objects;
    using System;
    using System.Net.Http;

    public class Context : ICommandContext
    {
        public Context(IDiscordClient client, IUserMessage message, IServiceProvider provider)
        {
            Client = client;
            Message = message;
            User = message.Author;
            Channel = message.Channel;
            Random = provider.GetRequiredService<Random>();
            Guild = (message.Channel as IGuildChannel).Guild;
            Config = provider.GetRequiredService<ConfigObject>();
            HttpClient = provider.GetRequiredService<HttpClient>();
            DatabaseHandler = provider.GetRequiredService<DatabaseHandler>();
            if (!(Guild is null))
                Server = provider.GetRequiredService<DatabaseHandler>().Execute<GuildObject>(Operation.Load, Id: $"{Guild.Id}");
        }

        public IMessageChannel Channel { get; }
        public IDiscordClient Client { get; }
        public ConfigObject Config { get; }
        public DatabaseHandler DatabaseHandler { get; }
        public IGuild Guild { get; }
        public HttpClient HttpClient { get; }
        public IUserMessage Message { get; }
        public Random Random { get; }
        public GuildObject Server { get; }
        public IUser User { get; }
    }
}