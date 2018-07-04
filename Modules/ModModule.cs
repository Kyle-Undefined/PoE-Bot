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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TwitchLib.Api;

    [Name("Moderator Commands"), RequireModerator, Ratelimit]
    public class ModModule : BotBase
    {
        [Command("Ban", RunMode = RunMode.Async), Remarks("Bans a user from the server."), Summary("Ban <@user> [reason]"), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (Context.Guild.HierarchyCheck(user))
            {
                await ReplyAsync($"{Extras.Cross} Oops, clumsy me! *`{user}` is higher than I.*");
                return;
            }

            try
            {
                Embed embed = Extras.Embed(Extras.Info)
                    .WithAuthor(Context.User)
                    .WithTitle("Mod Action")
                    .WithDescription($"You were banned in the {Context.Guild.Name} server.")
                    .WithThumbnailUrl(Context.User.GetAvatarUrl())
                    .AddField("Reason", reason)
                    .Build();

                await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: embed);
            }
            catch { }

            await Context.Guild.AddBanAsync(user, 7, reason).ConfigureAwait(false);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Ban, reason);
            await ReplyAsync($"You are remembered only for the mess you leave behind. *`{user}` was banned.* {Extras.Hammer}");
        }

        [Command("MassBan", RunMode = RunMode.Async), Remarks("Bans multiple users at once."), Summary("MassBan <@user1> <@user2> ..."), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(params IGuildUser[] users)
        {
            if (!users.Any())
                return;

            foreach (IGuildUser user in users)
            {
                if (Context.Guild.HierarchyCheck(user))
                    continue;

                await Context.Guild.AddBanAsync(user, 7, "Mass Ban.");
                await GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Bans, "Multiple bans.");
            }
            await ReplyAsync($"You are remembered only for the mess you leave behind. *{string.Join(", ", users.Select(x => $"`{x.Username}`"))} were banned.* {Extras.Hammer}");
        }

        [Command("BanUserID", RunMode = RunMode.Async), Remarks("Bans a user from the server."), Summary("BanUserID <userId> [reason]"), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(ulong UserId, [Remainder] string reason = null)
        {
            await Context.Guild.AddBanAsync(UserId, 7, reason ?? "user ID Ban.");
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, (await Context.Guild.GetUserAsync(UserId) as IUser), Context.User, CaseType.Ban, reason);
            await ReplyAsync($"You are remembered only for the mess you leave behind. *`{UserId}` was banned.* {Extras.Hammer}");
        }

        [Command("Case"), Remarks("Shows information about a specific case, or Deletes a case."), Summary("Case <action> [caseNumber] [user]")]
        public Task CaseAsync(CommandAction action = CommandAction.List, int caseNumber = 0, SocketGuildUser user = null)
        {
            switch (action)
            {
                case CommandAction.Delete:
                    CaseObject caseDelete = Context.Server.UserCases.FirstOrDefault(c => c.Number == caseNumber && c.UserId == user.Id);
                    Context.Server.UserCases.Remove(caseDelete);
                    return ReplyAsync($"It seems there's still glory in the old Empire yet! *Case Number `{caseNumber}` has been removed from `{user}`'s cases.* {Extras.OkHand}", save: DocumentType.Server);

                case CommandAction.List:
                    if (caseNumber is 0 && Context.Server.UserCases.Any())
                        caseNumber = Context.Server.UserCases.LastOrDefault().Number;

                    CaseObject caseObject = Context.Server.UserCases.FirstOrDefault(x => x.Number == caseNumber);
                    return caseObject is null
                        ? ReplyAsync($"{Extras.Cross} Case #{caseNumber} doesn't exist.")
                        : ReplyAsync(embed: Extras.Embed(Extras.Case)
                            .AddField("user", $"{caseObject.Username} ({caseObject.UserId})", true)
                            .AddField("Case Type", caseObject.CaseType, true)
                            .AddField("Moderator", $"{caseObject.Moderator} ({caseObject.ModeratorId})", true)
                            .AddField("reason", caseObject.Reason).Build());

                default:
                    return ReplyAsync($"{Extras.Cross} action is either `Delete` or `List`.");
            }
        }

        [Command("CaseReason"), Remarks("Specifies reason for a user case."), Summary("CaseReason <number> <reason>"), RequirePermission]
        public async Task CaseReasonAsync(int number, [Remainder] string reason)
        {
            CaseObject caseObject = number is -1 ? Context.Server.UserCases.LastOrDefault() : Context.Server.UserCases.FirstOrDefault(x => x.Number == number);
            if (caseObject is null)
            {
                await ReplyAsync(number is -1 ? $"{Extras.Cross} There aren't any user cases." : $"{Extras.Cross} Case #{number} was invalid case number");
                return;
            }

            caseObject.Reason = reason;
            IGuildUser user = await Context.Guild.GetUserAsync(caseObject.UserId);
            IGuildUser mod = await Context.Guild.GetUserAsync(caseObject.ModeratorId);
            if (!(user is null))
                caseObject.Username = $"{user}";

            if (!(mod is null))
                caseObject.Moderator = $"{mod}";

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(Context.Server.ModLog);
            if (!(channel is null) && await channel.GetMessageAsync(caseObject.MessageId) is IUserMessage message)
            {
                var userCases = Context.Server.UserCases.Where(x => x.UserId == user.Id);
                Embed embed = Extras.Embed(Extras.Case)
                    .WithAuthor($"Case Number: {caseObject.Number}")
                    .WithTitle(caseObject.CaseType.ToString())
                    .AddField("user", $"{user.Mention} `{user}` ({user.Id})")
                    .AddField("History",
                        $"Cases: {userCases.Count()}\n" +
                        $"Warnings: {userCases.Count(x => x.CaseType == CaseType.Warning)}\n" +
                        $"Mutes: {userCases.Count(x => x.CaseType == CaseType.Mute)}\n" +
                        $"Auto Mutes: {userCases.Count(x => x.CaseType == CaseType.AutoModMute)}\n")
                    .AddField("reason", caseObject.Reason)
                    .AddField("Moderator", $"{mod}")
                    .WithCurrentTimestamp()
                    .Build();
                await message.ModifyAsync(x => x.Embed = embed);
            }
            await ReplyAsync($"Case #{caseObject.Number} has been updated {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Cases"), Remarks("Lists all of the specified cases for the guild."), Summary("Cases <caseType>")]
        public Task CasesAsync(CaseType caseType)
            => PagedReplyAsync(MethodHelper.Pages(Context.Server.UserCases.Where(c => c.CaseType == caseType).Select(c =>
                $"Case Number: {c.Number}\nDate: {c.CaseDate.ToString("f")}\nUser: {c.Username}\nReason: {c.Reason}\nModerator: {c.Moderator}\n")), $"{Context.Guild.Name} {caseType} Cases");

        [Command("Cases"), Remarks("Lists all of the cases for a specified user in the guild."), Summary("Cases <user>")]
        public Task CasesAsync(SocketGuildUser user)
            => PagedReplyAsync(MethodHelper.Pages(Context.Server.UserCases.Where(c => c.UserId == user.Id).Select(c =>
                $"Case Number: {c.Number}\nDate: {c.CaseDate.ToString("f")}\nType: {c.CaseType}\nReason: {c.Reason}\nModerator: {c.Moderator}\n")), $"{user.Username}'s Cases");

        [Command("GuildInfo"), Remarks("Displays information about guild."), Summary("GuildInfo")]
        public Task GuildInfoAsync()
        {
            SocketGuild guild = Context.Guild as SocketGuild;
            return ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithAuthor($"{Context.Guild.Name}'s Information | {Context.Guild.Id}", Context.Guild.IconUrl)
                .WithFooter($"Created On: {guild.CreatedAt}")
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .AddField("Kitava", guild.Owner, true)
                .AddField("Default channel", guild.DefaultChannel.Name ?? "No Default channel", true)
                .AddField("Message Notifications", guild.DefaultMessageNotifications, true)
                .AddField("Verification Level", guild.VerificationLevel, true)
                .AddField("MFA Level", guild.MfaLevel, true)
                .AddField("Text Channels", guild.TextChannels.Count, true)
                .AddField("Voice Channels", guild.VoiceChannels.Count, true)
                .AddField("Characters", guild.MemberCount, true)
                .AddField("Lieutenants", guild.Users.Count(x => x.IsBot is true), true)
                .AddField("Exiles", guild.Users.Count(x => x.IsBot is false), true)
                .AddField("Classes", string.Join(", ", guild.Roles.OrderByDescending(x => x.Position).Select(x => x.Name))).Build());
        }

        [Command("Kick", RunMode = RunMode.Async), Remarks("Kicks a user out of the server."), Summary("Kick <@user> [reason]"), BotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (Context.Guild.HierarchyCheck(user))
            {
                await ReplyAsync($"{Extras.Cross} Oops, clumsy me! `{user}` is higher than I.");
                return;
            }

            try
            {
                Embed embed = Extras.Embed(Extras.Info)
                    .WithAuthor(Context.User)
                    .WithTitle("Mod Action")
                    .WithDescription($"You were kicked in the {Context.Guild.Name} server.")
                    .WithThumbnailUrl(Context.User.GetAvatarUrl())
                    .AddField("Reason", reason)
                    .Build();

                await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: embed);
            }
            catch { }

            await user.KickAsync(reason);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Kick, reason);
            await ReplyAsync($"Death to sin! *`{user}` was kicked.* {Extras.Hammer}");
        }

        [Command("MassKick", RunMode = RunMode.Async), Remarks("Kicks multiple users at once."), Summary("MassKick <@user1> <@user2> ..."), BotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(params IGuildUser[] users)
        {
            if (!users.Any())
                return;

            foreach (IGuildUser user in users)
            {
                if (Context.Guild.HierarchyCheck(user))
                    continue;

                await user.KickAsync("Multiple kicks.").ConfigureAwait(false);
                await GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Kicks, "Multiple kicks.");
            }
            await ReplyAsync($"Death to sin! *{string.Join(", ", users.Select(x => $"`{x.Username}`"))} were kicked.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("Leaderboard"), Summary("Leaderboard <action> [#channel] [enabled: True, False] [variant]"), Remarks("Adds, Deletes, or Updates a Leaderboard Variant. Lists Leaderboard Variants as well.")]
        public async Task LeaderboardAsync(CommandAction action, SocketTextChannel channel = null, bool enabled = false, [Remainder] string variant = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    variant = variant.Replace(" ", "_");
                    if (Context.Server.Leaderboards.Any(f => f.Variant == variant && f.ChannelId == channel.Id))
                        await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{variant}` is already in the list.*");

                    Context.Server.Leaderboards.Add(new LeaderboardObject
                    {
                        Variant = variant,
                        ChannelId = channel.Id,
                        Enabled = enabled
                    });

                    await ReplyAsync($"Slowness lends strength to one's enemies. *`{variant}` has been added to Leaderboard list.* {Extras.OkHand}", save: DocumentType.Server);
                    break;

                case CommandAction.Delete:
                    variant = variant.Replace(" ", "_");
                    if (!Context.Server.Leaderboards.Any(l => l.Variant == variant && l.ChannelId == channel.Id))
                        await ReplyAsync($"{Extras.Cross} Poor, corrupted creature. *Can't find the Variant: `{variant}`*");

                    LeaderboardObject boardDelete = Context.Server.Leaderboards.FirstOrDefault(l => l.Variant == variant && l.ChannelId == channel.Id);
                    Context.Server.Leaderboards.Remove(boardDelete);
                    await ReplyAsync($"Life is short, deal with it! *Removed `{variant}` from the Leaderboards list.* {Extras.OkHand}", save: DocumentType.Server);
                    break;

                case CommandAction.List:
                    StringBuilder sb = new StringBuilder();
                    if (Context.Server.Leaderboards.Any())
                        foreach (LeaderboardObject leaderboard in Context.Server.Leaderboards)
                            sb.AppendLine($"Variant: {leaderboard.Variant} | Channel: {(await Context.Guild.GetTextChannelAsync(leaderboard.ChannelId)).Mention} | Enabled: {leaderboard.Enabled.ToString()}");

                    await ReplyAsync(!Context.Server.Leaderboards.Any()
                        ? $"{Extras.Cross} Return to Kitava! *Wraeclast doesn't have any leaderboards.*"
                        : $"**Leaderboard Variants**:\n{sb.ToString()}");
                    break;

                case CommandAction.Update:
                    variant = variant.Replace(" ", "_");
                    if (!Context.Server.Leaderboards.Any(f => f.Variant == variant))
                        await ReplyAsync($"{Extras.Cross} Poor, corrupted creature. *Can't find the Variant `{variant}`*");

                    LeaderboardObject boardUpdate = Context.Server.Leaderboards.FirstOrDefault(l => l.Variant == variant);
                    Context.Server.Leaderboards.Remove(boardUpdate);
                    Context.Server.Leaderboards.Add(new LeaderboardObject
                    {
                        Variant = variant,
                        ChannelId = channel.Id,
                        Enabled = enabled
                    });

                    await ReplyAsync($"Slowness lends strength to one's enemies. *Updated Leaderboard Variant: `{variant}`* {Extras.OkHand}", save: DocumentType.Server);
                    break;

                default:
                    await ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete`, `List` or `Update`.");
                    break;
            }
        }

        [Command("Mute", RunMode = RunMode.Async), Remarks("Mutes a user for a given time. Time: Defaults to 5 minutes, can be specified as | Number(d/h/m/s) Example: 10m for 10 Minutes"), Summary("Mute <@user> [time] [reason]"), BotPermission(GuildPermission.ManageRoles)]
        public Task MuteAsync(IGuildUser user, TimeSpan? time, [Remainder] string reason = null)
            => GuildHelper.MuteUserAsync(Context, MuteType.Mod, user, (time.HasValue ? time : TimeSpan.FromMinutes(5)), (!(reason is null) ? reason : "No reason specified."));

        [Command("Mute", RunMode = RunMode.Async), Remarks("Mutes a user for 5 minutes."), Summary("Mute <@user> [reason]"), BotPermission(GuildPermission.ManageRoles)]
        public Task MuteAsync(IGuildUser user, [Remainder] string reason = null)
            => GuildHelper.MuteUserAsync(Context, MuteType.Mod, user, TimeSpan.FromMinutes(5), (!(reason is null) ? reason : "No reason specified."));

        [Command("Profanity"), Summary("Profanity <action> [word]"), Remarks("Adds or Deletes a word for the Profanity List.")]
        public Task ProfanityAsync(CommandAction action, string word = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    Context.Server.ProfanityList.Add(word.ToLower());
                    return ReplyAsync($"Death to sin! *`{word}` has been added to the filter.* {Extras.OkHand}", save: DocumentType.Server);

                case CommandAction.Delete:
                    Context.Server.ProfanityList.Remove(word.ToLower());
                    return ReplyAsync($"I like you better this way! *`{word}` has been removed from the filter.* {Extras.OkHand}", save: DocumentType.Server);

                case CommandAction.List:
                    return ReplyAsync($"`{String.Join("`,`", Context.Server.ProfanityList.Sort())}`");

                default:
                    return ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete` or `List`.");
            }
        }

        [Command("Purge"), Alias("Prune"), Remarks("Deletes Messages, and can specify a user"), Summary("Purge [amount] [@user]"), BotPermission(GuildPermission.ManageMessages)]
        public Task PurgeAsync(int amount = 20, IGuildUser user = null)
        {
            if (user is null)
            {
                (Context.Channel as SocketTextChannel).DeleteMessagesAsync(MethodHelper.RunSync(Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync()))
                .ContinueWith(x => ReplyAndDeleteAsync($"Beauty will grow from your remains. *Deleted `{amount}` messages.* {Extras.OkHand}", TimeSpan.FromSeconds(5)));
                return GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, Context.User, Context.User, CaseType.Purge, $"Purged {amount} Messages in #{Context.Channel.Name}");
            }
            else
            {
                (Context.Channel as SocketTextChannel).DeleteMessagesAsync(MethodHelper.RunSync(Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync()).Where(x => x.Author.Id == user.Id))
                .ContinueWith(x => ReplyAndDeleteAsync($"Beauty will grow from your remains. *Deleted `{amount}` of `{user}`'s messages.* {Extras.OkHand}", TimeSpan.FromSeconds(5)));
                return GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, Context.User, Context.User, CaseType.Purge, $"Purged {amount} of {user}'s Messages #{Context.Channel.Name}");
            }
        }

        [Command("RoleInfo"), Remarks("Displays information about a role."), Summary("RoleInfo <@role>")]
        public Task RoleInfoAsync(IRole role)
            => ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithTitle($"{role.Name} Information")
                .WithFooter($"Created On: {role.CreatedAt}")
                .AddField("ID", role.Id, true)
                .AddField("Rarity", role.Color, true)
                .AddField("Level", role.Position, true)
                .AddField("Shows Separately?", role.IsHoisted ? "Yep" : "Nope", true)
                .AddField("Managed By Discord?", role.IsManaged ? "Yep" : "Nope", true)
                .AddField("Can Mention?", role.IsMentionable ? "Yep" : "Nope", true)
                .AddField("Skills", string.Join(", ", role.Permissions)).Build());

        [Command("Rules Configure", RunMode = RunMode.Async), Remarks("Sets the rules that will be posted in the channel set by the Guild Config."), Summary("Rules Configure")]
        public async Task RulesConfigureAsync()
        {
            RuleObject rules = new RuleObject();

            var description = MethodHelper.CalculateResponse(await WaitAsync("What should the rules description be?", timeout: TimeSpan.FromMinutes(5)));
            if (!description.Item1)
            {
                await ReplyAsync(description.Item2);
                return;
            }

            rules.Description = description.Item2;

            var totalFields = MethodHelper.CalculateResponse(await WaitAsync("How many sections should there be?", timeout: TimeSpan.FromMinutes(1)));
            if (!totalFields.Item1)
            {
                await ReplyAsync(totalFields.Item2);
                return;
            }

            rules.TotalFields = Convert.ToInt32(totalFields.Item2);

            for (int i = 0; i < rules.TotalFields; i++)
            {
                var fieldTitle = MethodHelper.CalculateResponse(await WaitAsync("What should the section be called?", timeout: TimeSpan.FromMinutes(1)));
                if (!fieldTitle.Item1)
                {
                    await ReplyAsync(fieldTitle.Item2);
                    break;
                }

                var fieldContent = MethodHelper.CalculateResponse(await WaitAsync("What should the section contain?  *You can use Discord Markup*", timeout: TimeSpan.FromMinutes(10)));
                if (!fieldContent.Item1)
                {
                    await ReplyAsync(fieldContent.Item2);
                    break;
                }

                rules.Fields.Add(fieldTitle.Item2, fieldContent.Item2);
            }

            Context.Server.RulesConfig = rules;
            SaveDocument(DocumentType.Server);

            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} rules")
                .WithDescription(rules.Description);

            foreach (var field in rules.Fields)
                embed.AddField(field.Key, field.Value, false);

            await ReplyAsync($"*rules have been configured, here's a preview of them.* {Extras.OkHand}", embed: embed.Build());
        }

        [Command("Rules Post"), Summary("Rules Post"), Remarks("Posts the rules you've configured to the rules channel you setup in the Guild Config. Only done once, if you want to edit the rules, use Rules Configure followed by Rules Update.")]
        public async Task RulesPostAsync()
        {
            if (string.IsNullOrEmpty(Context.Server.RulesConfig.Description))
            {
                await ReplyAsync($"{Extras.Cross} *You have no rules to post, please use confrules to set them up.*");
                return;
            }

            if (Context.Server.RulesChannel is 0)
            {
                await ReplyAsync($"{Extras.Cross} *You have not configured a rules channel.*");
                return;
            }

            IGuildChannel chan = await Context.Guild.GetChannelAsync(Context.Server.RulesChannel);
            IMessageChannel ruleChan = chan as IMessageChannel;
            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} rules")
                .WithDescription(Context.Server.RulesConfig.Description);

            foreach (var field in Context.Server.RulesConfig.Fields)
                embed.AddField(field.Key, field.Value, false);

            await ruleChan.SendMessageAsync(embed: embed.Build());
            await ReplyAsync($"*rules have been posted.* {Extras.OkHand}");
        }

        [Command("Rules Update"), Summary("Rules Update"), Remarks("Updates the rules you've configured and posted to the rules channel.")]
        public async Task RulesUpdateAsync()
        {
            if (string.IsNullOrEmpty(Context.Server.RulesConfig.Description))
            {
                await ReplyAsync($"{Extras.Cross} *You have no rules to post, please use confrules to set them up.*");
                return;
            }

            if (Context.Server.RulesChannel is 0)
            {
                await ReplyAsync($"{Extras.Cross} *You have not configured a rules channel.*");
                return;
            }

            IGuildChannel chan = await Context.Guild.GetChannelAsync(Context.Server.RulesChannel);
            IMessageChannel ruleChan = chan as IMessageChannel;
            var msgs = await ruleChan.GetMessagesAsync().FlattenAsync();
            msgs = msgs.Where(x => x.Author.IsBot);

            if (msgs.Count() < 1)
            {
                await ReplyAsync($"{Extras.Cross} *No messages found to edit, please make sure you've posted the rules to the channel.*");
                return;
            }

            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} rules")
                .WithDescription(Context.Server.RulesConfig.Description);

            foreach (var field in Context.Server.RulesConfig.Fields)
                embed.AddField(field.Key, field.Value, false);

            foreach (IUserMessage msg in msgs)
                await msg.ModifyAsync(x => x.Embed = embed.Build());

            await ReplyAsync($"*rules have been edited.* {Extras.OkHand}");
        }

        [Command("Situation"), Summary("Situation <action> <@user1> <@user2> ..."), Remarks("Adds or Delete the Situation Room role to the specified users.")]
        public Task SituationAsync(CommandAction action, params IGuildUser[] users)
        {
            switch (action)
            {
                case CommandAction.Add:
                    foreach (IGuildUser user in users)
                        user.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(r => r.Name is "Situation Room"));
                    return ReplyAndDeleteAsync($"Purity will prevail! *users has been added to Situation Room.* {Extras.OkHand}");

                case CommandAction.Delete:
                    foreach (IGuildUser user in users)
                        user.RemoveRoleAsync(Context.Guild.Roles.FirstOrDefault(r => r.Name is "Situation Room"));
                    return ReplyAndDeleteAsync($"Purity will prevail! *users has been removed from Situation Room.* {Extras.OkHand}");

                default:
                    return ReplyAsync($"{Extras.Cross} action is either `Add` or `Delete`.");
            }
        }

        [Command("SoftBan", RunMode = RunMode.Async), Remarks("Bans a user then unbans them."), Summary("SoftBan <@user> [reason]"), BotPermission(GuildPermission.BanMembers)]
        public Task SoftBanAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (Context.Guild.HierarchyCheck(user))
                return ReplyAsync($"{Extras.Cross} Oops, clumsy me! `{user}` is higher than I.");

            Context.Guild.AddBanAsync(user, 7, reason).ConfigureAwait(false);
            Context.Guild.RemoveBanAsync(user);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Softban, reason);
            return ReplyAsync($"Go to bed, little nightmare! *`{user}` was soft banned.* {Extras.Hammer}");
        }

        [Command("Streamer", RunMode = RunMode.Async), Summary("Streamer <streamType> <userName> [#channel: Defaults to #streams]"), Remarks("Adds or Delete a streamer to the Stream list.")]
        public async Task StreamerAsync(CommandAction action, StreamType streamType = StreamType.Mixer, string userName = null, SocketTextChannel channel = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (Context.Server.Streams.Any(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == channel.Id))
                        await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{userName}` is already on the `{streamType}` list.*");

                    channel = channel ?? Context.Guild.DefaultStreamChannel() as SocketTextChannel;
                    switch (streamType)
                    {
                        case StreamType.Mixer:
                            MixerAPI mixer = new MixerAPI();
                            uint userId = await mixer.GetUserId(userName);
                            uint chanId = await mixer.GetChannelId(userName);
                            if (userId is 0 || chanId is 0)
                                await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *No user/channel found.*");

                            Context.Server.Streams.Add(new StreamObject
                            {
                                Name = userName,
                                ChannelId = channel.Id,
                                MixerUserId = userId,
                                MixerChannelId = chanId,
                                StreamType = streamType
                            });

                            break;

                        case StreamType.Twitch:
                            TwitchAPI twitchAPI = new TwitchAPI();
                            twitchAPI.Settings.ClientId = Context.Config.APIKeys["TC"];
                            twitchAPI.Settings.AccessToken = Context.Config.APIKeys["TA"];

                            var users = await twitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { userName }));
                            if (!users.Users.Any())
                                await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *Twitch user not found.*");

                            var user = users.Users[0];
                            Context.Server.Streams.Add(new StreamObject
                            {
                                Name = userName,
                                TwitchUserId = user.Id,
                                ChannelId = channel.Id,
                                StreamType = streamType
                            });

                            break;
                    }

                    await ReplyAsync($"I'm so good at this, I scare myself. *`{userName}` has been added to the `{streamType}` list.* {Extras.OkHand}", save: DocumentType.Server);
                    break;

                case CommandAction.Delete:
                    if (!Context.Server.Streams.Select(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == (channel ?? Context.Guild.DefaultStreamChannel() as SocketTextChannel).Id).Any())
                        await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{userName}` isn't on the `{streamType}` list.*");

                    StreamObject streamer = Context.Server.Streams.FirstOrDefault(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == (Context.Guild.DefaultStreamChannel() as SocketTextChannel).Id);
                    Context.Server.Streams.Remove(streamer);
                    await ReplyAsync($"Every death brings me life. *`{userName}` has been removed from the `{streamType}` list.* {Extras.OkHand}", save: DocumentType.Server);
                    break;

                case CommandAction.List:
                    await ReplyAsync(!Context.Server.Streams.Any()
                        ? $"{Extras.Cross} Return to Kitava! *Wraeclast doesn't have any streamers.*"
                        : $"**Streamers**:\n{String.Join("\n", Context.Server.Streams.OrderBy(s => s.StreamType).Select(async s => $"`{s.StreamType}`: {s.Name} {(Context.Guild.DefaultStreamChannel().Id == (await Context.Guild.GetTextChannelAsync(s.ChannelId)).Id ? "" : (await Context.Guild.GetTextChannelAsync(s.ChannelId)).Mention)}").ToList())}");
                    break;

                default:
                    await ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete` or `List`.");
                    break;
            }
        }

        [Command("Streamer MultiAdd", RunMode = RunMode.Async), Summary("Streamer MultiAdd <streamType> <userNames>"), Remarks("Adds a list of streamers to the Stream list.")]
        public async Task StreamerMultiAddAsync(StreamType streamType, params string[] userNames)
        {
            SocketTextChannel channel = Context.Guild.DefaultStreamChannel() as SocketTextChannel;

            foreach (string userName in userNames)
            {
                if (Context.Server.Streams.Any(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == channel.Id))
                    await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{userName}` is already on the `{streamType}` list.*");

                switch (streamType)
                {
                    case StreamType.Mixer:
                        MixerAPI mixer = new MixerAPI();
                        uint userId = await mixer.GetUserId(userName);
                        uint chanId = await mixer.GetChannelId(userName);
                        if (userId is 0 || chanId is 0)
                            await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *No user/channel found.*");

                        Context.Server.Streams.Add(new StreamObject
                        {
                            Name = userName,
                            ChannelId = channel.Id,
                            MixerUserId = userId,
                            MixerChannelId = chanId,
                            StreamType = streamType
                        });

                        break;

                    case StreamType.Twitch:
                        TwitchAPI twitchAPI = new TwitchAPI();
                        twitchAPI.Settings.ClientId = Context.Config.APIKeys["TC"];
                        twitchAPI.Settings.AccessToken = Context.Config.APIKeys["TA"];

                        var users = await twitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { userName }));
                        if (!users.Users.Any())
                            await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *Twitch user not found.*");

                        var user = users.Users[0];
                        Context.Server.Streams.Add(new StreamObject
                        {
                            Name = userName,
                            TwitchUserId = user.Id,
                            ChannelId = channel.Id,
                            StreamType = streamType
                        });

                        break;
                }

                await Task.Delay(1000);
            }

            await ReplyAsync($"I'm so good at this, I scare myself. *`{String.Join(", ", userNames)}` has been added to the `{streamType}` list.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Unban"), Summary("Unban <id>"), Remarks("Unbans a user whose Id has been provided."), BotPermission(GuildPermission.BanMembers)]
        public async Task UnbanAsync(ulong id)
        {
            bool check = (await Context.Guild.GetBansAsync()).Any(x => x.User.Id == id);
            if (!check)
            {
                await ReplyAsync($"{Extras.Cross} I have nothing more to give. *No user with `{id}` found.*");
                return;
            }
            await Context.Guild.RemoveBanAsync(id).ContinueWith(x => ReplyAsync($"It seems there's still glory in the old Empire yet! *Unbanned user with `{id}`* {Extras.OkHand}", save: DocumentType.Server));
        }

        [Command("Unmute", RunMode = RunMode.Async), Remarks("Umutes a user."), Summary("Unmute <@user>"), BotPermission(GuildPermission.ManageRoles)]
        public Task UnMuteAsync(IGuildUser user)
            => GuildHelper.UnmuteUserAsync(Context, user);

        [Command("UserInfo"), Remarks("Displays information about a user."), Summary("UserInfo [@user]")]
        public Task UserInfoAsync(SocketGuildUser user = null)
        {
            user = user ?? Context.User as SocketGuildUser;
            return ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithAuthor($"{user.Username} Information | {user.Id}", user.GetAvatarUrl())
                .WithThumbnailUrl(user.GetAvatarUrl())
                .AddField("Muted?", user.IsMuted ? "Yep" : "Nope", true)
                .AddField("Is Lieutenant?", user.IsBot ? "Yep" : "Nope", true)
                .AddField("Creation Date", user.CreatedAt, true)
                .AddField("Join Date", user.JoinedAt, true)
                .AddField("Status", user.Status, true)
                .AddField("Skills", string.Join(", ", user.GuildPermissions.ToList()), true)
                .AddField("Classes", string.Join(", ", (user as SocketGuildUser).Roles.OrderBy(x => x.Position).Select(x => x.Name)), true).Build());
        }

        [Command("Warn", RunMode = RunMode.Async), Remarks("Warns a user with a specified reason."), Summary("Warn <@user> <reason>"), BotPermission(GuildPermission.KickMembers), RequirePermission]
        public Task WarnAysnc(IGuildUser user, [Remainder] string reason)
            => GuildHelper.WarnUserAsync(Context, user, reason);

        [Command("WarnDelete"), Remarks("Deletes a number of users warnings."), Summary("WarnDelete <@user> <amount>"), RequirePermission]
        public Task WarnDeleteAsync(IGuildUser user, int amount = 1)
        {
            ProfileObject profile = GuildHelper.GetProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id);
            if (amount > profile.Warnings)
                return ReplyAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{user}` doesn't have `{amount}` warnings to remove.*");

            profile.Warnings = profile.Warnings - amount;
            GuildHelper.SaveProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id, profile);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *`{amount}` Warnings has been removed for `{user}`* {Extras.OkHand}");
        }

        [Command("WarnReset"), Remarks("Resets users warnings."), Summary("WarnReset <@user>"), RequirePermission]
        public Task WarnResetAsync(IGuildUser user)
        {
            ProfileObject profile = GuildHelper.GetProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id);
            profile.Warnings = 0;
            GuildHelper.SaveProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id, profile);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *Warnings has been reset for `{user}`* {Extras.OkHand}");
        }
    }
}