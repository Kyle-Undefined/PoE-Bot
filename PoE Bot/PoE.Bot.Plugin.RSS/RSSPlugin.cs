﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
using System.IO;
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
            this.RSSTimer = new Timer(new TimerCallback(RSS_Tick), null, 5000, 300000);
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
            AddFeed(uri, channel, null);
        }

        public void AddFeed(Uri uri, ulong channel, string tag)
        {
            this.conf.Feeds.Add(new Feed(uri, channel, tag));
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
                var ctx = wc.GetStringAsync(feed.FeedUri).GetAwaiter().GetResult();
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
                        var embed = new EmbedBuilder();

                        des = des.Replace("<br/>", "\n");
                        des = StripTagsCharArray(des);

                        switch (feed.ChannelId)
                        {
                            //#announcements
                            case 349951470189412363:
                            case 352983759840083988:
                                //embed.Title = string.Concat("<@&352462624379895809> ", Format.Bold(itt));
                                embed.Title = string.Concat(":wisdom: ", itt, " :wisdom:");
                                embed.Description = des;
                                var image = GetAnnouncementImage(itu.ToString());
                                if (!string.IsNullOrWhiteSpace(image))
                                    embed.ThumbnailUrl = image;
                                break;
                            //#ggg-tracker
                            case 352902423104323590:
                            case 396867733465202698:
                                embed.Title = string.Concat(":witchlove: ", itt, " :witchlove:");
                                break;
                        }
                        
                        embed.Url = itu.ToString();
                        embed.Timestamp = new DateTimeOffset(itd.ToUniversalTime());
                        embed.Color = new Color(255, 127, 0);

                        if (feed.Initialized) PoE_Bot.Client.SendEmbed(embed, feed.ChannelId);
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

        private string GetAnnouncementImage(string url)
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
    }
}
