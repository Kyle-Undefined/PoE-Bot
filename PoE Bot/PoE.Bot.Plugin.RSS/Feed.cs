﻿using System;
using System.Collections.Generic;

namespace PoE.Bot.Plugin.RSS
{
    internal class Feed
    {
        public Uri FeedUri { get; private set; }
        public ulong ChannelId { get; private set; }
        public ulong RoleId { get; private set; }
        public string Tag { get; private set; }
        public List<string> RecentUris { get; set; }
        public bool Initialized { get; set; }

        public Feed(Uri feed_uri, ulong channel)
            : this(feed_uri, channel, 0, null)
        { }

        public Feed(Uri feed_uri, ulong channel, ulong roleid, string tag)
        {
            this.FeedUri = feed_uri;
            this.ChannelId = channel;
            this.RoleId = roleid;
            this.Tag = tag;
            this.RecentUris = new List<string>();
            this.Initialized = false;
        }
    }
}
