using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands.Permissions;
using PoE.Bot.Config;
using PoE.Bot.Extensions;
using Microsoft.Extensions.PlatformAbstractions;
using SkiaSharp;

namespace PoE.Bot.Commands
{
    internal class CommandModule : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Commands Module"; } }

        #region Role Manipulation
        [Command("mkrole", "Creates a new role.", Aliases = "makerole;createrole", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task CreateRole(CommandContext ctx,
            [ArgumentParameter("Name of the new role.", true)] string name)
        {
            var grl = await ctx.Guild.CreateRoleAsync(name, new GuildPermissions(0x0635CC01u), null, false);

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            var embedmod = this.PrepareEmbed("Role created", $"```The role '{grl.Name}' was created successfully.```", EmbedType.Success);
            embedmod.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Message.DeleteAsync();

            if (mod != null)
                await mod.SendMessageAsync("", false, embedmod.Build());
        }

        [Command("rmrole", "Removes a role.", Aliases = "removerole;deleterole;delrole", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task DeleteRole(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to delete.", true)] IRole role)
        {
            var grp = role;
            if (grp == null)
                throw new ArgumentException("You must specify a role you want to delete.");
            await grp.DeleteAsync();

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            var embedmod = this.PrepareEmbed("Role deleted", $"```The role '{grp.Name}' was deleted successfully.```", EmbedType.Success);
            embedmod.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Message.DeleteAsync();

            if (mod != null)
                await mod.SendMessageAsync("", false, embedmod.Build());
        }

        [Command("modrole", "Edits a role.", Aliases = "modifyrole;editrole", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task ModifyRole(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to modify.", true)] IRole role,
            [ArgumentParameter("Properties to set. Format is property=value. Example: color=36393e", true)] params string[] properties)
        {
            var grp = role;
            if (grp == null)
                throw new ArgumentException("You must specify a role you want to modify.");

            var par = properties
                .Select(xrs => xrs.Split('='))
                .ToDictionary(xrs => xrs[0], xrs => xrs[1]);

            var gpr = par.ContainsKey("permissions") ? ulong.Parse(par["permissions"]) : 0;
            var gcl = par.ContainsKey("color") ? Convert.ToUInt32(par["color"], 16) : 0;
            var ghs = par.ContainsKey("hoist") ? par["hoist"] == "true" : false;
            var gps = par.ContainsKey("position") ? int.Parse(par["position"]) : 0;
            var gmt = par.ContainsKey("mention") ? par["mention"] == "true" : false;

            // TODO: figure out editing mentionability
            await grp.ModifyAsync(x =>
            {
                if (par.ContainsKey("color"))
                    x.Color = new Color(gcl);
                if (par.ContainsKey("hoist"))
                    x.Hoist = ghs;
                if (par.ContainsKey("permissions"))
                    x.Permissions = new GuildPermissions(gpr);
                if (par.ContainsKey("position"))
                    x.Position = gps;
            });

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            var embedmod = this.PrepareEmbed("Role edit", $"```The role '{grp.Name}' was modified successfully.```", EmbedType.Success);
            embedmod.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Message.DeleteAsync();

            if (mod != null)
                await mod.SendMessageAsync("", false, embedmod.Build());
        }

        [Command("roleinfo", "Dumps all properties of a role.", Aliases = "rinfo;dumprole;printrole", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task RoleInfo(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to display.", true)] IRole role)
        {
            var grp = role;
            if (grp == null)
                throw new ArgumentException("You must supply a role.");

            var grl = grp as SocketRole;
            var gls = ctx.Guild as SocketGuild;

            var embed = this.PrepareEmbed("Role Info", null, EmbedType.Info);
            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl())
                .AddField("Name", grl.Name, true)
                .AddField("ID", grl.Id.ToString(), true)
                .AddField("Color", grl.Color.RawValue.ToString("X6"), true)
                .AddField("Hoisted?", grl.IsHoisted ? "Yes" : "No", true)
                .AddField("Mentionable?", grl.IsMentionable ? "Yes" : "No", true);

            var perms = new List<string>(23);
            if (grl.Permissions.Administrator)
                perms.Add("Administrator");
            if (grl.Permissions.AttachFiles)
                perms.Add("Can attach files");
            if (grl.Permissions.BanMembers)
                perms.Add("Can ban members");
            if (grl.Permissions.ChangeNickname)
                perms.Add("Can change nickname");
            if (grl.Permissions.Connect)
                perms.Add("Can use voice chat");
            if (grl.Permissions.CreateInstantInvite)
                perms.Add("Can create instant invites");
            if (grl.Permissions.DeafenMembers)
                perms.Add("Can deafen members");
            if (grl.Permissions.EmbedLinks)
                perms.Add("Can embed links");
            if (grl.Permissions.KickMembers)
                perms.Add("Can kick members");
            if (grl.Permissions.ManageChannels)
                perms.Add("Can manage channels");
            if (grl.Permissions.ManageMessages)
                perms.Add("Can manage messages");
            if (grl.Permissions.ManageNicknames)
                perms.Add("Can manage nicknames");
            if (grl.Permissions.ManageRoles)
                perms.Add("Can manage roles");
            if (grl.Permissions.ManageGuild)
                perms.Add("Can manage guild");
            if (grl.Permissions.MentionEveryone)
                perms.Add("Can mention everyone group");
            if (grl.Permissions.MoveMembers)
                perms.Add("Can move members between voice channels");
            if (grl.Permissions.MuteMembers)
                perms.Add("Can mute members");
            if (grl.Permissions.ReadMessageHistory)
                perms.Add("Can read message history");
            if (grl.Permissions.ReadMessages)
                perms.Add("Can read messages");
            if (grl.Permissions.SendMessages)
                perms.Add("Can send messages");
            if (grl.Permissions.SendTTSMessages)
                perms.Add("Can send TTS messages");
            if (grl.Permissions.Speak)
                perms.Add("Can speak");
            if (grl.Permissions.UseVAD)
                perms.Add("Can use voice activation");
            embed.AddField("Permissions", $"```{string.Join(", ", perms)}```");

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("listroles", "Lists all roles on the server.", Aliases = "lsroles", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task ListRoles(CommandContext ctx)
        {
            var grp = ctx.Guild.Roles;
            if (grp == null)
                return;

            var embed = this.PrepareEmbed("Role List", string.Format("Listing of all {0:#,##0} role{1} in this Guild.", grp.Count, grp.Count > 1 ? "s" : ""), EmbedType.Info);
            embed.AddField("Role list", $"```{string.Join(", ", grp.Select(xr => xr.Name))}```");

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("roleadd", "Adds users to a role.", Aliases = "groupadd", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task RoleAdd(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to add to.", true)] IRole role,
            [ArgumentParameter("Mentions of users to add tp the role.", true)] params IUser[] users)
        {
            var grp = role as SocketRole;
            if (grp == null)
                throw new ArgumentException("You must supply a role.");

            var usrs = users.Cast<SocketGuildUser>();
            if (usrs.Count() == 0)
                throw new ArgumentException("You must mention users you want to add to a role.");

            foreach (var usm in usrs)
                await usm.AddRoleAsync(grp);

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;
            var embedmod = this.PrepareEmbed("Role Member Add", string.Concat("User", usrs.Count() > 1 ? "s were" : " was", " added to the role."), EmbedType.Success);
            embedmod.AddField($"User{string.Concat(usrs.Count() > 1 ? "s" : "")} added to role: {grp.Name}", $"```{string.Join(", ", usrs.Select(xusr => xusr.Username))}```")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Message.DeleteAsync();

            if (mod != null)
                await mod.SendMessageAsync("", false, embedmod.Build());
        }

        [Command("roleremove", "Removes users from a role.", Aliases = "groupremove", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task RoleRemove(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to remove from.", true)] IRole role,
            [ArgumentParameter("Mentions of users to remove from the role.", true)] params IUser[] users)
        {
            var grp = role as SocketRole;
            if (grp == null)
                throw new ArgumentException("You must supply a role.");

            var usrs = users.Cast<SocketGuildUser>();
            if (usrs.Count() == 0)
                throw new ArgumentException("You must mention users you want to remove from a role.");

            foreach (var usm in usrs)
                await usm.RemoveRoleAsync(grp);

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;
            var embedmod = this.PrepareEmbed("Role Member Remove", string.Concat("User", usrs.Count() > 1 ? "s were" : " was", " removed from the role."), EmbedType.Success);
            embedmod.AddField($"User{string.Concat(usrs.Count() > 1 ? "s" : "")} removed from role: {grp.Name}", $"```{string.Join(", ", usrs.Select(xusr => xusr.Username))}```")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Message.DeleteAsync();

            if (mod != null)
                await mod.SendMessageAsync("", false, embedmod.Build());
        }
        #endregion

        #region User Management
        [Command("report", "Reports a user to guild moderators.", Aliases = "reportuser", CheckPermissions = false)]
        public async Task Report(CommandContext ctx,
            [ArgumentParameter("User to report.", true)] IUser user,
            [ArgumentParameter("Reason for report.", true)] params string[] reason)
        {
            await ctx.Message.DeleteAsync();

            var rep = user;
            if (rep == null)
                throw new ArgumentException("You must supply a user to report.");

            var rsn = string.Join(" ", reason);
            if (string.IsNullOrWhiteSpace(rsn))
                throw new ArgumentException("You need to supply a report reason.");

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            if (cnf.ReportUserChannel == null)
                throw new InvalidOperationException("This guild does not have report log configured.");

            var mod = await ctx.Guild.GetTextChannelAsync(cnf.ReportUserChannel.Value);
            var embed = this.PrepareEmbed("User report", $"{rep.Mention} ({rep.Username}) was reported.", EmbedType.Warning);
            embed.AddField("Reason", $"```{rsn}```")
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl())
                .WithAuthor(ctx.User);

            await mod.SendMessageAsync("", false, embed.Build());
        }

        [Command("mute", "Mutes users.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task Mute(CommandContext ctx,
            [ArgumentParameter("Duration of the mute. Use 0 for permanent. In format of: 0d0h0m (days, hours, minutes). Ex: mute 5m user", true)] TimeSpan duration,
            [ArgumentParameter("Mention of a user to mute.", true)] IUser user,
            [ArgumentParameter("Reason for mute.", false)] params string[] reason)
        {
            await ctx.Message.DeleteAsync();

            var userMute = user as SocketGuildUser;
            if (userMute == null)
                throw new ArgumentException("You must mention a user you want to mute.");

            var rsn = "";

            if(reason.Count() > 0)
                rsn = string.Join(" ", reason);

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;
            var rep = cnf != null && cnf.ReportUserChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ReportUserChannel.Value) : null;
            var mrl = cnf != null && cnf.MuteRole != null ? ctx.Guild.GetRole(cnf.MuteRole.Value) : null;

            if (mrl == null)
                throw new InvalidOperationException("Mute role is not configured. Specify via guildconfig.");

            var now = DateTime.UtcNow;
            var unt = duration != TimeSpan.Zero ? now + duration : DateTime.MaxValue.ToUniversalTime();
            var dsr = duration != TimeSpan.Zero ? string.Concat(duration.Days, " days, ", duration.Hours, " hours, ", duration.Minutes, " minutes") : "permanently";

            await userMute.AddRoleAsync(mrl);
            var moda = cnf.ModActions.FirstOrDefault(xma => xma.UserId == userMute.Id && xma.ActionType == ModActionType.Mute);
            if (moda != null)
                cnf.ModActions.Remove(moda);
            cnf.ModActions.Add(new ModAction { ActionType = ModActionType.Mute, Issuer = ctx.User.Id, Until = unt, UserId = userMute.Id });

            PoE_Bot.ConfigManager.SetGuildConfig(gid, cnf);

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User muted", $"{userMute.Mention} ({userMute.Username})", EmbedType.Warning);
                embedmod.AddField("Duration", dsr)
                    .WithAuthor(ctx.User)
                    .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

                if (!string.IsNullOrWhiteSpace(rsn))
                    embedmod.AddField("Reason", $"```{rsn}```");
                    
                await mod.SendMessageAsync("", false, embedmod.Build());

                if (rep != null)
                    await rep.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Mod Action", $"You were muted in the {ctx.Guild.Name} server.", EmbedType.Warning);
            embed.AddField("Duration", dsr)
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl())
                .WithFooter($"You can PM {ctx.User.Username} directly to resolve the issue.");

            if (!string.IsNullOrWhiteSpace(rsn))
                embed.AddField("Reason", $"```{rsn}```");
                
            await userMute.SendMessageAsync("", false, embed.Build());
        }

        [Command("unmute", "Unmutes users.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task Unmute(CommandContext ctx,
            [ArgumentParameter("Mentions of users to unmute.", true)] params IUser[] users)
        {
            await ctx.Message.DeleteAsync();

            var gls = ctx.Guild as SocketGuild;
            var uss = users.Cast<SocketGuildUser>();
            if (uss.Count() < 1)
                throw new ArgumentException("You must mention users you want to unmute.");

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;
            var mrl = cnf != null && cnf.MuteRole != null ? ctx.Guild.GetRole(cnf.MuteRole.Value) : null;

            if (mrl == null)
                throw new InvalidOperationException("Mute role is not configured. Specify via guildconfig.");

            foreach (var usm in uss)
            {
                await usm.RemoveRoleAsync(mrl);
                var moda = cnf.ModActions.FirstOrDefault(xma => xma.UserId == usm.Id && xma.ActionType == ModActionType.Mute);
                if (moda != null)
                    cnf.ModActions.Remove(moda);
            }

            PoE_Bot.ConfigManager.SetGuildConfig(gid, cnf);

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User unmutes", $"{uss.Count()} user" + (uss.Count() > 1 ? "s were " : " was ") + "unmuted.", EmbedType.Success);
                embedmod.AddField("Users unmuted", $"```{string.Join(", ", uss.Select(xus => xus.Username))}```")
                    .WithAuthor(ctx.User)
                    .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

                await mod.SendMessageAsync("", false, embedmod.Build());
            }
        }

        [Command("muteinfo", "Lists current mutes or displays information about specific mute.", Aliases = "listmutes;mutelist", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task MuteInfo(CommandContext ctx,
            [ArgumentParameter("Mention of muted user to view info for.", false)] IUser user)
        {
            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var minf = cnf.ModActions.Where(xma => xma.ActionType == ModActionType.Mute);
            if (minf.Count() == 0)
                throw new InvalidOperationException("There are no mutes in place.");

            var embed = this.PrepareEmbed("Mute Information", null, EmbedType.Info);
            if (user == null)
                embed.AddField("Current mutes", string.Concat(string.Join(", ", minf.Select(xmute => ctx.Guild.GetUserAsync(xmute.UserId).GetAwaiter().GetResult() != null ? ctx.Guild.GetUserAsync(xmute.UserId).GetAwaiter().GetResult().Mention : xmute.UserId.ToString())), " (", string.Join(", ", minf.Select(xmute => ctx.Guild.GetUserAsync(xmute.UserId).GetAwaiter().GetResult() != null ? ctx.Guild.GetUserAsync(xmute.UserId).GetAwaiter().GetResult().Username : xmute.UserId.ToString()))));
            else
            {
                var mute = minf.FirstOrDefault(xma => xma.UserId == user.Id);
                if (mute == null)
                    throw new InvalidProgramException("User is not in mute registry.");
                var isr = await ctx.Guild.GetUserAsync(mute.Issuer);

                embed.WithAuthor(isr)
                    .WithThumbnailUrl(isr.GetAvatarUrl())
                    .AddField("User", $"{user.Mention} ({user.Username})")
                    .AddField("Mod Responsible", isr != null ? string.Concat(isr.Mention, " (", isr.Username, ")") : "<unknown>")
                    .AddField("Issued (UTC)", mute.Issued.ToString("yyyy-MM-dd HH:mm:ss"))
                    .AddField("Active until (UTC)", mute.Until != DateTime.MaxValue.ToUniversalTime() ? mute.Until.ToString("yyyy-MM-dd HH:mm:ss") : "End of the Universe");
            }

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("kick", "Kicks user.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task Kick(CommandContext ctx,
            [ArgumentParameter("Mentions of user to kick.", true)] IUser user,
            [ArgumentParameter("Reason for the kick.", true)] params string[] reason)
        {
            await ctx.Message.DeleteAsync();

            var userKick = user as SocketGuildUser;
            if (userKick == null)
                throw new ArgumentException("You must mention a user you want to kick.");

            var rsn = string.Join(" ", reason);
            if (string.IsNullOrWhiteSpace(rsn))
                throw new ArgumentException("You need to supply a kick reason.");

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User kicked", $"{userKick.Mention} ({userKick.Username})", EmbedType.Error);
                embedmod.AddField("Reason", $"```{rsn}```")
                    .WithAuthor(ctx.User)
                    .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            await userKick.KickAsync(rsn);
        }

        [Command("ban", "Bans user.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.BanMembers)]
        public async Task Ban(CommandContext ctx,
            [ArgumentParameter("Duration of the ban. Use 0 for permanent. In format of: 0d0h0m (days, hours, minutes). Ex: ban 5m user", true)] TimeSpan duration,
            [ArgumentParameter("Mentions of user to ban.", true)] IUser user,
            [ArgumentParameter("Ban reason.", true)] params string[] reason)
        {
            await ctx.Message.DeleteAsync();

            var gls = ctx.Guild as SocketGuild;
            var userBan = user as SocketGuildUser;
            if (userBan == null)
                throw new ArgumentException("You must mention a user you want to ban.");

            var rsn = string.Join(" ", reason);
            if (string.IsNullOrWhiteSpace(rsn))
                throw new ArgumentException("You need to supply a ban reason.");

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            var now = DateTime.UtcNow;
            var unt = duration != TimeSpan.Zero ? now + duration : DateTime.MaxValue.ToUniversalTime();
            var dsr = duration != TimeSpan.Zero ? string.Concat(duration.Days, " days, ", duration.Hours, " hours, ", duration.Minutes, " minutes") : "permanently";

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User banned", $"{userBan.Mention} ({userBan.Username})", EmbedType.Error);
                embedmod.AddField("Duration", dsr)
                    .AddField("Reason", $"```{rsn}```")
                    .WithAuthor(ctx.User)
                    .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var moda = cnf.ModActions.FirstOrDefault(xma => xma.UserId == user.Id && xma.ActionType == ModActionType.HardBan);
            if (moda != null)
                cnf.ModActions.Remove(moda);

            cnf.ModActions.Add(new Config.ModAction { ActionType = ModActionType.HardBan, Issuer = ctx.User.Id, Until = unt, UserId = user.Id, Reason = rsn });
            PoE_Bot.ConfigManager.SetGuildConfig(gid, cnf);

            await gls.AddBanAsync(user, 0, rsn);
        }

        [Command("unban", "Unbans users. Consult listbans for user IDs.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.BanMembers)]
        public async Task Unban(CommandContext ctx,
            [ArgumentParameter("IDs of users to unban.", true)] params ulong[] users)
        {
            await ctx.Message.DeleteAsync();

            var gls = ctx.Guild as SocketGuild;
            var bns = await ctx.Guild.GetBansAsync();
            var uss = bns.Where(xban => users.Contains(xban.User.Id));
            if (uss.Count() < 1)
                throw new ArgumentException("You must list IDs of users you want to unban.");

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            foreach (var usm in uss)
            {
                await gls.RemoveBanAsync(usm.User.Id);
                var moda = cnf.ModActions.FirstOrDefault(xma => xma.UserId == usm.User.Id && xma.ActionType == ModActionType.HardBan);
                if (moda != null)
                    cnf.ModActions.Remove(moda);
            }
            PoE_Bot.ConfigManager.SetGuildConfig(gid, cnf);

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User unbans", $"{uss.Count()} user" + (uss.Count() > 1 ? "s were " : " was ") + "unbanned.", EmbedType.Success);
                embedmod.AddField("Users unbanned", $"```{string.Join(", ", uss.Select(xus => xus.User.Username))}```")
                    .WithAuthor(ctx.User)
                    .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

                await mod.SendMessageAsync("", false, embedmod.Build());
            }
        }

        [Command("baninfo", "Lists current bans or displays information about specific ban.", Aliases = "listbans;banlist", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.BanMembers)]
        public async Task BanInfo(CommandContext ctx,
            [ArgumentParameter("ID of banned user to view info for.", false)] ulong id)
        {
            var embed = this.PrepareEmbed("Ban Information", null, EmbedType.Info);
            if (id == 0)
            {
                var bans = await ctx.Guild.GetBansAsync();
                if (bans.Count == 0)
                    throw new InvalidOperationException("There are no users banned at this time.");

                embed.AddField("Current bans", string.Join(", ", bans.Select(xban => string.Concat(xban.User.Mention, " (", xban.User.Id, ")"))));
            }
            else
            {
                var bans = await ctx.Guild.GetBansAsync();
                var ban = bans.FirstOrDefault(xban => xban.User.Id == id);
                if (ban == null)
                    throw new ArgumentException("Invalid ban ID.");

                var gid = ctx.Guild.Id;
                var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
                var binf = cnf.ModActions.FirstOrDefault(xma => xma.UserId == ban.User.Id && xma.ActionType == ModActionType.HardBan);

                embed.AddField("User", $"{ban.User.Mention} ({ban.User.Username})")
                    .AddField("Id", ban.User.Id.ToString());

                if (binf != null)
                {
                    var isr = await ctx.Guild.GetUserAsync(binf.Issuer);

                    embed.WithAuthor(isr)
                        .WithThumbnailUrl(isr.GetAvatarUrl())
                        .AddField("Mod Responsible", isr != null ? string.Concat(isr.Mention, " (", isr.Username, ")") : "<unknown>")
                        .AddField("Reason", string.IsNullOrWhiteSpace(binf.Reason) ? "<unknown>" : binf.Reason)
                        .AddField("Issued (UTC)", binf.Issued.ToString("yyyy-MM-dd HH:mm:ss"))
                        .AddField("Active until (UTC)", binf.Until != DateTime.MaxValue.ToUniversalTime() ? binf.Until.ToString("yyyy-MM-dd HH:mm:ss") : "End of the Universe");
                }
            }

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("userinfo", "Displays information about users matching given name.", Aliases = "uinfo;userlist;ulist;userfind;ufind", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task UserInfo(CommandContext ctx,
            [ArgumentParameter("Mention of the user to display.", true)] IUser user)
        {
            var usr = user as SocketGuildUser;
            if (usr == null)
                throw new ArgumentNullException("Specified user is invalid.");

            var embed = this.PrepareEmbed("User Info", null, EmbedType.Info);
            if (!string.IsNullOrWhiteSpace(usr.GetAvatarUrl()))
                embed.ThumbnailUrl = usr.GetAvatarUrl();

            embed.AddField("Username", string.Concat(usr.Username, "#", usr.DiscriminatorValue), true)
                .AddField("Nickname", usr.Nickname ?? usr.Username, true)
                .AddField("Activity", usr.Activity != null ? usr.Activity.Type.ToString() + ": " + usr.Activity.Name : "None", true)
                .AddField("Status", usr.Status.ToString(), true)
                .AddField("Roles", $"```{string.Join(", ", usr.Roles.Select(xid => ctx.Guild.GetRole(xid.Id).Name))}```")
                .WithAuthor(ctx.User);

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }
        #endregion

        #region Guild, Channel, and Bot Management
        [Command("purgechannel", "Cleans a channel up, can specify All, Bot, or Self messages.", Aliases = "purge;purgech;chpurge;chanpurge;purgechan;clean;cleanup;prune", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageMessages)]
        public async Task PurgeChannel(CommandContext ctx,
            [ArgumentParameter("The optional number of messages to delete; defaults to 10.", false)] int count,
            [ArgumentParameter("The type of messages to delete - Self, Bot, User, or All; defaults to All.", false)] string delType,
            [ArgumentParameter("The user of the messages you want to delete, must set DelType to User.", false)] IUser user,
            [ArgumentParameter("The strategy to delete messages - Bulk or Manual; defaults to Bulk.", false)] string delStrategy)
        {
            var chp = ctx.Channel as ITextChannel;
            if (count == 1 && (delType.ToLower() == "self" || delType.ToLower() == "all" || (delType.ToLower() == "user" && user.Id == ctx.User.Id)))
                throw new InvalidOperationException("You are trying to delete your own message, but if you want to delete the message you last sent, you need to use a count of at least 2.");

            if (!string.IsNullOrEmpty(delType))
                if (user == null && delType.ToLower() == "user")
                    throw new InvalidOperationException("You are trying to delete a users message, but you didn't mention a user.");

            if (count == 0)
                count = 10;
            if (string.IsNullOrEmpty(delType))
                delType = "All";
            if (string.IsNullOrEmpty(delStrategy))
                delStrategy = "Bulk";
            if (user != null)
                delType = "User";

            DeleteType deleteType = (DeleteType)Enum.Parse(typeof(DeleteType), UppercaseFirst(delType));
            DeleteStrategy deleteStrategy = (DeleteStrategy)Enum.Parse(typeof(DeleteStrategy), UppercaseFirst(delStrategy));

            int index = 0;
            var deleteMessages = new List<IMessage>(count);
            var messages = ctx.Channel.GetMessagesAsync();

            await messages.ForEachAsync(async m =>
            {
                IEnumerable<IMessage> delete = null;
                if (deleteType == DeleteType.Self)
                    delete = m.Where(msg => msg.Author.Id == ctx.User.Id);
                else if(deleteType == DeleteType.User)
                    delete = m.Where(msg => msg.Author.Id == user.Id);
                else if (deleteType == DeleteType.Bot)
                    delete = m.Where(msg => msg.Author.IsBot);
                else if (deleteType == DeleteType.All)
                    delete = m;

                foreach (var msg in delete.OrderByDescending(msg => msg.Timestamp))
                {
                    if (index >= count) { await EndClean(chp, deleteMessages, deleteStrategy); return; }
                    deleteMessages.Add(msg);
                    index++;
                }
            });

            var gid = ctx.Guild.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Channel Purge", string.Concat(ctx.User.Mention, " (", ctx.User.Username, ") has purged ", count.ToString("#,##0"), (count > 25 ? " (Showing the last 25) " : " "), deleteType, " style messages from channel ", chp.Mention, " (", chp.Name, ") using ", deleteStrategy, " delete."), EmbedType.Info);

                int i = 0;
                foreach (var msg in deleteMessages)
                {
                    i++;
                    if (i < 26)
                    {
                        var content = msg.Content.Replace("`", "");
                        var attachments = msg.Attachments;
                        var embeds = msg.Embeds;

                        foreach (var att in attachments)
                            content = att.Url + "\n";
                        foreach (var embed in embeds)
                            content = embed.Title + "\n" + embed.Description;

                        content = (content.Length > 1024) ? content.Remove(1018) : content;
                        embedmod.AddField($"{msg.Author.Username}", $"```{content}```");
                    }
                }

                await mod.SendMessageAsync("", false, embedmod.Build());
            }
        }

        [Command("guildconfig", "Manages  configuration for this guild.", Aliases = "guildconf;config;conf;modconfig;modconf", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task ModConfig(CommandContext ctx,
            [ArgumentParameter("Setting to modify.", true)] string setting,
            [ArgumentParameter("New value.", true)] params string[] value)
        {
            if (string.IsNullOrWhiteSpace(setting))
                throw new ArgumentException("You need to specify setting and value.");
            var val = string.Join(" ", value);
            var embed = null as EmbedBuilder;

            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(ctx.Guild.Id);
            var mod = null as ITextChannel;

            switch (setting)
            {
                case "alllog":
                    if (ctx.Message.MentionedChannelIds.Count() > 0)
                    {
                        mod = await ctx.Guild.GetTextChannelAsync(ctx.Message.MentionedChannelIds.First());
                        var bot = PoE_Bot.Client.CurrentUser;
                        var prm = mod.GetPermissionOverwrite(bot);
                        if (prm != null && prm.Value.SendMessages == PermValue.Deny)
                            throw new InvalidOperationException(" cannot write to specified channel.");
                    }

                    val = mod != null ? mod.Mention : "<null>";
                    cnf.AllLogChannel = mod != null ? (ulong?)mod.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("All log was ", mod != null ? string.Concat("set to ", mod.Mention) : "removed", "."), EmbedType.Success);
                    break;
                case "modlog":
                    if (ctx.Message.MentionedChannelIds.Count() > 0)
                    {
                        mod = await ctx.Guild.GetTextChannelAsync(ctx.Message.MentionedChannelIds.First());
                        var bot = PoE_Bot.Client.CurrentUser;
                        var prm = mod.GetPermissionOverwrite(bot);
                        if (prm != null && prm.Value.SendMessages == PermValue.Deny)
                            throw new InvalidOperationException(" cannot write to specified channel.");
                    }

                    val = mod != null ? mod.Mention : "<null>";
                    cnf.ModLogChannel = mod != null ? (ulong?)mod.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Moderator log was ", mod != null ? string.Concat("set to ", mod.Mention) : "removed", "."), EmbedType.Success);
                    break;
                case "replog":
                    if (ctx.Message.MentionedChannelIds.Count() > 0)
                    {
                        mod = await ctx.Guild.GetTextChannelAsync(ctx.Message.MentionedChannelIds.First());
                        var bot = PoE_Bot.Client.CurrentUser;
                        var prm = mod.GetPermissionOverwrite(bot);
                        if (prm != null && prm.Value.SendMessages == PermValue.Deny)
                            throw new InvalidOperationException(" cannot write to specified channel.");
                    }

                    val = mod != null ? mod.Mention : "<null>";
                    cnf.ReportUserChannel = mod != null ? (ulong?)mod.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Reported log was ", mod != null ? string.Concat("set to ", mod.Mention) : "removed", "."), EmbedType.Success);
                    break;
                case "muterole":
                    var mute = null as IRole;
                    if (ctx.Message.MentionedRoleIds.Count > 0)
                        mute = ctx.Guild.GetRole(ctx.Message.MentionedRoleIds.First());
                    else
                        mute = ctx.Guild.Roles.FirstOrDefault(xr => xr.Name == string.Join(" ", value));

                    if (mute != null &&
                        (mute.Permissions.SendMessages ||
                        mute.Permissions.SendTTSMessages ||
                        mute.Permissions.AttachFiles ||
                        mute.Permissions.EmbedLinks ||
                        mute.Permissions.Speak ||
                        mute.Permissions.UseVAD ||
                        mute.Permissions.AddReactions ||
                        mute.Permissions.UseExternalEmojis ||
                        mute.Permissions.CreateInstantInvite))
                        throw new InvalidOperationException("Specified role cannot have any of the following permissions: Send Messages, Send TTS Messages, Attach Files, Embed Links, Speak, Use Voice Activitiy, Add Reactions, Use External Emojis, or Create Instant Invite.");

                    val = mute != null ? (mute.IsMentionable ? mute.Mention : mute.Name) : "<null>";
                    cnf.MuteRole = mute != null ? (ulong?)mute.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Mute role was ", mute != null ? string.Concat("set to ", val) : "removed", "."), EmbedType.Success);
                    break;
                case "pricerole":
                    var price = null as IRole;
                    if (ctx.Message.MentionedRoleIds.Count > 0)
                        price = ctx.Guild.GetRole(ctx.Message.MentionedRoleIds.First());
                    else
                        price = ctx.Guild.Roles.FirstOrDefault(xr => xr.Name == string.Join(" ", value));

                    val = price != null ? (price.IsMentionable ? price.Mention : price.Name) : "<null>";
                    cnf.PriceCheckerRole = price != null ? (ulong?)price.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Price Checker role was ", price != null ? string.Concat("set to ", val) : "removed", "."), EmbedType.Success);
                    break;
                case "prefix":
                    var pfix = val;
                    if (string.IsNullOrWhiteSpace(val))
                        pfix = null;

                    val = pfix ?? "<default>";
                    cnf.CommandPrefix = pfix;
                    embed = this.PrepareEmbed("Success", string.Concat("Command prefix was set to ", pfix != null ? string.Concat("**", pfix, "**") : "default", "."), EmbedType.Success);
                    break;
                case "deletecommands":
                    var delcmd = false;
                    if (val == "enable")
                        delcmd = true;

                    val = delcmd.ToString();
                    cnf.DeleteCommands = delcmd;
                    embed = this.PrepareEmbed("Success", string.Concat("Command message deletion is now **", delcmd ? "enabled" : "disabled", "**."), EmbedType.Success);
                    break;
                case "ruleschannel":
                    if (ctx.Message.MentionedChannelIds.Count() > 0)
                    {
                        mod = await ctx.Guild.GetTextChannelAsync(ctx.Message.MentionedChannelIds.First());
                        var bot = PoE_Bot.Client.CurrentUser;
                        var prm = mod.GetPermissionOverwrite(bot);
                        if (prm != null && prm.Value.SendMessages == PermValue.Deny)
                            throw new InvalidOperationException(" cannot write to specified channel.");
                    }

                    val = mod != null ? mod.Mention : "<null>";
                    cnf.RulesChannel = mod != null ? (ulong?)mod.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Rules channel was ", mod != null ? string.Concat("set to ", mod.Mention) : "removed", "."), EmbedType.Success);
                    break;
                case "game":
                    var game = val;
                    if (string.IsNullOrWhiteSpace(val))
                        game = null;

                    cnf.Game = game != null ? game : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Bot Game has been updated to ", game != null ? string.Concat("**", game, "**") : "null", "."), EmbedType.Success);
                    PoE_Bot.Client.DiscordClient.SetGameAsync(game).GetAwaiter().GetResult();
                    break;
                default:
                    throw new ArgumentException("Invalid setting specified.");
            }
                
            PoE_Bot.ConfigManager.SetGuildConfig(ctx.Guild.Id, cnf);

            if (cnf.ModLogChannel != null)
            {
                mod = await ctx.Guild.GetTextChannelAsync(cnf.ModLogChannel.Value);
                var embedmod = this.PrepareEmbed("Config updated", null, EmbedType.Info);
                embedmod.AddField("Setting", $"```{setting}```")
                    .AddField("Value", $"```{val}```")
                    .WithAuthor(ctx.User)
                    .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("confrules", "Sets the rules that will be posted in the channel set by the Guild Config.", Aliases = "configrules;setuprules;cr", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageMessages)]
        public async Task ConfigRules(CommandContext ctx,
            [ArgumentParameter("Rules set by their HTML Markdown, character limit set to 2,000 by Discord.", true)] params string[] rules)
        {
            if (rules.Count() == 0)
                throw new ArgumentException("You must supply rules.");

            var rulesVal = string.Join(" ", rules);
            if (rulesVal.Length > 2000)
                throw new InvalidOperationException("Rules exceed 2,000 characters, please revise and try again.");

            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(ctx.Guild.Id);
            cnf.Rules = rulesVal;

            PoE_Bot.ConfigManager.SetGuildConfig(ctx.Guild.Id, cnf);

            var embed = this.PrepareEmbed("Success", "Rules have been configured.", EmbedType.Success);
            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("postrules", "Posts the rules you've configured to the rules channel you setup in the Guild Config. Only done once, if you want to edit the rules, use confrules followed by editrules.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageMessages)]
        public async Task PostRules(CommandContext ctx)
        {
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(ctx.Guild.Id);

            if (string.IsNullOrEmpty(cnf.Rules))
                throw new InvalidOperationException("You have no rules to post, please use confrules to set them up.");

            if (cnf.RulesChannel == null)
                throw new InvalidOperationException("You have not configured a rules channel, please use guildconfig to set that up.");

            var chan = await ctx.Guild.GetChannelAsync((ulong)cnf.RulesChannel);
            var ruleChan = chan as IMessageChannel;
            var msg = await ruleChan.SendMessageAsync(cnf.Rules);

            await msg.AddReactionAsync(new Emoji("📰"));
            await msg.AddReactionAsync(new Emoji("\uD83C\uDDF8"));  // Standard  (S)
            await msg.AddReactionAsync(new Emoji("\uD83C\uDDED"));  // Hardcore  (H)
            await msg.AddReactionAsync(new Emoji("\uD83C\uDDE8"));  // Challenge (C)

            var embed = this.PrepareEmbed("Success", "Rules have been posted.", EmbedType.Success);
            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("editrules", "Edits the rules you've configured and posted to the rules channel.", Aliases = "edrules;erules;er", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageMessages)]
        public async Task EditRules(CommandContext ctx)
        {
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(ctx.Guild.Id);

            if (string.IsNullOrEmpty(cnf.Rules))
                throw new InvalidOperationException("You have no rules to post, please use confrules to set them up.");

            if (cnf.RulesChannel == null)
                throw new InvalidOperationException("You have not configured a rules channel, please use guildconfig to set that up.");

            var chan = await ctx.Guild.GetChannelAsync((ulong)cnf.RulesChannel);
            var ruleChan = chan as IMessageChannel;
            var msgs = await ruleChan.GetMessagesAsync().FlattenAsync();
            msgs = msgs.Where(x => x.Author.IsBot);

            if (msgs.Count() < 1)
                throw new InvalidOperationException("No messages found to edit, please make sure you've posted the rules to the channel.");

            foreach(IUserMessage msg in msgs)
                await msg.ModifyAsync(x => x.Content = cnf.Rules);

            var embed = this.PrepareEmbed("Success", "Rules have been edited.", EmbedType.Success);
            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }
        #endregion

        #region Miscellaneous Commands
        [Command("help", "Shows command list. Add command name to learn more.", Aliases = "halp;h", CheckPermissions = false)]
        public async Task Help(CommandContext ctx,
            [ArgumentParameter("Command to display help for.", false)] string command)
        {
            var embed = null as EmbedBuilder;
            if (string.IsNullOrWhiteSpace(command))
            {
                embed = this.PrepareEmbed(" Help", string.Format("List of all  commands, with aliases, and descriptions. All commands use the **{0}** prefix. Run **{0}help** command to learn more about a specific command.", PoE_Bot.CommandManager.GetPrefix(ctx.Guild.Id)), EmbedType.Info);
                foreach (var cmdg in PoE_Bot.CommandManager.GetCommands().GroupBy(xcmd => xcmd.Module))
                {
                    var err = "";
                    var xcmds = cmdg.Where(xcmd => (xcmd.Checker != null && xcmd.Checker.CanRun(xcmd, ctx.User, ctx.Message, ctx.Channel, ctx.Guild, out err)) || xcmd.Checker == null)
                        .OrderBy(xcmd => xcmd.Name)
                        .Select(xcmd => xcmd.Name);

                    if (xcmds.Any())
                        embed.AddField(string.Format("Commands registered by {0}", cmdg.Key.Name), $"```{string.Join(", ", xcmds)}```");
                }
            }
            else
            {
                var cmd = PoE_Bot.CommandManager.GetCommand(command);
                if (cmd == null)
                    throw new InvalidOperationException(string.Format("Command **{0}** does not exist", command));

                var err = null as string;
                if (cmd.Checker != null && !cmd.Checker.CanRun(cmd, ctx.User, ctx.Message, ctx.Channel, ctx.Guild, out err))
                    throw new ArgumentException("You can't run this command.");

                embed = this.PrepareEmbed(" Help", string.Format("**{0}** Command help", cmd.Name), EmbedType.Info);

                if (cmd.Checker != null && cmd.RequiredPermission != Permission.None)
                    embed.AddField("Required permission", $"```{cmd.RequiredPermission.ToString()}```");

                embed.AddField("Description", $"```{cmd.Description}```");

                if (cmd.Aliases != null && cmd.Aliases.Count > 0)
                    embed.AddField("Aliases", $"```{string.Join(", ", cmd.Aliases.Select(xa => xa))}```");

                if (cmd.Parameters.Count > 0)
                {
                    var sb1 = new StringBuilder();
                    var sb2 = new StringBuilder();
                    sb1.Append(PoE_Bot.CommandManager.GetPrefix(ctx.Guild.Id)).Append(cmd.Name).Append(' ');
                    foreach (var param in cmd.Parameters.OrderBy(xp => xp.Order))
                    {
                        sb1.Append(param.IsRequired ? '<' : '[').Append(param.Name).Append(param.IsCatchAll ? "..." : "").Append(param.IsRequired ? '>' : ']').Append(' ');
                        sb2.Append(param.IsRequired ? '<' : '[').Append(param.Name).Append(param.IsRequired ? '>' : ']').Append(": ").AppendLine(param.Description);
                    }
                    sb1.AppendLine();
                    sb1.Append(sb2.ToString());

                    embed.AddField("Usage (<*> = Required | [*] = Optional)", $"```css\n{sb1.ToString()}```");
                }
            }

            embed.WithFooter("Have questions? Tag @Server Nerd");

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("regionalize", "Turns text into Discord Regional Indicator Text", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task Regionalize(CommandContext ctx,
           [ArgumentParameter("Text to regionalize, only works with A-Z text.", true)] params string[] text)
        {
            var sb = new StringBuilder();
            var charCount = 0;

            foreach (var str in text)
            {
                var txt = str;
                txt = txt.ToLower();
                txt = Regex.Replace(txt, "[^a-zA-Z]", "");

                foreach (char c in txt)
                    sb.Append(":regional_indicator_" + c + ": ");

                sb.Append("   ");
            }

            var sbStr = sb.ToString();
            var finalText = sbStr.Split("   ", StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / 2000).Select(g => string.Join("   ", g)).ToList();

            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync(finalText[0]);
        }

        [Command("regionalizeclap", "Turns text into Discord Regional Indicator Text", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task RegionalizeClap(CommandContext ctx,
           [ArgumentParameter("Text to regionalize, only works with A-Z text.", true)] params string[] text)
        {
            var sb = new StringBuilder();
            var charCount = 0;

            sb.Append(":clap: ");
            foreach (var str in text)
            {
                var txt = str;
                txt = txt.ToLower();
                txt = Regex.Replace(txt, "[^a-zA-Z]", "");

                foreach (char c in txt)
                    sb.Append(":regional_indicator_" + c + ": ");

                sb.Append(" :clap: ");
            }

            var sbStr = sb.ToString().Trim();
            var finalText = sbStr.Split(" :clap: ", StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / 2000).Select(g => string.Join(" :clap: ", g)).ToList();

            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync(finalText[0]);
        }

        [Command("mock", "Turns text into Spongebob Mocking Meme.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task Mock(CommandContext ctx,
            [ArgumentParameter("Text to mock.", true)] params string[] text)
        {
            var meme = string.Concat(string.Join(" ", text).ToLower().AsEnumerable().Select((c, i) => i % 2 == 0 ? c : char.ToUpper(c)));
            IEnumerable<string> chunkedMeme = null;
            var charCount = 0;
            var maxChar = 33;

            if (meme.Length > maxChar)
                chunkedMeme = meme.Split(' ', StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / maxChar).Select(g => string.Join(" ", g));

            string path = @"img/mock.jpg";
            string savePath = @"img/output/stoP-THAt-RiGHT-nOW-" + DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + ".png";

            var info = new SKImageInfo(583, 411);
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;

                Stream fileStream = File.OpenRead(path);
                canvas.DrawColor(SKColors.White);

                using (var stream = new SKManagedStream(fileStream))
                using (var bitmap = SKBitmap.Decode(stream))
                using (var paint = new SKPaint())
                {
                    var textPaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        TextAlign = SKTextAlign.Center,
                        TextSize = 32,
                        FakeBoldText = true
                    };

                    canvas.DrawBitmap(bitmap, SKRect.Create(info.Width, info.Height), paint);

                    var coord = new SKPoint(info.Width / 2, 32);

                    if(meme.Length > maxChar)
                    {
                        foreach (var str in chunkedMeme)
                        {
                            canvas.DrawText(str, coord, textPaint);
                            coord.Offset(0, 42);
                        }
                    }
                    else
                        canvas.DrawText(meme, coord, textPaint);

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var streamImg = File.OpenWrite(savePath))
                    {
                        data.SaveTo(streamImg);
                    }
                }
            }

            var chn = ctx.Channel;
            var msg = ctx.Message;

            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendFileAsync(savePath);
        }

        [Command("clap", "Claps your message out.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task Clap(CommandContext ctx,
           [ArgumentParameter("Text to clap out.", true)] params string[] text)
        {
            var charCount = 0;
            var clappedText = string.Join(" ", text).Trim().Replace(" ", " :clap: ");
            var clappedFinal = clappedText.Insert(clappedText.Length, " :clap:").Insert(0, ":clap: ").Split(' ', StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / 2000).Select(g => string.Join(" ", g)).ToList();

            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync(clappedFinal[0]);
        }
        #endregion

        #region Debug Commands
        [Command("fulldump", "Performs a full environment dump. This command can only be used by Kyle Undefined.", CheckerId = "CoreDebugChecker", CheckPermissions = true)]
        public async Task FullDump(CommandContext ctx)
        {
            //  assembly data
            var _a = PoE_Bot.PluginManager.MainAssembly;
            var _n = _a.GetName();
            var _l = _a.Location;

            //  process data
            var _p = Process.GetCurrentProcess();
            var _m = _p.Modules;

            //  environment
            var _e = PlatformServices.Default;

            // dump holders
            var _sb0 = null as StringBuilder;

            // create the dump
            var embed = this.PrepareEmbed(" Diagnostic Information", "Full dump of all diagnostic information about this  instance.", EmbedType.Warning);

            // dump process info
            _sb0 = new StringBuilder();
            _sb0.AppendFormat("**PID**: {0}", _p.Id).AppendLine();
            _sb0.AppendFormat("**Name**: '{0}'", _p.ProcessName).AppendLine();
            //_sb0.AppendFormat("**Is 64-bit**: {0}", Environment.Is64BitProcess ? "Yes" : "No").AppendLine();
            _sb0.AppendFormat("**Is 64-bit**: {0}", IntPtr.Size == 8 ? "Yes" : "No").AppendLine();
            //_sb0.AppendFormat("**Command line**: {0} {1}", _p.StartInfo.FileName, _p.StartInfo.Arguments).AppendLine();
            _sb0.AppendFormat("**Started**: {0:yyyy-MM-dd HH:mm:ss} UTC", _p.StartTime.ToUniversalTime()).AppendLine();
            _sb0.AppendFormat("**Thread count**: {0:#,##0}", _p.Threads.Count).AppendLine();
            _sb0.AppendFormat("**Total processor time**: {0:c}", _p.TotalProcessorTime).AppendLine();
            _sb0.AppendFormat("**User processor time**: {0:c}", _p.UserProcessorTime).AppendLine();
            _sb0.AppendFormat("**Privileged processor time**: {0:c}", _p.PrivilegedProcessorTime).AppendLine();
            //_sb0.AppendFormat("**Handle count**: {0:#,##0}", _p.HandleCount).AppendLine();
            _sb0.AppendFormat("**Working set**: {0}", _p.WorkingSet64.ToSizeString()).AppendLine();
            _sb0.AppendFormat("**Virtual memory size**: {0}", _p.VirtualMemorySize64.ToSizeString()).AppendLine();
            _sb0.AppendFormat("**Paged memory size**: {0}", _p.PagedMemorySize64.ToSizeString()).AppendLine();
            _sb0.AppendFormat("**Module count**: {0:#,##0}", _m.Count);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = " Process";
                x.Value = _sb0.ToString();
            });

            // dump process module info
            //_sb0 = new StringBuilder();
            //foreach (ProcessModule _xm in _m)
            //{
            //    _sb0.AppendFormat("**Name**: {0}", _xm.ModuleName).AppendLine();
            //    _sb0.AppendFormat("**File name**: {0}", _xm.FileName).AppendLine();
            //    _sb0.AppendFormat("**File version**: {0}", _xm.FileVersionInfo.FileVersion).AppendLine();
            //    _sb0.AppendFormat("**Product version**: {0}", _xm.FileVersionInfo.ProductVersion).AppendLine();
            //    _sb0.AppendFormat("**Product name**: {0}", _xm.FileVersionInfo.ProductName).AppendLine();
            //    _sb0.AppendFormat("**Base address**: {0}", _xm.BaseAddress.ToPointerString()).AppendLine();
            //    _sb0.AppendFormat("**Entry point address**: {0}", _xm.EntryPointAddress.ToPointerString()).AppendLine();
            //    _sb0.AppendFormat("**Memory size**: {0}", _xm.ModuleMemorySize.ToSizeString()).AppendLine();
            //    _sb0.AppendLine("---------");
            //}
            //embed.AddField(x =>
            //{
            //    x.IsInline = false;
            //    x.Name = " Process Modules";
            //    x.Value = _sb0.ToString();
            //});

            // dump assembly info
            _sb0 = new StringBuilder();
            _sb0.AppendFormat("**Name**: {0}", _n.FullName).AppendLine();
            _sb0.AppendFormat("**Version**: {0}", _n.Version).AppendLine();
            _sb0.AppendFormat("**Location**: {0}", _l).AppendLine();
            _sb0.AppendFormat("**Code base**: {0}", _a.CodeBase).AppendLine();
            _sb0.AppendFormat("**Entry point**: {0}.{1}", _a.EntryPoint.DeclaringType, _a.EntryPoint.Name).AppendLine();
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = " Assembly";
                x.Value = _sb0.ToString();
            });

            // dump environment info
            _sb0 = new StringBuilder();
            //_sb0.AppendFormat("**OS platform**: {0}", Environment.OSVersion.Platform.ToString()).AppendLine();
            //_sb0.AppendFormat("**OS version**: {0} ({1}); Service Pack: {2}", Environment.OSVersion.Version, Environment.OSVersion.VersionString, Environment.OSVersion.ServicePack).AppendLine();
            //_sb0.AppendFormat("**OS is 64-bit**: {0}", Environment.Is64BitOperatingSystem ? "Yes" : "No").AppendLine();
            _sb0.AppendFormat("**.NET environment version**: {0}", _e.Application.RuntimeFramework.Version).AppendLine();
            _sb0.AppendFormat("**.NET is Mono**: {0}", Type.GetType("Mono.Runtime") != null ? "Yes" : "No").AppendLine();
            _sb0.AppendFormat("**Heap size**: {0}", GC.GetTotalMemory(false).ToSizeString());
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "OS and .NET";
                x.Value = _sb0.ToString();
            });

            // dump appdomain assembly info
            //foreach (var _xa in _s)
            //{
            //    _sb0 = new StringBuilder();
            //    _sb0.AppendFormat("Name: {0}", _xa.FullName).AppendLine();
            //    _sb0.AppendFormat("Version: {0}", _xa.GetName().Version).AppendLine();
            //    if (!_xa.IsDynamic)
            //    {
            //        _sb0.AppendFormat("Location: {0}", _xa.Location).AppendLine();
            //        _sb0.AppendFormat("Code base: {0}", _xa.CodeBase).AppendLine();
            //    }
            //    if (_xa.EntryPoint != null)
            //        _sb0.AppendFormat("Entry point: {0}.{1}", _xa.EntryPoint.DeclaringType, _xa.EntryPoint.Name).AppendLine();
            //    _sb0.AppendLine("---------");
            //}
            //embed.AddField(x =>
            //{
            //    x.IsInline = false;
            //    x.Name = " AppDomain Assemblies";
            //    x.Value = _sb0.ToString();
            //});
            //_sb0 = null;

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("hang", "Hangs current thread. This command can only be used by Kyle Undefined.", CheckerId = "CoreDebugChecker", CheckPermissions = true)]
        public async Task Hang(CommandContext ctx,
            [ArgumentParameter("How long to hang the thread for.", false)] int duration)
        {
            if (duration == 0)
                duration = 42510;

            await Task.Delay(duration);

            var embed = this.PrepareEmbed("Thread hang complete", string.Concat("Thread was hanged for ", duration.ToString("#,##0"), "ms."), EmbedType.Warning);
            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }
        #endregion

        #region Embeds
        private EmbedBuilder PrepareEmbed(EmbedType type)
        {
            var embed = new EmbedBuilder();
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
        #endregion

        #region Miscellaneous Functions
        static IEnumerable<string> ChunkString(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
        #endregion

        #region EndClean
        internal async Task EndClean(ITextChannel Chan, IEnumerable<IMessage> messages, DeleteStrategy strategy)
        {
            if (strategy == DeleteStrategy.Bulk)
                await Chan.DeleteMessagesAsync(messages);
            else if (strategy == DeleteStrategy.Manual)
            {
                foreach (var msg in messages.Cast<IUserMessage>())
                {
                    await msg.DeleteAsync();
                }
            }
        }
        #endregion

        #region Enums
        public enum DeleteType
        {
            Self = 0,
            Bot = 1,
            All = 2,
            User = 3
        }

        public enum DeleteStrategy
        {
            Bulk = 0,
            Manual = 1,
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
    }
}
