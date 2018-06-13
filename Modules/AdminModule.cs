namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using Discord.WebSocket;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using PoE.Bot.Addons.Preconditions;

    [Name("Admin Commands"), RequireAdmin, Ratelimit]
    public class AdminModule : BotBase
    {
        [Command("Settings", RunMode = RunMode.Async), Remarks("Displays guilds settings."), Summary("Settings")]
        public Task SettingsAsync()
        {
            var Pages = new[] {
                 $"```ebnf\n- General Information\n\n" +
                $"Prefix             : {Context.Server.Prefix}\n" +
                $"Mod Log Channel    : {StringHelper.ValidateChannel(Context.Guild , Context.Server.ModLog)}\n" +
                $"All Log Channel    : {StringHelper.ValidateChannel(Context.Guild , Context.Server.AllLog)}\n" +
                $"Report Channel     : {StringHelper.ValidateChannel(Context.Guild , Context.Server.RepLog)}\n" +
                $"Rules Channel      : {StringHelper.ValidateChannel(Context.Guild , Context.Server.RulesChannel)}\n" +
                $"Bot Change Channel : {StringHelper.ValidateChannel(Context.Guild , Context.Server.BotChangeChannel)}\n" +
                $"Developer Channel  : {StringHelper.ValidateChannel(Context.Guild , Context.Server.DevChannel)}\n" +
                $"AFK Users          : {Context.Server.AFK.Count}\n" +
                $"```",

                 $"```diff\n- Mod Information\n\n" +
                $"+ Mute Role                  : {StringHelper.ValidateRole(Context.Guild , Context.Server.MuteRole)}\n" +
                $"+ Trade Mute Role            : {StringHelper.ValidateRole(Context.Guild , Context.Server.TradeMuteRole)}\n" +
                $"+ Log Messages               : {(Context.Server.LogDeleted ? "Enabled" : "Disabled")} (Deleted Messages)\n" +
                $"+ Profanity Check            : {(Context.Server.AntiProfanity ? "Enabled" : "Disabled")}\n" +
                $"+ RSS Feed                   : {(Context.Server.RssFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Mixer                      : {(Context.Server.MixerFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Twitch                     : {(Context.Server.TwitchFeed ? "Enabled" : "Disabled")}\n" +
                $"+ Leaderboard                : {(Context.Server.LeaderboardFeed ? "Enabled" : "Disabled")}\n" +
                $"+ RSS Feeds                  : {Context.Server.RssFeeds.Count}\n" +
                $"+ Mixer Streams              : {Context.Server.Streams.Count(s => s.StreamType is StreamType.MIXER)}\n" +
                $"+ Twitch Stream              : {Context.Server.Streams.Count(s => s.StreamType is StreamType.TWITCH)}\n" +
                $"+ Leaderboard Variants       : {Context.Server.Leaderboards.Count}\n" +
                $"+ Max Warnings (Mute)        : {Context.Server.MaxWarningsToMute}\n" +
                $"+ Max Warnings (Perm Mute)   : {Context.Server.MaxWarningsToPermMute}\n" +
                $"```",

                 $"```diff\n+ Server Statistics\n\n" +
                $"- Users Banned          : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.BAN).Count()}\n" +
                $"- Users Mass Banned     : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.BANS).Count()}\n" +
                $"- Users Soft Banned     : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.SOFTBAN).Count()}\n" +
                $"- Users Kicked          : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.KICK).Count()}\n" +
                $"- Users Mass Kicked     : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.KICKS).Count()}\n" +
                $"- Users Warned          : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.WARNING).Count()}\n" +
                $"- Users Muted           : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.MUTE).Count()}\n" +
                $"- Auto Mod Perm Mutes   : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.AUTOMODPERMMUTE).Count()}\n" +
                $"- Auto Mod Mutes        : {Context.Server.UserCases.Where(x => x.CaseType is CaseType.AUTOMODMUTE).Count()}\n" +
                $"- Total Currencies      : {Context.Server.Prices.Count}\n" +
                $"- Total Shop Items      : {Context.Server.Shops.Count}\n" +
                $"- Total Tags            : {Context.Server.Tags.Count}\n" +
                $"- Total Mod Cases       : {Context.Server.UserCases.Count}\n" +
                $"- Blacklisted Users     : {Context.Config.Blacklist.Count}\n" +
                $"```"
            };
            return PagedReplyAsync(Pages, $"{Context.Guild.Name} Settings");
        }

        [Command("Setup", RunMode = RunMode.Async), Remarks("Set ups PoE Bot for your server."), Summary("Setup")]
        public async Task SetupAsync()
        {
            if (Context.Server.IsConfigured is true)
            {
                await ReplyAsync($"{Extras.Cross} {Context.Guild} has already been configured.");
                return;
            }
            var Channels = await Context.Guild.GetTextChannelsAsync();
            var SetupMessage = await ReplyAsync($"Initializing *{Context.Guild}'s* config .... ");

            var ModLogChannel = Channels.FirstOrDefault(x => x.Name is "cases") ?? await Context.Guild.CreateTextChannelAsync("cases");
            var LogChannel = Channels.FirstOrDefault(x => x.Name is "logs") ?? await Context.Guild.CreateTextChannelAsync("logs");
            var ReportChannel = Channels.FirstOrDefault(x => x.Name is "reports") ?? await Context.Guild.CreateTextChannelAsync("reports");
            var RulesChannel = Channels.FirstOrDefault(x => x.Name is "rules") ?? await Context.Guild.CreateTextChannelAsync("rules");
            var StreamChannel = Channels.FirstOrDefault(x => x.Name is "streams") ?? await Context.Guild.CreateTextChannelAsync("streams");
            var RoleSetChannel = Channels.FirstOrDefault(x => x.Name is "role-setup") ?? await Context.Guild.CreateTextChannelAsync("role-setup");

            Context.Server.ModLog = ModLogChannel.Id;
            Context.Server.AllLog = LogChannel.Id;
            Context.Server.RepLog = ReportChannel.Id;
            Context.Server.RulesChannel = RulesChannel.Id;
            Context.Server.RoleSetChannel = RoleSetChannel.Id;

            Context.Server.LogDeleted = true;
            Context.Server.AntiProfanity = true;

            var MuteRole = Context.Guild.Roles.FirstOrDefault(x => x.Name is "Muted") ?? await Context.Guild.CreateRoleAsync("Muted", permissions: new GuildPermissions(sendMessages: false, sendTTSMessages: false, addReactions: false, mentionEveryone: false));
            Context.Server.MuteRole = MuteRole.Id;
            var TradeMuteRole = Context.Guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute") ?? await Context.Guild.CreateRoleAsync("Trade Mute", permissions: new GuildPermissions(sendMessages: false, sendTTSMessages: false, addReactions: false, mentionEveryone: false));
            Context.Server.TradeMuteRole = TradeMuteRole.Id;

            Context.Server.MaxWarningsToPermMute = 6;
            Context.Server.MaxWarningsToMute = 3;

            Context.Server.IsConfigured = true;
            SaveDocument('s');
            await SetupMessage.ModifyAsync(x => x.Content = $"Configuration completed {Extras.OkHand}");
        }

        [Command("Toggle"), Remarks("Sets certain values for current server's config. ToggleTypes: profanity, log, rssfeed, mixerfeed, twitchfeed, leaderboard"), Summary("Toggle <ToggleType>")]
        public Task SetAsync(string ToggleType)
        {
            string State, ToggleName = null;
            switch (ToggleType.ToLower())
            {
                case "profanity":
                    Context.Server.AntiProfanity = !Context.Server.AntiProfanity;
                    State = Context.Server.AntiProfanity ? "enabled" : "disabled";
                    ToggleName = "Anti profanity";
                    break;
                case "log":
                    Context.Server.LogDeleted = !Context.Server.LogDeleted;
                    State = Context.Server.LogDeleted ? "enabled" : "disabled";
                    ToggleName = "Deleted messages logging";
                    break;
                case "rssfeed":
                    Context.Server.RssFeed = !Context.Server.RssFeed;
                    State = Context.Server.RssFeed ? "enabled" : "disabled";
                    ToggleName = "Live RSS Feed";
                    break;
                case "mixerfeed":
                    Context.Server.MixerFeed = !Context.Server.MixerFeed;
                    State = Context.Server.MixerFeed ? "enabled" : "disabled";
                    ToggleName = "Live Mixer Feed";
                    break;
                case "twitchfeed":
                    Context.Server.TwitchFeed = !Context.Server.TwitchFeed;
                    State = Context.Server.TwitchFeed ? "enabled" : "disabled";
                    ToggleName = "Live Twitch Feed";
                    break;
                case "leaderboard":
                    Context.Server.LeaderboardFeed = !Context.Server.LeaderboardFeed;
                    State = Context.Server.LeaderboardFeed ? "enabled" : "disabled";
                    ToggleName = "Live Leaderboard Feed";
                    break;
                default:
                    return ReplyAsync($"{Extras.Cross} Current Toggle Types Are:\n`PROFANITY` Toggles profanity filter\n`LOG` Logs deleted messages\n`RSSFEED` Toggles RSS live feed\n`MIXERFEED` Toggles Mixer live announcements\n"+
               $"`TWITCHFEED` Toggles Twitch live announcements\n`LEADERBOARD` Toggles Leaderboard tracking");
            }
            return ReplyAsync($"`{ToggleName}` has been {State} {Extras.OkHand}", Save: 's');
        }

        [Command("Set"), Remarks("Sets certain values for current server's config. Settings: prefix, modlog, alllog, rptlog, rulechan, botchan, devchan, rolechan, muterole, trademuterole, maxwarnpermmute, maxwarnmute"), Summary("Set <Setting> [Value]")]
        public Task SetAsync(string Setting, [Remainder] string Value = null)
        {
            string SettingName = null;
            switch (Setting.ToLower())
            {
                case "prefix":
                    if (string.IsNullOrWhiteSpace(Value) || Value.Length > 2)
                        return ReplyAsync($"{Extras.Cross} Prefix can't be greater than 1 characters and can't be empty.");
                    Context.Server.Prefix = char.Parse(Value);
                    SettingName = "Guild Prefix";
                    break;
                case "modlog": Context.Server.ModLog = Context.GuildHelper.ParseUlong(Value); SettingName = "Mod Log Channel"; break;
                case "alllog": Context.Server.AllLog = Context.GuildHelper.ParseUlong(Value); SettingName = "All Log Channel";  break;
                case "rptlog": Context.Server.RepLog = Context.GuildHelper.ParseUlong(Value); SettingName = "Report Log Channel";  break;
                case "rulechan": Context.Server.RulesChannel = Context.GuildHelper.ParseUlong(Value); SettingName = "Rule Channel";  break;
                case "botchan": Context.Server.BotChangeChannel = Context.GuildHelper.ParseUlong(Value); SettingName = "Bot Change Channel"; break;
                case "devchan": Context.Server.DevChannel = Context.GuildHelper.ParseUlong(Value); SettingName = "Developer Channel"; break;
                case "rolechan": Context.Server.RoleSetChannel = Context.GuildHelper.ParseUlong(Value); SettingName = "Role Set Channel"; break;
                case "muterole": Context.Server.MuteRole = Context.GuildHelper.ParseUlong(Value); SettingName = "Mute Role";  break;
                case "trademuterole": Context.Server.TradeMuteRole = Context.GuildHelper.ParseUlong(Value); SettingName = "Trade Board Mute Role"; break;
                case "maxwarnpermmute":
                    if (!int.TryParse(Value, out int ParsedK) || ParsedK > 10)
                        return ReplyAsync($"{Extras.Cross} Value provided in incorrect format. Must be an number no greater than 10.");
                    Context.Server.MaxWarningsToPermMute = ParsedK;
                    SettingName = "Max Warnings To Perm Mute";
                    break;
                case "maxwarnmute":
                    if (!int.TryParse(Value, out int ParsedM) || ParsedM > 10)
                        return ReplyAsync($"{Extras.Cross} Value provided in incorrect format. Must be an number no greater than 10.");
                    Context.Server.MaxWarningsToMute = ParsedM;
                    SettingName = "Max Warnings To Mute";
                    break;
                default:
                    return ReplyAsync($"{Extras.Cross} Invalid settings option! Here are the current settings options:\n" +
               $"`PREFIX` Changes server's prefix\n" +
               $"`MODLOG` Changes modlog channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`ALLLOG` Changes log channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`RPTLOG` Changes reports channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`RULECHAN` Changes reports channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`BOTCHAN` Changes bot change channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`DEVCHAN` Changes developer channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`ROLECHAN` Changes the role channel (Mention Channel. Leave empty to set it to null)\n" +
               $"`MUTEROLE` Changes mute role (Mention Role)\n" +
               $"`TRADEMUTEROLE` Changes trade board mute role (Mention Role)\n" +
               $"`PRICEROLE` Changes price checker role (Mention Role)\n" +
               $"`MAXWARNKICK` Changes max number of warnings before Kick (0 = Disabled)\n" +
               $"`MAXWARNMUTE` Changes max number of warnings before Mute (0 = Disabled)\n");
            }
            return ReplyAsync($"`{SettingName}` has been updated {Extras.OkHand}", Save: 's');
        }

        [Command("Rss Add"), Remarks("Add RSS. You will get live feed from specified RSS feeds."), Summary("Rss Add <RSS> <#Channel> [Tag]")]
        public Task RssAddAsync(string RSS, SocketTextChannel Channel, string Tag = null)
        {
            if (Context.Server.RssFeeds.Where(f => f.FeedUri == new Uri(RSS) && f.ChannelId == Channel.Id).Any())
                return ReplyAsync($"{Extras.Cross} `{RSS}` for `{Channel.Name}` already exists.");
            Context.Server.RssFeeds.Add(new RssObject
            {
                FeedUri = new Uri(RSS),
                ChannelId = Channel.Id,
                Tag = Tag
            });
            return ReplyAsync($"`{RSS}` has been added to server's Rss Feed {Extras.OkHand}", Save: 's');
        }

        [Command("Rss Roles"), Remarks("Adds Role(s) to an Rss Feed."), Summary("Rss Roles <RSS> <#Channel> <@Role1> <@Role2> ...")]
        public Task RssRolesAsync(string RSS, SocketTextChannel Channel, params IRole[] Roles)
        {
            if (!Context.Server.RssFeeds.Where(f => f.FeedUri == new Uri(RSS) && f.ChannelId == Channel.Id).Any())
                return ReplyAsync($"{Extras.Cross} `{RSS}` for `{Channel.Name}` doesn't exist.");
            var Rss = Context.Server.RssFeeds.FirstOrDefault(f => f.FeedUri == new Uri(RSS) && f.ChannelId == Channel.Id);
            Context.Server.RssFeeds.Remove(Rss);
            Rss.RoleIds = Roles.Select(r => r.Id).ToList();
            Context.Server.RssFeeds.Add(Rss);
            return ReplyAsync($"{String.Join(", ", Roles.Select(r => r.Name))} have been added to the `{RSS}` feed {Extras.OkHand}", Save: 's');
        }

        [Command("Rss Remove"), Remarks("Remove RSS."), Summary("Rss Remove <RSS> <#Channel>")]
        public Task RssRemoveAsync(string RSS, SocketTextChannel Channel)
        {
            if (!Context.Server.RssFeeds.Where(f => f.FeedUri == new Uri(RSS) && f.ChannelId == Channel.Id).Any())
                return ReplyAsync($"{Extras.Cross} `{RSS}` for `{Channel.Name}` doesn't exist.");
            var Rss = Context.Server.RssFeeds.FirstOrDefault(f => f.FeedUri == new Uri(RSS) && f.ChannelId == Channel.Id);
            Context.Server.RssFeeds.Remove(Rss);
            return ReplyAsync($"`{RSS}` has been removed from server's Rss Feed {Extras.OkHand}", Save: 's');
        }

        [Command("Rss List"), Remarks("Shows all the RSS feeds this server is subbed to."), Summary("Rss List")]
        public Task RssAsync()
            => ReplyAsync(!Context.Server.RssFeeds.Any() ? $"This server isn't subscribed to any feeds {Extras.Cross}" :
                $"**Subbed To Following RSS Feeds**:\n{String.Join("\n", Context.Server.RssFeeds.Select(f => $"Feed: {f.FeedUri} | Channel: {Context.Guild.GetTextChannelAsync(f.ChannelId).GetAwaiter().GetResult().Mention}{(!string.IsNullOrEmpty(f.Tag) ? " | Tag: " + f.Tag : "")}{(f.RoleIds.Any() ? " | Role(s): `" + String.Join(",", Context.Guild.Roles.OrderByDescending(r => r.Position).Where(r => f.RoleIds.Contains(r.Id)).Select(r => r.Name)) : "")}").ToList())}`");

        [Command("SelfRole Add"), Remarks("Adds Role for users to Self Assign"), Summary("SelfRole Add <Role>")]
        public Task SelfRoleAddAsync(IRole Role)
        {
            if (Role == Context.Guild.EveryoneRole)
                return ReplyAsync($"{Extras.Cross} Role cannot be the everyone role.");
            Context.Server.SelfRoles.Add(Role.Id);
            return ReplyAsync($"`{Role}` has been added to Self Roles {Extras.OkHand}", Save: 's');
        }

        [Command("SelfRole Remove"), Remarks("Removes Role for users to Self Assign"), Summary("SelfRole Remove <Role>")]
        public Task SelfRoleRemoveAsync(IRole Role)
        {
            if (!Context.Server.SelfRoles.Contains(Role.Id))
                return ReplyAsync($"{Extras.Cross} Role is not assigned as a Self Role.");
            Context.Server.SelfRoles.Remove(Role.Id);
            return ReplyAsync($"`{Role}` has been removed from Self Roles {Extras.OkHand}", Save: 's');
        }
    }
}
