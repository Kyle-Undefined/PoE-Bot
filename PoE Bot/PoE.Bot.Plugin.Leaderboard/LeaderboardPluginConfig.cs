using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Leaderboard
{
    internal class LeaderboardPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new LeaderboardPluginConfig
                {
                    Leaderboards = new List<Leaderboard>()
                };
            }
        }

        public LeaderboardPluginConfig()
        {
            this.Leaderboards = new List<Leaderboard>();
        }

        public List<Leaderboard> Leaderboards { get; private set; }

        public void Load(JObject jo)
        {
            var ja = jo["leaderboards"] as JArray;
            foreach (var xjt in ja)
            {
                var xjo = xjt as JObject;
                var variant = (string)xjo["variant"];
                var chn = (ulong)xjo["channel"];
                var en = (bool)xjo["enabled"];
                this.Leaderboards.Add(new Leaderboard(variant, chn, en));
            }
        }

        public JObject Save()
        {
            var ja = new JArray();

            foreach (var lb in this.Leaderboards)
            {
                var xjo = new JObject();
                xjo.Add("variant", lb.Variant);
                xjo.Add("channel", lb.ChannelId);
                xjo.Add("enabled", lb.Enabled);
                ja.Add(xjo);
            }

            var jo = new JObject();
            jo.Add("leaderboards", ja);
            return jo;
        }
    }
}
