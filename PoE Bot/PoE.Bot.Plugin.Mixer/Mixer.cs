namespace PoE.Bot.Plugin.Mixer
{
    internal class Mixer
    {
        public string Name { get; private set; }
        public uint UserId { get; private set; }
        public uint MixerChannelId { get; private set; }
        public bool IsLive { get; set; }
        public ulong ChannelId { get; private set; }

        public Mixer(string name, uint userId, uint mixerChannelId, bool isLive, ulong channelId)
        {
            this.Name = name;
            this.UserId = userId;
            this.MixerChannelId = mixerChannelId;
            this.IsLive = isLive;
            this.ChannelId = channelId;
        }
    }
}
