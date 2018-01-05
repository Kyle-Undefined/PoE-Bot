using System;
using System.Collections.Generic;

namespace PoE.Bot.Plugin.Price
{
    internal class Currency
    {
        public string Name { get; private set; }
        public string Alias { get; private set; }
        public Double Quantity { get; private set; }
        public Double Price { get; private set; }
        public DateTime LastUpdated { get; private set; }

        public Currency(string Name, string Alias, Double Quantity, Double Price, DateTime LastUpdated)
        {
            this.Name = Name;
            this.Alias = Alias;
            this.Quantity = Quantity;
            this.Price = Price;
            this.LastUpdated = LastUpdated;
        }
    }
}
