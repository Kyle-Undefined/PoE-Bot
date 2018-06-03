namespace PoE.Bot.Handlers
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using System.Threading;
    using System.Reflection;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Handlers.Objects;
    using PoE.Bot.Modules;
    using Drawing = System.Drawing.Color;

    public class EventHandler
    {
        DBHandler DB { get; }
        Random Random { get; }
        ConfigObject Config { get; }
        GuildHelper GuildHelper { get; }
        EventHelper EventHelper { get; }
        IServiceProvider Provider { get; }
        DiscordSocketClient Client { get; }
        bool GuildCheck = true;
        CommandService CommandService { get; }
        CancellationTokenSource CancellationToken { get; set; }
        public EventHandler(DBHandler db, DiscordSocketClient client, ConfigObject config,
            GuildHelper helper, IServiceProvider service, CommandService commandService, Random random,
            EventHelper eventHelper)
        {
            DB = db;
            Client = client;
            Config = config;
            Provider = service;
            Random = random;
            GuildHelper = helper;
            EventHelper = eventHelper;
            CommandService = commandService;
            CancellationToken = new CancellationTokenSource();
        }

        public Task InitializeAsync()
            => CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);

        internal Task Ready()
        {
            Client.SetActivityAsync(new Game($"Use {Config.Prefix}Commands", ActivityType.Playing));
            LogHandler.Write(Source.EVT, $"Game has been set to: [{ActivityType.Playing}] Use {Config.Prefix}Commands");
            if (GuildCheck)
                _ = Task.Run(() =>
                {
                    var Servers = DB.Servers().Select(x => Convert.ToUInt64(x.Id));
                    foreach (var Guild in Client.Guilds.Select(x => x.Id))
                        if (!Servers.Contains(Guild))
                            DB.Execute<GuildObject>(Operation.CREATE, new GuildObject { Id = $"{Guild}", Prefix = '!' }, Guild);
                    foreach (var Server in Servers)
                        if (!Client.Guilds.Select(x => x.Id).Contains(Convert.ToUInt64(Server)))
                            DB.Execute<GuildObject>(Operation.DELETE, Id: Server);
                    LogHandler.ForceGC();
                    GuildCheck = false;
                });
            return Task.CompletedTask;
        }

        internal Task Connected()
            => Task.Run(()
                => CancellationToken.Cancel()).ContinueWith(x
                => CancellationToken = new CancellationTokenSource());

        internal Task Disconnected(Exception Error)
        {
            _ = Task.Delay(EventHelper.GlobalTimeout, CancellationToken.Token).ContinueWith(
                async _ => await EventHelper.CheckStateAsync(Client));
            return Task.CompletedTask;
        }

        internal Task Log(LogMessage Message)
            => Task.Run(() => LogHandler.Write(Message.Exception == null ? Source.DSD : Source.EXC,
                Message.Exception == null ? Message.Message : $"{Message.Exception?.Message}\n{Message.Exception.StackTrace}"));

        internal Task JoinedGuild(SocketGuild Guild) => Task.Run(()
            => DB.Execute<GuildObject>(Operation.CREATE, new GuildObject { Id = $"{Guild.Id}", Prefix = '!' }, Guild.Id));

        internal Task LeftGuild(SocketGuild Guild) => Task.Run(()
            => DB.Execute<GuildObject>(Operation.DELETE, Id: Guild.Id));

        internal async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage Message)) return;
            int argPos = 0;
            var Context = new IContext(Client, Message, Provider);
            if (Config.Blacklist.Contains(Message.Author.Id) || Message.Author.IsBot || Message.Author.IsWebhook) return;
            EventHelper.RunTasks(Message, Context.Server);

            // look into doing inline wiki search here

            if (!(Message.HasStringPrefix(Config.Prefix, ref argPos) || Message.HasCharPrefix(Context.Server.Prefix, ref argPos))) return;
            var Result = await CommandService.ExecuteAsync(Context, argPos, Provider, MultiMatchHandling.Best);
            switch (Result.Error)
            {
                case CommandError.UnmetPrecondition:
                    var Permissions = (Message.Channel as SocketGuildChannel).Guild.CurrentUser.GuildPermissions;
                    if (!string.IsNullOrWhiteSpace(Result.ErrorReason) && Permissions.SendMessages && Permissions.ViewChannel)
                        await Message.Channel.SendMessageAsync(Result?.ErrorReason); break;
                case CommandError.MultipleMatches: LogHandler.Write(Source.EXC, Result?.ErrorReason); break;
                case CommandError.ObjectNotFound: LogHandler.Write(Source.EXC, Result?.ErrorReason); break;
                case CommandError.Unsuccessful:
                    await Message.Channel.SendMessageAsync($"Is it a {Extras.Bug} that you found?! Please report this error to my owner Kyle Undefined#1745 or use {Context.Server.Prefix}Feedback");
                    break;
            }
        }

        internal async Task MessageDeletedAsync(Cacheable<IMessage, ulong> Cache, ISocketMessageChannel Channel)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: (Channel as SocketGuildChannel).Guild.Id);
            if (!Server.LogDeleted) return;
            var Message = Cache.HasValue ? Cache.Value : await Cache.GetOrDownloadAsync();
            if (string.IsNullOrWhiteSpace(Message.Content) || Message.Author.IsBot) return;
            Server.DeletedMessages.Add(new MessageObject
            {
                MessageId = Message.Id,
                ChannelId = Channel.Id,
                DateTime = DateTime.UtcNow,
                AuthorId = Message.Author.Id,
                Content = Message.Content ?? Message.Attachments.FirstOrDefault().Url
            });
            DB.Execute<GuildObject>(Operation.SAVE, Server, (Channel as SocketGuildChannel).Guild.Id);

            if (Server.AllLog == 0) return;
            var Embed = Extras.Embed(Drawing.Red)
                .WithAuthor(Message.Author)
                .WithThumbnailUrl(Message.Author.GetAvatarUrl())
                .WithTitle("Message Deleted")
                .AddField("**Channel**:", $"#{Message.Channel.Name}")
                .AddField("**Content**:", $"{(Message.Attachments.Any() ? Message.Attachments.FirstOrDefault()?.Url : Message.Content)}")
                .WithCurrentTimestamp()
                .Build();
            var Guild = (Message.Author as SocketGuildUser).Guild;
            var mod = Guild.GetTextChannel(Server.AllLog);
            await mod.SendMessageAsync(embed: Embed);
        }

        internal async Task ReactionHandlerAsync(Cacheable<IUserMessage, ulong> Cache, SocketReaction Reaction, bool ReactionAdded)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: (Reaction.Channel as SocketGuildChannel).Guild.Id);
            if (Server.RulesChannel == Reaction.Channel.Id)
            {
                var Guild = (Reaction.Channel as IGuildChannel).Guild;
                var User = await Guild.GetUserAsync(Reaction.UserId);
                var Roles = Guild.Roles;
                IRole Role = null;

                if (Reaction.Emote.Name == Extras.Newspaper.Name) Role = Roles.Where(r => r.Name == "News").First();
                if (Reaction.Emote.Name == Extras.Standard.Name) Role = Roles.Where(r => r.Name == "Standard").First();
                if (Reaction.Emote.Name == Extras.Hardcore.Name) Role = Roles.Where(r => r.Name == "Hardcore").First();
                if (Reaction.Emote.Name == Extras.Challenge.Name) Role = Roles.Where(r => r.Name == "Challenge").First();

                if (Role != null)
                    if (ReactionAdded)
                        await User.AddRoleAsync(Role);
                    else
                        await User.RemoveRoleAsync(Role);
            }
            else if (Server.DevChannel == Reaction.Channel.Id)
            {
                if (Reaction.Emote.Name == Extras.Check.Name && (Reaction.UserId == MethodHelper.RunSync(Client.GetApplicationInfoAsync()).Owner.Id))
                {
                    var Guild = (Reaction.Channel as SocketGuildChannel).Guild;
                    if (!(Guild.GetTextChannel(Server.BotChangeChannel) is IMessageChannel BotChangeChannel)) return;
                    var Message = Cache.HasValue ? Cache.Value : await Cache.GetOrDownloadAsync();
                    if(ReactionAdded)
                        await BotChangeChannel.SendMessageAsync(Message.Content);
                    else
                    {
                        var BotChanMessages = await BotChangeChannel.GetMessagesAsync().FlattenAsync();
                        var BotMessage = await BotChangeChannel.GetMessageAsync(BotChanMessages.FirstOrDefault(m => m.Content == Message.Content).Id, CacheMode.AllowDownload) as IUserMessage;
                        await BotMessage.DeleteAsync();
                    }
                }
            }
        }

        internal async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> Cache, ISocketMessageChannel Channel, SocketReaction Reaction)
            => await ReactionHandlerAsync(Cache, Reaction, true);

        internal async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> Cache, ISocketMessageChannel Channel, SocketReaction Reaction)
            => await ReactionHandlerAsync(Cache, Reaction, false);
    }
}
