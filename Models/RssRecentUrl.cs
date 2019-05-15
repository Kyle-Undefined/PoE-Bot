namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public class RssRecentUrl
	{
		[Key]
		public ulong Id { get; set; }

		public string RecentUrl { get; set; }
		public ulong RssFeedId { get; set; }
		public ulong GuildId { get; set; }

		public Guild Guild { get; set; }
		public RssFeed RssFeed { get; set; }
	}
}
