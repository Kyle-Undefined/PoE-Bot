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

namespace PoE.Bot.Plugin.Trials
{
    public class TrialPlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(TrialPluginConfig); } }
        public string Name { get { return "Trials Plugin"; } }
        private TrialPluginConfig conf;

        public void Initialize()
        {
            Log.W("Trials", "Initializing Trials");
            this.conf = new TrialPluginConfig();
            Log.W("Trials", "Done");
        }

        public void LoadConfig(IPluginConfig config)
        {
            //var cfg = config as WikiPluginConfig;
            //if (cfg != null)
            //    this.conf = cfg;
        }
    }
}
