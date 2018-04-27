using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands.Permissions;
using PoE.Bot.Config;
using PoE.Bot.Extensions;
using Microsoft.Extensions.PlatformAbstractions;

namespace PoE.Bot.Commands
{
    internal class CommandModule : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Commands Module"; } }

        #region Role Manipulation
        [Command("mkrole", "Creates a new role.", Aliases = "makerole;createrole;mkgroup;makegroup;creategroup;gmk;gmake;gcreate", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task CreateRole(CommandContext ctx,
            [ArgumentParameter("Name of the new role.", true)] string name)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var grl = await gld.CreateRoleAsync(name, new GuildPermissions(0x0635CC01u), null, false);

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Role create", string.Concat(usr.Mention, " has created role **", grl.Name, "**."), EmbedType.Success);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Success", string.Format("Role **{0}** was created successfully.", grl.Name), EmbedType.Success);
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("rmrole", "Removes a role.", Aliases = "removerole;deleterole;delrole;rmgroup;removegroup;deletegroup;delgroup;gdel;gdelete;grm;gremove", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task DeleteRole(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to delete.", true)] IRole role)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var grp = role;
            if (grp == null)
                throw new ArgumentException("You must specify a role you want to delete.");
            await grp.DeleteAsync();

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Role remove", string.Concat(usr.Mention, " has removed role **", grp.Name, "**."), EmbedType.Error);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Success", string.Format("Role **{0}** was deleted successfully.", grp.Name), EmbedType.Success);
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("modrole", "Edits a role.", Aliases = "modifyrole;editrole;modgroup;modifygroup;editgroup;gmod;gmodify;gedit", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task ModifyRole(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to modify.", true)] IRole role,
            [ArgumentParameter("Properties to set. Format is property=value.", true)] params string[] properties)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

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

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Role modify", string.Concat(usr.Mention, " has modified role **", grp.Name, "**."), EmbedType.Info);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Success", string.Format("Role **{0}** was edited successfully.", grp.Name), EmbedType.Success);
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("roleinfo", "Dumps all properties of a role.", Aliases = "rinfo;dumprole;printrole;dumpgroup;printgroup;gdump", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task RoleInfo(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to display.", true)] IRole role)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;

            var grp = role;
            if (grp == null)
                throw new ArgumentException("You must supply a role.");

            var grl = grp as SocketRole;
            var gls = gld as SocketGuild;

            var embed = this.PrepareEmbed("Role Info", null, EmbedType.Info);

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Name";
                x.Value = grl.Name;
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "ID";
                x.Value = grl.Id.ToString();
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Color";
                x.Value = grl.Color.RawValue.ToString("X6");
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Hoisted?";
                x.Value = grl.IsHoisted ? "Yes" : "No";
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Mentionable?";
                x.Value = grl.IsMentionable ? "Yes" : "No";
            });

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
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Permissions";
                x.Value = string.Join(", ", perms);
            });

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("listroles", "Lists all roles on the server.", Aliases = "lsroles;lsgroups;listgroups;glist;gls", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task ListRoles(CommandContext ctx)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;

            var grp = gld.Roles;
            if (grp == null)
                return;

            var embed = this.PrepareEmbed("Role List", string.Format("Listing of all {0:#,##0} role{1} in this Guild.", grp.Count, grp.Count > 1 ? "s" : ""), EmbedType.Info);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Role list";
                x.Value = string.Join(", ", grp.Select(xr => string.Concat("**", xr.Name, "**")));
            });
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("roleadd", "Adds users to a role.", Aliases = "groupadd;ugadd", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task RoleAdd(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to add to.", true)] IRole role,
            [ArgumentParameter("Mentions of users to add tp the role.", true)] params IUser[] users)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var grp = role as SocketRole;
            if (grp == null)
                throw new ArgumentException("You must supply a role.");

            var usrs = users.Cast<SocketGuildUser>();
            if (usrs.Count() == 0)
                throw new ArgumentException("You must mention users you want to add to a role.");

            foreach (var usm in usrs)
                await usm.AddRoleAsync(grp);

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Role Member Add", string.Concat(usr.Mention, " has added ", string.Join(", ", usrs.Select(xusr => xusr.Mention)), " to role **", grp.Name, "**."), EmbedType.Success);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Success", string.Concat("User", usrs.Count() > 1 ? "s were" : " was", " added to the role."), EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Details";
                x.Value = string.Concat("The following user", usrs.Count() > 1 ? "s were" : " was", " added to role **", grp.Name, "**: ", string.Join(", ", usrs.Select(xusr => xusr.Mention)));
            });
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("roleremove", "Removes users from a role.", Aliases = "groupremove;ugremove;ugrm", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageRoles)]
        public async Task RoleRemove(CommandContext ctx,
            [ArgumentParameter("Name or mention of the role to remove from.", true)] IRole role,
            [ArgumentParameter("Mentions of users to remove from the role.", true)] params IUser[] users)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var grp = role as SocketRole;
            if (grp == null)
                throw new ArgumentException("You must supply a role.");

            var usrs = users.Cast<SocketGuildUser>();
            if (usrs.Count() == 0)
                throw new ArgumentException("You must mention users you want to remove from a role.");

            foreach (var usm in usrs)
                await usm.RemoveRoleAsync(grp);

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Role Member Remove", string.Concat(usr.Mention, " has removed ", string.Join(", ", usrs.Select(xusr => xusr.Mention)), " from role **", grp.Name, "**."), EmbedType.Error);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Success", string.Concat("User", usrs.Count() > 1 ? "s were" : " was", " removed from the role."), EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Details";
                x.Value = string.Concat("The following user", usrs.Count() > 1 ? "s were" : " was", " removed from role **", grp.Name, "**: ", string.Join(", ", usrs.Select(xusr => xusr.Mention)));
            });
            await chn.SendMessageAsync("", false, embed.Build());
        }
        #endregion

        #region User Management
        [Command("report", "Reports a user to guild moderators.", Aliases = "reportuser", CheckPermissions = false)]
        public async Task Report(CommandContext ctx,
            [ArgumentParameter("User to report.", true)] IUser user,
            [ArgumentParameter("Reason for report.", true)] params string[] reason)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var rep = user;
            if (rep == null)
                throw new ArgumentException("You must supply a user to report.");

            var rsn = string.Join(" ", reason);
            if (string.IsNullOrWhiteSpace(rsn))
                throw new ArgumentException("You need to supply a report reason.");

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            if (cnf.ReportUserChannel == null)
                throw new InvalidOperationException("This guild does not have report log configured.");

            var mod = await gld.GetTextChannelAsync(cnf.ReportUserChannel.Value);

            var embed = this.PrepareEmbed("User report", string.Concat(usr.Mention, " (", usr.Username, ") reported ", rep.Mention, " (", rep.Username, ")."), EmbedType.Warning);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Reason";
                x.Value = rsn;
            });

            await mod.SendMessageAsync("", false, embed.Build());
            await msg.DeleteAsync();
        }

        [Command("mute", "Mutes users.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task Mute(CommandContext ctx,
            [ArgumentParameter("Duration of the mute. Use 0 for permanent. In format of: 0d0h0m (days, hours, minutes). Ex: mute 5m user", true)] TimeSpan duration,
            [ArgumentParameter("Mention of a user to mute.", true)] IUser user,
            [ArgumentParameter("Reason for mute.", false)] params string[] reason)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var gls = gld as SocketGuild;

            var userMute = user as SocketGuildUser;
            if (userMute == null)
                throw new ArgumentException("You must mention a user you want to mute.");

            var rsn = "";

            if(reason.Count() > 0)
                rsn = string.Join(" ", reason);

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;
            var rep = cnf != null && cnf.ReportUserChannel != null ? await gld.GetTextChannelAsync(cnf.ReportUserChannel.Value) : null;
            var mrl = cnf != null && cnf.MuteRole != null ? gld.GetRole(cnf.MuteRole.Value) : null;

            if (mrl == null)
                throw new InvalidOperationException("Mute role is not configured. Specify via guildconfig.");

            var now = DateTime.UtcNow;
            var unt = duration != TimeSpan.Zero ? now + duration : DateTime.MaxValue.ToUniversalTime();
            var dsr = duration != TimeSpan.Zero ? string.Concat("for ", duration.Days, " days, ", duration.Hours, " hours, ", duration.Minutes, " minutes") : "permanently";

            if (!userMute.GuildPermissions.Administrator)
            {
                await userMute.AddRoleAsync(mrl);
                var moda = cnf.ModActions.FirstOrDefault(xma => xma.UserId == userMute.Id && xma.ActionType == ModActionType.Mute);
                if (moda != null)
                    cnf.ModActions.Remove(moda);
                cnf.ModActions.Add(new ModAction { ActionType = ModActionType.Mute, Issuer = usr.Id, Until = unt, UserId = userMute.Id });
            }

            PoE_Bot.ConfigManager.SetGuildConfig(gid, cnf);

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User muted", string.Concat(usr.Mention, " (", usr.Username, ") has muted ", userMute.Mention, " (", userMute.Username, ") ", dsr, "."), EmbedType.Warning);

                if (!string.IsNullOrWhiteSpace(rsn))
                {
                    embedmod.AddField(x =>
                    {
                        x.IsInline = false;
                        x.Name = "Reason";
                        x.Value = rsn;
                    });
                }
                    
                await mod.SendMessageAsync("", false, embedmod.Build());

                if (rep != null)
                    await rep.SendMessageAsync("", false, embedmod.Build());
            }

            await msg.DeleteAsync();

            var embed = this.PrepareEmbed("You were muted", dsr, EmbedType.Warning);

            if (!string.IsNullOrWhiteSpace(rsn))
            {
                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Reason";
                    x.Value = rsn;
                });
            }
                
            await userMute.SendMessageAsync("", false, embed.Build());
        }

        [Command("unmute", "Unmutes users.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task Unmute(CommandContext ctx,
            [ArgumentParameter("Mentions of users to unmute.", true)] params IUser[] users)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var gls = gld as SocketGuild;
            var uss = users.Cast<SocketGuildUser>();
            if (uss.Count() < 1)
                throw new ArgumentException("You must mention users you want to unmute.");

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;
            var mrl = cnf != null && cnf.MuteRole != null ? gld.GetRole(cnf.MuteRole.Value) : null;

            if (mrl == null)
                throw new InvalidOperationException("Mute role is not configured. Specify via guildconfig.");

            uss = uss.Where(xus => !xus.GuildPermissions.Administrator);
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
                var embedmod = this.PrepareEmbed("User unmutes", string.Concat(usr.Mention, " (", usr.Username, ") has unmuted ", string.Join(", ", uss.Select(xus => xus.Mention)), " (", string.Join(", ", uss.Select(xus => xus.Username)), ")."), EmbedType.Success);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            await msg.DeleteAsync();
        }

        [Command("muteinfo", "Lists current mutes or displays information about specific mute.", Aliases = "listmutes;mutelist", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task MuteInfo(CommandContext ctx,
            [ArgumentParameter("Mention of muted user to view info for.", false)] IUser user)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var minf = cnf.ModActions.Where(xma => xma.ActionType == ModActionType.Mute);
            if (minf.Count() == 0)
                throw new InvalidOperationException("There are no mutes in place.");

            var embed = this.PrepareEmbed("Mute Information", null, EmbedType.Info);
            if (user == null)
            {
                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Current mutes";
                    x.Value = string.Concat(string.Join(", ", minf.Select(xmute => gld.GetUserAsync(xmute.UserId).GetAwaiter().GetResult() != null ? gld.GetUserAsync(xmute.UserId).GetAwaiter().GetResult().Mention : xmute.UserId.ToString())), " (", string.Join(", ", minf.Select(xmute => gld.GetUserAsync(xmute.UserId).GetAwaiter().GetResult() != null ? gld.GetUserAsync(xmute.UserId).GetAwaiter().GetResult().Username : xmute.UserId.ToString())), ")");
                });
            }
            else
            {
                var mute = minf.FirstOrDefault(xma => xma.UserId == user.Id);
                if (mute == null)
                    throw new InvalidProgramException("User is not in mute registry.");
                var isr = await gld.GetUserAsync(mute.Issuer);

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "User";
                    x.Value = string.Concat(user.Mention, " (", user.Username, ")");
                });

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "Id";
                    x.Value = user.Id.ToString();
                });

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "Mod responsible";
                    x.Value = isr != null ? string.Concat(isr.Mention, " (", isr.Username, ")") : "<unknown>";
                });

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "Issued (UTC)";
                    x.Value = mute.Issued.ToString("yyyy-MM-dd HH:mm:ss");
                });

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "Active until (UTC)";
                    x.Value = mute.Until != DateTime.MaxValue.ToUniversalTime() ? mute.Until.ToString("yyyy-MM-dd HH:mm:ss") : "End of the Universe";
                });
            }

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("kick", "Kicks users.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.KickMembers)]
        public async Task Kick(CommandContext ctx,
            [ArgumentParameter("Mentions of users to kick.", true)] params IUser[] users)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var gls = gld as SocketGuild;
            var uss = users.Cast<SocketGuildUser>();
            if (uss.Count() < 1)
                throw new ArgumentException("You must mention users you want to kick.");

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            uss = uss.Where(xus => !xus.GuildPermissions.Administrator);
            foreach (var usm in uss)
                await usm.KickAsync();

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User kicks", string.Concat(usr.Mention, " has kicked ", string.Join(", ", uss.Select(xus => xus.Mention)), "."), EmbedType.Error);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed(EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "User Kicked";
                x.Value = string.Concat("The following user", uss.Count() > 1 ? "s were" : " was", " kicked: ", string.Join(", ", uss.Select(xusr => xusr.Mention)), ".");
            });

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("ban", "Bans users.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.BanMembers)]
        public async Task Ban(CommandContext ctx,
            [ArgumentParameter("Duration of the ban. Use 0 for permanent.", true)] TimeSpan duration,
            [ArgumentParameter("Ban reason.", true)] string reason,
            [ArgumentParameter("Mentions of users to ban.", true)] params IUser[] users)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var gls = gld as SocketGuild;
            var uss = users.Cast<SocketGuildUser>();
            if (uss.Count() < 1)
                throw new ArgumentException("You must mention users you want to ban.");

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            var now = DateTime.UtcNow;
            var unt = duration != TimeSpan.Zero ? now + duration : DateTime.MaxValue.ToUniversalTime();
            var dsr = duration != TimeSpan.Zero ? string.Concat("for ", duration.Days, " days, ", duration.Hours, " hours, ", duration.Minutes, " minutes") : "permanently";
            uss = uss.Where(xus => !xus.GuildPermissions.Administrator);
            foreach (var usm in uss)
            {
                await gls.AddBanAsync(usm);
                var moda = cnf.ModActions.FirstOrDefault(xma => xma.UserId == usm.Id && xma.ActionType == ModActionType.HardBan);
                if (moda != null)
                    cnf.ModActions.Remove(moda);
                cnf.ModActions.Add(new Config.ModAction { ActionType = ModActionType.HardBan, Issuer = usr.Id, Until = unt, UserId = usm.Id, Reason = reason });
            }
            PoE_Bot.ConfigManager.SetGuildConfig(gid, cnf);

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("User bans", string.Concat(usr.Mention, " has banned ", string.Join(", ", uss.Select(xus => xus.Mention)), " ", dsr, ". Reason: ", reason, "."), EmbedType.Error);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed(EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "User Banned";
                x.Value = string.Concat("The following user", uss.Count() > 1 ? "s were" : " was", " banned ", dsr, ": ", string.Join(", ", uss.Select(xusr => xusr.Mention)), ". Reason: ", reason, ".");
            });

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("unban", "Unbans users. Consult listbans for user IDs.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.BanMembers)]
        public async Task Unban(CommandContext ctx,
            [ArgumentParameter("IDs of users to unban.", true)] params ulong[] users)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var gls = gld as SocketGuild;
            var bns = await gld.GetBansAsync();
            var uss = bns.Where(xban => users.Contains(xban.User.Id));
            if (uss.Count() < 1)
                throw new ArgumentException("You must list IDs of users you want to unban.");

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

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
                var embedmod = this.PrepareEmbed("User unbans", string.Concat(usr.Mention, " has unbanned ", string.Join(", ", uss.Select(xus => xus.User.Mention)), "."), EmbedType.Info);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed(EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "User Unbanned";
                x.Value = string.Concat("The following user", uss.Count() > 1 ? "s were" : " was", " unbanned: ", string.Join(", ", uss.Select(xusr => xusr.User.Mention)));
            });

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("baninfo", "Lists current bans or displays information about specific ban.", Aliases = "listbans;banlist", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.BanMembers)]
        public async Task BanInfo(CommandContext ctx,
            [ArgumentParameter("ID of banned user to view info for.", false)] ulong id)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var embed = this.PrepareEmbed("Ban Information", null, EmbedType.Info);
            if (id == 0)
            {
                var bans = await gld.GetBansAsync();
                if (bans.Count == 0)
                    throw new InvalidOperationException("There are no users banned at this time.");

                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Current bans";
                    x.Value = string.Join(", ", bans.Select(xban => string.Concat(xban.User.Mention, " (", xban.User.Id, ")")));
                });
            }
            else
            {
                var bans = await gld.GetBansAsync();
                var ban = bans.FirstOrDefault(xban => xban.User.Id == id);
                if (ban == null)
                    throw new ArgumentException("Invalid ban ID.");

                var gid = gld.Id;
                var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
                var binf = cnf.ModActions.FirstOrDefault(xma => xma.UserId == ban.User.Id && xma.ActionType == ModActionType.HardBan);

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "User";
                    x.Value = ban.User.Mention;
                });

                embed.AddField(x =>
                {
                    x.IsInline = true;
                    x.Name = "Id";
                    x.Value = ban.User.Id.ToString();
                });

                if (binf != null)
                {
                    var isr = await gld.GetUserAsync(binf.Issuer);

                    embed.AddField(x =>
                    {
                        x.IsInline = true;
                        x.Name = "Mod responsible";
                        x.Value = isr != null ? isr.Mention : "<unknown>";
                    });

                    embed.AddField(x =>
                    {
                        x.IsInline = true;
                        x.Name = "Reason";
                        x.Value = string.IsNullOrWhiteSpace(binf.Reason) ? "<unknown>" : binf.Reason;
                    });

                    embed.AddField(x =>
                    {
                        x.IsInline = true;
                        x.Name = "Issued (UTC)";
                        x.Value = binf.Issued.ToString("yyyy-MM-dd HH:mm:ss");
                    });

                    embed.AddField(x =>
                    {
                        x.IsInline = true;
                        x.Name = "Active until (UTC)";
                        x.Value = binf.Until != DateTime.MaxValue.ToUniversalTime() ? binf.Until.ToString("yyyy-MM-dd HH:mm:ss") : "End of the Universe";
                    });
                }
            }

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("userinfo", "Displays information about users matching given name.", Aliases = "uinfo;userlist;ulist;userfind;ufind", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task UserInfo(CommandContext ctx,
            [ArgumentParameter("Mention of the user to display.", true)] IUser user)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;

            var usr = user as SocketGuildUser;
            if (usr == null)
                throw new ArgumentNullException("Specified user is invalid.");

            var embed = this.PrepareEmbed(EmbedType.Info);
            if (!string.IsNullOrWhiteSpace(usr.GetAvatarUrl()))
                embed.ThumbnailUrl = usr.GetAvatarUrl();

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Username";
                x.Value = string.Concat("**", usr.Username, "**#", usr.DiscriminatorValue);
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "ID";
                x.Value = usr.Id.ToString();
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Nickname";
                x.Value = usr.Nickname ?? usr.Username;
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Status";
                x.Value = usr.Status.ToString();
            });

            embed.AddField(x =>
            {
                x.IsInline = true;
                x.Name = "Roles";
                x.Value = string.Join(", ", usr.Roles.Select(xid => string.Concat("**", gld.GetRole(xid.Id).Name, "**")));
            });

            await chn.SendMessageAsync("", false, embed.Build());
        }
        #endregion

        #region Guild, Channel, and Bot Management
        [Command("prune", "Prunes a channel by deleting up to 100 messages, can single out a certain user.", Aliases = "delete;dm;deletemessages", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageMessages)]
        public async Task Prune(CommandContext ctx,
            [ArgumentParameter("How many messages to prune.", true)] int count,
            [ArgumentParameter("Mention of the user to prune.", false)] IUser user = null)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;
            var chp = chn as ITextChannel;

            if (count < 1)
                throw new ArgumentNullException("Specified amount is invalid.");

            if (count > 100)
                count = 100;

            TimeSpan twoWeeks = TimeSpan.FromDays(14);
            Func<IMessage, bool> predicate;
            IMessage[] msgs;
            IMessage lastMessage = null;
            int totMsgs = 0;

            if (user != null)
            {
                predicate = (m => m.Author.Id == user.Id && DateTime.UtcNow - m.CreatedAt < twoWeeks);
            }
            else
            {
                predicate = (x => true);
            }

            msgs = (await chn.GetMessagesAsync(50).FlattenAsync()).Where(predicate).Take(count).ToArray();
            var allDeleted = new List<IMessage>();

            while (count > 0 && msgs.Any())
            {
                totMsgs = totMsgs + msgs.Count();
                lastMessage = msgs[msgs.Length - 1];

                var bulkDeletable = new List<IMessage>();
                var singleDeletable = new List<IMessage>();

                foreach (var x in msgs)
                {
                    if (DateTime.UtcNow - x.CreatedAt < twoWeeks)
                        bulkDeletable.Add(x);
                    else
                        singleDeletable.Add(x);
                }

                if (bulkDeletable.Count > 0)
                    foreach (var x in bulkDeletable)
                        allDeleted.Add(x);

                foreach (var x in singleDeletable)
                    allDeleted.Add(x);

                if (bulkDeletable.Count > 0)
                    await Task.WhenAll(Task.Delay(1000), chp.DeleteMessagesAsync(bulkDeletable)).ConfigureAwait(false);

                var i = 0;
                foreach (var group in singleDeletable.GroupBy(x => ++i / (singleDeletable.Count / 5)))
                    await Task.WhenAll(Task.Delay(1000), Task.WhenAll(group.Select(x => x.DeleteAsync()))).ConfigureAwait(false);

                //this isn't good, because this still work as if i want to remove only specific user's messages from the last
                //100 messages, Maybe this needs to be reduced by msgs.Length instead of 100
                count -= 50;
                if (count > 0)
                    msgs = (await chn.GetMessagesAsync(lastMessage, Direction.Before, 50).FlattenAsync()).Where(predicate).Take(count).ToArray();
            }

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var messagesDeleted = new StringBuilder();

                foreach (var x in allDeleted)
                    messagesDeleted.Append(string.Concat(x.Author, ":\n\"", x.Content, "\"\n", x.Timestamp.ToString("G"), "\n\n"));

                var embedChunks = ChunkString(messagesDeleted.ToString(), 1018);

                foreach(var chunk in embedChunks)
                {
                    var embedmod = this.PrepareEmbed("Message Prune", string.Concat(usr.Mention, " (", usr.Username, ") has pruned ", totMsgs.ToString("#,##0"), user != null ? string.Concat(" of ", user.Mention, " (", usr.Username, ")'s") : "", " messages from channel ", chp.Mention, " (", chp.Name, ")."), EmbedType.Info);

                    chunk.Insert(0, "```");
                    chunk.Insert(chunk.Length, "```");

                    embedmod.AddField(x =>
                    {
                        x.IsInline = false;
                        x.Name = "Messages";
                        x.Value = chunk;
                    });

                    await mod.SendMessageAsync("", false, embedmod.Build());
                }
            }
        }

        [Command("purgechannel", "Cleans a channel up, can specify All, Bot, or Self messages.", Aliases = "purge;purgech;chpurge;chanpurge;purgechan;clean;cleanup;", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.ManageMessages)]
        public async Task PurgeChannel(CommandContext ctx,
            [ArgumentParameter("The optional number of messages to delete; defaults to 10.", false)] int count,
            [ArgumentParameter("The type of messages to delete - Self, Bot, or All; defaults to Self.", false)] string delType,
            [ArgumentParameter("The strategy to delete messages - Bulk or Manual; defaults to Bulk.", false)] string delStrategy)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var usr = ctx.User;
            var chp = chn as ITextChannel;

            if (count == 0)
                count = 10;
            if (string.IsNullOrEmpty(delType))
                delType = "Self";
            if (string.IsNullOrEmpty(delStrategy))
                delStrategy = "Bulk";

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

            var gid = gld.Id;
            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gid);
            var mod = cnf != null && cnf.ModLogChannel != null ? await gld.GetTextChannelAsync(cnf.ModLogChannel.Value) : null;

            if (mod != null)
            {
                var embedmod = this.PrepareEmbed("Channel Purge", string.Concat(usr.Mention, " (", usr.Username, ") has purged ", count.ToString("#,##0"), " ", deleteType, " style messages from channel ", chp.Mention, " (", chp.Name, ") using ", deleteStrategy, " delete."), EmbedType.Info);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            var embed = this.PrepareEmbed("Success", string.Format("Purged {0:#,##0} {1} style message{2} from channel {3} using {4} delete.", count, deleteType, count > 1 ? "s" : "", chp.Mention, deleteStrategy), EmbedType.Success);
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("guildconfig", "Manages  configuration for this guild.", Aliases = "guildconf;config;conf;modconfig;modconf", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task ModConfig(CommandContext ctx,
            [ArgumentParameter("Setting to modify.", true)] string setting,
            [ArgumentParameter("New value.", true)] params string[] value)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            if (string.IsNullOrWhiteSpace(setting))
                throw new ArgumentException("You need to specify setting and value.");
            var val = string.Join(" ", value);
            var embed = null as EmbedBuilder;

            var cnf = PoE_Bot.ConfigManager.GetGuildConfig(gld.Id);
            var mod = null as ITextChannel;

            switch (setting)
            {
                case "modlog":
                    if (msg.MentionedChannelIds.Count() > 0)
                    {
                        mod = await gld.GetTextChannelAsync(msg.MentionedChannelIds.First());
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
                    if (msg.MentionedChannelIds.Count() > 0)
                    {
                        mod = await gld.GetTextChannelAsync(msg.MentionedChannelIds.First());
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
                    if (msg.MentionedRoleIds.Count > 0)
                        mute = gld.GetRole(msg.MentionedRoleIds.First());
                    else
                        mute = gld.Roles.FirstOrDefault(xr => xr.Name == string.Join(" ", value));

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
                    if (msg.MentionedRoleIds.Count > 0)
                        price = gld.GetRole(msg.MentionedRoleIds.First());
                    else
                        price = gld.Roles.FirstOrDefault(xr => xr.Name == string.Join(" ", value));

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
                    if (msg.MentionedChannelIds.Count() > 0)
                    {
                        mod = await gld.GetTextChannelAsync(msg.MentionedChannelIds.First());
                        var bot = PoE_Bot.Client.CurrentUser;
                        var prm = mod.GetPermissionOverwrite(bot);
                        if (prm != null && prm.Value.SendMessages == PermValue.Deny)
                            throw new InvalidOperationException(" cannot write to specified channel.");
                    }

                    val = mod != null ? mod.Mention : "<null>";
                    cnf.RulesChannel = mod != null ? (ulong?)mod.Id : null;
                    embed = this.PrepareEmbed("Success", string.Concat("Rules channel was ", mod != null ? string.Concat("set to ", mod.Mention) : "removed", "."), EmbedType.Success);
                    break;
                default:
                    throw new ArgumentException("Invalid setting specified.");
            }
                
            PoE_Bot.ConfigManager.SetGuildConfig(gld.Id, cnf);

            if (cnf.ModLogChannel != null)
            {
                mod = await gld.GetTextChannelAsync(cnf.ModLogChannel.Value);
                var embedmod = this.PrepareEmbed("Config updated", string.Concat(usr.Mention, " has has updated guild setting **", setting, "** with value **", val, "**."), EmbedType.Info);
                await mod.SendMessageAsync("", false, embedmod.Build());
            }

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("setgame", "Sets the game the Bot is playing", Aliases = "sg;setbotgame;setg", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task SetBotGame(CommandContext ctx,
            [ArgumentParameter("Game to set.", true)] params string[] game)
        {
            if (game.Count() == 0)
                throw new ArgumentException("You must put in a game.");

            var chn = ctx.Channel;
            var val = string.Join(" ", game);
            var embed = null as EmbedBuilder;

            await PoE_Bot.Client.DiscordClient.SetGameAsync(val);
            embed = this.PrepareEmbed("Success", "Bot Game has been updated to **" + val + "**.", EmbedType.Success);

            await chn.SendMessageAsync("", false, embed.Build());
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
            var msgs = await ruleChan.GetMessagesAsync(1).FlattenAsync();

            if (msgs.Count() < 1)
                throw new InvalidOperationException("No messages found to edit, please make sure you've posted the rules to the channel.");

            foreach(IUserMessage msg in msgs)
                await msg.ModifyAsync(x => x.Content = cnf.Rules);

            var embed = this.PrepareEmbed("Success", "Rules have been edited.", EmbedType.Success);
            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }
        #endregion

        #region Miscellaneous Commands
        [Command("help", "Shows command list. Add command name to learn more.", Aliases = "halp;h", CheckPermissions = false)]
        public async Task Help(CommandContext ctx,
            [ArgumentParameter("Command to display help for.", false)] string command)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var embed = null as EmbedBuilder;
            if (string.IsNullOrWhiteSpace(command))
            {
                embed = this.PrepareEmbed(" Help", string.Format("List of all  commands, with aliases, and descriptions. All commands use the **{0}** prefix. Run **{0}help** command to learn more about a specific command.", PoE_Bot.CommandManager.GetPrefix(gld.Id)), EmbedType.Info);
                foreach (var cmdg in PoE_Bot.CommandManager.GetCommands().GroupBy(xcmd => xcmd.Module))
                {
                    var err = "";
                    var xcmds = cmdg.Where(xcmd => (xcmd.Checker != null && xcmd.Checker.CanRun(xcmd, usr, msg, chn, gld, out err)) || xcmd.Checker == null)
                        .OrderBy(xcmd => xcmd.Name)
                        .Select(xcmd => string.Concat("**", xcmd.Name, "**"));

                    if (xcmds.Any())
                    {
                        embed.AddField(x =>
                        {
                            x.IsInline = false;
                            x.Name = string.Format("Commands registered by {0}", cmdg.Key.Name);
                            x.Value = string.Join(", ", xcmds);
                        });
                    }
                }
            }
            else
            {
                var cmd = PoE_Bot.CommandManager.GetCommand(command);
                if (cmd == null)
                    throw new InvalidOperationException(string.Format("Command **{0}** does not exist", command));
                var err = null as string;
                if (cmd.Checker != null && !cmd.Checker.CanRun(cmd, usr, msg, chn, gld, out err))
                    throw new ArgumentException("You can't run this command.");

                embed = this.PrepareEmbed(" Help", string.Format("**{0}** Command help", cmd.Name), EmbedType.Info);

                if (cmd.Checker != null && cmd.RequiredPermission != Permission.None)
                {
                    embed.AddField(x =>
                    {
                        x.IsInline = false;
                        x.Name = "Required permission";
                        x.Value = cmd.RequiredPermission.ToString();
                    });
                }

                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Description";
                    x.Value = cmd.Description;
                });

                if (cmd.Aliases != null && cmd.Aliases.Count > 0)
                {
                    embed.AddField(x =>
                    {
                        x.IsInline = false;
                        x.Name = "Aliases";
                        x.Value = string.Join(", ", cmd.Aliases.Select(xa => string.Concat("**", xa, "**")));
                    });
                }

                if (cmd.Parameters.Count > 0)
                {
                    var sb1 = new StringBuilder();
                    var sb2 = new StringBuilder();
                    sb1.Append(PoE_Bot.CommandManager.GetPrefix(gld.Id)).Append(cmd.Name).Append(' ');
                    foreach (var param in cmd.Parameters.OrderBy(xp => xp.Order))
                    {
                        sb1.Append(param.IsRequired ? '<' : '[').Append(param.Name).Append(param.IsCatchAll ? "..." : "").Append(param.IsRequired ? '>' : ']').Append(' ');
                        sb2.Append("**").Append(param.Name).Append("**: ").AppendLine(param.Description);
                    }
                    sb1.AppendLine();
                    sb1.Append(sb2.ToString());

                    embed.AddField(x =>
                    {
                        x.IsInline = false;
                        x.Name = "Usage";
                        x.Value = sb1.ToString();
                    });
                }
            }

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("regionalize", "Turns text into Discord Regional Indicator Text", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task Regionalize(CommandContext ctx,
           [ArgumentParameter("Text to regionalize, only works with A-Z text.", true)] params string[] text)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;
            var sb = new StringBuilder();

            foreach (var str in text)
            {
                var txt = str;
                txt = txt.ToLower();
                txt = Regex.Replace(txt, "[^a-zA-Z]", "");

                foreach (char c in txt)
                {
                    sb.Append(":regional_indicator_" + c + ": ");
                }

                sb.Append("   ");
            }

            var finalText = sb.ToString();
            if (finalText.Length > 2000)
                finalText = finalText.Remove(2000);

            await chn.SendMessageAsync(finalText);
        }
        #endregion

        #region Debug Commands
        [Command("fulldump", "Performs a full environment dump. This command can only be used by Kyle Undefined.", CheckerId = "CoreDebugChecker", CheckPermissions = true)]
        public async Task FullDump(CommandContext ctx)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;

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

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("hang", "Hangs current thread. This command can only be used by Kyle Undefined.", CheckerId = "CoreDebugChecker", CheckPermissions = true)]
        public async Task Hang(CommandContext ctx,
            [ArgumentParameter("How long to hang the thread for.", false)] int duration)
        {
            var chn = ctx.Channel;

            if (duration == 0)
                duration = 42510;

            await Task.Delay(duration);

            var embed = this.PrepareEmbed("Thread hang complete", string.Concat("Thread was hanged for ", duration.ToString("#,##0"), "ms."), EmbedType.Warning);
            await chn.SendMessageAsync("", false, embed.Build());
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
            embed.Timestamp = DateTime.Now;
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
            All = 2
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
