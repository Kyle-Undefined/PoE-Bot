namespace PoE.Bot.Handlers.Objects
{
    using System;

    public class RemindObject
    {
        public string Message { get; set; }
        public ulong TextChannel { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime RequestedDate { get; set; }
    }
}
