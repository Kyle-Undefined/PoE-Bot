namespace PoE.Bot.Objects
{
    using System;

    public class TagObject
    {
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }
        public string Name { get; set; }
        public ulong Owner { get; set; }
        public int Uses { get; set; }
    }
}