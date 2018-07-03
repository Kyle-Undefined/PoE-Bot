namespace PoE.Bot.Helpers
{
    using Handlers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public static class StringHelper
    {
        public const string FullWidth = "０１２３４５６７８９ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺ！＃＄％＆（）＊＋、ー。／：；〈＝〉？＠［\\］＾＿‘｛｜｝～ ";
        public const string Normal = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!#$%&()*+,-./:;<=>?@[\\]^_`{|}~ ";

        public static Regex CheckMatch(this string pattern)
            => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        public static bool DoesStringHaveProfanity(string data, IList<string> badWords)
        {
            foreach (string word in badWords)
            {
                string expword = ExpandBadWordToIncludeIntentionalMisspellings(word);
                Regex regex = new Regex(expword, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Match match = regex.Match(data);
                if (match.Success)
                    return match.Success;
            }
            return false;
        }

        public static async Task<string> DownloadImageAsync(HttpClient httpClient, string url)
        {
            byte[] clientBytes = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            string fileName = $"PoE-Bot-{Guid.NewGuid().ToString("n").Substring(0, 8)}";
            using (FileStream userImage = File.Create($"{fileName}.png"))
                await userImage.WriteAsync(clientBytes, 0, clientBytes.Length).ConfigureAwait(false);
            LogHandler.ForceGC();

            return $"{fileName}.png";
        }

        public static string ExpandBadWordToIncludeIntentionalMisspellings(string word)
        {
            char[] chars = word.ToCharArray();
            string op = @"(^|\s)[" + string.Join("][", chars) + @"](\s|$)";

            return op
                .Replace("[a]", "[aA@]+")
                .Replace("[b]", "(?:(I3)|(l3)|(i3)|(13)|[bB])+")
                .Replace("[c]", "(?:[cC\\(]|[kK])+")
                .Replace("[d]", "[dD]+")
                .Replace("[e]", "[eE3]+")
                .Replace("[f]", "(?:[fF]|[pPhH])+")
                .Replace("[g]", "[gG6]+")
                .Replace("[h]", "[hH]+")
                .Replace("[i]", "[iIl!1]+")
                .Replace("[j]", "[jJ]+")
                .Replace("[k]", "(?:[cC\\(]|[kK])+")
                .Replace("[l]", "[lL1!i]+")
                .Replace("[m]", "[mM]+")
                .Replace("[n]", "[nN]+")
                .Replace("[o]", "[oO0]+")
                .Replace("[p]", "[pP]+")
                .Replace("[q]", "[qQ9]+")
                .Replace("[r]", "[rR]+")
                .Replace("[s]", "[sS$5]+")
                .Replace("[t]", "[tT7]+")
                .Replace("[u]", "[uUvV]+")
                .Replace("[v]", "[vVuU]+")
                .Replace("[w]", "[wWvvVV]+")
                .Replace("[x]", "[xX]+")
                .Replace("[y]", "[yY]+")
                .Replace("[z]", "[zZ2]+");
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            string FormatTS(int ts, string name) => ts > 0 ? $"{ts} {name}{(ts > 1 ? "s" : "")}" : null;
            return string.Join(", ", new[]
            {
                FormatTS(timeSpan.Days, "day"),
                FormatTS(timeSpan.Hours, "hour"),
                FormatTS(timeSpan.Minutes, "minute"),
                FormatTS(timeSpan.Seconds, "second")
            }.Where(x => !(x is null)));
        }

        public static ulong ParseULong(this string value)
        {
            MatchCollection match = "[0-9]".CheckMatch().Matches(value);
            return !match.Any() || string.IsNullOrWhiteSpace(value)
                ? 0
                : Convert.ToUInt64(string.Join(string.Empty, match.Select(x => x.Value)));
        }

        public static bool ProfanityMatch(this string message, IList<string> profanityList)
            => DoesStringHaveProfanity(message, profanityList);

        public static string Replace(string message, string guild = null, string user = null)
            => new StringBuilder(message).Replace("{guild}", guild).Replace("{user}", user).ToString();
    }
}