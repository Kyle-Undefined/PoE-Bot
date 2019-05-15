namespace PoE.Bot.Models
{
	using System.ComponentModel.DataAnnotations;

	public enum StreamType
	{
		Mixer,
		Twitch
	}

	public class Stream
	{
		[Key]
		public ulong Id { get; set; }

		public bool IsLive { get; set; }
		public StreamType StreamType { get; set; }
		public string Username { get; set; }
		public uint MixerChannelId { get; set; }
		public uint MixerUserId { get; set; }
		public ulong ChannelId { get; set; }
		public ulong GuildId { get; set; }
		public ulong TwitchUserId { get; set; }

		public Guild Guild { get; set; }
	}
}