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

namespace PoE.Bot.Plugin.Wiki
{
    public class WikiPlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(WikiPluginConfig); } }
        public string Name { get { return "Wiki Plugin"; } }
        private WikiPluginConfig conf;

        public static WikiPlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W("Wiki", "Initializing Wiki");
            Instance = this;
            this.conf = new WikiPluginConfig();
            Log.W("Wiki", "Done");
        }

        public void LoadConfig(IPluginConfig config)
        {
            //var cfg = config as WikiPluginConfig;
            //if (cfg != null)
            //    this.conf = cfg;
        }
    }
}
