namespace PoE.Bot.Objects
{
    using System;
    using System.Collections.Generic;

    public class RssObject
    {
        public ulong ChannelId { get; set; }
        public Uri FeedUri { get; set; }
        public IList<string> RecentUris { get; set; } = new List<string>();
        public IList<ulong> RoleIds { get; set; } = new List<ulong>();
        public string Tag { get; set; }
    }
}