namespace PoE.Bot.Addons
{
    using Discord;
    using EmbedColor = System.Drawing.Color;

    public class Extras
    {
        public static Emote Hammer { get => Emote.Parse("<:bant:398328079778447370>"); }
        public static Emoji Newspaper { get => new Emoji("\uD83D\uDCF0"); }
        public static Emoji Standard { get => new Emoji("\uD83C\uDDF8"); }
        public static Emoji Hardcore { get => new Emoji("\uD83C\uDDED"); }
        public static Emoji Challenge { get => new Emoji("\uD83C\uDDE8"); }
        public static Emoji Warning { get => new Emoji("\u26A0"); }
        public static Emoji Next { get => new Emoji("\u27A1"); }
        public static Emoji Back { get => new Emoji("\u2B05"); }
        public static Emoji Bug { get => new Emoji("\uD83D\uDC1B"); }
        public static Emoji Cross { get => new Emoji("\u274C"); }
        public static Emoji OkHand { get => new Emoji("\uD83D\uDC4C"); }
        public static Emoji Check { get => new Emoji("\u2705"); }
        public static Emoji Trash { get => new Emoji("\uD83D\uDDD1"); }

        public static EmbedColor Info { get => EmbedColor.Aqua; }
        public static EmbedColor Deleted { get => EmbedColor.Red; }
        public static EmbedColor Case { get => EmbedColor.Khaki; }
        public static EmbedColor Leaderboard { get => EmbedColor.CornflowerBlue; }
        public static EmbedColor RSS { get => EmbedColor.Aqua; }
        public static EmbedColor Mixer { get => EmbedColor.RoyalBlue; }
        public static EmbedColor Twitch { get => EmbedColor.BlueViolet; }
        public static EmbedColor Report { get => EmbedColor.Goldenrod; }
        public static EmbedColor Added { get => EmbedColor.Green; }

        public static EmbedBuilder Embed(EmbedColor EmbedColor)
            => new EmbedBuilder { Color = new Color(EmbedColor.R, EmbedColor.G, EmbedColor.B) };
    }
}
