using System;
using System.Collections.Generic;
using System.Linq;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Twitch
{
    internal class TwitchPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new TwitchPluginConfig
                {
                    Streams = new List<Twitch>()
                };
            }
        }

        public TwitchPluginConfig()
        {
            this.Streams = new List<Twitch>();
        }

        public List<Twitch> Streams { get; private set; }

        public void Load(JObject jo)
        {
            var ja = jo["streams"] as JArray;
            foreach (var xjt in ja)
            {
                var xjo = xjt as JObject;

                var name = (string)xjo["name"];
                var userid = (string)xjo["userid"];
                var live = xjo["islive"] != null ? (bool)xjo["islive"] : false;
                var channel = (ulong)xjo["channel"];
                this.Streams.Add(new Twitch(name, userid, channel));
            }
        }

        public JObject Save()
        {
            var ja = new JArray();

            foreach (var stream in this.Streams)
            {
                var xjo = new JObject();
                xjo.Add("name", stream.Name);
                xjo.Add("userid", stream.UserId);
                xjo.Add("islive", stream.IsLive);
                xjo.Add("channel", stream.ChannelId);
                ja.Add(xjo);
            }

            var jo = new JObject();
            jo.Add("streams", ja);
            return jo;
        }
    }
}
