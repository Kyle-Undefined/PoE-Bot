namespace PoE.Bot.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;

	public enum CaseType
	{
		AutoMute,
		Ban,
		Kick,
		Mute,
		Purge,
		Softban,
		Warning
	}

	public class Case
	{
		[Key]
		public ulong Id { get; set; }

		public CaseType CaseType { get; set; }
		public DateTime CaseDate { get; set; }
		public int Number { get; set; }
		public string Reason { get; set; }
		public ulong GuildId { get; set; }
		public ulong MessageId { get; set; }
		public ulong ModeratorId { get; set; }
		public ulong UserId { get; set; }

		public Guild Guild { get; set; }
	}
}