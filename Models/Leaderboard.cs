namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public class Leaderboard
	{
		[Key]
		public ulong Id { get; set; }

		public bool Enabled { get; set; }
		public string Variant { get; set; }
		public string Console { get; set; }
		public ulong ChannelId { get; set; }
		public ulong GuildId { get; set; }

		public Guild Guild { get; set; }
	}
}