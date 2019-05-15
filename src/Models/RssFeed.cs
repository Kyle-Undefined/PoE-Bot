namespace PoE.Bot.Models
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;

	public class RssFeed
	{
		[Key]
		public ulong Id { get; set; }

		public string FeedUrl { get; set; }
		public string Tag { get; set; }
		public ulong ChannelId { get; set; }
		public ulong GuildId { get; set; }

		public Guild Guild { get; set; }
	}
}