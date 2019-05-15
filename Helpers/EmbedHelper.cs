namespace PoE.Bot.Helpers
{
	using Discord;
	using EmbedColor = System.Drawing.Color;

	public static class EmbedHelper
	{
		public static EmbedColor Added => EmbedColor.Green;
		public static EmbedColor Case => EmbedColor.Khaki;
		public static EmbedColor Deleted => EmbedColor.Red;
		public static EmbedColor Info => EmbedColor.Aqua;
		public static EmbedColor Leaderboard => EmbedColor.CornflowerBlue;
		public static EmbedColor Mixer => EmbedColor.RoyalBlue;
		public static EmbedColor Report => EmbedColor.Goldenrod;
		public static EmbedColor RSS => EmbedColor.Aqua;
		public static EmbedColor Twitch => EmbedColor.BlueViolet;

		public static void AddEmptyField(this EmbedBuilder embed, bool inline = false) => embed.AddField(EmptyField(inline));

		public static EmbedBuilder Embed(in EmbedColor embedColor) => new EmbedBuilder
		{
			Color = new Color(embedColor.R, embedColor.G, embedColor.B)
		};

		public static EmbedFieldBuilder EmptyField(bool inline = false) => new EmbedFieldBuilder
		{
			Name = "\u200b",
			Value = "\u200b",
			IsInline = inline
		};
	}
}