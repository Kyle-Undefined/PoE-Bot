namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public class Rule
	{
		[Key]
		public ulong Id { get; set; }

		public int TotalFields { get; set; }
		public string Description { get; set; }
		public ulong GuildId { get; set; }
		public ulong MessageId { get; set; }

		public Guild Guild { get; set; }
	}
}