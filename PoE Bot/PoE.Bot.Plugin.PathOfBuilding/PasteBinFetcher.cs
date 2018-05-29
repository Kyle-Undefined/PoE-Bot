using Discord;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PoE.Bot.Plugin.PathOfBuilding
{
    public class PasteBinFetcher
    {
        private const string RawPasteBin = "https://pastebin.com/raw/";

        public PasteBinFetcher() { }

        public async Task<string> GetRawCode(string url)
        {
            if (!url.StartsWith("https://pastebin.com/"))
                throw new ArgumentException("That's not a valid pastebin url", nameof(url));

            using (var cli = new HttpClient())
            {
                var code = url.Split('/').Last();
                return await cli.GetStringAsync($"{RawPasteBin}{code}");
            }
        }
    }
}
