namespace PoE.Bot.Handlers
{
    using Addons;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using Objects;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class EventHandler
    {
        private bool guildCheck = true;

        public EventHandler(DatabaseHandler databaseHandler, DiscordSocketClient client, ConfigObject config, IServiceProvider service, CommandService commandService, EventHelper eventHelper)
        {
            DatabaseHandler = databaseHandler;
            Client = client;
            Config = config;
            Provider = service;
            EventHelper = eventHelper;
            CommandService = commandService;
            CancellationToken = new CancellationTokenSource();
        }

        private CancellationTokenSource CancellationToken { get; set; }
        private DiscordSocketClient Client { get; }
        private CommandService CommandService { get; }
        private ConfigObject Config { get; }
        private DatabaseHandler DatabaseHandler { get; }
        private EventHelper EventHelper { get; }
        private IServiceProvider Provider { get; }

        public Task InitializeAsync()
            => CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);

        internal Task Connected()
            => Task.Run(()
                => CancellationToken.Cancel()).ContinueWith(_ => CancellationToken = new CancellationTokenSource());

        internal Task Disconnected(Exception error)
            => Task.Factory.StartNew(() => Task.Delay(EventHelper.GlobalTimeout, CancellationToken.Token).ContinueWith(async _ => await EventHelper.CheckStateAsync(Client).ConfigureAwait(false)));

        internal Task JoinedGuild(SocketGuild guild)
            => Task.Run(()
                => DatabaseHandler.Execute<GuildObject>(Operation.Create, new GuildObject { Id = $"{guild.Id}", Prefix = '!' }, guild.Id));

        internal Task LeftGuild(SocketGuild guild)
            => Task.Run(()
                => DatabaseHandler.Execute<GuildObject>(Operation.Delete, id: guild.Id));

        internal Task Log(LogMessage message)
            => Task.Run(()
                => LogHandler.Write(message.Exception is null ? Source.Discord : Source.Exception, message.Exception is null ? message.Message : $"{message.Exception?.Message}\n{message.Exception.StackTrace}"));

        internal async Task MessageDeletedAsync(Cacheable<IMessage, ulong> cache, ISocketMessageChannel channel)
        {
            GuildObject server = DatabaseHandler.Execute<GuildObject>(Operation.Load, id: (channel as SocketGuildChannel).Guild.Id);
            if (!server.LogDeleted)
                return;
            if (server.RoleSetChannel == channel.Id)
                return;

            IMessage message = cache.HasValue ? cache.Value : await Task.Factory.StartNew(async () => await cache.GetOrDownloadAsync().ConfigureAwait(false)).Result.ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(message.Content) || message.Author.IsBot)
                return;

            server.DeletedMessages.Add(new MessageObject
            {
                MessageId = message.Id,
                ChannelId = channel.Id,
                DateTime = DateTime.Now,
                AuthorId = message.Author.Id,
                Content = message.Content ?? message.Attachments.FirstOrDefault().Url
            });

            DatabaseHandler.Save<GuildObject>(server, (channel as SocketGuildChannel).Guild.Id);

            if (server.AllLog is 0)
                return;

            Embed embed = Extras.Embed(Extras.Deleted)
                .WithAuthor(message.Author)
                .WithThumbnailUrl(message.Author.GetAvatarUrl())
                .WithTitle("Message Deleted")
                .AddField("**Channel**:", $"#{message.Channel.Name}")
                .AddField("**Content**:", $"{(message.Attachments.Any() ? message.Attachments.FirstOrDefault()?.Url : message.Content)}")
                .WithCurrentTimestamp()
                .Build();
            SocketGuild guild = (message.Author as SocketGuildUser).Guild;
            SocketTextChannel mod = guild.GetTextChannel(server.AllLog);
            await Task.Factory.StartNew(async () => await mod.SendMessageAsync(embed: embed).ConfigureAwait(false));
        }

        internal async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message) || message.Channel is IDMChannel)
                return;

            int argPos = 0;
            Context context = new Context(Client, message, Provider);
            if (Config.Blacklist.Contains(message.Author.Id) || message.Author.IsBot || message.Author.IsWebhook)
                return;

            EventHelper.RunTasks(message, context);

            // Inline Wiki command, just because the users want it so bad
            if (message.Content.Contains("[["))
            {
                string item = message.Content.Split('[', ']')[2];
                IResult result = await Task.Factory.StartNew(async () => await CommandService.ExecuteAsync(context, $"Wiki {item}", Provider, MultiMatchHandling.Best).ConfigureAwait(false)).Result.ConfigureAwait(false);
            }
            else
            {
                if (!(message.HasStringPrefix(Config.Prefix, ref argPos) || message.HasCharPrefix(context.Server.Prefix, ref argPos)))
                    return;

                IResult result = await Task.Factory.StartNew(async () => await CommandService.ExecuteAsync(context, argPos, Provider, MultiMatchHandling.Best).ConfigureAwait(false)).Result.ConfigureAwait(false);
                switch (result.Error)
                {
                    case CommandError.UnmetPrecondition:
                        GuildPermissions permissions = (message.Channel as SocketGuildChannel).Guild.CurrentUser.GuildPermissions;
                        if (!string.IsNullOrWhiteSpace(result.ErrorReason) && permissions.SendMessages && permissions.ViewChannel)
                        {
                            var msg = await message.Channel.SendMessageAsync(result.ErrorReason).ConfigureAwait(false);
                            _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(async _ => await msg.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
                            break;
                        }
                        else
                            break;

                    case CommandError.Unsuccessful:
                        await message.Channel.SendMessageAsync($"{Extras.Cross} When one defiles the effigy, one defiles the emperor. Use the {context.Server.Prefix}Feedback command to report this {Extras.Bug}").ConfigureAwait(false);
                        break;
                }
            }
        }

        internal async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
            => await ReactionHandlerAsync(cache, reaction, true).ConfigureAwait(false);

        internal async Task ReactionHandlerAsync(Cacheable<IUserMessage, ulong> cache, SocketReaction reaction, bool reactionAdded)
        {
            GuildObject server = DatabaseHandler.Execute<GuildObject>(Operation.Load, id: (reaction.Channel as SocketGuildChannel).Guild.Id);
            if (server.DevChannel == reaction.Channel.Id)
            {
                if (reaction.Emote.Name == Extras.Check.Name && (reaction.UserId == MethodHelper.RunSync(Client.GetApplicationInfoAsync()).Owner.Id))
                {
                    SocketGuild guild = (reaction.Channel as SocketGuildChannel).Guild;
                    if (!(guild.GetTextChannel(server.BotChangeChannel) is IMessageChannel BotChangeChannel))
                        return;

                    IUserMessage message = cache.HasValue ? cache.Value : await cache.GetOrDownloadAsync().ConfigureAwait(false);
                    if (reactionAdded)
                        await BotChangeChannel.SendMessageAsync(message.Content).ConfigureAwait(false);
                    else
                    {
                        var botChanMessages = await BotChangeChannel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
                        IUserMessage botMessage = await BotChangeChannel.GetMessageAsync(botChanMessages.FirstOrDefault(m => m.Content == message.Content).Id, CacheMode.AllowDownload).ConfigureAwait(false) as IUserMessage;
                        await botMessage.DeleteAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        internal async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
            => await ReactionHandlerAsync(cache, reaction, false).ConfigureAwait(false);

        internal Task Ready()
        {
            Client.SetActivityAsync(new Game($"Use {Config.Prefix}Commands", ActivityType.Playing));
            LogHandler.Write(Source.Event, $"Game has been set to: [{ActivityType.Playing}] Use {Config.Prefix}Commands");
            if (guildCheck)
                Task.Run(() =>
                {
                    var Servers = DatabaseHandler.Servers().Select(x => Convert.ToUInt64(x.Id));
                    foreach (ulong Guild in Client.Guilds.Select(x => x.Id))
                        if (!Servers.Contains(Guild))
                            DatabaseHandler.Execute<GuildObject>(Operation.Create, new GuildObject { Id = $"{Guild}", Prefix = '!' }, Guild);

                    foreach (ulong Server in Servers)
                        if (!Client.Guilds.Select(x => x.Id).Contains(Convert.ToUInt64(Server)))
                            DatabaseHandler.Execute<GuildObject>(Operation.Delete, id: Server);

                    LogHandler.ForceGC();
                    guildCheck = false;
                });
            return Task.CompletedTask;
        }
    }
}