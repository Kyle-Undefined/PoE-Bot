namespace PoE.Bot.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;

	public class Tag
	{
		[Key]
		public ulong Id { get; set; }

		public DateTime CreationDate { get; set; }
		public int Uses { get; set; }
		public string Content { get; set; }
		public string Name { get; set; }
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }

		public Guild Guild { get; set; }
	}
}