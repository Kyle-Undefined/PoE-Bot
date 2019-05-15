namespace PoE.Bot.Services
{
	using ImageMagick;
	using PoE.Bot.Attributes;
	using PoE.Bot.Extensions;
	using System.IO;

	[Service]
	public class ImageService
	{
		public MemoryStream CreateMockImage(string memeText)
		{
			var stream = new MemoryStream();

			using (var image = new MagickImage(@"Resources\Images\mock.jpg"))
			{
				new Drawables()
					.Font("Hack")
					.FontPointSize(40)
					.StrokeColor(MagickColors.Black)
					.FillColor(MagickColors.White)
					.TextAlignment(TextAlignment.Center)
					.Text(image.Width / 2, 45, string.Join("\n", memeText.SplitInParts(23)))
					.Draw(image);

				image.Write(stream, MagickFormat.Png);
			}

			stream.Position = 0;
			return stream;
		}
	}
}