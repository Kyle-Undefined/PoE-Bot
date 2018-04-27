using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Price
{
    internal class PricePluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new PricePluginConfig
                {
                    Currency = new List<Currency>()
                };
            }
        }

        public PricePluginConfig()
        {
            this.Currency = new List<Currency>();
        }

        public List<Currency> Currency { get; private set; }

        public void Load(JObject jo)
        {
            var ja = jo["currency"] as JArray;
            foreach (var xjt in ja)
            {
                var xjo = xjt as JObject;

                var name = (string)xjo["name"];
                var alias = (string)xjo["alias"];
                var quantity = (double)xjo["quantity"];
                var price = (double)xjo["price"];
                var lastupdated = (DateTime)xjo["lastupdated"];
                this.Currency.Add(new Currency(name, alias, quantity, price, lastupdated) { });
            }
        }

        public JObject Save()
        {
            var ja = new JArray();

            foreach (var curr in this.Currency)
            {
                var xjo = new JObject();
                xjo.Add("name", curr.Name);
                xjo.Add("alias", curr.Alias);
                xjo.Add("quantity", curr.Quantity);
                xjo.Add("price", curr.Price);
                xjo.Add("lastupdated", curr.LastUpdated);
                ja.Add(xjo);
            }

            var jo = new JObject();
            jo.Add("currency", ja);
            return jo;
        }
    }
}
