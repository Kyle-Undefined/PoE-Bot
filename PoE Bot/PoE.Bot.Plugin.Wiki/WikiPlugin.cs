using System;
using Discord;
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
            Log.W(new LogMessage(LogSeverity.Info, "Wiki Plugin", "Initializing Wiki"));
            Instance = this;
            this.conf = new WikiPluginConfig();
            Log.W(new LogMessage(LogSeverity.Info, "Wiki Plugin", "Wiki Initialized"));
        }

        public void LoadConfig(IPluginConfig config)
        {
            //var cfg = config as WikiPluginConfig;
            //if (cfg != null)
            //    this.conf = cfg;
        }
    }
}
