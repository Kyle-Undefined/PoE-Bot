using System;
using Discord;
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
            Log.W(new LogMessage(LogSeverity.Info, "Trials Plugin", "Initializing Trials"));
            this.conf = new TrialPluginConfig();
            Log.W(new LogMessage(LogSeverity.Info, "Trials Plugin", "Trials Initialized"));
        }

        public void LoadConfig(IPluginConfig config)
        {
            //var cfg = config as WikiPluginConfig;
            //if (cfg != null)
            //    this.conf = cfg;
        }
    }
}
