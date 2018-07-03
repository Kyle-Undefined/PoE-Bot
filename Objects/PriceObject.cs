namespace PoE.Bot.Objects
{
    using System;

    public class PriceObject
    {
        public string Alias { get; set; }
        public DateTime LastUpdated { get; set; }
        public Leagues League { get; set; }
        public string Name { get; set; }
        public Double Price { get; set; }
        public Double Quantity { get; set; }
        public ulong UserId { get; set; }
    }
}