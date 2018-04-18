using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Globalization;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;
using HtmlAgilityPack;

namespace PoE.Bot.Plugin.RSS
{
    public class RSSComands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.RSS Module"; } }

        [Command("addrss", "Adds an RSS feed to a specified channel.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task AddRss(CommandContext ctx,
            [ArgumentParameter("Mention of the channel to add the feed to.", true)] ITextChannel channel,
            [ArgumentParameter("URL of the RSS feed.", true)] string url,
            [ArgumentParameter("Mention of the role to tag.", false)] IRole role,
            [ArgumentParameter("Tag of the feed to use as title prefix.", false)] string tag)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;

            var chf = channel as SocketTextChannel;
            if (chf == null)
                throw new ArgumentException("Invalid channel specified.");

            RSSPlugin.Instance.AddFeed(new Uri(url), chf.Id, role != null ? role.Id : 0, tag);
            var embed = this.PrepareEmbed("Success", "Feed was added successfully.", EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Details";
                x.Value = string.Concat("Feed pointing to <", url, ">", tag != null ? string.Concat(" and **", tag, "** tag") : "", " was added to ", chf.Mention, role != null ? string.Concat(" and will mention the ", role.Mention, " role.") : ".");
            });
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("rmrss", "Removes an RSS feed from a specified channel.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task RemoveRss(CommandContext ctx,
            [ArgumentParameter("Mention of the channel to remove the feed from.", true)] ITextChannel channel,
            [ArgumentParameter("URL of the RSS feed.", true)] string url,
            [ArgumentParameter("Tag of the feed to use as title prefix.", false)] string tag)
        {
            var gld = ctx.Guild;
            var chn = ctx.Channel;
            var msg = ctx.Message;

            var chf = channel as SocketTextChannel;
            if (chf == null)
                throw new ArgumentException("Invalid channel specified.");

            RSSPlugin.Instance.RemoveFeed(new Uri(url), chf.Id, tag);
            var embed = this.PrepareEmbed("Success", "Feed was removed successfully.", EmbedType.Success);
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Details";
                x.Value = string.Concat("Feed pointing to <", url, ">", tag != null ? string.Concat(" and **", tag, "** tag") : "", " was removed from ", chf.Mention, ".");
            });
            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("listrss", "Lists RSS feeds active in the current guild.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task ListRss(CommandContext ctx)
        {
            var gld = ctx.Guild as SocketGuild;
            var chn = ctx.Channel;
            var msg = ctx.Message;
            var usr = ctx.User;

            var feeds = RSSPlugin.Instance.GetFeeds(gld.Channels.Select(xch => xch.Id).ToArray());

            var sb = new StringBuilder();
            foreach (var feed in feeds)
            {
                var xch = gld.GetChannel(feed.ChannelId) as SocketTextChannel;
                SocketRole role = null;

                if (feed.RoleId > 0)
                    role = gld.GetRole((ulong)feed.RoleId);

                sb.AppendFormat("**URL**: <{0}>", feed.FeedUri).AppendLine();
                if(!string.IsNullOrEmpty(feed.Tag))
                    sb.AppendFormat("**Tag**: {0}", feed.Tag).AppendLine();
                sb.AppendFormat("**Channel**: {0}", xch.Mention).AppendLine();
                sb.AppendFormat("**Role**: {0}", role != null ? role.Mention : "None").AppendLine();
                sb.AppendLine("---------");
            }

            var embedChunks = ChunkString(sb.ToString(), 1024);
            foreach (var chunk in embedChunks)
            {
                var chunkedEmbed = this.PrepareEmbed("RSS Feeds", "Listing of all RSS feeds on this server.", EmbedType.Info);
                chunkedEmbed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "RSS Feeds";
                    x.Value = chunk;
                });
                await chn.SendMessageAsync("", false, chunkedEmbed.Build());
            }
        }

        [Command("testrss", "Tests the RSS feeds active in the current guild.", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
        public async Task TestRss(CommandContext ctx)
        {
            var gld = ctx.Guild as SocketGuild;
            var feeds = RSSPlugin.Instance.GetFeeds(gld.Channels.Select(xch => xch.Id).ToArray());
            var wc = new HttpClient();

            foreach (var feed in feeds)
            {
                var uri_root_builder = new UriBuilder(feed.FeedUri);
                var gsa = wc.GetStringAsync(feed.FeedUri).GetAwaiter().GetResult();
                var rss = XDocument.Parse(gsa);
                var chn = rss.Root.Element("channel");
                var img = chn.Element("image");
                var thm = (string)null;
                if (img != null)
                    thm = img.Element("url").Value;
                var its = chn.Elements("item");
                foreach (var it in its)
                {
                    var itt = (string)it.Element("title");
                    var itl = (string)it.Element("link");
                    var itp = (string)it.Element("pubDate");
                    var des = (string)it.Element("description");
                    var cat = (string)it.Element("category");

                    if (!string.IsNullOrWhiteSpace(cat))
                        itp = itp.Substring(0, itp.Length - 2);

                    if (itl.StartsWith("/"))
                        uri_root_builder.Path = itl;
                    else
                        uri_root_builder = new UriBuilder(itl);
                    var itu = uri_root_builder.Uri;
                    var itd = DateTime.Parse(itp, CultureInfo.InvariantCulture);

                    IMessageChannel chan = (IMessageChannel)gld.GetChannel(feed.ChannelId);

                    var embed = this.PrepareEmbed(EmbedType.Info);

                    switch (cat)
                    {
                        case "live":
                            if (itd >= DateTime.Now || itd.ToShortDateString() == DateTime.Now.ToShortDateString())
                            {
                                var desHTML = HtmlEntity.DeEntitize(des);
                                var doc = new HtmlDocument();
                                doc.LoadHtml(desHTML);
                                HtmlNode node = doc.DocumentNode.SelectSingleNode("//img");
                                var liveimage = node.Attributes["src"].Value;

                                embed.Title = itt;
                                embed.ImageUrl = liveimage;
                                embed.Url = itu.ToString();
                                embed.Timestamp = new DateTimeOffset(itd.ToUniversalTime());
                                embed.Color = new Color(0, 127, 255);
                            }

                            break;
                        case "archive":
                        case "highlight":
                        case "upload":
                            break;
                        default:
                            des = HtmlEntity.DeEntitize(des);
                            des = RSSPlugin.StripTagsCharArray(des.Replace("<br/>", "\n"));
                            if (des.Length >= 2048)
                                des = des.Substring(0, 2044).Insert(2044, "....");

                            embed.Title = itt;
                            embed.Description = des;

                            var newsimage = RSSPlugin.GetAnnouncementImage(itu.ToString());
                            if (!string.IsNullOrWhiteSpace(newsimage))
                                embed.ImageUrl = newsimage;

                            embed.Url = itu.ToString();
                            embed.Timestamp = new DateTimeOffset(itd.ToUniversalTime());
                            break;
                    }

                    if (feed.RoleId > 0)
                    {
                        IRole role = gld.GetRole(feed.RoleId);
                        await chan.SendMessageAsync(role.Mention);
                    }
                    
                    if(!string.IsNullOrWhiteSpace(embed.Title))
                        await chan.SendMessageAsync("", false, embed.Build());

                    break;
                }
            }
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

        static IEnumerable<string> ChunkString(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}
