using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace PoE.Bot.Plugin.Mixer
{
    public class MixerAPI
    {
        private const string MIXER_API_URL = "https://mixer.com/api/v1/";

        public MixerAPI() { }

        public async Task<uint> GetUserId(string username)
        {
            uint ID = 0;
            using(HttpClient client = new HttpClient())
            {
                using(HttpResponseMessage response = await client.GetAsync(MIXER_API_URL + "users/search?query=" + username, HttpCompletionOption.ResponseHeadersRead))
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
            using(HttpClient client = new HttpClient())
            {
                using(HttpResponseMessage response = await client.GetAsync(MIXER_API_URL + "channels/" + id.ToString() + "/details", HttpCompletionOption.ResponseHeadersRead))
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
            return (string)jo["thumbnail"]["url"];
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
