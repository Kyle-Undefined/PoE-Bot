namespace PoE.Bot.Addons
{
    using Discord;
    using Drawing = System.Drawing.Color;

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

        public static EmbedBuilder Embed(Drawing Drawing)
            => new EmbedBuilder { Color = new Color(Drawing.R, Drawing.G, Drawing.B) };
    }

    public enum Leagues
    {
        Standard,
        Hardcore,
        Challenge,
        ChallengeHC
    }
}
