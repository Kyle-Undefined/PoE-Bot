namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public class Profanity
	{
		[Key]
		public ulong Id { get; set; }

		public string Word { get; set; }
		public ulong GuildId { get; set; }

		public Guild Guild { get; set; }
	}
}