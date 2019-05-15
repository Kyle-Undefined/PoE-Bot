namespace PoE.Bot.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;

	public class User
	{
		[Key]
		public ulong Id { get; set; }

		public bool Muted { get; set; }
		public DateTime MutedUntil { get; set; }
		public int Warnings { get; set; }
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }

		public Guild Guild { get; set; }
	}
}