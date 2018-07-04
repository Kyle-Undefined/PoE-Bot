namespace PoE.Bot.Modules
{
    using Addons;
    using Addons.Preconditions;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using Objects;
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Name("Admin Commands"), RequireAdmin, Ratelimit]
    public class AdminModule : BotBase
    {
        public enum Setting
        {
            AllLog,
            BotChan,
            DevChan,
            MainRole,
            MaxWarnMute,
            ModLog,
            MuteRole,
            Prefix,
            RoleChan,
            RptLog,
            RuleChan,
            TradeMuteRole
        }

        public enum ToggleType
        {
            Leaderboard,
            MessageLog,
            MixerFeed,
            Profanity,
            RssFeed,
            TwitchFeed
        }

        [Command("Rss"), Remarks("Add or Delete an rss feed, also List all feeds."), Summary("Rss <action> [rss] [#channel] [tag]")]
        public async Task RssAsync(CommandAction action, string rss = null, SocketTextChannel channel = null, string tag = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (Context.Server.RssFeeds.Any(f => f.FeedUri == new Uri(rss) && f.ChannelId == channel.Id))
                    {
                        await ReplyAsync($"{Extras.Cross} `{rss}` for `{channel.Name}` already exists.");
                        return;
                    }

                    Context.Server.RssFeeds.Add(new RssObject
                    {
                        FeedUri = new Uri(rss),
                        ChannelId = channel.Id,
                        Tag = tag
                    });

                    await ReplyAsync($"`{rss}` has been added to server's Rss Feed {Extras.OkHand}", save: DocumentType.Server);
                    break;

                case CommandAction.Delete:
                    if (!Context.Server.RssFeeds.Any(f => f.FeedUri == new Uri(rss) && f.ChannelId == channel.Id))
                    {
                        await ReplyAsync($"{Extras.Cross} `{rss}` for `{channel.Name}` doesn't exist.");
                        return;
                    }

                    RssObject rssObject = Context.Server.RssFeeds.FirstOrDefault(f => f.FeedUri == new Uri(rss) && f.ChannelId == channel.Id);
                    Context.Server.RssFeeds.Remove(rssObject);
                    await ReplyAsync($"`{rss}` has been removed from server's Rss Feed {Extras.OkHand}", save: DocumentType.Server);
                    break;

                case CommandAction.List:
                    StringBuilder sb = new StringBuilder();
                    if (Context.Server.RssFeeds.Any())
                        foreach (RssObject feed in Context.Server.RssFeeds)
                            sb.AppendLine($"Feed: {feed.FeedUri} | Channel: {(await Context.Guild.GetTextChannelAsync(feed.ChannelId)).Mention}{(string.IsNullOrEmpty(feed.Tag) ? "" : " | Tag: " + feed.Tag)}{(feed.RoleIds.Any() ? " | Role(s):" + String.Join(",", Context.Guild.Roles.OrderByDescending(r => r.Position).Where(r => feed.RoleIds.Contains(r.Id)).Select(r => r.Name)) : "")}");

                    await ReplyAsync(!Context.Server.RssFeeds.Any()
                        ? $"{Extras.Cross}This server isn't subscribed to any feeds."
                        : $"**Subbed To Following RSS Feeds**:\n{sb.ToString()}");
                    break;

                default:
                    await ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete` or `List`.");
                    break;
            }
        }

        [Command("RssRoles"), Remarks("Add or Delete role(s) for a rss Feed."), Summary("RssRoles <rss> <#channel> <@role1> <@role2> ...")]
        public Task RssRolesAsync(string rss, SocketTextChannel channel, params IRole[] roles)
        {
            if (!Context.Server.RssFeeds.Any(f => f.FeedUri == new Uri(rss) && f.ChannelId == channel.Id))
                return ReplyAsync($"{Extras.Cross} `{rss}` for `{channel.Name}` doesn't exist.");

            RssObject rssObject = Context.Server.RssFeeds.FirstOrDefault(f => f.FeedUri == new Uri(rss) && f.ChannelId == channel.Id);
            Context.Server.RssFeeds.Remove(rssObject);
            rssObject.RoleIds = roles.Select(r => r.Id).ToList();
            Context.Server.RssFeeds.Add(rssObject);

            return ReplyAsync($"{String.Join(", ", roles.Select(r => r.Name))} have been added to the `{rss}` feed {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("SelfRole"), Remarks("Add or Delete a role for users to Self Assign"), Summary("SelfRole <action> <role>")]
        public Task SelfRoleAsync(CommandAction action, IRole role)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (role == Context.Guild.EveryoneRole)
                        return ReplyAsync($"{Extras.Cross} role cannot be the everyone role.");

                    Context.Server.SelfRoles.Add(role.Id);
                    return ReplyAsync($"`{role}` has been added to Self roles {Extras.OkHand}", save: DocumentType.Server);

                case CommandAction.Delete:
                    if (!Context.Server.SelfRoles.Contains(role.Id))
                        return ReplyAsync($"{Extras.Cross} role is not assigned as a Self role.");

                    Context.Server.SelfRoles.Remove(role.Id);
                    return ReplyAsync($"`{role}` has been removed from Self roles {Extras.OkHand}", save: DocumentType.Server);

                default:
                    return ReplyAsync($"{Extras.Cross} action is either `Add` or `Remove`.");
            }
        }

        [Command("Set"), Remarks("Sets certain values for current server's config."), Summary("Set <setting> [value]")]
        public Task SetAsync(Setting setting, [Remainder] string value = null)
        {
            ulong channel = Context.Guild.FindChannel(value);
            ulong role = Context.Guild.FindRole(value);
            switch (setting)
            {
                case Setting.Prefix:
                    if (string.IsNullOrWhiteSpace(value) || value.Length > 2)
                        return ReplyAsync($"{Extras.Cross} Prefix can't be greater than 1 characters and can't be empty.");
                    Context.Server.Prefix = char.Parse(value);
                    break;

                case Setting.ModLog:
                    Context.Server.ModLog = channel;
                    break;

                case Setting.AllLog:
                    Context.Server.AllLog = channel;
                    break;

                case Setting.RptLog:
                    Context.Server.RepLog = channel;
                    break;

                case Setting.RuleChan:
                    Context.Server.RulesChannel = channel;
                    break;

                case Setting.BotChan:
                    Context.Server.BotChangeChannel = channel;
                    break;

                case Setting.DevChan:
                    Context.Server.DevChannel = channel;
                    break;

                case Setting.RoleChan:
                    Context.Server.RoleSetChannel = channel;
                    break;

                case Setting.MainRole:
                    Context.Server.MainRole = role;
                    break;

                case Setting.MuteRole:
                    Context.Server.MuteRole = role;
                    break;

                case Setting.TradeMuteRole:
                    Context.Server.TradeMuteRole = role;
                    break;

                case Setting.MaxWarnMute:
                    if (!int.TryParse(value, out int parsedM) || parsedM > 10)
                        return ReplyAsync($"{Extras.Cross} value provided in incorrect format. Must be an number no greater than 10.");
                    Context.Server.MaxWarningsToMute = parsedM;
                    break;

                default:
                    return ReplyAsync($"{Extras.Cross} Invalid settings option! Here are the current settings options:\n" +
                       "`Prefix` Changes server's prefix\n" +
                       "`ModLog` Changes modlog channel (Mention channel. Leave empty to set it to null)\n" +
                       "`AllLog` Changes log channel (Mention channel. Leave empty to set it to null)\n" +
                       "`RptLog` Changes reports channel (Mention channel. Leave empty to set it to null)\n" +
                       "`RuleChan` Changes reports channel (Mention channel. Leave empty to set it to null)\n" +
                       "`BotChan` Changes bot change channel (Mention channel. Leave empty to set it to null)\n" +
                       "`DevChan` Changes developer channel (Mention channel. Leave empty to set it to null)\n" +
                       "`RoleChan` Changes the role channel (Mention channel. Leave empty to set it to null)\n" +
                       "`MainRole` Changes role users get when they agree to rules (Mention role)\n" +
                       "`MuteRole` Changes mute role (Mention role)\n" +
                       "`TradeMuteRole` Changes trade board mute role (Mention role)\n" +
                       "`MaxWarnMute` Changes max number of warnings before Mute (0 = Disabled)\n");
            }
            return ReplyAsync($"`{setting}` has been updated {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Settings", RunMode = RunMode.Async), Remarks("Displays guilds settings."), Summary("Settings")]
        public Task SettingsAsync()
        {
            string[] pages = new[] {
                "```ebnf\n- General Information\n\n" +
                $"Prefix             : {Context.Server.Prefix}\n" +
                $"Mod Log channel    : {Context.Guild.ValidateChannel(Context.Server.ModLog)}\n" +
                $"All Log channel    : {Context.Guild.ValidateChannel(Context.Server.AllLog)}\n" +
                $"Report channel     : {Context.Guild.ValidateChannel(Context.Server.RepLog)}\n" +
                $"Rules channel      : {Context.Guild.ValidateChannel(Context.Server.RulesChannel)}\n" +
                $"Bot Change channel : {Context.Guild.ValidateChannel(Context.Server.BotChangeChannel)}\n" +
                $"Developer channel  : {Context.Guild.ValidateChannel(Context.Server.DevChannel)}\n" +
                $"Role Set channel   : {Context.Guild.ValidateChannel(Context.Server.RoleSetChannel)}\n" +
                $"AFK Users          : {Context.Server.AFK.Count}\n" +
                "```",

                "```diff\n- Mod Information\n\n" +
                $"+ Main role                  : {Context.Guild.ValidateRole(Context.Server.MainRole)}\n" +
                $"+ Mute role                  : {Context.Guild.ValidateRole(Context.Server.MuteRole)}\n" +
                $"+ Trade Mute role            : {Context.Guild.ValidateRole(Context.Server.TradeMuteRole)}\n" +
                $"+ Log Messages               : {(Context.Server.LogDeleted ? "Enabled" : "Disabled")} (Deleted Messages)\n" +
                $"+ Profanity Check            : {(Context.Server.AntiProfanity ? "Enabled" : "Disabled")}\n" +
                $"+ Rss Feed                   : {(Context.Server.RssFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Mixer                      : {(Context.Server.MixerFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Twitch                     : {(Context.Server.TwitchFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Leaderboard                : {(Context.Server.LeaderboardFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Rss Feeds                  : {Context.Server.RssFeeds.Count}\n" +
                $"+ Mixer Streams              : {Context.Server.Streams.Count(s => s.StreamType is StreamType.Mixer)}\n" +
                $"+ Twitch Stream              : {Context.Server.Streams.Count(s => s.StreamType is StreamType.Twitch)}\n" +
                $"+ Leaderboard Variants       : {Context.Server.Leaderboards.Count}\n" +
                $"+ Max Warnings (Mute)        : {Context.Server.MaxWarningsToMute}\n" +
                "```",

                "```diff\n+ Server Statistics\n\n" +
                $"- Users Banned          : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Ban)}\n" +
                $"- Users Mass Banned     : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Bans)}\n" +
                $"- Users Soft Banned     : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Softban)}\n" +
                $"- Users Kicked          : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Kick)}\n" +
                $"- Users Mass Kicked     : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Kicks)}\n" +
                $"- Users Warned          : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Warning)}\n" +
                $"- Users Muted           : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.Mute)}\n" +
                $"- Auto Mod Mutes        : {Context.Server.UserCases.Count(x => x.CaseType is CaseType.AutoModMute)}\n" +
                $"- Total Currencies      : {Context.Server.Prices.Count}\n" +
                $"- Total Shop Items      : {Context.Server.Shops.Count}\n" +
                $"- Total Tags            : {Context.Server.Tags.Count}\n" +
                $"- Total Mod Cases       : {Context.Server.UserCases.Count}\n" +
                $"- Blacklisted Users     : {Context.Config.Blacklist.Count}\n" +
                "```"
            };
            return PagedReplyAsync(pages, $"{Context.Guild.Name} Settings");
        }

        [Command("Setup", RunMode = RunMode.Async), Remarks("Set ups PoE Bot for your server."), Summary("Setup")]
        public async Task SetupAsync()
        {
            if (Context.Server.IsConfigured)
            {
                await ReplyAsync($"{Extras.Cross} {Context.Guild} has already been configured.");
                return;
            }
            var channels = await Context.Guild.GetTextChannelsAsync();
            IUserMessage setupMessage = await ReplyAsync($"Initializing *{Context.Guild}'s* config .... ");

            ITextChannel modLogChannel = channels.FirstOrDefault(x => x.Name is "cases") ?? await Context.Guild.CreateTextChannelAsync("cases");
            ITextChannel logChannel = channels.FirstOrDefault(x => x.Name is "logs") ?? await Context.Guild.CreateTextChannelAsync("logs");
            ITextChannel reportChannel = channels.FirstOrDefault(x => x.Name is "reports") ?? await Context.Guild.CreateTextChannelAsync("reports");
            ITextChannel rulesChannel = channels.FirstOrDefault(x => x.Name is "rules") ?? await Context.Guild.CreateTextChannelAsync("rules");
            ITextChannel streamChannel = channels.FirstOrDefault(x => x.Name is "streams") ?? await Context.Guild.CreateTextChannelAsync("streams");
            ITextChannel roleSetChannel = channels.FirstOrDefault(x => x.Name is "role-setup") ?? await Context.Guild.CreateTextChannelAsync("role-setup");

            Context.Server.ModLog = modLogChannel.Id;
            Context.Server.AllLog = logChannel.Id;
            Context.Server.RepLog = reportChannel.Id;
            Context.Server.RulesChannel = rulesChannel.Id;
            Context.Server.RoleSetChannel = roleSetChannel.Id;

            Context.Server.LogDeleted = true;
            Context.Server.AntiProfanity = true;

            IRole mainRole = Context.Guild.Roles.FirstOrDefault(x => x.Name is "Exile") ??
                await Context.Guild.CreateRoleAsync("Exile", permissions: new GuildPermissions(sendMessages: true, readMessageHistory: true, viewChannel: true, mentionEveryone: false));
            Context.Server.MainRole = mainRole.Id;

            IRole muteRole = Context.Guild.Roles.FirstOrDefault(x => x.Name is "Muted") ??
                await Context.Guild.CreateRoleAsync("Muted", permissions: new GuildPermissions(sendMessages: false, sendTTSMessages: false, addReactions: false, mentionEveryone: false));
            Context.Server.MuteRole = muteRole.Id;

            IRole tradeMuteRole = Context.Guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute") ??
                await Context.Guild.CreateRoleAsync("Trade Mute", permissions: new GuildPermissions(sendMessages: false, sendTTSMessages: false, addReactions: false, mentionEveryone: false));
            Context.Server.TradeMuteRole = tradeMuteRole.Id;

            Context.Server.MaxWarningsToMute = 3;

            Context.Server.IsConfigured = true;
            SaveDocument(DocumentType.Server);
            await setupMessage.ModifyAsync(x => x.Content = $"Configuration completed {Extras.OkHand}");
        }

        [Command("Toggle"), Remarks("Sets certain values for current server's config."), Summary("Toggle <toggleType>")]
        public Task ToggleAsync(ToggleType toggleType)
        {
            string state;
            switch (toggleType)
            {
                case ToggleType.Profanity:
                    Context.Server.AntiProfanity = !Context.Server.AntiProfanity;
                    state = Context.Server.AntiProfanity ? "enabled" : "disabled";
                    break;

                case ToggleType.MessageLog:
                    Context.Server.LogDeleted = !Context.Server.LogDeleted;
                    state = Context.Server.LogDeleted ? "enabled" : "disabled";
                    break;

                case ToggleType.RssFeed:
                    Context.Server.RssFeed = !Context.Server.RssFeed;
                    state = Context.Server.RssFeed ? "enabled" : "disabled";
                    break;

                case ToggleType.MixerFeed:
                    Context.Server.MixerFeed = !Context.Server.MixerFeed;
                    state = Context.Server.MixerFeed ? "enabled" : "disabled";
                    break;

                case ToggleType.TwitchFeed:
                    Context.Server.TwitchFeed = !Context.Server.TwitchFeed;
                    state = Context.Server.TwitchFeed ? "enabled" : "disabled";
                    break;

                case ToggleType.Leaderboard:
                    Context.Server.LeaderboardFeed = !Context.Server.LeaderboardFeed;
                    state = Context.Server.LeaderboardFeed ? "enabled" : "disabled";
                    break;

                default:
                    return ReplyAsync($"{Extras.Cross} Current Toggle Types Are:\n`{String.Join("`,`", Enum.GetNames(typeof(ToggleType)).ToList())}`");
            }
            return ReplyAsync($"`{toggleType}` has been {state} {Extras.OkHand}", save: DocumentType.Server);
        }
    }
}