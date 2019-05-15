namespace PoE.Bot.Models
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;

	public class Guild
	{
		[Key]
		public ulong Id { get; set; }

		public bool EnableAntiProfanity { get; set; }
		public bool EnableDeletionLog { get; set; }
		public bool EnableLeaderboardFeed { get; set; }
		public bool EnableMixerFeed { get; set; }
		public bool EnableRssFeed { get; set; }
		public bool EnableTwitchFeed { get; set; }
		public int MaxWarnings { get; set; }
		public ICollection<Case> Cases { get; } = new HashSet<Case>();
		public ICollection<CurrencyItem> CurrencyItems { get; } = new HashSet<CurrencyItem>();
		public ICollection<Leaderboard> Leaderboards { get; } = new HashSet<Leaderboard>();
		public ICollection<Profanity> Profanities { get; } = new HashSet<Profanity>();
		public ICollection<RssFeed> RssFeeds { get; } = new HashSet<RssFeed>();
		public ICollection<RssRecentUrl> RssRecentUrls { get; } = new HashSet<RssRecentUrl>();
		public ICollection<RssRole> RssRoles { get; } = new HashSet<RssRole>();
		public ICollection<RuleField> RuleFields { get; } = new HashSet<RuleField>();
		public ICollection<Shop> Shops { get; } = new HashSet<Shop>();
		public ICollection<Stream> Streams { get; } = new HashSet<Stream>();
		public ICollection<Tag> Tags { get; } = new HashSet<Tag>();
		public ICollection<User> Users { get; } = new HashSet<User>();
		public Rule Rules { get; set; }
		public ulong AnnouncementRole { get; set; }
		public ulong BotChangeChannel { get; set; }
		public ulong CaseLogChannel { get; set; }
		public ulong GuildId { get; set; }
		public ulong LotteryRole { get; set; }
		public ulong MessageLogChannel { get; set; }
		public ulong MuteRole { get; set; }
		public ulong PlaystationRole { get; set; }
		public ulong ReportLogChannel { get; set; }
		public ulong RulesChannel { get; set; }
		public ulong XboxRole { get; set; }
	}
}