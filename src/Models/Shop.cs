namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public enum League
	{
		Challenge,
		ChallengeHC,
		Hardcore,
		Standard
	}

	public class Shop
	{
		[Key]
		public ulong Id { get; set; }

		public League League { get; set; }
		public string Item { get; set; }
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }

		public Guild Guild { get; set; }
	}
}