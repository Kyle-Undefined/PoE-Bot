namespace PoE.Bot.Handlers.Objects
{
    using System;
    using PoE.Bot.Addons;
    
    public class PriceObject
    {
        public Leagues League { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public Double Quantity { get; set; }
        public Double Price { get; set; }
        public DateTime LastUpdated { get; set; }
        public ulong UserId { get; set; }
    }
}
