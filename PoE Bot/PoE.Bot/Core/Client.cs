using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Discord.Commands;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Core
{
    public sealed class Client
    {
        public IUser CurrentUser { get { return this.DiscordClient.CurrentUser; } }
        public DiscordSocketClient _discordClient { get { return this.DiscordClient; } }
        public string Game { get; private set; }
        internal DiscordSocketClient DiscordClient { get; private set; }
        internal JObject ConfigJson { get; private set; }
        private Timer PoEBot { get; set; }
        private string Token { get; set; }
        private readonly CommandService _commands;

        internal Client()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Initializing Discord"));

            var dsc = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 10
            };

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug,
                CaseSensitiveCommands = false,
            });

            this.DiscordClient = new DiscordSocketClient(dsc);
            this.DiscordClient.Log += Client_Log;
            this.DiscordClient.Ready += Client_Ready;
            this.DiscordClient.ReactionAdded += ReactionHandlerAdd;
            this.DiscordClient.ReactionRemoved += ReactionHandlerRemove;
            _commands.Log += Client_Log;

            // Reliability Service, to work with auto reconnects with the Daemon script
            //ReliabilityService rs = new ReliabilityService(this.DiscordClient, Client_Log);

            // modlog events
            this.DiscordClient.MessageDeleted += MessageDeleted;

            var a = typeof(Client).GetTypeInfo().Assembly;
            var n = a.GetName();
            var l = Path.GetDirectoryName(a.Location);

            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Loading config"));
            var sp = Path.Combine(l, "config.json");
            var sjson = File.ReadAllText(sp, PoE_Bot.UTF8);
            var sjo = JObject.Parse(sjson);
            this.ConfigJson = sjo;
            this.Token = (string)sjo["token"];
            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Discord initialized"));
        }

        /// <summary>
        /// Registers a message received handler.
        /// </summary>
        /// <param name="handler">Handler to register.</param>
        public void RegisterMessageHandler(Func<SocketMessage, Task> handler)
        {
            this.DiscordClient.MessageReceived += handler;
        }

        /// <summary>
        /// Sends an embed to a sepcified channeLog.
        /// </summary>
        /// <param name="embed">Embed to send.</param>
        /// <param name="channel">Channel to send the embed to.</param>
        public void SendEmbed(EmbedBuilder embed, ulong channel)
        {
            var ch = null as SocketTextChannel;
            var tg = DateTime.Now;
            while (ch == null && (DateTime.Now - tg).TotalSeconds < 10)
                ch = this.DiscordClient.GetChannel(channel) as SocketTextChannel;
            if (ch == null)
                return;
            this.SendEmbed(embed, ch);
        }

        internal void Initialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Connecting"));
            this.DiscordClient.LoginAsync(TokenType.Bot, this.Token).GetAwaiter().GetResult();
            this.DiscordClient.StartAsync();
        }

        internal void Deinitialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Saving configs"));
            PoE_Bot.PluginManager.UpdateAllConfigs();
            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Disconnecting"));
            this.DiscordClient.StopAsync();
            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Disconnected"));
        }

        /// <summary>
        /// Sends a message to a specified channeLog.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="channel">Channel to send the message to.</param>
        public void SendMessage(string message, ulong channel)
        {
            var ch = null as SocketTextChannel;
            var tg = DateTime.Now;
            while (ch == null && (DateTime.Now - tg).TotalSeconds < 10)
                ch = this.DiscordClient.GetChannel(channel) as SocketTextChannel;
            if (ch == null)
                return;
            this.SendMessage(message, ch);
        }

        public void PurgeChannel(ulong channel)
        {
            var chan = null as ITextChannel;
            var tg = DateTime.Now;
            while (chan == null && (DateTime.Now - tg).TotalSeconds < 10)
                chan = this.DiscordClient.GetChannel(channel) as ITextChannel;
            if (chan == null)
                return;
            var messages = chan.GetMessagesAsync();
            var deleteMessages = new List<IMessage>(100);
            messages.ForEach(m =>
            {
                foreach(var msg in m)
                    deleteMessages.Add(msg);
            });
            this.PurgeChannel(chan, deleteMessages);
        }

        /// <summary>
        /// Sends a message to a specified channel
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="channel">Channel to send the message to.</param>
        internal void SendMessage(string message, SocketTextChannel channel)
        {
            channel.SendMessageAsync(message).GetAwaiter().GetResult();
        }

        internal void SendEmbed(EmbedBuilder embed, SocketTextChannel channel)
        {
            channel.SendMessageAsync("", false, embed.Build()).GetAwaiter().GetResult();
        }

        internal void PurgeChannel(ITextChannel channel, IEnumerable<IMessage> messages)
        {
            channel.DeleteMessagesAsync(messages).GetAwaiter().GetResult();
        }

        internal void WriteConfig()
        {
            var a = PoE_Bot.PluginManager.MainAssembly;
            var n = a.GetName();
            var l = Path.GetDirectoryName(a.Location);
            var sp = Path.Combine(l, "config.json");
            File.WriteAllText(sp, this.ConfigJson.ToString(), PoE_Bot.UTF8);
        }

        private Task Client_Log(LogMessage message)
        {
            Log.W(message);
            Console.ResetColor();
            return Task.CompletedTask;
        }

        private Task Client_Ready()
        {
            this.PoEBot = new Timer(new TimerCallback(PoEBot_Tick), null, 0, 60000);
            return Task.CompletedTask;
        }

        private void ReactionHandler(ISocketMessageChannel channel, SocketReaction reaction, bool ReactionAdded)
        {
            var guild = (channel as IGuildChannel).Guild;
            var userId = reaction.UserId;
            var user = guild.GetUserAsync(userId).GetAwaiter().GetResult();
            var roles = guild.Roles;
            IRole role = null;

            switch (reaction.Emote.Name)
            {
                case "📰":
                    role = roles.Where(x => x.Name == "News").First();
                    break;
                case "\uD83C\uDDF8":
                    role = roles.Where(x => x.Name == "Standard").First();
                    break;
                case "\uD83C\uDDED":
                    role = roles.Where(x => x.Name == "Hardcore").First();
                    break;
                case "\uD83C\uDDE8":
                    role = roles.Where(x => x.Name == "Challenge").First();
                    break;
            }

            if (ReactionAdded)
                user.AddRoleAsync(role).GetAwaiter().GetResult();
            else
                user.RemoveRoleAsync(role).GetAwaiter().GetResult();
        }

        private Task ReactionHandlerAdd(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var gconfs = PoE_Bot.ConfigManager != null ? PoE_Bot.ConfigManager.GetGuildConfigs() : new KeyValuePair<ulong, GuildConfig>[0];
            if (gconfs.Count() > 0)
            {
                foreach (var kvp in gconfs.ToList())
                {
                    if (kvp.Value.RulesChannel != null)
                    {
                        if (kvp.Value.RulesChannel == channel.Id)
                            ReactionHandler(channel, reaction, true);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task ReactionHandlerRemove(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var gconfs = PoE_Bot.ConfigManager != null ? PoE_Bot.ConfigManager.GetGuildConfigs() : new KeyValuePair<ulong, GuildConfig>[0];
            if (gconfs.Count() > 0)
            {
                foreach (var kvp in gconfs.ToList())
                {
                    if (kvp.Value.RulesChannel != null)
                    {
                        if (kvp.Value.RulesChannel == channel.Id)
                            ReactionHandler(channel, reaction, false);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private void PoEBot_Tick(object _)
        {
            var gconfs = PoE_Bot.ConfigManager != null ? PoE_Bot.ConfigManager.GetGuildConfigs() : new KeyValuePair<ulong, GuildConfig>[0];
            string botGame = null;

            if (gconfs.Count() > 0)
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in gconfs.ToList())
                {
                    var gld = PoE_Bot.Client.DiscordClient.GetGuild(kvp.Key) as SocketGuild;
                    var mrl = kvp.Value.MuteRole != null ? gld.GetRole(kvp.Value.MuteRole.Value) : null;
                    if (gld == null)
                        continue;

                    var done = new List<ModAction>();
                    foreach (var ma in kvp.Value.ModActions)
                    {
                        if (ma.Until <= now)
                        {
                            if (ma.ActionType == ModActionType.Mute && mrl != null)
                            {
                                var usr = gld.GetUser(ma.UserId);
                                if (usr == null)
                                    continue;

                                usr.RemoveRoleAsync(mrl).GetAwaiter().GetResult();
                                done.Add(ma);
                            }
                            else if (ma.ActionType == ModActionType.HardBan)
                            {
                                var ban = gld.GetBansAsync().GetAwaiter().GetResult().FirstOrDefault(xban => xban.User.Id == ma.UserId);
                                if (ban == null)
                                    continue;

                                gld.RemoveBanAsync(ma.UserId).GetAwaiter().GetResult();
                                done.Add(ma);
                            }
                        }
                    }

                    foreach (var ma in done)
                        kvp.Value.ModActions.Remove(ma);

                    botGame = kvp.Value.Game;
                    PoE_Bot.ConfigManager.SetGuildConfig(kvp.Key, kvp.Value);
                }
            }

            if (this.CurrentUser.Activity == null)
                this.DiscordClient.SetGameAsync(botGame).GetAwaiter().GetResult();
            else
                if (this.CurrentUser.Activity.Name != botGame)
                    this.DiscordClient.SetGameAsync(botGame).GetAwaiter().GetResult();

            Log.W(new LogMessage(LogSeverity.Info, "Core Client", "Ticked PoE.Bot"));
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var msg = await message.GetOrDownloadAsync();
            var usr = msg.Author as SocketGuildUser;
            var gld = usr.Guild;
            var gid = gld.Id;

            var cfg = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            if (cfg == null || cfg.AllLogChannel == null)
                return;

            var chn = gld.GetTextChannel(cfg.AllLogChannel.Value);
            if (chn == null)
                return;
            
            if(msg.Embeds.Count() == 0)
            {
                var dChn = msg.Channel as SocketTextChannel;
                var embed = this.PrepareEmbed("Message Deleted", "", EmbedType.Error);
                embed.AddField("User", $"```{usr.Username}```")
                    .AddField("Channel", $"```#{dChn.Name}```")
                    .AddField("Message", $"```{msg.Content.Replace("`", "")}```")
                    .WithAuthor(usr)
                    .WithThumbnailUrl((!string.IsNullOrEmpty(usr.GetAvatarUrl()) ? usr.GetAvatarUrl() : usr.GetDefaultAvatarUrl()));

                await chn.SendMessageAsync("", false, embed.Build());
            }
            
        }

        #region Embeds
        private EmbedBuilder PrepareEmbed(EmbedType type)
        {
            var embed = new EmbedBuilder();
            embed.WithCurrentTimestamp();
            switch (type)
            {
                case EmbedType.Info:
                    embed.Color = new Color(0, 127, 255);
                    break;

                case EmbedType.Success:
                    embed.Color = new Color(127, 255, 0);
                    break;

                case EmbedType.Warning:
                    embed.Color = new Color(255, 255, 0);
                    break;

                case EmbedType.Error:
                    embed.Color = new Color(255, 127, 0);
                    break;

                default:
                    embed.Color = new Color(255, 255, 255);
                    break;
            }
            return embed;
        }

        private EmbedBuilder PrepareEmbed(string title, string desc, EmbedType type)
        {
            var embed = this.PrepareEmbed(type);
            embed.Title = title;
            embed.Description = desc;
            embed.WithCurrentTimestamp();
            return embed;
        }

        private enum EmbedType : uint
        {
            Unknown,
            Success,
            Error,
            Warning,
            Info
        }
        #endregion

        #region Unwanted Code atm
        //private async Task DiscordClient_UserJoined(SocketGuildUser arg)
        //{
        //    var usr = arg;
        //    var gld = usr.Guild;
        //    var gid = gld.Id;

        //    var cfg = PoE_Bot.ConfigManager.GetGuildConfig(gid);
        //    if (cfg == null || cfg.ModLogChannel == null)
        //        return;

        //    var chn = gld.GetTextChannel(cfg.ModLogChannel.Value);
        //    if (chn == null)
        //        return;

        //    await chn.SendMessageAsync("", false, this.PrepareEmbed("User joined", usr.Mention, EmbedType.Info));
        //}

        //private async Task DiscordClient_UserLeft(SocketGuildUser arg)
        //{
        //    var usr = arg;
        //    var gld = usr.Guild;
        //    var gid = gld.Id;

        //    var cfg = PoE_Bot.ConfigManager.GetGuildConfig(gid);
        //    if (cfg == null || cfg.ModLogChannel == null)
        //        return;

        //    var chn = gld.GetTextChannel(cfg.ModLogChannel.Value);
        //    if (chn == null)
        //        return;

        //    await chn.SendMessageAsync("", false, this.PrepareEmbed("User left", usr.Mention, EmbedType.Info));
        //}

        //private async Task DiscordClient_UserBanned(SocketUser arg1, SocketGuild arg2)
        //{
        //    // figure out non-conflicting bans
        //    await Task.Delay(1);
        //}

        //private async Task DiscordClient_UserUnbanned(SocketUser arg1, SocketGuild arg2)
        //{
        //    // figure out non-conflicting bans
        //    await Task.Delay(1);
        //}
        #endregion
    }
}
