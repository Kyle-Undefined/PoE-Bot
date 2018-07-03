namespace PoE.Bot.Objects
{
    public enum StreamType
    {
        Mixer,
        Twitch
    }

    public class StreamObject
    {
        public bool IsLive { get; set; }
        public StreamType StreamType { get; set; }
        public string Name { get; set; }
        public string TwitchUserId { get; set; }
        public uint MixerChannelId { get; set; }
        public uint MixerUserId { get; set; }
        public ulong ChannelId { get; set; }
    }
}