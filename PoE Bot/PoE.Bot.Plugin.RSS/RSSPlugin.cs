using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
            Log.W("RSS", "Initializing RSS");
            Instance = this;
            this.conf = new RSSPluginConfig();
            this.RSSTimer = new Timer(new TimerCallback(RSS_Tick), null, 5000, 900000);
            Log.W("RSS", "Done");
        }

        public void LoadConfig(IPluginConfig config)
        {
            var cfg = config as RSSPluginConfig;
            if (cfg != null)
                this.conf = cfg;
        }

        public void AddFeed(Uri uri, ulong channel)
        {
            AddFeed(uri, channel, 0, null);
        }

        public void AddFeed(Uri uri, ulong channel, ulong role, string tag)
        {
            this.conf.Feeds.Add(new Feed(uri, channel, role, tag));
            Log.W("RSS", "Added RSS feed for {0}: {1} with tag [{2}]", channel, uri, tag == null ? "<null>" : tag);

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
            Log.W("RSS", "Removed RSS feed for {0}: {1} with tag [{2}]", channel, uri, tag == null ? "<null>" : tag);

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
            Log.W("RSS", "Updating config");

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
                    Log.W("RSS", "Cannot get FeedUri");
                    break;
                }

                var rss = XDocument.Parse(ctx);
                var chn = rss.Root.Element("channel");
                var img = chn.Element("image");
                var thm = (string)null;
                if (img != null)
                    thm = img.Element("url").Value;
                var its = chn.Elements("item").Reverse();
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

                    if (cat != "archive" && cat != "highlight" && cat != "upload")
                    {
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

                            IMessageChannel chan = (IMessageChannel)gld.GetChannel(feed.ChannelId);

                            var embed = new EmbedBuilder();

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

                                default:
                                    des = HtmlEntity.DeEntitize(des);
                                    des = StripTagsCharArray(des.Replace("<br/>", "\n"));
                                    if (des.Length >= 2048)
                                        des = des.Substring(0, 2044).Insert(2044, "....");

                                    embed.Title = itt;
                                    embed.Description = des;

                                    var newsimage = GetAnnouncementImage(itu.ToString());
                                    if (!string.IsNullOrWhiteSpace(newsimage))
                                        embed.ImageUrl = newsimage;

                                    embed.Url = itu.ToString();
                                    embed.Timestamp = new DateTimeOffset(itd.ToUniversalTime());
                                    embed.Color = new Color(0, 127, 255);
                                    break;
                            }

                            if (feed.Initialized)
                            {
                                if (feed.RoleId > 0)
                                {
                                    IRole role = gld.GetRole(feed.RoleId);
                                    PoE_Bot.Client.SendMessage(role.Mention, feed.ChannelId);
                                }

                                PoE_Bot.Client.SendEmbed(embed, feed.ChannelId);
                            }
                        }
                    }
                    else
                    {
                        changed = true;
                        rec = new List<string>();
                    }
                }

                feed.Initialized = true;
                if (changed)
                    feed.RecentUris = rec;
            }
            if (changed)
                UpdateConfig();

            Log.W("RSS", "Ticked RSS");
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
    }
}
