namespace PoE.Bot.Helpers
{
	using Discord;

	public static class EmoteHelper
	{
		public static Emoji Back => new Emoji("\u2B05");
		public static Emoji Bug => new Emoji("\uD83D\uDC1B");
		public static Emoji Check => new Emoji("\u2705");
		public static Emoji Cross => new Emoji("\u274C");
		public static Emoji First => new Emoji("\u23EE");
		public static Emote Hammer => Emote.Parse("<:bant:398328079778447370>");
		public static Emoji Info => new Emoji("\u2139");
		public static Emoji Jump => new Emoji("\uD83D\uDD22");
		public static Emoji Last => new Emoji("\u23EF");
		public static Emoji Next => new Emoji("\u27A1");
		public static Emoji OkHand => new Emoji("\uD83D\uDC4C");
		public static Emoji Warning => new Emoji("\u26A0");
		public static Emote Xbox => Emote.Parse("<:Xbox:514508128721698838>");
		public static Emote Playstation => Emote.Parse("<:Playstation:514508160896204822>");
		public static Emoji Announcement => new Emoji("\uD83D\uDCE3");
		public static Emoji Lottery => new Emoji("\uD83C\uDF9F");
	}
}