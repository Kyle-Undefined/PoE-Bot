namespace PoE.Bot.Helpers
{
    using Addons;
    using Discord;
    using Discord.WebSocket;
    using Handlers;
    using Newtonsoft.Json.Linq;
    using Objects;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TwitchLib.Api;

    public partial class MixerAPI
    {
        private const string MIXER_API_URL = "https://mixer.com/api/v1/";
        private HttpClient httpClient;

        public MixerAPI(HttpClient client)
        {
            httpClient = client;
        }

        public async Task<string> GetChannel(uint id)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(MIXER_API_URL + "channels/" + id.ToString() + "/details", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return null;
        }

        public string GetChannelGame(string json)
        {
            JObject jo = JObject.Parse(json);
            return (string)jo["type"]["name"];
        }

        public string GetChannelGameCover(string json)
        {
            JObject jo = JObject.Parse(json);
            return (string)jo["type"]["coverUrl"];
        }

        public async Task<uint> GetChannelId(string username)
        {
            uint id = 0;
            using (HttpResponseMessage response = await httpClient.GetAsync(MIXER_API_URL + "channels/" + username, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                if (response.IsSuccessStatusCode)
                {
                    JObject jo = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    id = (uint)jo["id"];
                }

            return id;
        }

        public string GetChannelThumbnail(string json)
        {
            JObject jo = JObject.Parse(json);
            if (jo["thumbnail"].HasValues)
                return (string)jo["thumbnail"]["url"];
            else
                return (string)jo["bannerUrl"];
        }

        public string GetChannelTitle(string json)
        {
            JObject jo = JObject.Parse(json);
            return (string)jo["name"];
        }

        public async Task<string> GetUser(uint id)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(MIXER_API_URL + "users/" + id.ToString(), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return null;
        }

        public string GetUserAvatar(uint id)
        {
            string json = GetUser(id).GetAwaiter().GetResult();
            JObject jo = JObject.Parse(json);
            return (string)jo["avatarUrl"];
        }

        public async Task<uint> GetUserId(string username)
        {
            uint id = 0;
            using (HttpResponseMessage response = await httpClient.GetAsync(MIXER_API_URL + "users/search?query=" + username, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                if (response.IsSuccessStatusCode)
                {
                    JArray ja = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    JObject jo = JObject.Parse(ja[0].ToString());
                    id = (uint)jo["id"];
                }
            return id;
        }

        public int GetViewerCount(string json)
        {
            JObject jo = JObject.Parse(json);
            return (int)jo["viewersCurrent"];
        }

        public bool IsChannelLive(string json)
        {
            JObject jo = JObject.Parse(json);
            return (bool)jo["online"];
        }
    }

    public partial class StreamHelper
    {
        public static async Task BuildAndSend(StreamObject streamObject, SocketGuild guild, GuildObject server, ConfigObject config, DatabaseHandler databaseHandler, HttpClient httpClient)
        {
            bool streamWasLive = streamObject.IsLive;

            switch (streamObject.StreamType)
            {
                case StreamType.Mixer:
                    if (!server.MixerFeed)
                        return;

                    MixerAPI mixer = new MixerAPI(httpClient);
                    string chanJson = await mixer.GetChannel(streamObject.MixerChannelId).ConfigureAwait(false);
                    bool chanIsLive = mixer.IsChannelLive(chanJson);
                    if (!chanIsLive)
                        streamObject.IsLive = false;

                    if (chanIsLive && !streamObject.IsLive)
                    {
                        Embed embed = Extras.Embed(Extras.Mixer)
                            .WithTitle(mixer.GetChannelTitle(chanJson))
                            .WithDescription($"\n**{streamObject.Name}** is playing **{mixer.GetChannelGame(chanJson)}** for {mixer.GetViewerCount(chanJson).ToString()} viewers!\n\n**https://mixer.com/{streamObject.Name}**")
                            .WithAuthor(streamObject.Name, mixer.GetUserAvatar(streamObject.MixerUserId), $"https://mixer.com/{streamObject.Name}")
                            .WithThumbnailUrl(mixer.GetChannelGameCover(chanJson))
                            .WithImageUrl(mixer.GetChannelThumbnail(chanJson)).Build();

                        SocketTextChannel channel = guild.GetChannel(streamObject.ChannelId) as SocketTextChannel;
                        await channel.SendMessageAsync(embed: embed).ConfigureAwait(false);

                        streamObject.IsLive = true;
                        databaseHandler.Save<GuildObject>(server, guild.Id);
                    }
                    break;

                case StreamType.Twitch:
                    if (!server.TwitchFeed)
                        return;

                    TwitchAPI twitchAPI = new TwitchAPI();
                    twitchAPI.Settings.ClientId = config.APIKeys["TC"];
                    twitchAPI.Settings.AccessToken = config.APIKeys["TA"];

                    var streamData = await twitchAPI.Streams.helix.GetStreamsAsync(null, null, 1, null, null, "live", new List<string>(new string[] { streamObject.TwitchUserId }), null).ConfigureAwait(false);
                    if (!streamData.Streams.Any())
                        streamObject.IsLive = false;

                    if (streamData.Streams.Any())
                    {
                        foreach (var stream in streamData.Streams)
                        {
                            if (!streamObject.IsLive)
                            {
                                var twitchUser = await twitchAPI.Users.helix.GetUsersAsync(new List<string>(new string[] { stream.UserId })).ConfigureAwait(false);
                                var twitchGame = await twitchAPI.Games.helix.GetGamesAsync(new List<string>(new string[] { stream.GameId })).ConfigureAwait(false);
                                Embed embed = Extras.Embed(Extras.Twitch)
                                    .WithTitle(stream.Title)
                                    .WithDescription($"\n**{twitchUser.Users[0].DisplayName}** is playing **{twitchGame.Games[0].Name}** for {stream.ViewerCount} viewers!\n\n**http://www.twitch.tv/{twitchUser.Users[0].DisplayName}**")
                                    .WithAuthor(twitchUser.Users[0].DisplayName, twitchUser.Users[0].ProfileImageUrl, $"http://www.twitch.tv/{twitchUser.Users[0].DisplayName}")
                                    .WithThumbnailUrl(twitchGame.Games[0].BoxArtUrl.Replace("{width}x{height}", "285x380"))
                                    .WithImageUrl(stream.ThumbnailUrl.Replace("{width}x{height}", "640x360"))
                                    .Build();

                                SocketTextChannel channel = guild.GetChannel(streamObject.ChannelId) as SocketTextChannel;
                                await channel.SendMessageAsync(embed: embed).ConfigureAwait(false);

                                streamObject.IsLive = true;
                                databaseHandler.Save<GuildObject>(server, guild.Id);
                            }
                        }
                    }
                    break;
            }

            if (streamWasLive && !streamObject.IsLive)
                databaseHandler.Save<GuildObject>(server, guild.Id);
        }
    }
}