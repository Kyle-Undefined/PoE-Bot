namespace PoE.Bot.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;

	public class CurrencyItem
	{
		[Key]
		public ulong Id { get; set; }

		public DateTime LastUpdated { get; set; }
		public double Price { get; set; }
		public double Quantity { get; set; }
		public League League { get; set; }
		public string Alias { get; set; }
		public string Name { get; set; }
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }

		public Guild Guild { get; set; }
	}
}