using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;
using System.IO;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Config;
using PoE.Bot.Plugins;

namespace PoE.Bot.Plugin.Price
{
    public class PricePlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(PricePluginConfig); } }
        public string Name { get { return "Price Plugin"; } }
        private PricePluginConfig conf;

        public static PricePlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W("Price", "Initializing Price");
            Instance = this;
            this.conf = new PricePluginConfig();
            Log.W("Price", "Done");
        }

        public void LoadConfig(IPluginConfig config)
        {
            var cfg = config as PricePluginConfig;
            if (cfg != null)
                this.conf = cfg;
        }

        public void AddCurrency(string Name, string Alias, Double Quantity, Double Price, DateTime LastUpdated)
        {
            this.conf.Currency.Add(new Currency(Name, Alias, Quantity, Price, DateTime.Now));
            Log.W("Price", "Added Price for {0} with a ratio of {1} to {2} Chaos", Name, Quantity, Price);

            UpdateConfig();
        }

        public void RemoveCurrency(string Name, string Alias, Double Quantity, Double Price)
        {
            var curr = this.conf.Currency.FirstOrDefault(xf => xf.Name == Name && xf.Alias == Alias);
            this.conf.Currency.Remove(curr);
            Log.W("Price", "Removed Price for {0}", Name);

            UpdateConfig();
        }

        internal IEnumerable<Currency> GetCurrency()
        {
            return this.conf.Currency;
        }

        internal Currency GetCurrency(string alias)
        {
            var currency = this.conf.Currency.FirstOrDefault(xf => xf.Alias.Contains(alias));
            return currency;
        }

        internal string GetCurrencyName(string alias)
        {
            var currency = this.conf.Currency.FirstOrDefault(xf => xf.Alias == alias).Name;
            return currency;
        }

        private void UpdateConfig()
        {
            Log.W("Price", "Updating config");

            PoE_Bot.ConfigManager.UpdateConfig(this);
        }
    }
}
