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
        public static async Task<IAsyncResult> BuildAndSend(RssObject Feed, SocketGuild Guild, GuildObject Server, DatabaseHandler DB)
        {
            var PostUrls = Feed.RecentUris.Any() ? Feed.RecentUris : new List<string>();
            RssDataObject CheckRss = await RssAsync(Feed.FeedUri).ConfigureAwait(false);
            if (CheckRss is null)
                return Task.CompletedTask;

            foreach (RssItem Item in CheckRss.Data.Items.Take(10).Reverse())
            {
                if (PostUrls.Contains(Item.Link))
                    continue;

                SocketTextChannel Channel = Guild.GetChannel(Feed.ChannelId) as SocketTextChannel;
                EmbedBuilder Embed = Extras.Embed(Extras.RSS);
                StringBuilder sb = new StringBuilder();

                string Description = StripTagsCharArray(RoughStrip(HtmlEntity.DeEntitize(Item.Description)));
                Description = Description.Length > 800 ? $"{Description.Substring(0, 800)} [...]" : Description;

                switch (Feed.FeedUri.ToString().Contains("gggtracker"))
                {
                    case true:
                        sb.AppendLine("-----------------------------------------------------------");
                        sb.AppendLine($":newspaper: ***{Item.Title}***\n");
                        sb.AppendLine(Item.Link);
                        sb.AppendLine($"```{Description}```");

                        break;

                    default:
                        Embed.WithTitle(Item.Title)
                            .WithDescription(Description)
                            .WithUrl(Item.Link)
                            .WithTimestamp(new DateTimeOffset(Convert.ToDateTime(Item.PubDate).ToUniversalTime()));

                        string newsImage = GetAnnouncementImage(Item.Link);
                        if (!string.IsNullOrWhiteSpace(newsImage))
                            Embed.WithImageUrl(newsImage);

                        break;
                }

                IRole RoleToMention = null;
                if (Feed.RoleIds.Any())
                {
                    foreach (ulong RoleId in Feed.RoleIds)
                    {
                        IRole Role = Guild.GetRole(RoleId);
                        if (Role.Name.ToLower().Contains("everyone") && !string.IsNullOrEmpty(Feed.Tag))
                        {
                            if (Embed.Title.ToLower().Contains(Feed.Tag.ToLower()))
                            {
                                RoleToMention = Role;
                                break;
                            }
                        }
                        else if (!Role.Name.ToLower().Contains("everyone"))
                        {
                            RoleToMention = Role;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Embed.Title))
                    await Channel.SendMessageAsync((RoleToMention?.Mention), embed: Embed.Build());
                else if (!string.IsNullOrEmpty(sb.ToString()))
                    await Channel.SendMessageAsync(sb.ToString());

                PostUrls.Add(Item.Link);

                await Task.Delay(900);
            }

            Feed.RecentUris = PostUrls;
            DB.Save<GuildObject>(Server, Guild.Id);
            return Task.CompletedTask;
        }

        private static string GetAnnouncementImage(string url)
        {
            string imageURL = string.Empty;
            HtmlDocument doc = new HtmlDocument();

            try
            {
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                using (HttpContent content = response.Content)
                {
                    string result = content.ReadAsStringAsync().Result;
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

        private static async Task<RssDataObject> RssAsync(Uri RssFeed)
        {
            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
                response = await client.GetAsync(RssFeed).ConfigureAwait(false);
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