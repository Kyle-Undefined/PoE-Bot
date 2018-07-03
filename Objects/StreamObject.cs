namespace PoE.Bot.Objects
{
    public enum StreamType
    {
        Mixer,
        Twitch
    }

    public class StreamObject
    {
        public ulong ChannelId { get; set; }
        public bool IsLive { get; set; }
        public uint MixerChannelId { get; set; }
        public uint MixerUserId { get; set; }
        public string Name { get; set; }
        public StreamType StreamType { get; set; }
        public string TwitchUserId { get; set; }
    }
}