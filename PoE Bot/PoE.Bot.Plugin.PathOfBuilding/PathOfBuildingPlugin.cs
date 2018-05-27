using System;
using Discord;
using PoE.Bot.Config;
using PoE.Bot.Plugins;

namespace PoE.Bot.Plugin.PathOfBuilding
{
    public class PathOfBuildingPlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(PathOfBuildingPluginConfig); } }
        public string Name { get { return "Path Of Building Plugin"; } }
        private PathOfBuildingPluginConfig conf;

        public static PathOfBuildingPlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Path Of Building Plugin", "Initializing Path Of Building"));
            Instance = this;
            this.conf = new PathOfBuildingPluginConfig();
            Log.W(new LogMessage(LogSeverity.Info, "Path Of Building Plugin", "Path Of Building Initialized"));
        }

        public void LoadConfig(IPluginConfig config)
        {
            //var cfg = config as PathOfBuildingPluginConfig;
            //if (cfg != null)
            //    this.conf = cfg;
        }
    }
}
