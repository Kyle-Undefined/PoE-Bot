using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using PoE.Bot.Config;
using PoE.Bot.Plugins;

namespace PoE.Bot.Plugin.Mixer
{
    public class MixerPlugin : IPlugin
    {
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(MixerPluginConfig); } }
        public string Name { get { return "Mixer Plugin"; } }
        private Timer MixerTimer { get; set; }
        private MixerPluginConfig conf;

        public static MixerPlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Mixer Plugin", "Initializing Mixer"));
            Instance = this;
            this.conf = new MixerPluginConfig();
            this.MixerTimer = new Timer(new TimerCallback(Mixer_Tick), null, 5000, 900000);
            Log.W(new LogMessage(LogSeverity.Info, "Mixer Plugin", "Mixer Initialized"));
        }

        public void LoadConfig(IPluginConfig config)
        {
            var cfg = config as MixerPluginConfig;
            if (cfg != null)
                this.conf = cfg;
        }

        public void AddStream(string name, uint userid, uint mixerChannelId, ulong channel)
        {
            this.conf.Streams.Add(new Mixer(name, userid, mixerChannelId, false, channel));
            Log.W(new LogMessage(LogSeverity.Info, "Mixer Plugin", $"Added Mixer stream for {name}: {channel}"));

            UpdateConfig();
        }

        public void RemoveStream(string name, ulong channel)
        {
            var feed = this.conf.Streams.FirstOrDefault(xf => xf.Name == name && xf.ChannelId == channel);
            this.conf.Streams.Remove(feed);
            Log.W(new LogMessage(LogSeverity.Info, "Mixer Plugin", $"Removed Mixer stream for {name}: {channel}"));

            UpdateConfig();
        }

        internal IEnumerable<Mixer> GetStreams(ulong[] channels)
        {
            foreach (var stream in this.conf.Streams)
                if (channels.Contains(stream.ChannelId))
                    yield return stream;
        }

        private void UpdateConfig()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Mixer Plugin", "Updating config"));
            PoE_Bot.ConfigManager.UpdateConfig(this);
        }

        private async void Mixer_Tick(object _)
        {
            foreach (var stream in this.conf.Streams)
            {
                var streamWasLive = stream.IsLive;

                MixerAPI mixer = new MixerAPI();
                string chanJson = await mixer.GetChannel(stream.MixerChannelId);
                bool chanIsLive = mixer.IsChannelLive(chanJson);

                if (!chanIsLive)
                    stream.IsLive = false;

                if (chanIsLive && !stream.IsLive)
                {
                    var embed = new EmbedBuilder();
                    embed.WithTitle(mixer.GetChannelTitle(chanJson))
                        .WithDescription($"\n**{stream.Name}** is playing **{mixer.GetChannelGame(chanJson)}** for {mixer.GetViewerCount(chanJson).ToString()} viewers!\n\n**https://mixer.com/{stream.Name}**")
                        .WithAuthor(stream.Name, mixer.GetUserAvatar(stream.UserId), $"https://mixer.com/{stream.Name}")
                        .WithThumbnailUrl(mixer.GetChannelGameCover(chanJson))
                        .WithImageUrl(mixer.GetChannelThumbnail(chanJson))
                        .WithColor(new Color(0, 127, 255));

                    PoE_Bot.Client.SendEmbed(embed, stream.ChannelId);

                    stream.IsLive = true;
                    UpdateConfig();
                    await Task.Delay(15000);
                }

                if (streamWasLive && !stream.IsLive)
                    UpdateConfig();
            }

            Log.W(new LogMessage(LogSeverity.Info, "Mixer Plugin", "Ticked Mixer"));
        }
    }
}
