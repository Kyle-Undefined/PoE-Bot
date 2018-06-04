namespace PoE.Bot.Helpers
{
    using System;
    using Discord;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net.Http;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml.Serialization;
    using System.IO;
    using PoE.Bot.Handlers;
    using PoE.Bot.Handlers.Objects;
    using PoE.Bot.Helpers.Objects;
    using PoE.Bot.Addons;
    using Drawing = System.Drawing.Color;
    using HtmlAgilityPack;

    public class RssHelper
    {
        public static Task BuildAndSend(RssObject Feed, SocketGuild Guild, GuildObject Server, DBHandler DB)
        {
            var PostUrls = Feed.RecentUris.Any() ? Feed.RecentUris : new List<string>();
            var CheckRss = RssAsync(Feed.FeedUri).ConfigureAwait(false).GetAwaiter().GetResult();
            if (CheckRss == null) return Task.CompletedTask;

            CheckRss.Data.Items.Reverse();
            foreach (RssItem Item in CheckRss.Data.Items)
            {
                if (PostUrls.Contains(Item.Link)) return Task.CompletedTask;

                var Channel = Guild.GetChannel(Feed.ChannelId) as SocketTextChannel;
                var Embed = Extras.Embed(Drawing.Aqua);
                StringBuilder sb = new StringBuilder();

                var Description = StripTagsCharArray(RoughStrip(HtmlEntity.DeEntitize(Item.Description)));
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

                        var newsImage = GetAnnouncementImage(Item.Link);
                        if (!string.IsNullOrWhiteSpace(newsImage))
                            Embed.WithImageUrl(newsImage);

                        break;
                }

                if (Feed.RoleIds.Any())
                {
                    foreach (ulong RoleId in Feed.RoleIds)
                    {
                        IRole Role = Guild.GetRole(RoleId);
                        if (Role.Name.ToLower().Contains("everyone") && Embed.Title.ToLower().Contains(Feed.Tag.ToLower()))
                            Channel.SendMessageAsync(Role.Mention);
                        else if (!Role.Name.ToLower().Contains("everyone"))
                            Channel.SendMessageAsync(Role.Mention);
                    }
                }


                if (!string.IsNullOrEmpty(Embed.Title))
                    Channel.SendMessageAsync(embed: Embed.Build());
                else if (!string.IsNullOrEmpty(sb.ToString()))
                    Channel.SendMessageAsync(sb.ToString());

                PostUrls.Add(Item.Link);
            }

            Feed.RecentUris = PostUrls;
            DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
            return Task.CompletedTask;
        }

        private static async Task<RssDataObject> RssAsync(Uri RssFeed)
        {
            HttpResponseMessage Get;
            using (HttpClient client = new HttpClient())
                Get = client.GetAsync(RssFeed).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!Get.IsSuccessStatusCode) return null;
            XmlSerializer serializer = new XmlSerializer(typeof(RssDataObject));
            string xml = await Get.Content.ReadAsStringAsync().ConfigureAwait(false);
            Stream xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = serializer.Deserialize(xmlStream) as RssDataObject;
            return result;
        }

        private static string GetAnnouncementImage(string url)
        {
            var imageURL = string.Empty;
            var doc = new HtmlDocument();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = client.GetAsync(url).Result)
                    {
                        using (HttpContent content = response.Content)
                        {
                            string result = content.ReadAsStringAsync().Result;
                            doc.LoadHtml(result);
                        }
                    }
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

            }

            return imageURL;
        }

        private static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
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

        private static string RoughStrip(string source)
        {
            var val = source.Replace("<ul>", "")
                .Replace("</ul><br/>", "</ul>")
                .Replace("</ul>", "")
                .Replace("<li>", " * ")
                .Replace("</li>", "\n")
                .Replace("<br/>\n<br/>\n", "\n\n");

            if (val.StartsWith("<style>"))
                val = val.Substring(val.IndexOf("</style>") + 8);

            return val;
        }
    }
}
