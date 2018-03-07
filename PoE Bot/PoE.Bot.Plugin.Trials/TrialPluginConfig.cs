using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Trials
{
    internal class TrialPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new TrialPluginConfig
                {
                    Trial = new Trial()
                };
            }
        }

        public TrialPluginConfig()
        {
            this.Trial = new Trial();
        }

        public Trial Trial { get; private set; }

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
