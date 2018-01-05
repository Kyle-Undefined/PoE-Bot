using System;
using PoE.Bot.Config;

namespace PoE.Bot.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        IPluginConfig Config { get; }
        Type ConfigType { get; }
        void Initialize();
        void LoadConfig(IPluginConfig config);
    }
}
