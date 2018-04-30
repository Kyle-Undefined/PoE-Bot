using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Plugin.Trials
{
    public class TrialCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.Trials Module"; } }

        [Command("trialadd", "Add a Trial of Ascendancy that you're looking for to be notified when someone has found it. Can be used like: trialadd burning or trialadd all for all trials", Aliases = "tadd;addtrial;addt", CheckPermissions = false)]
        public async Task TrialAdd(CommandContext ctx,
            [ArgumentParameter("Name of trial, or All", true)] string trial)
        {
            if (ctx.Channel.Name.ToLower() != "lab-and-trials")
                throw new ArgumentException("You can only run this in the #lab-and-trials channel.");

            if (string.IsNullOrWhiteSpace(trial))
                throw new ArgumentException("You must enter a Trial.");

            var trl = trial.ToLower();

            if(trl == "all")
            {
                var roles = ctx.Guild.Roles;
                foreach(var role in roles)
                {
                    if (role.Name.ToLower().Contains("piercing") || role.Name.ToLower().Contains("swirling") || role.Name.ToLower().Contains("crippling") || role.Name.ToLower().Contains("burning") || role.Name.ToLower().Contains("lingering") || role.Name.ToLower().Contains("stinging"))
                        await ctx.User.AddRoleAsync(role);
                }
            }
            else
            {
                var roles = ctx.Guild.Roles;
                var role = roles.First(x => x.Name.ToLower().Contains(trl));
                await ctx.User.AddRoleAsync(role);
            }

            var embed = this.PrepareEmbed("Success", "Trial added to list!", EmbedType.Success);
            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());
            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("trialdelete", "Delete a Trial of Ascendancy that you have completed. Can be used like: trialdelete burning or trialdelete all for all trials", Aliases = "tdelete;deletetrial;deletet;trialdel", CheckPermissions = false)]
        public async Task TrialDelete(CommandContext ctx,
            [ArgumentParameter("Name of trial, or All", true)] string trial)
        {
            if (ctx.Channel.Name.ToLower() != "lab-and-trials")
                throw new ArgumentException("You can only run this in the #lab-and-trials channel.");

            if (string.IsNullOrWhiteSpace(trial))
                throw new ArgumentException("You must enter a Trial.");

            var trl = trial.ToLower();

            if (trl == "all")
            {
                var roles = ctx.Guild.Roles;
                foreach (var role in roles)
                {
                    if (role.Name.ToLower().Contains("piercing") || role.Name.ToLower().Contains("swirling") || role.Name.ToLower().Contains("crippling") || role.Name.ToLower().Contains("burning") || role.Name.ToLower().Contains("lingering") || role.Name.ToLower().Contains("stinging"))
                        await ctx.User.RemoveRoleAsync(role);
                }
            }
            else
            {
                var roles = ctx.Guild.Roles;
                var role = roles.First(x => x.Name.ToLower().Contains(trl));
                await ctx.User.RemoveRoleAsync(role);
            }

            var embed = this.PrepareEmbed("Success", "Trial deleted from list!", EmbedType.Success);
            embed.WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());
            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("trial", "Announce a Trial of Ascendancy that you have come acrross. Can be used like: trial burning", CheckPermissions = false)]
        public async Task Trial(CommandContext ctx,
            [ArgumentParameter("Name of trial", true)] string trial)
        {
            if (ctx.Channel.Name.ToLower() != "lab-and-trials")
                throw new ArgumentException("You can only run this in the #lab-and-trials channel.");

            if (string.IsNullOrWhiteSpace(trial))
                throw new ArgumentException("You must enter a Trial.");

            var trl = trial.ToLower();
            var roles = ctx.Guild.Roles;
            var role = roles.First(x => x.Name.ToLower().Contains(trl));

            await ctx.Channel.SendMessageAsync(ctx.User.Mention + " has found the Trial of " + role.Mention, false);
        }

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
    }
}
