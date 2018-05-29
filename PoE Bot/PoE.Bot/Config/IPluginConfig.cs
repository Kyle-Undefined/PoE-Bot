using Newtonsoft.Json.Linq;

namespace PoE.Bot.Config
{
    public interface IPluginConfig
    {
        /// <summary>
        /// Gets the default configuration.
        /// </summary>
        IPluginConfig DefaultConfig { get; }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="jo">JSON to load from.</param>
        void Load(JObject jo);

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <returns>Saved JSON.</returns>
        JObject Save();
    }
}
