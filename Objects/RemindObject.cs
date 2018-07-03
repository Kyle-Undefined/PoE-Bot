namespace PoE.Bot.Objects
{
    using System;

    public class RemindObject
    {
        public DateTime ExpiryDate { get; set; }
        public string Message { get; set; }
        public DateTime RequestedDate { get; set; }
        public ulong TextChannel { get; set; }
    }
}