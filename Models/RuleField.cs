namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public class RuleField
	{
		[Key]
		public ulong Id { get; set; }

		public string Title { get; set; }
		public string Content { get; set; }
		public int Order { get; set; }
		public ulong GuildId { get; set; }

		public Guild Guild { get; set; }
	}
}