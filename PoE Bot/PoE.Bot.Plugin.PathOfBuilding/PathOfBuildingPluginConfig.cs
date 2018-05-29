using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.PathOfBuilding
{
    internal class PathOfBuildingPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new PathOfBuildingPluginConfig
                {
                    PathOfBuilding = new PathOfBuilding()
                };
            }
        }

        public PathOfBuildingPluginConfig()
        {
            this.PathOfBuilding = new PathOfBuilding();
        }

        public PathOfBuilding PathOfBuilding { get; private set; }

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
