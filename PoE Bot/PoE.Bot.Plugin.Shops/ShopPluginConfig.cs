using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Shops
{
    internal class ShopPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new ShopPluginConfig
                {
                    Shop = new Shop()
                };
            }
        }

        public ShopPluginConfig()
        {
            this.Shop = new Shop();
        }

        public Shop Shop { get; private set; }

        public void Load(JObject jo)
        {
        }

        public JObject Save()
        {
            var jo = new JObject();
            return jo;
        }
    }
}
