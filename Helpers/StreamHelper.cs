namespace PoE.Bot.Helpers
{
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using PoE.Bot.Objects;
    using PoE.Bot.Handlers;
    using PoE.Bot.Addons;
    using Drawing = System.Drawing.Color;
    using TwitchLib.Api;

    public class StreamHelper
    {
        public static async Task BuildAndSend(StreamObject Stream, SocketGuild Guild, GuildObject Server, ConfigObject Config, DatabaseHandler DB)
        {
            var streamWasLive = Stream.IsLive;

            switch (Stream.StreamType)
            {
                case StreamType.MIXER:
                    if (!Server.MixerFeed)
                        return;

                    MixerAPI Mixer = new MixerAPI();
                    string chanJson = await Mixer.GetChannel(Stream.MixerChannelId);
                    bool chanIsLive = Mixer.IsChannelLive(chanJson);
                    if (!chanIsLive) Stream.IsLive = false;

                    if (chanIsLive && !Stream.IsLive)
                    {
                        var Embed = Extras.Embed(Drawing.Aqua)
                            .WithTitle(Mixer.GetChannelTitle(chanJson))
                            .WithDescription($"\n**{Stream.Name}** is playing **{Mixer.GetChannelGame(chanJson)}** for {Mixer.GetViewerCount(chanJson).ToString()} viewers!\n\n**https://mixer.com/{Stream.Name}**")
                            .WithAuthor(Stream.Name, Mixer.GetUserAvatar(Stream.MixerUserId), $"https://mixer.com/{Stream.Name}")
                            .WithThumbnailUrl(Mixer.GetChannelGameCover(chanJson))
                            .WithImageUrl(Mixer.GetChannelThumbnail(chanJson)).Build();

                        var Channel = Guild.GetChannel(Stream.ChannelId) as SocketTextChannel;
                        await Channel.SendMessageAsync(embed: Embed);

                        Stream.IsLive = true;
                        DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
                    }
                    break;

                case StreamType.TWITCH:
                    if (!Server.TwitchFeed)
                        return;

                    TwitchAPI TwitchAPI = new TwitchAPI();
                    TwitchAPI.Settings.ClientId = Config.APIKeys["TC"];
                    TwitchAPI.Settings.AccessToken = Config.APIKeys["TA"];

                    var StreamData = await TwitchAPI.Streams.helix.GetStreamsAsync(null, null, 1, null, null, "live", new List<string>(new string[] { Stream.TwitchUserId }), null);
                    if (!StreamData.Streams.Any()) Stream.IsLive = false;

                    if (StreamData.Streams.Any())
                    {
                        foreach (var stream in StreamData.Streams)
                        {
                            if (!Stream.IsLive)
                            {
                                var TwitchUser = await TwitchAPI.Users.helix.GetUsersAsync(new List<string>(new string[] { stream.UserId }));
                                var TwitchGame = await TwitchAPI.Games.helix.GetGamesAsync(new List<string>(new string[] { stream.GameId }));
                                var Embed = Extras.Embed(Drawing.Aqua)
                                    .WithTitle(stream.Title)
                                    .WithDescription($"\n**{TwitchUser.Users[0].DisplayName}** is playing **{TwitchGame.Games[0].Name}** for {stream.ViewerCount} viewers!\n\n**http://www.twitch.tv/{TwitchUser.Users[0].DisplayName}**")
                                    .WithAuthor(TwitchUser.Users[0].DisplayName, TwitchUser.Users[0].ProfileImageUrl, $"http://www.twitch.tv/{TwitchUser.Users[0].DisplayName}")
                                    .WithThumbnailUrl(TwitchGame.Games[0].BoxArtUrl.Replace("{width}x{height}", "285x380"))
                                    .WithImageUrl(stream.ThumbnailUrl.Replace("{width}x{height}", "640x360"))
                                    .Build();

                                var Channel = Guild.GetChannel(Stream.ChannelId) as SocketTextChannel;
                                await Channel.SendMessageAsync(embed: Embed);

                                Stream.IsLive = true;
                                DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
                            }
                        }
                    }
                    break;
            }

            if (streamWasLive && !Stream.IsLive) DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
        }
    }

    public class MixerAPI
    {
        public MixerAPI() { }

        private const string MIXER_API_URL = "https://mixer.com/api/v1/";

        public async Task<uint> GetUserId(string username)
        {
            uint ID = 0;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(MIXER_API_URL + "users/search?query=" + username, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var ja = JArray.Parse(await response.Content.ReadAsStringAsync());
                        var jo = JObject.Parse(ja[0].ToString());
                        ID = (uint)jo["id"];
                    }
                }
            }
            return ID;
        }

        public async Task<uint> GetChannelId(string username)
        {
            uint ID = 0;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(MIXER_API_URL + "channels/" + username, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var jo = JObject.Parse(await response.Content.ReadAsStringAsync());
                        ID = (uint)jo["id"];
                    }
                }
            }
            return ID;
        }

        public async Task<string> GetChannel(uint id)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(MIXER_API_URL + "channels/" + id.ToString() + "/details", HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadAsStringAsync();
                }
            }

            return null;
        }

        public async Task<string> GetUser(uint id)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(MIXER_API_URL + "users/" + id.ToString(), HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadAsStringAsync();
                }
            }

            return null;
        }

        public string GetUserAvatar(uint id)
        {
            string json = GetUser(id).GetAwaiter().GetResult();
            var jo = JObject.Parse(json);
            return (string)jo["avatarUrl"];
        }

        public string GetChannelTitle(string json)
        {
            var jo = JObject.Parse(json);
            return (string)jo["name"];
        }

        public bool IsChannelLive(string json)
        {
            var jo = JObject.Parse(json);
            return (bool)jo["online"];
        }

        public int GetViewerCount(string json)
        {
            var jo = JObject.Parse(json);
            return (int)jo["viewersCurrent"];
        }

        public string GetChannelThumbnail(string json)
        {
            var jo = JObject.Parse(json);
            if (jo["thumbnail"].HasValues)
                return (string)jo["thumbnail"]["url"];
            else
                return (string)jo["bannerUrl"];
        }

        public string GetChannelGame(string json)
        {
            var jo = JObject.Parse(json);
            return (string)jo["type"]["name"];
        }

        public string GetChannelGameCover(string json)
        {
            var jo = JObject.Parse(json);
            return (string)jo["type"]["coverUrl"];
        }
    }
}
