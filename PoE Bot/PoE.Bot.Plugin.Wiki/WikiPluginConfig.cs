using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Wiki
{
    internal class WikiPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new WikiPluginConfig
                {
                    Wiki = new Wiki()
                };
            }
        }

        public WikiPluginConfig()
        {
            this.Wiki = new Wiki();
        }

        public Wiki Wiki { get; private set; }

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
