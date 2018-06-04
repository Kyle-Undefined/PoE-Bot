namespace PoE.Bot.Handlers.Objects
{
    public class TwitchObject
    {
        public string Name { get; set; }
        public string UserId { get; set; }
        public bool IsLive { get; set; }
        public ulong ChannelId { get; set; }
    }
}
