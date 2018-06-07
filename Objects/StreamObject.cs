namespace PoE.Bot.Objects
{
    public class StreamObject
    {
        public string Name { get; set; }
        public string TwitchUserId { get; set; }
        public uint MixerUserId { get; set; }
        public uint MixerChannelId { get; set; }
        public StreamType StreamType { get; set; }
        public bool IsLive { get; set; }
        public ulong ChannelId { get; set; }
    }

    public enum StreamType
    {
        MIXER,
        TWITCH
    }
}
