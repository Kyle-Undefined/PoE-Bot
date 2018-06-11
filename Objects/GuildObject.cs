namespace PoE.Bot.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    public class GuildObject
    {
        public string Id { get; set; }
        public string Rules { get; set; }
        public char Prefix { get; set; }
        public bool AntiProfanity { get; set; }
        public bool LogDeleted { get; set; }
        public bool RssFeed { get; set; }
        public bool MixerFeed { get; set; }
        public bool TwitchFeed { get; set; }
        public bool LeaderboardFeed { get; set; }
        public bool IsConfigured { get; set; }        
        public int MaxWarningsToMute { get; set; }
        public int MaxWarningsToKick { get; set; }
        public ulong BotChangeChannel { get; set; }
        public ulong DevChannel { get; set; }
        public ulong MuteRole { get; set; }
        public ulong TradeMuteRole { get; set; }
        public ulong ModLog { get; set; }
        public ulong RepLog { get; set; }
        public ulong AllLog { get; set; }
        public ulong RulesChannel { get; set; }

        public IList<ShopObject> Shops { get; set; } = new List<ShopObject>();
        public IList<PriceObject> Prices { get; set; } = new List<PriceObject>();
        public IList<RssObject> RssFeeds { get; set; } = new List<RssObject>();
        public IList<StreamObject> Streams { get; set; } = new List<StreamObject>();
        public IList<LeaderboardObject> Leaderboards { get; set; } = new List<LeaderboardObject>();
        public IList<TagObject> Tags { get; set; } = new List<TagObject>();
        public IList<CaseObject> UserCases { get; set; } = new List<CaseObject>();
        public Dictionary<ulong, string> AFK { get; set; } = new Dictionary<ulong, string>();
        public IList<MessageObject> DeletedMessages { get; set; } = new List<MessageObject>();
        public Dictionary<ulong, ProfileObject> Profiles { get; set; } = new Dictionary<ulong, ProfileObject>();
        public ConcurrentDictionary<ulong, DateTime> Muted { get; set; } = new ConcurrentDictionary<ulong, DateTime>();
        public ConcurrentDictionary<ulong, List<RemindObject>> Reminders { get; set; } = new ConcurrentDictionary<ulong, List<RemindObject>>();
    }

    public enum MuteType
    {
        TRADE,
        MOD
    }
}
