namespace PoE.Bot.Addons
{
    using Discord;
    using EmbedColor = System.Drawing.Color;

    public class Extras
    {
        public static EmbedColor Added => EmbedColor.Green;
        public static Emoji Back => new Emoji("\u2B05");
        public static Emoji Bug => new Emoji("\uD83D\uDC1B");
        public static EmbedColor Case => EmbedColor.Khaki;
        public static Emoji Challenge => new Emoji("\uD83C\uDDE8");
        public static Emoji Check => new Emoji("\u2705");
        public static Emoji Cross => new Emoji("\u274C");
        public static EmbedColor Deleted => EmbedColor.Red;
        public static Emote Hammer => Emote.Parse("<:bant:398328079778447370>");
        public static Emoji Hardcore => new Emoji("\uD83C\uDDED");
        public static EmbedColor Info => EmbedColor.Aqua;
        public static EmbedColor Leaderboard => EmbedColor.CornflowerBlue;
        public static EmbedColor Mixer => EmbedColor.RoyalBlue;
        public static Emoji Newspaper => new Emoji("\uD83D\uDCF0");
        public static Emoji Next => new Emoji("\u27A1");
        public static Emoji OkHand => new Emoji("\uD83D\uDC4C");
        public static EmbedColor Report => EmbedColor.Goldenrod;
        public static EmbedColor RSS => EmbedColor.Aqua;
        public static Emoji Standard => new Emoji("\uD83C\uDDF8");
        public static Emoji Trash => new Emoji("\uD83D\uDDD1");
        public static EmbedColor Twitch => EmbedColor.BlueViolet;
        public static Emoji Warning => new Emoji("\u26A0");

        public static EmbedBuilder Embed(EmbedColor embedColor)
            => new EmbedBuilder { Color = new Color(embedColor.R, embedColor.G, embedColor.B) };
    }
}