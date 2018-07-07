namespace PoE.Bot.Helpers
{
    using Addons;
    using Discord;
    using Discord.WebSocket;
    using Handlers;
    using Objects;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public static class GuildHelper
    {
        public static IMessageChannel DefaultChannel(this IGuild iguild)
        {
            SocketGuild guild = iguild as SocketGuild;
            string[] validNames = new[] { "general", "chat", "lobby", "discussion" };
            return guild.TextChannels.Where(x => guild.CurrentUser.GetPermissions(x).SendMessages).FirstOrDefault(x => validNames.Contains(x.Name) || x.Id == guild.Id) ?? guild.DefaultChannel;
        }

        public static IMessageChannel DefaultStreamChannel(this IGuild iguild)
        {
            SocketGuild guild = iguild as SocketGuild;
            string[] validNames = new[] { "streams", "streamers", "live" };
            return guild.TextChannels.Where(x => guild.CurrentUser.GetPermissions(x).SendMessages).FirstOrDefault(x => validNames.Contains(x.Name)) ?? guild.DefaultChannel;
        }

        public static ulong FindChannel(this IGuild guild, string name)
        {
            ulong parse = name.ParseULong();
            if (!(parse is 0))
                return parse;

            SocketTextChannel chn = (guild as SocketGuild).TextChannels?.FirstOrDefault(x => x.Name == name.ToLower());
            return chn?.Id ?? 0;
        }

        public static ulong FindRole(this IGuild guild, string name)
        {
            ulong parse = name.ParseULong();
            if (!(parse is 0))
                return parse;

            IRole role = guild.Roles?.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
            return role?.Id ?? 0;
        }

        public static ProfileObject GetProfile(DatabaseHandler databaseHandler, ulong guildId, ulong userId)
        {
            GuildObject server = databaseHandler.Execute<GuildObject>(Operation.Load, id: guildId);
            if (server.Profiles.ContainsKey(userId))
                return server.Profiles[userId];

            server.Profiles.Add(userId, new ProfileObject());
            databaseHandler.Save<GuildObject>(server, guildId);
            return server.Profiles[userId];
        }

        public static bool HierarchyCheck(this IGuild guild, IGuildUser user)
        {
            int highestRole = (guild as SocketGuild).CurrentUser.Roles.OrderByDescending(x => x.Position).FirstOrDefault().Position;
            return (user as SocketGuildUser).Roles.Any(x => x.Position >= highestRole);
        }

        public static async Task LogAsync(DatabaseHandler databaseHandler, IGuild guild, IUser user, IUser mod, CaseType caseType, string reason)
        {
            GuildObject server = databaseHandler.Execute<GuildObject>(Operation.Load, id: guild.Id);
            reason = string.IsNullOrWhiteSpace(reason) ? $"*Exile, please type `{server.Prefix}Reason {server.UserCases.Count + 1} <reason>`*" : reason;
            ITextChannel modChannel = await guild.GetTextChannelAsync(server.ModLog);
            IUserMessage message = null;
            if (!(modChannel is null))
            {
                var userCases = server.UserCases.Where(x => x.UserId == user.Id);
                Embed embed = Extras.Embed(Extras.Case)
                    .WithAuthor($"Case Number: {server.UserCases.Count + 1}")
                    .WithTitle(caseType.ToString())
                    .AddField("User", $"{user.Mention} `{user}` ({user.Id})")
                    .AddField("History",
                        $"Cases: {userCases.Count()}\n" +
                        $"Warnings: {userCases.Count(x => x.CaseType == CaseType.Warning)}\n" +
                        $"Mutes: {userCases.Count(x => x.CaseType == CaseType.Mute)}\n" +
                        $"Auto Mutes: {userCases.Count(x => x.CaseType == CaseType.AutoModMute)}\n")
                    .AddField("Reason", reason)
                    .AddField("Moderator", $"{mod}")
                    .WithCurrentTimestamp()
                    .Build();
                message = await modChannel.SendMessageAsync(embed: embed);
            }

            server.UserCases.Add(new CaseObject
            {
                UserId = user.Id,
                Reason = reason,
                CaseType = caseType,
                Username = $"{user}",
                Moderator = $"{mod}",
                ModeratorId = mod.Id,
                CaseDate = DateTime.Now,
                Number = server.UserCases.Count + 1,
                MessageId = message?.Id ?? 0
            });

            databaseHandler.Save<GuildObject>(server, guild.Id);
        }

        public static async Task MuteUserAsync(Context context, MuteType muteType, IGuildUser user, TimeSpan? time, string message, bool logMute = true)
            => await context.Message.DeleteAsync().ContinueWith(async _ =>
            {
                if ((context.Server.MuteRole is 0 && muteType == MuteType.Mod) || (context.Server.TradeMuteRole is 0 && muteType == MuteType.Trade))
                    return context.Channel.SendMessageAsync($"{Extras.Cross} I'm baffled by this at the moment. *No Mute Role Configured*");
                if (user.RoleIds.Contains(context.Server.MuteRole) || user.RoleIds.Contains(context.Server.TradeMuteRole))
                    return context.Channel.SendMessageAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{user}` is already muted.*");
                if (context.Guild.HierarchyCheck(user))
                    return context.Channel.SendMessageAsync($"{Extras.Cross} Oops, clumsy me! *`{user}` is higher than I.*");

                switch (muteType)
                {
                    case MuteType.Mod:
                        await user.AddRoleAsync(context.Guild.GetRole(context.Server.MuteRole));
                        context.Server.Muted.TryAdd(user.Id, DateTime.Now.Add((TimeSpan)time));
                        context.DatabaseHandler.Save<GuildObject>(context.Server, context.Guild.Id);
                        break;

                    case MuteType.Trade:
                        await user.AddRoleAsync(context.Guild.GetRole(context.Server.TradeMuteRole));
                        context.Server.Muted.TryAdd(user.Id, DateTime.Now.Add((TimeSpan)time));
                        context.DatabaseHandler.Save<GuildObject>(context.Server, context.Guild.Id);
                        break;
                }

                if (logMute)
                    await LogAsync(context.DatabaseHandler, context.Guild, user, context.User, CaseType.Mute, $"{message} ({StringHelper.FormatTimeSpan((TimeSpan)time)})");

                await context.Channel.SendMessageAsync($"Rest now, tormented soul. *`{user}` has been muted for {StringHelper.FormatTimeSpan((TimeSpan)time)}* {Extras.OkHand}");

                Embed embed = Extras.Embed(Extras.Info)
                    .WithAuthor(context.User)
                    .WithTitle("Mod Action")
                    .WithDescription($"You were muted in the {context.Guild.Name} server.")
                    .WithThumbnailUrl(context.User.GetAvatarUrl())
                    .WithFooter($"You can PM {context.User.Username} directly to resolve the issue.")
                    .AddField("Reason", message)
                    .AddField("Duration", StringHelper.FormatTimeSpan((TimeSpan)time))
                    .Build();

                return (await user.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: embed);
            });

        public static async Task MuteUserAsync(DatabaseHandler databaseHandler, SocketMessage message, GuildObject server, IGuildUser user, CaseType caseType, TimeSpan time, string reason)
        {
            SocketGuild guild = (message.Author as SocketGuildUser).Guild;

            await user.AddRoleAsync(guild.GetRole(server.MuteRole));
            server.Muted.TryAdd(user.Id, DateTime.Now.Add(time));
            databaseHandler.Save<GuildObject>(server, guild.Id);

            await LogAsync(databaseHandler, guild, user, guild.CurrentUser, caseType, $"{reason} ({StringHelper.FormatTimeSpan(time)})");

            Embed embed = Extras.Embed(Extras.Info)
                .WithAuthor(guild.CurrentUser)
                .WithTitle("Mod Action")
                .WithDescription($"You were muted in the {guild.Name} server.")
                .WithThumbnailUrl(guild.CurrentUser.GetAvatarUrl())
                .WithFooter($"You can PM any Moderator directly to resolve the issue.")
                .AddField("Reason", reason)
                .AddField("Duration", StringHelper.FormatTimeSpan(time))
                .Build();

            await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: embed);
        }

        public static void SaveProfile(DatabaseHandler databaseHandler, ulong guildId, ulong userId, ProfileObject profile)
        {
            GuildObject server = databaseHandler.Execute<GuildObject>(Operation.Load, id: guildId);
            server.Profiles[userId] = profile;
            databaseHandler.Save<GuildObject>(server, guildId);
        }

        public static async Task<IAsyncResult> UnmuteUserAsync(ulong userId, SocketGuild guild, GuildObject server)
        {
            SocketGuildUser user = guild.GetUser(userId);
            SocketRole muteRole = guild.GetRole(server.MuteRole) ?? guild.Roles.FirstOrDefault(x => x.Name is "Muted");
            SocketRole tradeMuteRole = guild.GetRole(server.TradeMuteRole) ?? guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute");
            if (!user.Roles.Contains(muteRole) && !user.Roles.Contains(tradeMuteRole))
                return Task.CompletedTask;
            if (user.Roles.Contains(muteRole))
                await user.RemoveRoleAsync(muteRole);
            else if (user.Roles.Contains(tradeMuteRole))
                await user.RemoveRoleAsync(tradeMuteRole);

            return Task.CompletedTask;
        }

        public static async Task UnmuteUserAsync(Context context, IGuildUser user)
        {
            IRole muteRole = context.Guild.GetRole(context.Server.MuteRole) ?? context.Guild.Roles.FirstOrDefault(x => x.Name is "Muted");
            IRole tradeMuteRole = context.Guild.GetRole(context.Server.MuteRole) ?? context.Guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute");
            if (!user.RoleIds.Contains(muteRole.Id) && !user.RoleIds.Contains(tradeMuteRole.Id))
            {
                await context.Channel.SendMessageAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{user}` doesn't have any mute role.*");
                return;
            }

            if (user.RoleIds.Contains(muteRole.Id))
                await user.RemoveRoleAsync(muteRole);
            else if (user.RoleIds.Contains(tradeMuteRole.Id))
                await user.RemoveRoleAsync(tradeMuteRole);

            if (context.Server.Muted.ContainsKey(user.Id))
                context.Server.Muted.TryRemove(user.Id, out _);

            context.DatabaseHandler.Save<GuildObject>(context.Server, context.Guild.Id);
            await context.Channel.SendMessageAsync($"It seems there's still glory in the old Empire yet! *`{user}` has been unmuted.* {Extras.OkHand}");
        }

        public static string ValidateChannel(this IGuild guild, ulong id)
        {
            if (id is 0)
                return "Not Set.";

            SocketTextChannel channel = (guild as SocketGuild)?.GetTextChannel(id);
            return channel is null
                ? $"Unknown ({id})"
                : channel.Name;
        }

        public static string ValidateRole(this IGuild guild, ulong id)
        {
            if (id is 0)
                return "Not Set";

            IRole role = guild.GetRole(id);
            return role is null
                ? $"Unknown ({id})"
                : role.Name;
        }

        public static string ValidateUser(this IGuild guild, ulong id)
        {
            SocketGuildUser user = (guild as SocketGuild)?.GetUser(id);
            return user is null
                ? "Unknown User"
                : user.Username;
        }

        public static async Task WarnUserAsync(Context context, IGuildUser user, string reason, MuteType muteType = MuteType.Mod)
        {
            if (context.Server.MaxWarningsToMute is 0 || user.Id == context.Guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels ||
                user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
                return;

            ProfileObject profile = GetProfile(context.DatabaseHandler, context.Guild.Id, user.Id);
            profile.Warnings++;

            if (profile.Warnings >= context.Server.MaxWarningsToMute)
            {
                await MuteUserAsync(context, muteType, user, TimeSpan.FromDays(1), $"Muted for 1 day due to reaching Max number of warnings. {reason}", false);
                await LogAsync(context.DatabaseHandler, context.Guild, user, context.User, CaseType.AutoModMute, $"Muted for 1 day due to reaching max number of warnings. {reason}");
            }
            else
            {
                await LogAsync(context.DatabaseHandler, context.Guild, user, context.User, CaseType.Warning, reason);
                await context.Channel.SendMessageAsync($"Purity will prevail! *`{user}` has been warned.* {Extras.Warning}");
            }
            SaveProfile(context.DatabaseHandler, context.Guild.Id, user.Id, profile);
        }

        public static async Task WarnUserAsync(SocketMessage message, GuildObject server, DatabaseHandler databaseHandler, string warning)
            => await message.DeleteAsync().ContinueWith(async _ =>
            {
                SocketGuild guild = (message.Author as SocketGuildUser).Guild;
                SocketGuildUser user = message.Author as SocketGuildUser;
                if (server.MaxWarningsToMute is 0 || user.Id == guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels ||
                    user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
                    return;

                ProfileObject profile = GetProfile(databaseHandler, guild.Id, user.Id);
                profile.Warnings++;

                if (profile.Warnings >= server.MaxWarningsToMute)
                    await MuteUserAsync(databaseHandler, message, server, user, CaseType.AutoModMute, TimeSpan.FromDays(1), $"Muted by AutoMod. {warning} For saying: `{message.Content}`");
                else
                    await LogAsync(databaseHandler, guild, user, guild.CurrentUser, CaseType.Warning, $"{warning} For saying: `{message.Content}`");

                SaveProfile(databaseHandler, guild.Id, user.Id, profile);
                await message.Channel.SendMessageAsync(warning);
            });
    }
}