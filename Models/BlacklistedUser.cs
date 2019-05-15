namespace PoE.Bot.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;

	public class BlacklistedUser
	{
		[Key]
		public ulong Id { get; set; }

		public ulong UserId { get; set; }
		public DateTime BlacklistedWhen { get; set; }
		public string Reason { get; set; }
	}
}