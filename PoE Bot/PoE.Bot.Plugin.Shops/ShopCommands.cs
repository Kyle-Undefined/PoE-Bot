using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;
using System.Collections.Generic;

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
            var gld = ctx.Guild;
            var usr = ctx.User;
            var chn = ctx.Channel;

            var chans = await gld.GetChannelsAsync();
            var usrChnN = Regex.Replace(usr.Username, @"[^a-zA-Z]+", "-").Trim('-').ToLower();

            var snglChn = chans.Where(x => x.Name == usrChnN);
            bool exists = snglChn.Any();

            switch (cmd)
            {
                case "create":
                    if(exists)
                        throw new ArgumentException("Sorry, that channel already exists.");

                    var nChn = await gld.CreateTextChannelAsync(usrChnN);

                    await nChn.ModifyAsync(x => x.CategoryId = (chn as IGuildChannel).CategoryId);
                    await nChn.AddPermissionOverwriteAsync(usr, new OverwritePermissions(manageMessages: PermValue.Allow));

                    var embed = this.PrepareEmbed("Your personal shop has been created!", "You may now list your items here: " + nChn.Mention, EmbedType.Success);
                    await chn.SendMessageAsync("", false, embed.Build());

                    break;

                case "delete":
                    if (!exists)
                        throw new ArgumentException("Sorry, that channel doesn't exist.");

                    var dChn = chans.SingleOrDefault(x => x.Name == usrChnN) as ITextChannel;
                    await dChn.DeleteAsync();
                    var dembed = this.PrepareEmbed(usr.Username + ", your personal shop has been deleted!", "", EmbedType.Success);
                    await chn.SendMessageAsync("", false, dembed.Build());

                    break;
            }
        }

        [Command("shopspurge", "Purges all channels under the Shops category", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task ShopsPurge(CommandContext ctx)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var chans = await gld.GetChannelsAsync();
            var catChans = chans.Where(x => x.CategoryId == (chn as IGuildChannel).CategoryId);

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
