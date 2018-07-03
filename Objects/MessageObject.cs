namespace PoE.Bot.Objects
{
    using System;

    public class MessageObject
    {
        public ulong AuthorId { get; set; }
        public ulong ChannelId { get; set; }
        public string Content { get; set; }
        public DateTime DateTime { get; set; }
        public ulong MessageId { get; set; }
    }
}