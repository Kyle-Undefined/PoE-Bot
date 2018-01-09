using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.RSS
{
    internal class RSSPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new RSSPluginConfig
                {
                    Feeds = new List<Feed>()
                };
            }
        }

        public RSSPluginConfig()
        {
            this.Feeds = new List<Feed>();
        }

        public List<Feed> Feeds { get; private set; }

        public void Load(JObject jo)
        {
            var ja = (JArray)jo["feeds"];
            foreach (var xjt in ja)
            {
                var xjo = (JObject)xjt;

                var tag = (string)xjo["tag"];
                var uri_ = (string)xjo["uri"];
                var uri = new Uri(uri_);
                var chn = (ulong)xjo["channel"];
                var role = (ulong)xjo["role"];
                var ini = xjo["initialized"] != null ? (bool)xjo["initialized"] : true;
                var ris = (JArray)xjo["recent"];
                this.Feeds.Add(new Feed(uri, chn, role, tag) { RecentUris = ris.Select(xjv => (string)xjv).ToList() });
            }
        }

        public JObject Save()
        {
            var ja = new JArray();

            foreach (var feed in this.Feeds)
            {
                var xjo = new JObject();
                xjo.Add("tag", feed.Tag);
                xjo.Add("uri", feed.FeedUri.ToString());
                xjo.Add("channel", feed.ChannelId);
                xjo.Add("role", feed.RoleId);
                xjo.Add("initialized", feed.Initialized);
                xjo.Add("recent", new JArray(feed.RecentUris));
                ja.Add(xjo);
            }

            var jo = new JObject();
            jo.Add("feeds", ja);
            return jo;
        }
    }
}
