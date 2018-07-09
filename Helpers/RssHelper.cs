namespace PoE.Bot.Helpers
{
    using Addons;
    using Discord;
    using Discord.WebSocket;
    using Handlers;
    using HtmlAgilityPack;
    using Objects;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    public class RssHelper
    {
        public static async Task BuildAndSend(RssObject feed, SocketGuild guild, GuildObject server, DatabaseHandler databaseHandler, HttpClient httpClient)
        {
            var postUrls = feed.RecentUris.Any() ? feed.RecentUris : new List<string>();
            RssDataObject checkRss = await RssAsync(feed.FeedUri, httpClient).ConfigureAwait(false);
            if (checkRss is null)
                return;

            foreach (RssItem item in checkRss.Data.Items.Take(10).Reverse())
            {
                if (postUrls.Contains(item.Link))
                    continue;

                SocketTextChannel channel = guild.GetChannel(feed.ChannelId) as SocketTextChannel;
                EmbedBuilder embed = Extras.Embed(Extras.RSS);
                StringBuilder sb = new StringBuilder();

                string description = StripTagsCharArray(RoughStrip(HtmlEntity.DeEntitize(item.Description)));
                description = description.Length > 800 ? $"{description.Substring(0, 800)} [...]" : description;

                switch (feed.FeedUri)
                {
                    case Uri uri when feed.FeedUri.Host is "www.gggtracker.com":
                        sb.AppendLine("-----------------------------------------------------------");
                        sb.AppendLine($":newspaper: ***{item.Title}***\n");
                        sb.AppendLine(item.Link);
                        sb.AppendLine($"```{description}```");

                        break;

                    case Uri uri when feed.FeedUri.Host is "www.poelab.com":
                        sb.AppendLine("-----------------------------------------------------------");
                        sb.AppendLine($"***{item.Title}***");
                        sb.AppendLine("*Please turn off any Ad Blockers you have to help the team keep doing Izaros work.*");
                        sb.AppendLine(item.Link);

                        string labDescription = "Lab notes not added.";

                        if (item.Comments > 0 && item.Title.Contains("Uber"))
                        {
                            RssDataObject commentRSS = await RssAsync(new Uri(item.CommentRss), httpClient).ConfigureAwait(false);
                            RssItem comment = commentRSS.Data.Items.FirstOrDefault(c => c.Title is "By: SuitSizeSmall");
                            if (!(comment is null))
                                labDescription = comment.Description;
                        }

                        sb.AppendLine($"```{labDescription}```");

                        break;

                    case Uri uri when feed.FeedUri.Host is "www.pathofexile.com":
                        embed.WithTitle(item.Title)
                            .WithDescription(description)
                            .WithUrl(item.Link)
                            .WithTimestamp(new DateTimeOffset(Convert.ToDateTime(item.PubDate).ToUniversalTime()));

                        string newsImage = await GetAnnouncementImage(item.Link, httpClient).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(newsImage))
                            embed.WithImageUrl(newsImage);

                        break;

                    default:
                        sb.AppendLine($"***{item.Title}***\n");
                        sb.AppendLine(item.Link);
                        sb.AppendLine($"```{description}```");

                        break;
                }

                IRole roleToMention = null;
                if (feed.RoleIds.Any())
                {
                    foreach (ulong RoleId in feed.RoleIds)
                    {
                        IRole Role = guild.GetRole(RoleId);
                        if (Role.Name.ToLower().Contains("everyone") && !string.IsNullOrEmpty(feed.Tag))
                        {
                            if (embed.Title.ToLower().Contains(feed.Tag.ToLower()))
                            {
                                roleToMention = Role;
                                break;
                            }
                        }
                        else if (!Role.Name.ToLower().Contains("everyone"))
                        {
                            roleToMention = Role;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(embed.Title))
                    await channel.SendMessageAsync((roleToMention?.Mention), embed: embed.Build()).ConfigureAwait(false);
                else if (!string.IsNullOrEmpty(sb.ToString()))
                    await channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);

                postUrls.Add(item.Link);
            }

            feed.RecentUris = postUrls;
            databaseHandler.Save<GuildObject>(server, guild.Id);
        }

        private static async Task<string> GetAnnouncementImage(string url, HttpClient httpClient)
        {
            string imageURL = string.Empty;
            HtmlDocument doc = new HtmlDocument();

            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(url).ConfigureAwait(false))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync().ConfigureAwait(false);
                    doc.LoadHtml(result);
                }

                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//img"))
                {
                    if (node.Attributes["src"].Value.Contains("/news/"))
                    {
                        imageURL = node.Attributes["src"].Value;
                        break;
                    }
                }
            }
            catch
            {
                // Just eat it
            }

            return imageURL;
        }

        private static string RoughStrip(string source)
        {
            string val = source.Replace("<ul>", "")
                .Replace("</ul><br/>", "</ul>")
                .Replace("</ul>", "")
                .Replace("<li>", " * ")
                .Replace("</li>", "\n")
                .Replace("<br/>\n<br/>\n", "\n\n");

            if (val.StartsWith("<style>"))
                val = val.Substring(val.IndexOf("</style>") + 8);

            return val;
        }

        private static async Task<RssDataObject> RssAsync(Uri rssFeed, HttpClient httpClient)
        {
            HttpResponseMessage response = await httpClient.GetAsync(rssFeed).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            XmlSerializer serializer = new XmlSerializer(typeof(RssDataObject));
            string xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Stream xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            RssDataObject result = serializer.Deserialize(xmlStream) as RssDataObject;

            return result;
        }

        private static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let is '<')
                {
                    inside = true;
                    continue;
                }
                if (let is '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        public partial class RssData
        {
            [XmlElement("item")]
            public List<RssItem> Items { get; set; }
        }

        [XmlRoot("rss")]
        public class RssDataObject
        {
            [XmlElement("channel")]
            public RssData Data { get; set; }
        }

        public partial class RssItem
        {
            [XmlElement(ElementName = "commentRss", Namespace = "http://wellformedweb.org/CommentAPI/")]
            public string CommentRss { get; set; }

            [XmlElement(ElementName = "comments", Namespace = "http://purl.org/rss/1.0/modules/slash/")]
            public int Comments { get; set; }

            [XmlElement("description")]
            public string Description { get; set; }

            [XmlElement("link")]
            public string Link { get; set; }

            [XmlElement("pubDate")]
            public string PubDate { get; set; }

            [XmlElement("title")]
            public string Title { get; set; }
        }
    }
}