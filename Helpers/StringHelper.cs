namespace PoE.Bot.Helpers
{
    using System;
    using Discord;
    using System.Linq;
    using System.Text;
    using System.Net.Http;
    using PoE.Bot.Handlers;
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class StringHelper
    {
        public static string Normal = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!#$%&()*+,-./:;<=>?@[\\]^_`{|}~ ";
        public static string FullWidth = "０１２３４５６７８９ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯ" +
            "ＰＱＲＳＴＵＶＷＸＹＺ！＃＄％＆（）＊＋、ー。／：；〈＝〉？＠［\\］＾＿‘｛｜｝～ ";

        public static string Replace(string Message, string Guild = null, string User = null, int Level = 0, double Eridium = 0)
            => new StringBuilder(Message).Replace("{guild}", Guild)
            .Replace("{user}", User).Replace("{level}", $"{Level}")
            .Replace("{eridium}", $"{Eridium}").ToString();

        public static async Task<string> DownloadImageAsync(HttpClient HttpClient, string URL)
        {
            var Get = await HttpClient.GetByteArrayAsync(URL).ConfigureAwait(false);
            string FileName = $"PoE-Bot-{Guid.NewGuid().ToString("n").Substring(0, 8)}";
            using (var UserImage = System.IO.File.Create($"{FileName}.png"))
                await UserImage.WriteAsync(Get, 0, Get.Length).ConfigureAwait(false);
            HttpClient = null;
            LogHandler.ForceGC();
            return $"{FileName}.png";
        }

        public static string ValidateUser(IGuild Guild, ulong Id)
        {
            var User = (Guild as SocketGuild).GetUser(Id);
            return User is null ? "Unknown User" : User.Username;
        }

        public static string ValidateChannel(IGuild Guild, ulong Id)
        {
            if (Id is 0)
                return "Not Set.";
            var Channel = (Guild as SocketGuild).GetTextChannel(Id);
            return Channel is null ? $"Unknown ({Id})" : Channel.Name;
        }

        public static string ValidateRole(IGuild Guild, ulong Id)
        {
            if (Id is 0)
                return "Not Set";
            var Role = Guild.GetRole(Id);
            return Role is null ? $"Unknown ({Id})" : Role.Name;
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            string FormatPart(int quantity, string name) => quantity > 0 ? $"{quantity} {name}{(quantity > 1 ? "s" : "")}" : null;
            return string.Join(", ", new[] {
                FormatPart(timeSpan.Days, "day"),
                FormatPart(timeSpan.Hours, "hour"),
                FormatPart(timeSpan.Minutes, "minute"),
                FormatPart(timeSpan.Seconds, "second") }.Where(x => !(x is null)));
        }
    }
}
