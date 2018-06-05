namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Handlers.Objects;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;
    using System.Collections.Generic;
    using TwitchLib.Api;

    [Name("Moderator Commands"), RequireRole("Moderator"), Ratelimit]
    public class ModModule : Base
    {
        [Command("Kick", RunMode = RunMode.Async), Remarks("Kicks a user out of the server."), Summary("Kick <@User> [Reason]"), BotPermission(GuildPermission.KickMembers),
            UserPermission(GuildPermission.KickMembers, "Woops, it seems you lack kicking permissions.")]
        public Task KickAsync(IGuildUser User, [Remainder] string Reason = null)
        {
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                return ReplyAsync($"{User} has higher super powers than me {Extras.Cross}");
            User.KickAsync(Reason);
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.KICK, Reason);
            return ReplyAsync($"***{User} was kicked*** {Extras.Hammer}");
        }

        [Command("MassKick", RunMode = RunMode.Async), Remarks("Kicks multiple users at once."), Summary("MassKick <@User1> <@User2> ..."), BotPermission(GuildPermission.KickMembers),
            UserPermission(GuildPermission.KickMembers, "Woops, it seems you lack kicking permissions.")]
        public async Task KickAsync(params IGuildUser[] Users)
        {
            if (!Users.Any()) return;
            foreach (var User in Users)
            {
                if (Context.GuildHelper.HierarchyCheck(Context.Guild, User)) continue;
                await User.KickAsync("Multiple kicks.").ConfigureAwait(false);
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.KICKS, "Multiple kicks.");
            }
            await ReplyAsync($"{string.Join(", ", Users.Select(x => $"*{x.Username}*"))} were kicked {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("Ban", RunMode = RunMode.Async), Remarks("Bans a user from the server."), Summary("Ban <@User> [Reason]"), BotPermission(GuildPermission.BanMembers),
            UserPermission(GuildPermission.BanMembers, "Woops, it seems you lack banning permissions.")]
        public Task BanAsync(IGuildUser User, [Remainder] string Reason = null)
        {
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                return ReplyAsync($"{User} has higher super powers than me {Extras.Cross}");
            Context.Guild.AddBanAsync(User, 7, Reason).ConfigureAwait(false);
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.BAN, Reason);
            return ReplyAsync($"***{User} was banned*** {Extras.Hammer}");
        }

        [Command("MassBan", RunMode = RunMode.Async), Remarks("Bans multiple users at once."), Summary("MassBan <@User1> <@User2> ..."), BotPermission(GuildPermission.BanMembers),
            UserPermission(GuildPermission.BanMembers, "Woops, it seems you lack banning permissions.")]
        public async Task BanAsync(params IGuildUser[] Users)
        {
            if (!Users.Any()) return;
            foreach (var User in Users)
            {
                if (Context.GuildHelper.HierarchyCheck(Context.Guild, User)) continue;
                await Context.Guild.AddBanAsync(User, 7, "Mass Ban.");
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.BANS, "Multiple bans.");
            }
            await ReplyAsync($"{string.Join(", ", Users.Select(x => $"*{x.Username}*"))} were banned {Extras.Hammer}");
        }

        [Command("Ban", RunMode = RunMode.Async), Remarks("Bans a user from the server."), Summary("Ban <UserId> [Reason]"), BotPermission(GuildPermission.BanMembers),
            UserPermission(GuildPermission.BanMembers, "Woops, it seems you lack banning permissions.")]
        public async Task BanAsync(ulong UserId, [Remainder] string Reason = null)
        {
            await Context.Guild.AddBanAsync(UserId, 7, Reason ?? "Secret Ban.");
            await ReplyAsync($"***{UserId} was banned*** {Extras.Hammer}");
        }

        [Command("SoftBan", RunMode = RunMode.Async), Remarks("Bans a user then unbans them."), Summary("SoftBan <@User> [Reason]"), BotPermission(GuildPermission.BanMembers),
            UserPermission(GuildPermission.BanMembers, "Woops, it seems you lack banning permissions.")]
        public Task SoftBanAsync(IGuildUser User, [Remainder] string Reason = null)
        {
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                return ReplyAsync($"{User} has higher super powers than me {Extras.Cross}");
            Context.Guild.AddBanAsync(User, 7, Reason).ConfigureAwait(false);
            Context.Guild.RemoveBanAsync(User);
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.SOFTBAN, Reason);
            return ReplyAsync($"***{User} was soft banned*** {Extras.Hammer}");
        }

        [Command("Unban"), Summary("Unban <id>"), Remarks("Unbans a user whose Id has been provided."), BotPermission(GuildPermission.BanMembers),
            UserPermission(GuildPermission.BanMembers, "Woops, it seems you lack banning permissions.")]
        public async Task UnbanAsync(ulong Id)
        {
            var Check = (await Context.Guild.GetBansAsync()).Any(x => x.User.Id == Id);
            if (!Check) { await ReplyAsync($"{Extras.Cross} Couldn't find user with `{Id}` id in guild bans!"); return; }
            await Context.Guild.RemoveBanAsync(Id).ContinueWith(x => ReplyAsync($"Unbanned user with `{Id}` id {Extras.OkHand}"));
        }

        [Command("Reason"), Remarks("Specifies reason for a user case."), Summary("Reason <Number> <Reason>"), UserPermission]
        public async Task ReasonAsync(int Number, [Remainder] string Reason)
        {
            var Case = Number == -1 ? Context.Server.UserCases.LastOrDefault() :
                Context.Server.UserCases.FirstOrDefault(x => x.Number == Number);
            if (Case == null)
            {
                await ReplyAsync(Number == -1 ? $"{Extras.Cross}There aren't any user cases."
                    : $"Case #{Number} was invalid case number {Extras.Cross}");
                return;
            }
            Case.Reason = Reason;
            var User = await Context.Guild.GetUserAsync(Case.UserId);
            var Mod = await Context.Guild.GetUserAsync(Case.ModeratorId);
            if (User != null) Case.Username = $"{User}";
            if (Mod != null) Case.Moderator = $"{Mod}";
            var Channel = await Context.Guild.GetTextChannelAsync(Context.Server.ModLog);
            if (Channel != null && await Channel?.GetMessageAsync(Case.MessageId) is IUserMessage Message)
                await Message.ModifyAsync(
                  x => x.Content = $"**{Case.CaseType}** | Case {Case.Number}\n**User:** {Case.Username} ({Case.UserId})" +
                             $"\n**Reason:** {Reason}\n**Responsible Moderator:** {Case.Moderator} ({Case.ModeratorId})");
            await ReplyAsync($"Case #{Case.Number} has been updated {Extras.OkHand}", Save: 's');
        }

        [Command("Purge"), Remarks("Deletes Messages"), Summary("Purge [Amount]"), BotPermission(GuildPermission.ManageMessages),
            UserPermission(GuildPermission.ManageMessages, "Hmmm, where are your `manage messages` permission at?")]
        public Task PurgeAsync(int Amount = 20)
        {
            (Context.Channel as SocketTextChannel).DeleteMessagesAsync(
                 MethodHelper.RunSync(Context.Channel.GetMessagesAsync(Amount + 1).FlattenAsync()))
                .ContinueWith(x => ReplyAndDeleteAsync($"Deleted `{Amount}` messages {Extras.OkHand}", TimeSpan.FromSeconds(5)));
            return Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, Context.User, Context.User, CaseType.PURGE, $"Purged {Amount} Messages in #{Context.Channel.Name}");
        }

        [Command("Purge"), Remarks("Deletes User Messages"), Summary("Purge <@User> [Amount]"), BotPermission(GuildPermission.ManageMessages),
            UserPermission(GuildPermission.ManageMessages, "Hmmm, where are your `manage messages` permission at?")]
        public Task PurgeAsync(IGuildUser User, int Amount = 20)
        {
            (Context.Channel as SocketTextChannel).DeleteMessagesAsync(
                 MethodHelper.RunSync(Context.Channel.GetMessagesAsync(Amount + 1).FlattenAsync()).Where(x => x.Author.Id == User.Id))
                .ContinueWith(x => ReplyAndDeleteAsync($"Deleted `{Amount}` of `{User}`'s messages {Extras.OkHand}", TimeSpan.FromSeconds(5)));
            return Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, Context.User, Context.User, CaseType.PURGE, $"Purged {Amount} of {User}'s Messages #{Context.Channel.Name}");
        }

        [Command("Mute", RunMode = RunMode.Async), Remarks("Mutes a user."), Summary("Mute <@User> [Time: Defaults to 5 minutes, can be specified as | Number(d/h/m/s) Example: 10m for 10 Minutes] [Reason]"), BotPermission(GuildPermission.ManageRoles),
        UserPermission(GuildPermission.ManageRoles, "Oh man, you don't have the permission to manage roles.")]
        public Task MuteAsync(IGuildUser User, TimeSpan? Time = null, [Remainder] string Reason = null) 
            => MuteCommandAsync(User, (Time.HasValue ? Time : TimeSpan.FromMinutes(5)), (!string.IsNullOrEmpty(Reason) ? Reason : "No Reason specified.")).ContinueWith(
                x =>
                {
                    Context.Server.Muted.TryAdd(User.Id, DateTime.UtcNow.Add((TimeSpan)(Time.HasValue ? Time : TimeSpan.FromMinutes(5))).ToUniversalTime());
                    SaveDocument('s');
                });

        [Command("SoftMute", RunMode = RunMode.Async), Remarks("Mutes a user."), Summary("Mute <@User> [Reason]"), BotPermission(GuildPermission.ManageRoles),
        UserPermission(GuildPermission.ManageRoles, "Oh man, you don't have the permission to manage roles.")]
        public Task SoftMuteAsync(IGuildUser User, [Remainder] string Reason = null)
            => MuteCommandAsync(User, TimeSpan.FromMinutes(5), (!string.IsNullOrEmpty(Reason) ? Reason : "No Reason specified.")).ContinueWith(
                x =>
                {
                    Context.Server.Muted.TryAdd(User.Id, DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)).ToUniversalTime());
                    SaveDocument('s');
                });

        [Command("Unmute", RunMode = RunMode.Async), Remarks("Umutes a user."), Summary("Unmute <@User>"), BotPermission(GuildPermission.ManageRoles),
        UserPermission(GuildPermission.ManageRoles, "Oh man, you don't have the permission to manage roles.")]
        public Task UnMuteAsync(IGuildUser User)
        {
            var Role = Context.Guild.GetRole(Context.Server.MuteRole) ?? Context.Guild.Roles.FirstOrDefault(x => x.Name == "Muted");
            if (!(User as SocketGuildUser).Roles.Contains(Role) || Role == null) return ReplyAsync($"`{User}` doesn't have the mute role {Extras.Cross}");
            User.RemoveRoleAsync(Role);
            var Save = 'n';
            if (Context.Server.Muted.ContainsKey(User.Id)) { Context.Server.Muted.TryRemove(User.Id, out _); Save = 's'; }
            return ReplyAsync($"`{User}` has been unmuted {Extras.OkHand}", Save: Save);
        }

        async Task MuteCommandAsync(IGuildUser User, TimeSpan? Time, string Message)
        {
            await Context.Message.DeleteAsync();

            if (User.RoleIds.Contains(Context.Server.MuteRole)) { await ReplyAsync($"`{User}` is already muted {Extras.Cross}"); return; }
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
            { await ReplyAsync($"`{User}` has higher super powers than me {Extras.Cross}"); return; }
            var MuteRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Muted") ??
                (Context.Server.MuteRole != 0 ? Context.Guild.GetRole(Context.Server.MuteRole) :
                await Context.Guild.CreateRoleAsync("Muted", GuildPermissions.None, Color.DarkerGrey));
            var Permissions = new OverwritePermissions(createInstantInvite: PermValue.Deny,
                addReactions: PermValue.Deny, sendMessages: PermValue.Deny, sendTTSMessages: PermValue.Deny,
                attachFiles: PermValue.Deny);
            foreach (var Channel in (Context.Guild as SocketGuild).TextChannels)
                if (!Channel.PermissionOverwrites.Select(x => x.Permissions).Contains(Permissions))
                    await Channel.AddPermissionOverwriteAsync(MuteRole, Permissions).ConfigureAwait(false);
            var Save = 'n';
            if (Context.Server.MuteRole != MuteRole.Id)
            {
                Context.Server.MuteRole = MuteRole.Id;
                Save = 's';
            }
            await User.AddRoleAsync(Context.Guild.GetRole(Context.Server.MuteRole));
            await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.MUTE, Message);
            await ReplyAsync($"`{User}` has been muted for {StringHelper.FormatTimeSpan((TimeSpan)Time)} {Extras.OkHand}", Save: Save);

            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor(Context.User)
                .WithTitle("Mod Action")
                .WithDescription($"You were muted in the {Context.Guild.Name} server.")
                .WithThumbnailUrl(Context.User.GetAvatarUrl())
                .WithFooter($"You can PM {Context.User.Username} directly to resolve the issue.")
                .AddField("Reason", Message)
                .AddField("Duration", StringHelper.FormatTimeSpan((TimeSpan)Time))
                .Build();

            await (await User.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: Embed);
        }

        [Command("Warn", RunMode = RunMode.Async), Remarks("Warns a user with a specified reason."), Summary("Warn <@User> <Reason>"), BotPermission(GuildPermission.KickMembers), UserPermission]
        public async Task WarnAysnc(IGuildUser User, [Remainder]string Reason)
        {
            string WarnMessage = $"**[Warned in {Context.Guild.Name}]** ```{Reason}```";
            await User.SendMessageAsync(WarnMessage);
            var Profile = Context.GuildHelper.GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            Profile.Warnings++;
            Context.GuildHelper.SaveProfile(Context.DBHandler, Context.Guild.Id, User.Id, Profile);
            await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.WARNING, Reason);
            await ReplyAsync($"***{User}*** has been warned {Extras.Warning}");
            if (Profile.Warnings >= Context.Server.MaxWarningsToKick)
            {
                await User.KickAsync($"{User} was Kicked due to reaching max number of warnings.");
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.KICK, $"{User} was Kicked due to reaching max number of warnings.");
            }
            else if(Profile.Warnings >= Context.Server.MaxWarningsToMute)
            {
                await MuteAsync((User as SocketGuildUser), TimeSpan.FromDays(1), "Muted for 1 day due to reaching Max number of warnings.");
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.MUTE, $"{User} was muted for 1 day due to reaching max number of warnings.");
            }
        }

        [Command("ResetWarns"), Remarks("Resets users warnings."), Summary("ResetWarns <@User>"), UserPermission]
        public Task ResetWarnsAsync(IGuildUser User)
        {
            var Profile = Context.GuildHelper.GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            Profile.Warnings = 0;
            Context.GuildHelper.SaveProfile(Context.DBHandler, Context.Guild.Id, User.Id, Profile);
            return ReplyAsync($"Warnings has been reset for `{User}` {Extras.OkHand}");
        }

        [Command("ConfigureRules"), Remarks("Sets the rules that will be posted in the channel set by the Guild Config."), Summary("ConfigureRules <Rules>"), Alias("cr", "configrules")]
        public Task ConfigureRulesAsync([Remainder] string Rules)
        {
            if (Rules.Length > 2000) return ReplyAsync("Rules exceed 2,000 characters, please revise and try again.");

            Context.Server.Rules = Rules;

            return ReplyAsync($"Rules have been configured {Extras.OkHand}", Save: 's');
        }

        [Command("PostRules"), Summary("PostRules"), Remarks("Posts the rules you've configured to the rules channel you setup in the Guild Config. Only done once, if you want to edit the rules, use ConfigureRules followed by EditRules.")]
        public async Task PostRulesAsync()
        {
            if (string.IsNullOrEmpty(Context.Server.Rules)) { await ReplyAsync("You have no rules to post, please use confrules to set them up."); return; }
            if (Context.Server.RulesChannel == 0) { await ReplyAsync("You have not configured a rules channel."); return; }

            var chan = await Context.Guild.GetChannelAsync((ulong)Context.Server.RulesChannel);
            var ruleChan = chan as IMessageChannel;
            var msg = await ruleChan.SendMessageAsync(Context.Server.Rules);

            await msg.AddReactionAsync(Extras.Newspaper);
            await msg.AddReactionAsync(Extras.Standard);
            await msg.AddReactionAsync(Extras.Hardcore);
            await msg.AddReactionAsync(Extras.Challenge);

            await ReplyAsync($"Rules have been posted {Extras.OkHand}");
        }

        [Command("EditRules"), Summary("EditRules"), Remarks("Edits the rules you've configured and posted to the rules channel."), Alias("er")]
        public async Task EditRulesAsync()
        {
            if (string.IsNullOrEmpty(Context.Server.Rules)) { await ReplyAsync("You have no rules to post, please use confrules to set them up."); return; }
            if (Context.Server.RulesChannel == 0) { await ReplyAsync("You have not configured a rules channel."); return; }

            var chan = await Context.Guild.GetChannelAsync((ulong)Context.Server.RulesChannel);
            var ruleChan = chan as IMessageChannel;
            var msgs = await ruleChan.GetMessagesAsync().FlattenAsync();
            msgs = msgs.Where(x => x.Author.IsBot);

            if (msgs.Count() < 1) { await ReplyAsync("No messages found to edit, please make sure you've posted the rules to the channel."); return; }

            foreach (IUserMessage msg in msgs)
                await msg.ModifyAsync(x => x.Content = Context.Server.Rules);

            await ReplyAsync($"Rules have been edited {Extras.OkHand}");
        }

        [Command("MixerAdd", RunMode = RunMode.Async), Summary("MixerAdd <MixerUser> [#Channel: Defaults to #streams]"), Remarks("Add Mixer stream. You will get live feed from specified Mixer stream.")]
        public async Task MixerAddAsync(string MixerUser, SocketTextChannel Channel = null)
        {
            if (Context.Server.MixerStreams.Where(f => f.Name == MixerUser && f.ChannelId == Channel.Id).Any()) await ReplyAsync($"`{MixerUser}` is already in the list {Extras.Cross}");

            MixerHelper Mixer = new MixerHelper();
            uint UserId = await Mixer.GetUserId(MixerUser);
            uint ChanId = await Mixer.GetChannelId(MixerUser);
            if (UserId == 0 || ChanId == 0) await ReplyAsync($"No User/Channel found {Extras.Cross}");
            var channel = Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel;
            Context.Server.MixerStreams.Add(new MixerObject
            {
                Name = MixerUser,
                ChannelId = channel.Id,
                UserId = UserId,
                MixerChannelId = ChanId
            });
            await ReplyAsync($"`{MixerUser}` has been added to server's Mixer streams {Extras.OkHand}", Save: 's');
        }

        [Command("MixerRemove"), Remarks("Remove Mixer stream."), Summary("MixerRemove <MixerUser> [#Channel: Defaults to #streams]")]
        public Task MixerRemoveAsync(string MixerUser, SocketTextChannel Channel = null)
        {
            if (!Context.Server.MixerStreams.Select(s => s.Name == MixerUser && s.ChannelId == (Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel).Id).Any()) return ReplyAsync($"You aren't following `{MixerUser}`");
            Context.Server.MixerStreams.Remove(new MixerObject() { Name = MixerUser, ChannelId = (Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel).Id });
            return ReplyAsync($"`{MixerUser}` has been removed from the server's Mixer streams {Extras.OkHand}", Save: 's');
        }

        [Command("Mixer"), Remarks("Shows all the Mixer streams this server is following."), Summary("Mixer")]
        public Task MixerAsync()
            => ReplyAsync(!Context.Server.MixerStreams.Any() ? $"This server isn't following any streams {Extras.Cross}" :
                $"**Mixer Streams Followed**:\n{String.Join("\n", Context.Server.MixerStreams.Select(s => $"Streamer: {s.Name} | Channel: {Context.Guild.GetTextChannelAsync(s.ChannelId).GetAwaiter().GetResult().Mention}").ToList())}");

        [Command("TwitchAdd", RunMode = RunMode.Async), Summary("TwitchAdd <TwitchUser> [#Channel: Defaults to #streams]"), Remarks("Add Twitch stream. You will get live feed from specified Twitch stream.")]
        public async Task TwitchAddAsync(string TwitchUser, SocketTextChannel Channel = null)
        {
            if (Context.Server.TwitchStreams.Where(f => f.Name == TwitchUser && f.ChannelId == Channel.Id).Any()) await ReplyAsync($"`{TwitchUser}` is already in the list {Extras.Cross}");

            TwitchAPI TwitchAPI = new TwitchAPI();
            TwitchAPI.Settings.ClientId = Context.Config.APIKeys["TC"];
            TwitchAPI.Settings.AccessToken = Context.Config.APIKeys["TA"];

            var users = await TwitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { TwitchUser }));
            if (!users.Users.Any()) await ReplyAsync($"Twitch user not found {Extras.Cross}");
            var User = users.Users[0];
            var channel = Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel;

            Context.Server.TwitchStreams.Add(new TwitchObject
            {
                Name = TwitchUser,
                UserId = User.Id,
                ChannelId = channel.Id
            });
            await ReplyAsync($"`{TwitchUser}` has been added to server's Twitch streams {Extras.OkHand}", Save: 's');
        }

        [Command("TwitchRemove"), Remarks("Remove Twitch stream."), Summary("TwitchRemove <TwitchUser> [#Channel: Defaults to #streams]")]
        public Task TwitchRemoveAsync(string TwitchUser, SocketTextChannel Channel = null)
        {
            if (!Context.Server.TwitchStreams.Select(s => s.Name == TwitchUser && s.ChannelId == (Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel).Id).Any()) return ReplyAsync($"You aren't following `{TwitchUser}`");
            Context.Server.TwitchStreams.Remove(new TwitchObject() { Name = TwitchUser, ChannelId = (Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel).Id });
            return ReplyAsync($"`{TwitchUser}` has been removed from the server's Twitch streams {Extras.OkHand}", Save: 's');
        }

        [Command("Twitch"), Remarks("Shows all the Twitch streams this server is following."), Summary("Twitch")]
        public Task TwitchAsync()
            => ReplyAsync(!Context.Server.TwitchStreams.Any() ? $"This server isn't following any streams {Extras.Cross}" :
                $"**Twitch Streams Followed**:\n{String.Join("\n", Context.Server.TwitchStreams.Select(s => $"Streamer: {s.Name} | Channel: {Context.Guild.GetTextChannelAsync(s.ChannelId).GetAwaiter().GetResult().Mention}").ToList())}");

        [Command("LeaderboardAdd"), Summary("LeaderboardAdd <Variant> <#Channel> <Enabled: True, False>"), Remarks("Add Leaderboard Variant. You will get live feed from specified Leaderboard Variant.")]
        public async Task LeaderboardAddAsync(SocketTextChannel Channel, bool Enabled, [Remainder] string Variant)
        {
            Variant = Variant.Replace(" ", "_");
            if (Context.Server.Leaderboards.Where(f => f.Variant == Variant && f.ChannelId == Channel.Id).Any()) await ReplyAsync($"`{Variant}` is already in the list {Extras.Cross}");
            Context.Server.Leaderboards.Add(new LeaderboardObject
            {
                Variant = Variant,
                ChannelId = Channel.Id,
                Enabled = Enabled
            });
            await ReplyAsync($"`{Variant}` has been added to server's Leaderboard list {Extras.OkHand}", Save: 's');
        }

        [Command("LeaderboardUpdate"), Summary("LeaderboardUpdate <Variant> <#Channel> <Enabled: True, False>"), Remarks("Update Leaderboard Variant. You can not edit the Variant name, just Channel and Enabled.")]
        public Task LeaderboardUpdateAsync(SocketTextChannel Channel, bool Enabled, [Remainder] string Variant)
        {
            Variant = Variant.Replace(" ", "_");
            if (!Context.Server.Leaderboards.Where(f => f.Variant == Variant).Any()) return ReplyAsync($"Can't find the Variant `{Variant}` {Extras.Cross}");
            Context.Server.Leaderboards.Remove(new LeaderboardObject() { Variant = Variant });
            Context.Server.Leaderboards.Add(new LeaderboardObject
            {
                Variant = Variant,
                ChannelId = Channel.Id,
                Enabled = Enabled
            });
            return ReplyAsync($"Updated Leaderboard Variant: `{Variant}` {Extras.OkHand}", Save: 's');
        }

        [Command("LeaderboardRemove"), Summary("LeaderboardRemove <Variant> <#Channel>"), Remarks("Remove Leaderboard Variant.")]
        public Task LeaderboardRemove(SocketTextChannel Channel, [Remainder] string Variant)
        {
            Variant = Variant.Replace(" ", "_");
            if (!Context.Server.Leaderboards.Select(l => l.Variant == Variant && l.ChannelId == Channel.Id).Any()) return ReplyAsync($"Can't find the Variant: `{Variant}` {Extras.Cross}");
            Context.Server.Leaderboards.Remove(new LeaderboardObject() { Variant = Variant, ChannelId = Channel.Id });
            return ReplyAsync($"Removed `{Variant}` from server's Leaderboards", Save: 's');
        }

        [Command("Leaderboard"), Summary("Leaderboard"), Remarks("Shows all the Leaderboard Variants this server is watching.")]
        public Task LeaderboardAsync()
            => ReplyAsync(!Context.Server.Leaderboards.Any() ? $"This server isn't watching any Leaderboards {Extras.Cross}" :
                $"**Leaderboard Variants**:\n{String.Join("\n", Context.Server.Leaderboards.Select(l => $"Variant: {l.Variant} | Channel: {Context.Guild.GetTextChannelAsync(l.ChannelId).GetAwaiter().GetResult().Mention} | Enabled: {l.Enabled.ToString()}").ToList())}");
    }
}
