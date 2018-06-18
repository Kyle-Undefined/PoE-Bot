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
    using PoE.Bot.Objects;
    using PoE.Bot.Addons.Preconditions;
    using System.Collections.Generic;
    using TwitchLib.Api;

    [Name("Moderator Commands"), RequireModerator, Ratelimit]
    public class ModModule : BotBase
    {
        [Command("Kick", RunMode = RunMode.Async), Remarks("Kicks a user out of the server."), Summary("Kick <@User> [Reason]"), BotPermission(GuildPermission.KickMembers)]
        public Task KickAsync(IGuildUser User, [Remainder] string Reason = null)
        {
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                return ReplyAsync($"{Extras.Cross} Oops, clumsy me! `{User}` is higher than I.");
            User.KickAsync(Reason);
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.KICK, Reason);
            return ReplyAsync($"Death to sin! *`{User}` was kicked.* {Extras.Hammer}");
        }

        [Command("MassKick", RunMode = RunMode.Async), Remarks("Kicks multiple users at once."), Summary("MassKick <@User1> <@User2> ..."), BotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(params IGuildUser[] Users)
        {
            if (!Users.Any())
                return;
            foreach (var User in Users)
            {
                if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                    continue;
                await User.KickAsync("Multiple kicks.").ConfigureAwait(false);
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.KICKS, "Multiple kicks.");
            }
            await ReplyAsync($"Death to sin! *{string.Join(", ", Users.Select(x => $"`{x.Username}`"))} were kicked.* {Extras.Hammer}").ConfigureAwait(false);
        }

        [Command("Ban", RunMode = RunMode.Async), Remarks("Bans a user from the server."), Summary("Ban <@User> [Reason]"), BotPermission(GuildPermission.BanMembers)]
        public Task BanAsync(IGuildUser User, [Remainder] string Reason = null)
        {
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                return ReplyAsync($"{Extras.Cross} Oops, clumsy me! *`{User}` is higher than I.*");
            Context.Guild.AddBanAsync(User, 7, Reason).ConfigureAwait(false);
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.BAN, Reason);
            return ReplyAsync($"You are remembered only for the mess you leave behind. *`{User}` was banned.* {Extras.Hammer}");
        }

        [Command("MassBan", RunMode = RunMode.Async), Remarks("Bans multiple users at once."), Summary("MassBan <@User1> <@User2> ..."), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(params IGuildUser[] Users)
        {
            if (!Users.Any())
                return;
            foreach (var User in Users)
            {
                if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                    continue;
                await Context.Guild.AddBanAsync(User, 7, "Mass Ban.");
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.BANS, "Multiple bans.");
            }
            await ReplyAsync($"You are remembered only for the mess you leave behind. *{string.Join(", ", Users.Select(x => $"`{x.Username}`"))} were banned.* {Extras.Hammer}");
        }

        [Command("BanUserID", RunMode = RunMode.Async), Remarks("Bans a user from the server."), Summary("BanUserID <UserId> [Reason]"), BotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(ulong UserId, [Remainder] string Reason = null)
        {
            await Context.Guild.AddBanAsync(UserId, 7, Reason ?? "User ID Ban.");
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, (await Context.Guild.GetUserAsync(UserId) as IUser), Context.User, CaseType.BAN, Reason);
            await ReplyAsync($"You are remembered only for the mess you leave behind. *`{UserId}` was banned.* {Extras.Hammer}");
        }

        [Command("SoftBan", RunMode = RunMode.Async), Remarks("Bans a user then unbans them."), Summary("SoftBan <@User> [Reason]"), BotPermission(GuildPermission.BanMembers)]
        public Task SoftBanAsync(IGuildUser User, [Remainder] string Reason = null)
        {
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
                return ReplyAsync($"{Extras.Cross} Oops, clumsy me! `{User}` is higher than I.");
            Context.Guild.AddBanAsync(User, 7, Reason).ConfigureAwait(false);
            Context.Guild.RemoveBanAsync(User);
            _ = Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.SOFTBAN, Reason);
            return ReplyAsync($"Go to bed, little nightmare! *`{User}` was soft banned.* {Extras.Hammer}");
        }

        [Command("Unban"), Summary("Unban <id>"), Remarks("Unbans a user whose Id has been provided."), BotPermission(GuildPermission.BanMembers)]
        public async Task UnbanAsync(ulong Id)
        {
            var Check = (await Context.Guild.GetBansAsync()).Any(x => x.User.Id == Id);
            if (!Check)
            {
                await ReplyAsync($"{Extras.Cross} I have nothing more to give. *No User with `{Id}` found.*");
                return;
            }
            await Context.Guild.RemoveBanAsync(Id).ContinueWith(x => ReplyAsync($"It seems there's still glory in the old Empire yet! *Unbanned user with `{Id}`* {Extras.OkHand}", Save: 's'));
        }

        [Command("Reason"), Remarks("Specifies reason for a user case."), Summary("Reason <Number> <Reason>"), RequirePermission]
        public async Task ReasonAsync(int Number, [Remainder] string Reason)
        {
            var Case = Number is -1 ? Context.Server.UserCases.LastOrDefault() : Context.Server.UserCases.FirstOrDefault(x => x.Number == Number);
            if (Case is null)
            {
                await ReplyAsync(Number is -1 ? $"{Extras.Cross} There aren't any user cases." : $"{Extras.Cross} Case #{Number} was invalid case number");
                return;
            }
            Case.Reason = Reason;
            var User = await Context.Guild.GetUserAsync(Case.UserId);
            var Mod = await Context.Guild.GetUserAsync(Case.ModeratorId);
            if (!(User is null))
                Case.Username = $"{User}";
            if (!(Mod is null))
                Case.Moderator = $"{Mod}";
            var Channel = await Context.Guild.GetTextChannelAsync(Context.Server.ModLog);
            if (!(Channel is null) && await Channel?.GetMessageAsync(Case.MessageId) is IUserMessage Message)
            {
                var UserCases = Context.Server.UserCases.Where(x => x.UserId == User.Id);
                var Embed = Extras.Embed(Extras.Case)
                    .WithAuthor($"Case Number: {Case.Number}")
                    .WithTitle(Case.CaseType.ToString())
                    .AddField("User", $"{User.Mention} `{User}` ({User.Id})")
                    .AddField("History", $"Cases: {UserCases.Count()}\nWarnings: {UserCases.Where(x => x.CaseType == CaseType.WARNING).Count()}\n" +
                        $"Mutes: {UserCases.Where(x => x.CaseType == CaseType.MUTE).Count()}\nAuto Mutes: {UserCases.Where(x => x.CaseType == CaseType.AUTOMODMUTE).Count()}\n" +
                        $"Auto Perm Mutes: {UserCases.Where(x => x.CaseType == CaseType.AUTOMODPERMMUTE).Count()}")
                    .AddField("Reason", Case.Reason)
                    .AddField("Moderator", $"{Mod}")
                    .WithCurrentTimestamp()
                    .Build();
                await Message.ModifyAsync(x => x.Embed = Embed);
            }
            await ReplyAsync($"Case #{Case.Number} has been updated {Extras.OkHand}", Save: 's');
        }

        [Command("Purge"), Alias("Prune"), Remarks("Deletes Messages, and can specify a User"), Summary("Purge [Amount] [@User]"), BotPermission(GuildPermission.ManageMessages)]
        public Task PurgeAsync(int Amount = 20, IGuildUser User = null)
        {
            if(User is null)
            {
                (Context.Channel as SocketTextChannel).DeleteMessagesAsync(MethodHelper.RunSync(Context.Channel.GetMessagesAsync(Amount + 1).FlattenAsync()))
                .ContinueWith(x => ReplyAndDeleteAsync($"Beauty will grow from your remains. *Deleted `{Amount}` messages.* {Extras.OkHand}", TimeSpan.FromSeconds(5)));
                return Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, Context.User, Context.User, CaseType.PURGE, $"Purged {Amount} Messages in #{Context.Channel.Name}");
            }
            else
            {
                (Context.Channel as SocketTextChannel).DeleteMessagesAsync(MethodHelper.RunSync(Context.Channel.GetMessagesAsync(Amount + 1).FlattenAsync()).Where(x => x.Author.Id == User.Id))
                .ContinueWith(x => ReplyAndDeleteAsync($"Beauty will grow from your remains. *Deleted `{Amount}` of `{User}`'s messages.* {Extras.OkHand}", TimeSpan.FromSeconds(5)));
                return Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, Context.User, Context.User, CaseType.PURGE, $"Purged {Amount} of {User}'s Messages #{Context.Channel.Name}");
            }
        }

        [Command("TimedMute", RunMode = RunMode.Async), Remarks("Mutes a user for a given time. Time: Defaults to 5 minutes, can be specified as | Number(d/h/m/s) Example: 10m for 10 Minutes"), Summary("Mute <@User> [Time] [Reason]"), BotPermission(GuildPermission.ManageRoles)]
        public Task MuteAsync(IGuildUser User, TimeSpan? Time = null, [Remainder] string Reason = null) 
            => Context.GuildHelper.MuteUserAsync(Context, MuteType.MOD, User, (Time.HasValue ? Time : TimeSpan.FromMinutes(5)), (!(Reason is null) ? Reason : "No Reason specified."));

        [Command("Mute", RunMode = RunMode.Async), Remarks("Mutes a user for 5 minutes."), Summary("Mute <@User> [Reason]"), BotPermission(GuildPermission.ManageRoles)]
        public Task MuteAsync(IGuildUser User, [Remainder] string Reason = null)
            => Context.GuildHelper.MuteUserAsync(Context, MuteType.MOD, User, TimeSpan.FromMinutes(5), (!(Reason is null) ? Reason : "No Reason specified."));

        [Command("Unmute", RunMode = RunMode.Async), Remarks("Umutes a user."), Summary("Unmute <@User>"), BotPermission(GuildPermission.ManageRoles)]
        public Task UnMuteAsync(IGuildUser User)
            => Context.GuildHelper.UnmuteUserAsync(Context, User as SocketGuildUser);

        [Command("Warn", RunMode = RunMode.Async), Remarks("Warns a user with a specified reason."), Summary("Warn <@User> <Reason>"), BotPermission(GuildPermission.KickMembers), RequirePermission]
        public async Task WarnAysnc(IGuildUser User, [Remainder] string Reason)
        {
            var Profile = Context.GuildHelper.GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            Profile.Warnings++;
            if (Profile.Warnings >= Context.Server.MaxWarningsToPermMute)
            {
                DateTime Now = DateTime.Now;
                TimeSpan Span = Now.AddYears(999) - Now;
                await Context.GuildHelper.MuteUserAsync(Context, MuteType.MOD, (User as SocketGuildUser), Span, $"Muted permanently for reaching Max number of warnings. {Reason}", false);
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.AUTOMODPERMMUTE, $"Muted permanently due to reaching max number of warnings. {Reason}");
            }
            else if (Profile.Warnings >= Context.Server.MaxWarningsToMute)
            {
                await Context.GuildHelper.MuteUserAsync(Context, MuteType.MOD, (User as SocketGuildUser), TimeSpan.FromDays(1), $"Muted for 1 day due to reaching Max number of warnings. {Reason}", false);
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.AUTOMODMUTE, $"Muted for 1 day due to reaching max number of warnings. {Reason}");
            }
            else
            {
                await Context.GuildHelper.LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.WARNING, Reason);
                await ReplyAsync($"Purity will prevail! *`{User}` has been warned.* {Extras.Warning}");
                string WarnMessage = $"**[Warned in {Context.Guild.Name}]** ```{Reason}```";
                await User.SendMessageAsync(WarnMessage);
            }
            Context.GuildHelper.SaveProfile(Context.DBHandler, Context.Guild.Id, User.Id, Profile);
        }

        [Command("RemoveWarns"), Remarks("Removes a number of users warnings."), Summary("RemoveWarns <@User> <Amount>"), RequirePermission]
        public Task RemoveWarnsAsync(IGuildUser User, int Amount = 1)
        {
            var Profile = Context.GuildHelper.GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            if (Amount > Profile.Warnings)
                return ReplyAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{User}` doesn't have `{Amount}` warnings to remove.*");
            Profile.Warnings = Profile.Warnings - Amount;
            Context.GuildHelper.SaveProfile(Context.DBHandler, Context.Guild.Id, User.Id, Profile);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *`{Amount}` Warnings has been removed for `{User}`* {Extras.OkHand}");
        }

        [Command("ResetWarns"), Remarks("Resets users warnings."), Summary("ResetWarns <@User>"), RequirePermission]
        public Task ResetWarnsAsync(IGuildUser User)
        {
            var Profile = Context.GuildHelper.GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            Profile.Warnings = 0;
            Context.GuildHelper.SaveProfile(Context.DBHandler, Context.Guild.Id, User.Id, Profile);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *Warnings has been reset for `{User}`* {Extras.OkHand}");
        }

        [Command("GuildInfo"), Remarks("Displays information about guild."), Summary("GuildInfo")]
        public Task GuildInfoAsync()
        {
            var Guild = Context.Guild as SocketGuild;
            return ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithAuthor($"{Context.Guild.Name}'s Information | {Context.Guild.Id}", Context.Guild.IconUrl)
                .WithFooter($"Created On: {Guild.CreatedAt}")
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .AddField("Kitava", Guild.Owner, true)
                .AddField("Default Channel", Guild.DefaultChannel.Name ?? "No Default Channel", true)
                .AddField("Message Notifications", Guild.DefaultMessageNotifications, true)
                .AddField("Verification Level", Guild.VerificationLevel, true)
                .AddField("MFA Level", Guild.MfaLevel, true)
                .AddField("Text Channels", Guild.TextChannels.Count, true)
                .AddField("Voice Channels", Guild.VoiceChannels.Count, true)
                .AddField("Characters", Guild.MemberCount, true)
                .AddField("Lieutenants", Guild.Users.Count(x => x.IsBot is true), true)
                .AddField("Exiles", Guild.Users.Count(x => x.IsBot is false), true)
                .AddField("Classes", string.Join(", ", Guild.Roles.OrderByDescending(x => x.Position).Select(x => x.Name))).Build());
        }

        [Command("RoleInfo"), Remarks("Displays information about a role."), Summary("RoleInfo <@Role>")]
        public Task RoleInfoAsync(IRole Role)
            => ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithTitle($"{Role.Name} Information")
                .WithFooter($"Created On: {Role.CreatedAt}")
                .AddField("ID", Role.Id, true)
                .AddField("Rarity", Role.Color, true)
                .AddField("Level", Role.Position, true)
                .AddField("Shows Separately?", Role.IsHoisted ? "Yep" : "Nope", true)
                .AddField("Managed By Discord?", Role.IsManaged ? "Yep" : "Nope", true)
                .AddField("Can Mention?", Role.IsMentionable ? "Yep" : "Nope", true)
                .AddField("Skills", string.Join(", ", Role.Permissions)).Build());

        [Command("UserInfo"), Remarks("Displays information about a user."), Summary("UserInfo [@User]")]
        public Task UserInfoAsync(SocketGuildUser User = null)
        {
            User = User ?? Context.User as SocketGuildUser;
            return ReplyAsync(embed: Extras.Embed(Extras.Info)
                .WithAuthor($"{User.Username} Information | {User.Id}", User.GetAvatarUrl())
                .WithThumbnailUrl(User.GetAvatarUrl())
                .AddField("Muted?", User.IsMuted ? "Yep" : "Nope", true)
                .AddField("Is Lieutenant?", User.IsBot ? "Yep" : "Nope", true)
                .AddField("Creation Date", User.CreatedAt, true)
                .AddField("Join Date", User.JoinedAt, true)
                .AddField("Status", User.Status, true)
                .AddField("Skills", string.Join(", ", User.GuildPermissions.ToList()), true)
                .AddField("Classes", string.Join(", ", (User as SocketGuildUser).Roles.OrderBy(x => x.Position).Select(x => x.Name)), true).Build());
        }

        [Command("Case"), Remarks("Shows information about a specific case."), Summary("Case [CaseNumber]")]
        public Task CaseAsync(int CaseNumber = 0)
        {
            if (CaseNumber is 0 && Context.Server.UserCases.Any())
                CaseNumber = Context.Server.UserCases.LastOrDefault().Number;
            var Case = Context.Server.UserCases.FirstOrDefault(x => x.Number == CaseNumber);
            if (Case is null)
                return ReplyAsync($"Case #{CaseNumber} doesn't exist.");
            return ReplyAsync(embed: Extras.Embed(Extras.Case)
                .AddField("User", $"{Case.Username} ({Case.UserId})", true)
                .AddField("Case Type", Case.CaseType, true)
                .AddField("Moderator", $"{Case.Moderator} ({Case.ModeratorId})", true)
                .AddField("Reason", Case.Reason).Build());
        }

        [Command("Cases"), Remarks("Lists all of the specified cases for the guild."), Summary("Cases <CaseType>")]
        public Task CasesAsync(CaseType CaseType)
            => PagedReplyAsync(Context.GuildHelper.Pages(Context.Server.UserCases.Where(c => c.CaseType == CaseType).Select(c => 
                $"Case Number: {c.Number}\nDate: {c.CaseDate.ToString("f")}\nUser: {c.Username}\nReason: {c.Reason}\nModerator: {c.Moderator}\n")), $"{Context.Guild.Name} {CaseType} Cases");

        [Command("Cases"), Remarks("Lists all of the cases for a specified user in the guild."), Summary("Cases <User>")]
        public Task CasesAsync(SocketGuildUser User)
            => PagedReplyAsync(Context.GuildHelper.Pages(Context.Server.UserCases.Where(c => c.UserId == User.Id).Select(c =>
                $"Case Number: {c.Number}\nDate: {c.CaseDate.ToString("f")}\nType: {c.CaseType}\nReason: {c.Reason}\nModerator: {c.Moderator}\n")), $"{User.Username}'s Cases");

        [Command("CaseRemove"), Remarks("Removes the specified case from the user."), Summary("CaseRemove <User> <CaseNumber>")]
        public Task CaseRemoveAsync(SocketGuildUser User, int CaseNumber)
        {
            var Case = Context.Server.UserCases.FirstOrDefault(c => c.Number == CaseNumber && c.UserId == User.Id);
            Context.Server.UserCases.Remove(Case);
            return ReplyAsync($"It seems there's still glory in the old Empire yet! *Case Number `{CaseNumber}` has been removed from `{User}`'s cases.* {Extras.OkHand}", Save: 's');
        }

        [Command("ConfigureRules", RunMode = RunMode.Async), Remarks("Sets the rules that will be posted in the channel set by the Guild Config."), Summary("ConfigureRules")]
        public async Task ConfigureRulesAsync()
        {
            RuleObject Rules = new RuleObject();

            var Description = Context.GuildHelper.CalculateResponse(await WaitAsync("What should the Rules description be?", Timeout: TimeSpan.FromMinutes(5)));
            if (!Description.Item1)
            {
                await ReplyAsync(Description.Item2);
                return;
            }
            Rules.Description = Description.Item2;

            var TotalFields = Context.GuildHelper.CalculateResponse(await WaitAsync("How many sections should there be?", Timeout: TimeSpan.FromMinutes(1)));
            if (!TotalFields.Item1)
            {
                await ReplyAsync(TotalFields.Item2);
                return;
            }
            Rules.TotalFields = Convert.ToInt32(TotalFields.Item2);

            for (int i = 0; i < Rules.TotalFields; i++)
            {
                var FieldTitle = Context.GuildHelper.CalculateResponse(await WaitAsync("What should the section be called?", Timeout: TimeSpan.FromMinutes(1)));
                if (!FieldTitle.Item1)
                {
                    await ReplyAsync(FieldTitle.Item2);
                    break;
                }

                var FieldContent = Context.GuildHelper.CalculateResponse(await WaitAsync("What should the section contain?  *You can use Discord Markup*", Timeout: TimeSpan.FromMinutes(10)));
                if (!FieldContent.Item1)
                {
                    await ReplyAsync(FieldContent.Item2);
                    break;
                }

                Rules.Fields.Add(FieldTitle.Item2, FieldContent.Item2);
            }

            Context.Server.RulesConfig = Rules;
            SaveDocument('s');

            var Embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} Rules")
                .WithDescription(Rules.Description);

            foreach (var Field in Rules.Fields)
                Embed.AddField(Field.Key, Field.Value, false);

            await ReplyAsync($"*Rules have been configured, here's a preview of them.* {Extras.OkHand}", embed: Embed.Build());
        }

        [Command("PostRules"), Summary("PostRules"), Remarks("Posts the rules you've configured to the rules channel you setup in the Guild Config. Only done once, if you want to edit the rules, use ConfigureRules followed by EditRules.")]
        public async Task PostRulesAsync()
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

            var chan = await Context.Guild.GetChannelAsync(Context.Server.RulesChannel);
            var ruleChan = chan as IMessageChannel;
            var Embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} Rules")
                .WithDescription(Context.Server.RulesConfig.Description);

            foreach (var Field in Context.Server.RulesConfig.Fields)
                Embed.AddField(Field.Key, Field.Value, false);

            await ruleChan.SendMessageAsync(embed: Embed.Build());
            await ReplyAsync($"*Rules have been posted.* {Extras.OkHand}");
        }

        [Command("EditRules"), Summary("EditRules"), Remarks("Edits the rules you've configured and posted to the rules channel.")]
        public async Task EditRulesAsync()
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

            var chan = await Context.Guild.GetChannelAsync(Context.Server.RulesChannel);
            var ruleChan = chan as IMessageChannel;
            var msgs = await ruleChan.GetMessagesAsync().FlattenAsync();
            msgs = msgs.Where(x => x.Author.IsBot);

            if (msgs.Count() < 1)
            {
                await ReplyAsync($"{Extras.Cross} *No messages found to edit, please make sure you've posted the rules to the channel.*");
                return;
            }

            var Embed = Extras.Embed(Extras.Info)
                .WithTitle($"{Context.Guild.Name} Rules")
                .WithDescription(Context.Server.RulesConfig.Description);

            foreach (var Field in Context.Server.RulesConfig.Fields)
                Embed.AddField(Field.Key, Field.Value, false);

            foreach (IUserMessage msg in msgs)
                await msg.ModifyAsync(x => x.Embed = Embed.Build());

            await ReplyAsync($"*Rules have been edited.* {Extras.OkHand}");
        }

        [Command("Streamer Add", RunMode = RunMode.Async), Summary("Streamer Add <StreamType> <UserName> [#Channel: Defaults to #streams]"), Remarks("Adds a streamer to the Stream list. You will get `Is Live` posts in the specified channel.")]
        public async Task StreamerAddAsync(StreamType StreamType, string UserName, SocketTextChannel Channel = null)
        {
            if (Context.Server.Streams.Where(s => s.StreamType == StreamType && s.Name == UserName && s.ChannelId == Channel.Id).Any())
                await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{UserName}` is already on the `{StreamType}` list.*");

            var channel = Channel ?? Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel;
            switch (StreamType)
            {
                case StreamType.MIXER:
                    MixerAPI Mixer = new MixerAPI();
                    uint UserId = await Mixer.GetUserId(UserName);
                    uint ChanId = await Mixer.GetChannelId(UserName);
                    if (UserId is 0 || ChanId is 0)
                        await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *No User/Channel found.*");
                    
                    Context.Server.Streams.Add(new StreamObject
                    {
                        Name = UserName,
                        ChannelId = channel.Id,
                        MixerUserId = UserId,
                        MixerChannelId = ChanId,
                        StreamType = StreamType
                    });

                    break;

                case StreamType.TWITCH:
                    TwitchAPI TwitchAPI = new TwitchAPI();
                    TwitchAPI.Settings.ClientId = Context.Config.APIKeys["TC"];
                    TwitchAPI.Settings.AccessToken = Context.Config.APIKeys["TA"];

                    var users = await TwitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { UserName }));
                    if (!users.Users.Any())
                        await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *Twitch user not found.*");

                    var User = users.Users[0];
                    Context.Server.Streams.Add(new StreamObject
                    {
                        Name = UserName,
                        TwitchUserId = User.Id,
                        ChannelId = channel.Id,
                        StreamType = StreamType
                    });

                    break;
            }

            await ReplyAsync($"I'm so good at this, I scare myself. *`{UserName}` has been added to the `{StreamType}` list.* {Extras.OkHand}", Save: 's');
        }

        [Command("Streamer MultiAdd", RunMode = RunMode.Async), Summary("Streamer MultiAdd <StreamType> <UserNames>"), Remarks("Adds a list of streamers to the Stream list. You will get `Is Live` posts in the specified channel.")]
        public async Task StreamerMultiAddAsync(StreamType StreamType, params string[] UserNames)
        {
            var Channel = Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel;

            foreach(var UserName in UserNames)
            {
                if (Context.Server.Streams.Where(s => s.StreamType == StreamType && s.Name == UserName && s.ChannelId == Channel.Id).Any())
                    await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{UserName}` is already on the `{StreamType}` list.*");

                switch (StreamType)
                {
                    case StreamType.MIXER:
                        MixerAPI Mixer = new MixerAPI();
                        uint UserId = await Mixer.GetUserId(UserName);
                        uint ChanId = await Mixer.GetChannelId(UserName);
                        if (UserId is 0 || ChanId is 0)
                            await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *No User/Channel found.*");

                        Context.Server.Streams.Add(new StreamObject
                        {
                            Name = UserName,
                            ChannelId = Channel.Id,
                            MixerUserId = UserId,
                            MixerChannelId = ChanId,
                            StreamType = StreamType
                        });

                        break;

                    case StreamType.TWITCH:
                        TwitchAPI TwitchAPI = new TwitchAPI();
                        TwitchAPI.Settings.ClientId = Context.Config.APIKeys["TC"];
                        TwitchAPI.Settings.AccessToken = Context.Config.APIKeys["TA"];

                        var users = await TwitchAPI.Users.helix.GetUsersAsync(null, new List<string>(new string[] { UserName }));
                        if (!users.Users.Any())
                            await ReplyAsync($"{Extras.Cross} I don't think I need to be doing that right now. *Twitch user not found.*");

                        var User = users.Users[0];
                        Context.Server.Streams.Add(new StreamObject
                        {
                            Name = UserName,
                            TwitchUserId = User.Id,
                            ChannelId = Channel.Id,
                            StreamType = StreamType
                        });

                        break;
                }

                await Task.Delay(1000);
            }

            await ReplyAsync($"I'm so good at this, I scare myself. *`{String.Join(", ", UserNames)}` has been added to the `{StreamType}` list.* {Extras.OkHand}", Save: 's');
        }

        [Command("Streamer Remove"), Remarks("Remove a Streamer"), Summary("Streamer Remove <StreamType> <UserName> [#Channel: Defaults to #streams]")]
        public Task StreamerRemoveAsync(StreamType StreamType, string UserName, SocketTextChannel Channel = null)
        {
            if (!Context.Server.Streams.Select(s => s.StreamType == StreamType && s.Name == UserName && s.ChannelId == (Channel ?? 
                Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel).Id).Any())
                return ReplyAsync($"{Extras.Cross} My spirit is spent. *`{UserName}` isn't on the `{StreamType}` list.*");
            var streamer = Context.Server.Streams.FirstOrDefault(s => s.StreamType == StreamType && s.Name == UserName && s.ChannelId == (Context.GuildHelper.DefaultStreamChannel(Context.Guild) as SocketTextChannel).Id);
            Context.Server.Streams.Remove(streamer);
            return ReplyAsync($"Every death brings me life. *`{UserName}` has been removed from the `{StreamType}` list.* {Extras.OkHand}", Save: 's');
        }

        [Command("Streamers"), Remarks("Shows a list of all Streamers"), Summary("Streamers")]
        public Task StreamersAsync()
            => ReplyAsync(!Context.Server.Streams.Any() ? $"{Extras.Cross} Return to Kitava! *Wraeclast doesn't have any streamers.*" : 
                $"**Streamers**:\n{String.Join("\n", Context.Server.Streams.OrderBy(s => s.StreamType).Select(s => $"`{s.StreamType}`: {s.Name} {(Context.GuildHelper.DefaultStreamChannel(Context.Guild).Id == Context.Guild.GetTextChannelAsync(s.ChannelId).GetAwaiter().GetResult().Id ? "" : Context.Guild.GetTextChannelAsync(s.ChannelId).GetAwaiter().GetResult().Mention)}").ToList())}");

        [Command("Leaderboard Add"), Summary("Leaderboard Add <#Channel> <Enabled: True, False> <Variant>"), Remarks("Add Leaderboard Variant. You will get live feed from specified Leaderboard Variant.")]
        public async Task LeaderboardAddAsync(SocketTextChannel Channel, bool Enabled, [Remainder] string Variant)
        {
            Variant = Variant.Replace(" ", "_");
            if (Context.Server.Leaderboards.Where(f => f.Variant == Variant && f.ChannelId == Channel.Id).Any())
                await ReplyAsync($"{Extras.Cross} My spirit is spent. *`{Variant}` is already in the list.*");
            Context.Server.Leaderboards.Add(new LeaderboardObject
            {
                Variant = Variant,
                ChannelId = Channel.Id,
                Enabled = Enabled
            });
            await ReplyAsync($"Slowness lends strength to one's enemies. *`{Variant}` has been added to Leaderboard list.* {Extras.OkHand}", Save: 's');
        }

        [Command("Leaderboard Update"), Summary("Leaderboard Update <#Channel> <Enabled: True, False> <Variant>"), Remarks("Update Leaderboard Variant. You can not edit the Variant name, just Channel and Enabled.")]
        public Task LeaderboardUpdateAsync(SocketTextChannel Channel, bool Enabled, [Remainder] string Variant)
        {
            Variant = Variant.Replace(" ", "_");
            if (!Context.Server.Leaderboards.Where(f => f.Variant == Variant).Any())
                return ReplyAsync($"{Extras.Cross} Poor, corrupted creature. *Can't find the Variant `{Variant}`*");
            var leaderboard = Context.Server.Leaderboards.FirstOrDefault(l => l.Variant == Variant);
            Context.Server.Leaderboards.Remove(leaderboard);
            Context.Server.Leaderboards.Add(new LeaderboardObject
            {
                Variant = Variant,
                ChannelId = Channel.Id,
                Enabled = Enabled
            });
            return ReplyAsync($"Slowness lends strength to one's enemies. *Updated Leaderboard Variant: `{Variant}`* {Extras.OkHand}", Save: 's');
        }

        [Command("Leaderboard Remove"), Summary("Leaderboard Remove <#Channel> <Variant>"), Remarks("Remove Leaderboard Variant.")]
        public Task LeaderboardRemove(SocketTextChannel Channel, [Remainder] string Variant)
        {
            Variant = Variant.Replace(" ", "_");
            if (!Context.Server.Leaderboards.Select(l => l.Variant == Variant && l.ChannelId == Channel.Id).Any())
                return ReplyAsync($"{Extras.Cross} Poor, corrupted creature. *Can't find the Variant: `{Variant}`*");
            var leaderboard = Context.Server.Leaderboards.FirstOrDefault(l => l.Variant == Variant && l.ChannelId == Channel.Id);
            Context.Server.Leaderboards.Remove(leaderboard);
            return ReplyAsync($"Life is short, deal with it! *Removed `{Variant}` from the Leaderboards list.* {Extras.OkHand}", Save: 's');
        }

        [Command("Leaderboard List"), Summary("Leaderboard List"), Remarks("Shows all the Leaderboard Variants this server is watching.")]
        public Task LeaderboardAsync()
            => ReplyAsync(!Context.Server.Leaderboards.Any() ? $"{Extras.Cross} Return to Kitava! *Wraeclast doesn't have any leaderboards.*" :
                $"**Leaderboard Variants**:\n{String.Join("\n", Context.Server.Leaderboards.Select(l => $"Variant: {l.Variant} | Channel: {Context.Guild.GetTextChannelAsync(l.ChannelId).GetAwaiter().GetResult().Mention} | Enabled: {l.Enabled.ToString()}").ToList())}");

        [Command("Situation Add"), Summary("Situation Add <@User1> <@User2> ..."), Remarks("Adds the Situation Room role to the specified users.")]
        public Task SituationAddAsync(params IGuildUser[] Users)
        {
            foreach (var User in Users)
                User.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(r => r.Name is "Situation Room"));
            return ReplyAndDeleteAsync($"Purity will prevail! *Users has been added to Situation Room.* {Extras.OkHand}");
        }

        [Command("Situation Remove"), Summary("Situation Remove <@User1> <@User2> ..."), Remarks("Removes the Situation Room role to the specified users.")]
        public Task SituationRemoveAsync(params IGuildUser[] Users)
        {
            foreach (var User in Users)
                User.RemoveRoleAsync(Context.Guild.Roles.FirstOrDefault(r => r.Name is "Situation Room"));
            return ReplyAndDeleteAsync($"Purity will prevail! *Users has been removed from Situation Room.* {Extras.OkHand}");
        }

        [Command("Profanity Add"), Summary("Profanity Add <Word>"), Remarks("Adds a word to the Profanity List.")]
        public Task ProfanityAddAsync(string Word)
        {
            Context.Server.ProfanityList.Add(Word.ToLower());
            return ReplyAsync($"Death to sin! *`{Word}` has been added to the filter.* {Extras.OkHand}", Save: 's');
        }

        [Command("Profanity Remove"), Summary("Profanity Remove <Word>"), Remarks("Removes a word from the Profanity List.")]
        public Task ProfanityRemoveAsync(string Word)
        {
            Context.Server.ProfanityList.Remove(Word.ToLower());
            return ReplyAsync($"I like you better this way! *`{Word}` has been removed from the filter.* {Extras.OkHand}", Save: 's');
        }

        [Command("Profanity List"), Summary("Profanity List"), Remarks("Lists all words in the Profanity List.")]
        public Task ProfanityListAsync()
            => ReplyAsync($"`{String.Join("`,`", Context.Server.ProfanityList)}`");
    }
}
