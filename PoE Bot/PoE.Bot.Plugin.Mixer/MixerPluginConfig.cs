using System.Collections.Generic;
using PoE.Bot.Config;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Mixer
{
    internal class MixerPluginConfig : IPluginConfig
    {
        public IPluginConfig DefaultConfig
        {
            get
            {
                return new MixerPluginConfig
                {
                    Streams = new List<Mixer>()
                };
            }
        }

        public MixerPluginConfig()
        {
            this.Streams = new List<Mixer>();
        }

        public List<Mixer> Streams { get; private set; }

        public void Load(JObject jo)
        {
            var ja = jo["streams"] as JArray;
            foreach (var xjt in ja)
            {
                var xjo = xjt as JObject;

                var name = (string)xjo["name"];
                var userid = (uint)xjo["userid"];
                var mixerchannelid = (uint)xjo["mixerchannelid"];
                var live = (bool)xjo["islive"];
                var channel = (ulong)xjo["channel"];
                this.Streams.Add(new Mixer(name, userid, mixerchannelid, live, channel));
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
                xjo.Add("mixerchannelid", stream.MixerChannelId);
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
