namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public class BotConfig
	{
		[Key]
		public ulong Id { get; set; }

		public string BotToken { get; set; }
		public string Prefix { get; set; }
		public string TwitchClientId { get; set; }
		public ulong ProjectChannel { get; set; }
		public ulong SupportChannel { get; set; }
	}
}