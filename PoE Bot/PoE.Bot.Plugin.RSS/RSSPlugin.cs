using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Config;
using PoE.Bot.Plugins;
using HtmlAgilityPack;


namespace PoE.Bot.Plugin.RSS
{
    public class RSSPlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(RSSPluginConfig); } }
        public string Name { get { return "RSS Plugin"; } }
        private Timer RSSTimer { get; set; }
        private RSSPluginConfig conf;

        public static RSSPlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", "Initializing RSS"));
            Instance = this;
            this.conf = new RSSPluginConfig();
            this.RSSTimer = new Timer(new TimerCallback(RSS_Tick), null, 5000, 900000);
            Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", "RSS Initialized"));
        }

        public void LoadConfig(IPluginConfig config)
        {
            var cfg = config as RSSPluginConfig;
            if (cfg != null)
                this.conf = cfg;
        }

        public void AddFeed(Uri uri, ulong channel)
        {
            AddFeed(uri, channel, null, null);
        }

        public void AddFeed(Uri uri, ulong channel, string roles, string tag)
        {
            this.conf.Feeds.Add(new Feed(uri, channel, roles, tag));
            Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", string.Format("Added RSS feed for {0}: {1} with tag [{2}]", channel, uri, tag == null ? "<null>" : tag)));

            UpdateConfig();
        }

        public void RemoveFeed(Uri uri, ulong channel)
        {
            RemoveFeed(uri, channel, null);
        }

        public void RemoveFeed(Uri uri, ulong channel, string tag)
        {
            var feed = this.conf.Feeds.FirstOrDefault(xf => xf.FeedUri == uri && xf.ChannelId == channel && xf.Tag == tag);
            this.conf.Feeds.Remove(feed);
            Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", string.Format("Removed RSS feed for {0}: {1} with tag [{2}]", channel, uri, tag == null ? "<null>" : tag)));

            UpdateConfig();
        }

        internal IEnumerable<Feed> GetFeeds(ulong[] channels)
        {
            foreach (var feed in this.conf.Feeds)
                if (channels.Contains(feed.ChannelId))
                    yield return feed;
        }

        private void UpdateConfig()
        {
            Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", "Updating config"));

            PoE_Bot.ConfigManager.UpdateConfig(this);
        }

        private void RSS_Tick(object _)
        {
            var wc = new HttpClient();
            bool changed = false;
            foreach (var feed in this.conf.Feeds)
            {
                var rec = new List<string>();

                var uri_root_builder = new UriBuilder(feed.FeedUri);
                string ctx = string.Empty;

                try
                {
                    ctx = wc.GetStringAsync(feed.FeedUri).GetAwaiter().GetResult();
                }
                catch
                {
                    Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", "Cannot get FeedUri"));
                    break;
                }

                var rss = XDocument.Parse(ctx);
                var chn = rss.Root.Element("channel");
                var img = chn.Element("image");
                var thm = null as string;
                if (img != null)
                    thm = img.Element("url").Value;
                var its = chn.Elements("item").Reverse();
                foreach (var it in its)
                {
                    var itt = (string)it.Element("title");
                    var itl = (string)it.Element("link");
                    var itp = (string)it.Element("pubDate");
                    var des = (string)it.Element("description");

                    if (itl.StartsWith("/"))
                        uri_root_builder.Path = itl;
                    else
                        uri_root_builder = new UriBuilder(itl);
                    var itu = uri_root_builder.Uri;
                    var itd = DateTime.Parse(itp, CultureInfo.InvariantCulture);

                    rec.Add(itu.ToString());
                    if (!feed.RecentUris.Contains(itu.ToString()))
                    {
                        changed = true;

                        var discordClient = PoE_Bot.Client._discordClient;
                        var guilds = discordClient.Guilds;
                        SocketGuild gld = null;

                        foreach (var guild in guilds)
                        {
                            gld = guild;
                            break;
                        }

                        IMessageChannel chan = gld.GetChannel(feed.ChannelId) as IMessageChannel;

                        var embed = new EmbedBuilder();
                        StringBuilder sb = new StringBuilder();

                        des = HtmlEntity.DeEntitize(des);
                        des = RSSPlugin.RoughStrip(des);
                        des = RSSPlugin.StripTagsCharArray(des);
                        if (des.StartsWith("\""))
                            des = des.Substring(1);
                        if (des.Length >= 2000)
                            des = des.Substring(0, 2000).Insert(2000, "[...]");

                        switch (feed.FeedUri.ToString().Contains("gggtracker"))
                        {
                            case true:
                                if (des.Length >= 2000)
                                    des = des.Substring(0, 1800).Insert(1800, "[...]");

                                sb.AppendLine("-----------------------------------------------------------");
                                sb.AppendLine($":newspaper: ***{itt}***\n");
                                sb.AppendLine(itu.ToString());
                                sb.AppendLine($"```{des}```");

                                break;
                            default:
                                embed.Title = itt;
                                embed.Description = des;

                                var newsimage = RSSPlugin.GetAnnouncementImage(itu.ToString());
                                if (!string.IsNullOrWhiteSpace(newsimage))
                                    embed.ImageUrl = newsimage;

                                embed.Url = itu.ToString();
                                embed.Timestamp = new DateTimeOffset(itd.ToUniversalTime());
                                embed.Color = new Color(0, 127, 255);

                                break;
                        }

                        if (feed.Initialized)
                        {
                            if (!string.IsNullOrEmpty(feed.RoleIds) && feed.RoleIds != "0")
                            {
                                foreach (var rl in feed.RoleIds.Split(","))
                                {
                                    IRole role = gld.GetRole((ulong)Convert.ChangeType(rl, typeof(ulong)));
                                    if (role.Name.ToLower().Contains("everyone") && embed.Title.ToLower().Contains(feed.Tag.ToLower()))
                                        PoE_Bot.Client.SendMessage(role.Mention, feed.ChannelId);
                                    else if (!role.Name.ToLower().Contains("everyone"))
                                        PoE_Bot.Client.SendMessage(role.Mention, feed.ChannelId);
                                }
                            }

                            if (!string.IsNullOrEmpty(embed.Title))
                                PoE_Bot.Client.SendEmbed(embed, feed.ChannelId);
                            else if (!string.IsNullOrEmpty(sb.ToString()))
                                PoE_Bot.Client.SendMessage(sb.ToString(), feed.ChannelId);
                        }
                    }
                }

                feed.Initialized = true;
                if (changed)
                    feed.RecentUris = rec;
            }
            if (changed)
                UpdateConfig();

            Log.W(new LogMessage(LogSeverity.Info, "RSS Plugin", "Ticked RSS"));
        }

        public static string GetAnnouncementImage(string url)
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

        public static string StripTagsCharArray(string source)
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

        public static string RoughStrip(string source)
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
