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
        [Command("Ban", RunMode = RunMode.Async), Summary("Bans a user from the server."), Remarks("Ban <@user> [reason]"), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (Context.Guild.HierarchyCheck(user))
            {
                await ReplyAsync($"{Extras.Cross} Oops, clumsy me! *`{user}` is higher than I.*").ConfigureAwait(false);
                return;
            }

            try
            {
                Embed embed = Extras.Embed(Extras.Info)
                    .WithAuthor(Context.User)
                    .WithTitle("Mod Action")
                    .WithDescription($"You were banned in the {Context.Guild.Name} server.")
                    .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                    .AddField("Reason", reason)
                    .Build();

                await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch { }

            await Context.Guild.AddBanAsync(user, 7, reason).ConfigureAwait(false);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Ban, reason).ConfigureAwait(false);
            await ReplyAsync($"You are remembered only for the mess you leave behind. *`{user}` was banned.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("MassBan", RunMode = RunMode.Async), Summary("Bans multiple users at once."), Remarks("MassBan <@user1> <@user2> ..."), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(params IGuildUser[] users)
        {
            if (!users.Any())
                return;

            foreach (IGuildUser user in users)
            {
                if (Context.Guild.HierarchyCheck(user))
                    continue;

                await Context.Guild.AddBanAsync(user, 7, "Mass Ban.").ConfigureAwait(false);
                await GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Bans, "Multiple bans.").ConfigureAwait(false);
            }
            await ReplyAsync($"You are remembered only for the mess you leave behind. *{string.Join(", ", users.Select(x => $"`{x.Nickname ?? x.Username}`"))} were banned.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("BanUserID", RunMode = RunMode.Async), Summary("Bans a user from the server."), Remarks("BanUserID <userId> [reason]"), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(ulong UserId, [Remainder] string reason = null)
        {
            await Context.Guild.AddBanAsync(UserId, 7, reason ?? "user ID Ban.").ConfigureAwait(false);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, (await Context.Guild.GetUserAsync(UserId) as IUser), Context.User, CaseType.Ban, reason).ConfigureAwait(false);
            await ReplyAsync($"You are remembered only for the mess you leave behind. *`{UserId}` was banned.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("Case"), Summary("Shows information about a specific case, or Deletes a case."), Remarks("Case <action> [caseNumber] [user]")]
        public Task CaseAsync(CommandAction action = CommandAction.List, int caseNumber = 0, IGuildUser user = null)
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
                            .AddField("User", $"{caseObject.Username} ({caseObject.UserId})", true)
                            .AddField("Case Type", caseObject.CaseType, true)
                            .AddField("Moderator", $"{caseObject.Moderator} ({caseObject.ModeratorId})", true)
                            .AddField("Reason", caseObject.Reason).Build());

                default:
                    return ReplyAsync($"{Extras.Cross} action is either `Delete` or `List`.");
            }
        }

        [Command("CaseReason"), Summary("Specifies reason for a user case."), Remarks("CaseReason <number> <reason>"), RequirePermission]
        public async Task CaseReasonAsync(int number, [Remainder] string reason)
        {
            CaseObject caseObject = number is -1 ? Context.Server.UserCases.LastOrDefault() : Context.Server.UserCases.FirstOrDefault(x => x.Number == number);
            if (caseObject is null)
            {
                await ReplyAsync(number is -1 ? $"{Extras.Cross} There aren't any user cases." : $"{Extras.Cross} Case #{number} was invalid case number").ConfigureAwait(false);
                return;
            }

            caseObject.Reason = reason;
            IGuildUser user = await Context.Guild.GetUserAsync(caseObject.UserId).ConfigureAwait(false);
            IGuildUser mod = await Context.Guild.GetUserAsync(caseObject.ModeratorId).ConfigureAwait(false);
            if (!(user is null))
                caseObject.Username = $"{user}";

            if (!(mod is null))
                caseObject.Moderator = $"{mod}";

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(Context.Server.ModLog).ConfigureAwait(false);
            if (!(channel is null) && await channel.GetMessageAsync(caseObject.MessageId).ConfigureAwait(false) is IUserMessage message)
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
                await message.ModifyAsync(x => x.Embed = embed).ConfigureAwait(false);
            }
            await ReplyAsync($"Case #{caseObject.Number} has been updated {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
        }

        [Command("Cases"), Summary("Lists all of the specified cases for the guild."), Remarks("Cases <caseType>")]
        public Task CasesAsync(CaseType caseType)
            => PagedReplyAsync(MethodHelper.Pages(Context.Server.UserCases.Where(c => c.CaseType == caseType).Select(c =>
                $"Case Number: {c.Number}\nDate: {c.CaseDate.ToString("f")}\nUser: {c.Username}\nReason: {c.Reason}\nModerator: {c.Moderator}\n")), $"{Context.Guild.Name} {caseType} Cases");

        [Command("Cases"), Summary("Lists all of the cases for a specified user in the guild."), Remarks("Cases <user>")]
        public Task CasesAsync(IGuildUser user)
            => PagedReplyAsync(MethodHelper.Pages(Context.Server.UserCases.Where(c => c.UserId == user.Id).Select(c =>
                $"Case Number: {c.Number}\nDate: {c.CaseDate.ToString("f")}\nType: {c.CaseType}\nReason: {c.Reason}\nModerator: {c.Moderator}\n")), $"{user.Username}'s Cases");

        [Command("GuildInfo"), Summary("Displays information about guild."), Remarks("GuildInfo")]
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

        [Command("Kick", RunMode = RunMode.Async), Summary("Kicks a user out of the server."), Remarks("Kick <@user> [reason]"), BotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (Context.Guild.HierarchyCheck(user))
            {
                await ReplyAsync($"{Extras.Cross} Oops, clumsy me! `{user}` is higher than I.").ConfigureAwait(false);
                return;
            }

            try
            {
                Embed embed = Extras.Embed(Extras.Info)
                    .WithAuthor(Context.User)
                    .WithTitle("Mod Action")
                    .WithDescription($"You were kicked in the {Context.Guild.Name} server.")
                    .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                    .AddField("Reason", reason)
                    .Build();

                await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch { }

            await user.KickAsync(reason).ConfigureAwait(false);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Kick, reason).ConfigureAwait(false);
            await ReplyAsync($"Death to sin! *`{user}` was kicked.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("MassKick", RunMode = RunMode.Async), Summary("Kicks multiple users at once."), Remarks("MassKick <@user1> <@user2> ..."), BotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(params IGuildUser[] users)
        {
            if (!users.Any())
                return;

            foreach (IGuildUser user in users)
            {
                if (Context.Guild.HierarchyCheck(user))
                    continue;

                await user.KickAsync("Multiple kicks.").ConfigureAwait(false);
                await GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Kicks, "Multiple kicks.").ConfigureAwait(false);
            }
            await ReplyAsync($"Death to sin! *{string.Join(", ", users.Select(x => $"`{x.Nickname ?? x.Username}`"))} were kicked.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("Leaderboard"), Remarks("Leaderboard <action> [#channel] [enabled: True, False] [variant]"), Summary("Adds, Deletes, or Updates a Leaderboard Variant. Lists Leaderboard Variants as well.")]
        public async Task LeaderboardAsync(CommandAction action, IGuildChannel channel = null, bool enabled = false, [Remainder] string variant = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    variant = variant.Replace(" ", "_");
                    if (Context.Server.Leaderboards.Any(f => f.Variant == variant && f.ChannelId == channel.Id))
                        await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{variant}` is already in the list.*").ConfigureAwait(false);

                    Context.Server.Leaderboards.Add(new LeaderboardObject
                    {
                        Variant = variant,
                        ChannelId = channel.Id,
                        Enabled = enabled
                    });

                    await ReplyAsync($"Slowness lends strength to one's enemies. *`{variant}` has been added to Leaderboard list.* {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
                    break;

                case CommandAction.Delete:
                    variant = variant.Replace(" ", "_");
                    if (!Context.Server.Leaderboards.Any(l => l.Variant == variant && l.ChannelId == channel.Id))
                        await ReplyAsync($"{Extras.Cross} Poor, corrupted creature. *Can't find the Variant: `{variant}`*").ConfigureAwait(false);

                    LeaderboardObject boardDelete = Context.Server.Leaderboards.FirstOrDefault(l => l.Variant == variant && l.ChannelId == channel.Id);
                    Context.Server.Leaderboards.Remove(boardDelete);
                    await ReplyAsync($"Life is short, deal with it! *Removed `{variant}` from the Leaderboards list.* {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
                    break;

                case CommandAction.List:
                    StringBuilder sb = new StringBuilder();
                    if (Context.Server.Leaderboards.Any())
                        foreach (LeaderboardObject leaderboard in Context.Server.Leaderboards)
                            sb.AppendLine($"Variant: {leaderboard.Variant} | Channel: {(await Context.Guild.GetTextChannelAsync(leaderboard.ChannelId).ConfigureAwait(false)).Mention} | Enabled: {leaderboard.Enabled.ToString()}");

                    await ReplyAsync(!Context.Server.Leaderboards.Any()
                        ? $"{Extras.Cross} Return to Kitava! *Wraeclast doesn't have any leaderboards.*"
                        : $"**Leaderboard Variants**:\n{sb.ToString()}").ConfigureAwait(false);
                    break;

                case CommandAction.Update:
                    variant = variant.Replace(" ", "_");
                    if (!Context.Server.Leaderboards.Any(f => f.Variant == variant))
                        await ReplyAsync($"{Extras.Cross} Poor, corrupted creature. *Can't find the Variant `{variant}`*").ConfigureAwait(false);

                    LeaderboardObject boardUpdate = Context.Server.Leaderboards.FirstOrDefault(l => l.Variant == variant);
                    Context.Server.Leaderboards.Remove(boardUpdate);
                    Context.Server.Leaderboards.Add(new LeaderboardObject
                    {
                        Variant = variant,
                        ChannelId = channel.Id,
                        Enabled = enabled
                    });

                    await ReplyAsync($"Slowness lends strength to one's enemies. *Updated Leaderboard Variant: `{variant}`* {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
                    break;

                default:
                    await ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete`, `List` or `Update`.").ConfigureAwait(false);
                    break;
            }
        }

        [Command("Mute", RunMode = RunMode.Async), Summary("Mutes a user for a given time. Time: Defaults to 5 minutes, can be specified as | Number(d/h/m/s) Example: 10m for 10 Minutes"), Remarks("Mute <@user> [time] [reason]"), BotPermission(GuildPermission.ManageRoles)]
        public Task MuteAsync(IGuildUser user, TimeSpan? time, [Remainder] string reason = null)
            => GuildHelper.MuteUserAsync(Context, MuteType.Mod, user, (time.HasValue ? time : TimeSpan.FromMinutes(5)), (!(reason is null) ? reason : "No reason specified."));

        [Command("Mute", RunMode = RunMode.Async), Summary("Mutes a user for 5 minutes."), Remarks("Mute <@user> [reason]"), BotPermission(GuildPermission.ManageRoles)]
        public Task MuteAsync(IGuildUser user, [Remainder] string reason = null)
            => GuildHelper.MuteUserAsync(Context, MuteType.Mod, user, TimeSpan.FromMinutes(5), (!(reason is null) ? reason : "No reason specified."));

        [Command("Profanity"), Remarks("Profanity <action> [word]"), Summary("Adds or Deletes a word for the Profanity List.")]
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

        [Command("Purge"), Alias("Prune"), Summary("Deletes Messages, and can specify a user"), Remarks("Purge [amount] [@user]"), BotPermission(GuildPermission.ManageMessages)]
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

        [Command("RoleInfo"), Summary("Displays information about a role."), Remarks("RoleInfo <@role>")]
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

        [Command("Rules Configure", RunMode = RunMode.Async), Summary("Sets the rules that will be posted in the channel set by the Guild Config."), Remarks("Rules Configure")]
        public async Task RulesConfigureAsync()
        {
            RuleObject rules = new RuleObject();

            var description = MethodHelper.CalculateResponse(await WaitAsync("What should the rules description be?", timeout: TimeSpan.FromMinutes(5)).ConfigureAwait(false));
            if (!description.Item1)
            {
                await ReplyAsync(description.Item2).ConfigureAwait(false);
                return;
            }

            rules.Description = description.Item2;

            var totalFields = MethodHelper.CalculateResponse(await WaitAsync("How many sections should there be?", timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false));
            if (!totalFields.Item1)
            {
                await ReplyAsync(totalFields.Item2).ConfigureAwait(false);
                return;
            }

            rules.TotalFields = Convert.ToInt32(totalFields.Item2);

            for (int i = 0; i < rules.TotalFields; i++)
            {
                var fieldTitle = MethodHelper.CalculateResponse(await WaitAsync("What should the section be called?", timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false));
                if (!fieldTitle.Item1)
                {
                    await ReplyAsync(fieldTitle.Item2).ConfigureAwait(false);
                    break;
                }

                var fieldContent = MethodHelper.CalculateResponse(await WaitAsync("What should the section contain?  *You can use Discord Markup*", timeout: TimeSpan.FromMinutes(10)).ConfigureAwait(false));
                if (!fieldContent.Item1)
                {
                    await ReplyAsync(fieldContent.Item2).ConfigureAwait(false);
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

            await ReplyAsync($"*Rules have been configured, here's a preview of them.* {Extras.OkHand}", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("Rules Post"), Remarks("Rules Post"), Summary("Posts the rules you've configured to the rules channel you setup in the Guild Config. Only done once, if you want to edit the rules, use Rules Configure followed by Rules Update.")]
        public async Task RulesPostAsync()
        {
            if (string.IsNullOrEmpty(Context.Server.RulesConfig.Description))
            {
                await ReplyAsync($"{Extras.Cross} *You have no rules to post, please use confrules to set them up.*").ConfigureAwait(false);
                return;
            }

            if (Context.Server.RulesChannel is 0)
            {
                await ReplyAsync($"{Extras.Cross} *You have not configured a rules channel.*").ConfigureAwait(false);
                return;
            }

            IGuildChannel chan = await Context.Guild.GetChannelAsync(Context.Server.RulesChannel).ConfigureAwait(false);
            IMessageChannel ruleChan = chan as IMessageChannel;
            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} rules")
                .WithDescription(Context.Server.RulesConfig.Description);

            foreach (var field in Context.Server.RulesConfig.Fields)
                embed.AddField(field.Key, field.Value, false);

            await ruleChan.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
            await ReplyAsync($"*Rules have been posted.* {Extras.OkHand}").ConfigureAwait(false);
        }

        [Command("Rules Update"), Remarks("Rules Update"), Summary("Updates the rules you've configured and posted to the rules channel.")]
        public async Task RulesUpdateAsync()
        {
            if (string.IsNullOrEmpty(Context.Server.RulesConfig.Description))
            {
                await ReplyAsync($"{Extras.Cross} *You have no rules to post, please use confrules to set them up.*").ConfigureAwait(false);
                return;
            }

            if (Context.Server.RulesChannel is 0)
            {
                await ReplyAsync($"{Extras.Cross} *You have not configured a rules channel.*").ConfigureAwait(false);
                return;
            }

            IGuildChannel chan = await Context.Guild.GetChannelAsync(Context.Server.RulesChannel).ConfigureAwait(false);
            IMessageChannel ruleChan = chan as IMessageChannel;
            var msgs = await ruleChan.GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
            msgs = msgs.Where(x => x.Author.IsBot);

            if (msgs.Count() < 1)
            {
                await ReplyAsync($"{Extras.Cross} *No messages found to edit, please make sure you've posted the rules to the channel.*").ConfigureAwait(false);
                return;
            }

            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} rules")
                .WithDescription(Context.Server.RulesConfig.Description);

            foreach (var field in Context.Server.RulesConfig.Fields)
                embed.AddField(field.Key, field.Value, false);

            foreach (IUserMessage msg in msgs)
                await msg.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);

            await ReplyAsync($"*Rules have been edited.* {Extras.OkHand}").ConfigureAwait(false);
        }

        [Command("Situation"), Remarks("Situation <action> <@user1> <@user2> ..."), Summary("Adds or Delete the Situation Room role to the specified users.")]
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

        [Command("SoftBan", RunMode = RunMode.Async), Summary("Bans a user then unbans them."), Remarks("SoftBan <@user> [reason]"), BotPermission(GuildPermission.BanMembers)]
        public Task SoftBanAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (Context.Guild.HierarchyCheck(user))
                return ReplyAsync($"{Extras.Cross} Oops, clumsy me! `{user}` is higher than I.");

            Context.Guild.AddBanAsync(user, 7, reason).ConfigureAwait(false);
            Context.Guild.RemoveBanAsync(user).ConfigureAwait(false);
            _ = GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, user, Context.User, CaseType.Softban, reason).ConfigureAwait(false);
            return ReplyAsync($"Go to bed, little nightmare! *`{user}` was soft banned.* {Extras.Hammer}");
        }

        [Command("Streamer", RunMode = RunMode.Async), Remarks("Streamer <streamType> <userName> [#channel: Defaults to #streams]"), Summary("Adds or Delete a streamer to the Stream list.")]
        public async Task StreamerAsync(CommandAction action, StreamType streamType = StreamType.Mixer, string userName = null, IGuildChannel channel = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (Context.Server.Streams.Any(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == channel.Id))
                        await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{userName}` is already on the `{streamType}` list.*").ConfigureAwait(false);

                    channel = channel ?? Context.Guild.DefaultStreamChannel() as IGuildChannel;
                    switch (streamType)
                    {
                        case StreamType.Mixer:
                            MixerAPI mixer = new MixerAPI(Context.HttpClient);
                            uint userId = await mixer.GetUserId(userName).ConfigureAwait(false);
                            uint chanId = await mixer.GetChannelId(userName).ConfigureAwait(false);
                            if (userId is 0 || chanId is 0)
                                await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *No user/channel found.*").ConfigureAwait(false);

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

                            var users = await twitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { userName })).ConfigureAwait(false);
                            if (!users.Users.Any())
                                await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *Twitch user not found.*").ConfigureAwait(false);

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

                    await ReplyAsync($"I'm so good at this, I scare myself. *`{userName}` has been added to the `{streamType}` list.* {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
                    break;

                case CommandAction.Delete:
                    if (!Context.Server.Streams.Select(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == (channel ?? Context.Guild.DefaultStreamChannel() as IGuildChannel).Id).Any())
                        await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{userName}` isn't on the `{streamType}` list.*").ConfigureAwait(false);

                    StreamObject streamer = Context.Server.Streams.FirstOrDefault(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == (channel ?? Context.Guild.DefaultStreamChannel() as IGuildChannel).Id);
                    Context.Server.Streams.Remove(streamer);
                    await ReplyAsync($"Every death brings me life. *`{userName}` has been removed from the `{streamType}` list.* {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
                    break;

                case CommandAction.List:
                    await ReplyAsync(!Context.Server.Streams.Any()
                        ? $"{Extras.Cross} Return to Kitava! *Wraeclast doesn't have any streamers.*"
                        : $"**Streamers**:\n{String.Join("\n", Context.Server.Streams.OrderBy(s => s.StreamType).Select(async s => $"`{s.StreamType}`: {s.Name} {(Context.Guild.DefaultStreamChannel().Id == (await Context.Guild.GetTextChannelAsync(s.ChannelId).ConfigureAwait(false)).Id ? "" : (await Context.Guild.GetTextChannelAsync(s.ChannelId).ConfigureAwait(false)).Mention)}").ToList())}").ConfigureAwait(false);
                    break;

                default:
                    await ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete` or `List`.").ConfigureAwait(false);
                    break;
            }
        }

        [Command("Streamer MultiAdd", RunMode = RunMode.Async), Remarks("Streamer MultiAdd <streamType> <userNames>"), Summary("Adds a list of streamers to the Stream list.")]
        public async Task StreamerMultiAddAsync(StreamType streamType, params string[] userNames)
        {
            IGuildChannel channel = Context.Guild.DefaultStreamChannel() as IGuildChannel;

            foreach (string userName in userNames)
            {
                if (Context.Server.Streams.Any(s => s.StreamType == streamType && s.Name == userName && s.ChannelId == channel.Id))
                    await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{userName}` is already on the `{streamType}` list.*").ConfigureAwait(false);

                switch (streamType)
                {
                    case StreamType.Mixer:
                        MixerAPI mixer = new MixerAPI(Context.HttpClient);
                        uint userId = await mixer.GetUserId(userName).ConfigureAwait(false);
                        uint chanId = await mixer.GetChannelId(userName).ConfigureAwait(false);
                        if (userId is 0 || chanId is 0)
                            await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *No user/channel found.*").ConfigureAwait(false);

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

                        var users = await twitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { userName })).ConfigureAwait(false);
                        if (!users.Users.Any())
                            await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *Twitch user not found.*").ConfigureAwait(false);

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

                await Task.Delay(1000).ConfigureAwait(false);
            }

            await ReplyAsync($"I'm so good at this, I scare myself. *`{String.Join(", ", userNames)}` has been added to the `{streamType}` list.* {Extras.OkHand}", save: DocumentType.Server).ConfigureAwait(false);
        }

        [Command("Unban"), Remarks("Unban <id>"), Summary("Unbans a user whose Id has been provided."), BotPermission(GuildPermission.BanMembers)]
        public async Task UnbanAsync(ulong id)
        {
            bool check = (await Context.Guild.GetBansAsync().ConfigureAwait(false)).Any(x => x.User.Id == id);
            if (!check)
            {
                await ReplyAsync($"{Extras.Cross} I have nothing more to give. *No user with `{id}` found.*").ConfigureAwait(false);
                return;
            }
            await Context.Guild.RemoveBanAsync(id).ContinueWith(x => ReplyAsync($"It seems there's still glory in the old Empire yet! *Unbanned user with `{id}`* {Extras.OkHand}", save: DocumentType.Server)).ConfigureAwait(false);
        }

        [Command("Unmute", RunMode = RunMode.Async), Summary("Umutes a user."), Remarks("Unmute <@user>"), BotPermission(GuildPermission.ManageRoles)]
        public Task UnMuteAsync(IGuildUser user)
            => GuildHelper.UnmuteUserAsync(Context, user);

        [Command("UserInfo"), Summary("Displays information about a user."), Remarks("UserInfo [@user]")]
        public Task UserInfoAsync(IGuildUser user = null)
        {
            user = user ?? Context.User as IGuildUser;
            return ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithAuthor($"{user.Nickname ?? user.Username} Information | {user.Id}", user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .AddField("Muted?", user.IsMuted ? "Yep" : "Nope", true)
                .AddField("Is Lieutenant?", user.IsBot ? "Yep" : "Nope", true)
                .AddField("Creation Date", user.CreatedAt, true)
                .AddField("Join Date", user.JoinedAt, true)
                .AddField("Status", user.Status, true)
                .AddField("Skills", string.Join(", ", user.GuildPermissions.ToList()), true)
                .AddField("Classes", string.Join(", ", (user as SocketGuildUser).Roles.OrderBy(x => x.Position).Select(x => x.Name)), true).Build());
        }

        [Command("Warn", RunMode = RunMode.Async), Summary("Warns a user with a specified reason."), Remarks("Warn <@user> <reason>"), BotPermission(GuildPermission.KickMembers), RequirePermission]
        public Task WarnAysnc(IGuildUser user, [Remainder] string reason)
            => GuildHelper.WarnUserAsync(Context, user, reason);

        [Command("WarnDelete"), Summary("Deletes a number of users warnings."), Remarks("WarnDelete <@user> <amount>"), RequirePermission]
        public Task WarnDeleteAsync(IGuildUser user, int amount = 1)
        {
            ProfileObject profile = GuildHelper.GetProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id);
            if (amount > profile.Warnings)
                return ReplyAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{user}` doesn't have `{amount}` warnings to remove.*");

            profile.Warnings = profile.Warnings - amount;
            GuildHelper.SaveProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id, profile);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *`{amount}` Warnings has been removed for `{user}`* {Extras.OkHand}");
        }

        [Command("WarnReset"), Summary("Resets users warnings."), Remarks("WarnReset <@user>"), RequirePermission]
        public Task WarnResetAsync(IGuildUser user)
        {
            ProfileObject profile = GuildHelper.GetProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id);
            profile.Warnings = 0;
            GuildHelper.SaveProfile(Context.DatabaseHandler, Context.Guild.Id, user.Id, profile);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *Warnings has been reset for `{user}`* {Extras.OkHand}");
        }
    }
}