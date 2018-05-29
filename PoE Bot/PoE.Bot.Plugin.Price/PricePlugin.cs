using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
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
            Log.W(new LogMessage(LogSeverity.Info, "Price Plugin", "Initializing Price"));
            Instance = this;
            this.conf = new PricePluginConfig();
            Log.W(new LogMessage(LogSeverity.Info, "Price Plugin", "Price Initialized"));
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
            Log.W(new LogMessage(LogSeverity.Info, "Price Plugin", $"Added Price for {Name} with a ratio of {Quantity} to {Price} Chaos"));

            UpdateConfig();
        }

        public void RemoveCurrency(string Name, string Alias)
        {
            var curr = this.conf.Currency.FirstOrDefault(xf => xf.Name == Name && xf.Alias.Contains(Alias));
            this.conf.Currency.Remove(curr);
            Log.W(new LogMessage(LogSeverity.Info, "Price Plugin", $"Removed Price for {Name}"));

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
            var currency = this.conf.Currency.FirstOrDefault(xf => xf.Alias.Contains(alias)).Name;
            return currency;
        }

        private void UpdateConfig()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Price Plugin", "Updating config"));

            PoE_Bot.ConfigManager.UpdateConfig(this);
        }
    }
}
