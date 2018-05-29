using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Plugin.Shops
{
    public class ShopCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.Shop Module"; } }

        [Command("shop", "Allows you to create or delete your own shop channel. Must be used in the #setup channel.", CheckPermissions = false)]
        public async Task Shop(CommandContext ctx,
            [ArgumentParameter("Create or Delete", true)] string command)
        {
            if(ctx.Channel.Name.ToLower() != "setup")
                throw new ArgumentException("You can only run this in the #setup channel.");

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("You must enter a command.");

            var cmd = command.ToLower();
            var chans = await ctx.Guild.GetChannelsAsync();
            var usrChnN = Regex.Replace(ctx.User.Username, @"[^a-zA-Z]+", "-").Trim('-').ToLower();

            var snglChn = chans.Where(x => x.Name == usrChnN);
            bool exists = snglChn.Any();
            var embed = this.PrepareEmbed(EmbedType.Success);

            switch (cmd)
            {
                case "create":
                    if(exists)
                        throw new ArgumentException("Sorry, that channel already exists.");

                    var nChn = await ctx.Guild.CreateTextChannelAsync(usrChnN);

                    await nChn.ModifyAsync(x => x.CategoryId = (ctx.Channel as ITextChannel).CategoryId);
                    await nChn.AddPermissionOverwriteAsync(ctx.User, new OverwritePermissions(manageMessages: PermValue.Allow));

                    embed.WithTitle("Your personal shop has been created!")
                        .WithDescription($"You may now list your items here: {nChn.Mention}")
                        .WithAuthor(ctx.User)
                        .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());
                    await ctx.Channel.SendMessageAsync("", false, embed.Build());

                    break;

                case "delete":
                    if (!exists)
                        throw new ArgumentException("Sorry, that channel doesn't exist.");

                    var dChn = chans.SingleOrDefault(x => x.Name == usrChnN) as ITextChannel;
                    await dChn.DeleteAsync();
                    embed.WithTitle($"{ctx.User.Username}, your personal shop has been deleted!")
                        .WithDescription("")
                        .WithAuthor(ctx.User)
                        .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());
                    await ctx.Channel.SendMessageAsync("", false, embed.Build());

                    break;
            }
        }

        [Command("shopspurge", "Purges all channels under the Shops category", CheckerId = "CoreModerator", CheckPermissions = true)]
        public async Task ShopsPurge(CommandContext ctx)
        {
            var chans = await ctx.Guild.GetTextChannelsAsync();
            var catChans = chans.Where(x => x.CategoryId == (ctx.Channel as ITextChannel).CategoryId);

            foreach (var chan in catChans)
            {
                if (chan.Name.ToLower() != "setup")
                {
                    var dChan = catChans.SingleOrDefault(x => x.Name == chan.Name) as ITextChannel;
                    await dChan.DeleteAsync();
                }
            }

            await ctx.Message.DeleteAsync();
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
