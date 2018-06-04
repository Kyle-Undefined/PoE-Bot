namespace PoE.Bot.Handlers.Objects
{
    public class MixerObject
    {
        public string Name { get; set; }
        public uint UserId { get; set; }
        public uint MixerChannelId { get; set; }
        public bool IsLive { get; set; }
        public ulong ChannelId { get; set; }
    }
}
