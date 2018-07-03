namespace PoE.Bot.Objects
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public enum MuteType
    {
        Trade,
        Mod
    }

    public class GuildObject
    {
        public Dictionary<ulong, string> AFK { get; } = new Dictionary<ulong, string>();
        public ulong AllLog { get; set; }
        public bool AntiProfanity { get; set; }
        public ulong BotChangeChannel { get; set; }
        public IList<MessageObject> DeletedMessages { get; } = new List<MessageObject>();
        public ulong DevChannel { get; set; }
        public string Id { get; set; }
        public bool IsConfigured { get; set; }
        public bool LeaderboardFeed { get; set; }
        public IList<LeaderboardObject> Leaderboards { get; } = new List<LeaderboardObject>();
        public bool LogDeleted { get; set; }
        public ulong MainRole { get; set; }
        public int MaxWarningsToMute { get; set; }
        public int MaxWarningsToPermMute { get; set; }
        public bool MixerFeed { get; set; }
        public ulong ModLog { get; set; }
        public ConcurrentDictionary<ulong, DateTime> Muted { get; } = new ConcurrentDictionary<ulong, DateTime>();
        public ulong MuteRole { get; set; }
        public char Prefix { get; set; }
        public IList<PriceObject> Prices { get; } = new List<PriceObject>();
        public IList<string> ProfanityList { get; } = new List<string>();
        public Dictionary<ulong, ProfileObject> Profiles { get; } = new Dictionary<ulong, ProfileObject>();
        public ConcurrentDictionary<ulong, List<RemindObject>> Reminders { get; } = new ConcurrentDictionary<ulong, List<RemindObject>>();
        public ulong RepLog { get; set; }
        public ulong RoleSetChannel { get; set; }
        public bool RssFeed { get; set; }
        public IList<RssObject> RssFeeds { get; } = new List<RssObject>();
        public ulong RulesChannel { get; set; }
        public RuleObject RulesConfig { get; set; }
        public IList<ulong> SelfRoles { get; } = new List<ulong>();
        public IList<ShopObject> Shops { get; } = new List<ShopObject>();
        public IList<StreamObject> Streams { get; } = new List<StreamObject>();
        public IList<TagObject> Tags { get; } = new List<TagObject>();
        public ulong TradeMuteRole { get; set; }
        public bool TwitchFeed { get; set; }
        public IList<CaseObject> UserCases { get; } = new List<CaseObject>();
    }
}