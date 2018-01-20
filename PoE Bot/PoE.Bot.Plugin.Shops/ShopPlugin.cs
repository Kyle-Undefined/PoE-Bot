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

namespace PoE.Bot.Plugin.Shops
{
    public class ShopPlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(ShopPluginConfig); } }
        public string Name { get { return "Shop Plugin"; } }
        private ShopPluginConfig conf;

        public static ShopPlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W("Shop", "Initializing Shop");
            Instance = this;
            this.conf = new ShopPluginConfig();
            Log.W("Shop", "Done");
        }

        public void LoadConfig(IPluginConfig config)
        {
            //var cfg = config as ShopPluginConfig;
            //if (cfg != null)
            //    this.conf = cfg;
        }
    }
}
