namespace PoE.Bot.Modules
{
	using Discord;
	using Discord.WebSocket;
	using PoE.Bot.Attributes;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.ModuleBases;
	using PoE.Bot.Services;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;

	[Name("Fun Module")]
	[Description("Fun Commands")]
	public class FunModule : PoEBotBase
	{
		public ImageService ImageService { get; set; }
		public Random Random { get; set; }

		[Command("Clap")]
		[Name("Clap")]
		[Description("Replaces spaces in your message with a clap emoji.")]
		[Usage("clap boi you better")]
		public Task Clap(
			[Name("Message")]
			[Description("The message to have clapped")]
			[Remainder] string message) => ReplyAsync(message.Replace(" ", " 👏 "));

		[Command("Enhance")]
		[Name("Enhance")]
		[Description("Enhances the Emote into a larger size.")]
		[Usage("enhance :ok_hand:")]
		public Task Enhance(
			[Name("Emote")]
			[Description("The emote to enhance")]
			IEmote emote) => Context.Message.DeleteAsync().ContinueWith(_ =>
		{
			if (emote is Emote)
			{
				return ReplyAsync(embed: new EmbedBuilder()
					.WithImageUrl((emote as Emote).Url)
					.WithColor(new Color(Random.Next(255), Random.Next(255), Random.Next(255)))
					.Build());
			}
			else if (emote is Emoji)
			{
				var emoji = emote as Emoji;
				return ReplyAsync(embed: new EmbedBuilder()
					.WithImageUrl(("https://i.kuro.mu/emoji/256x256/" + string.Join("-", emoji.Name.GetUnicodeCodePoints().Select(x => x.ToString("x2"))) + ".png"))
					.WithColor(new Color(Random.Next(255), Random.Next(255), Random.Next(255)))
					.Build());
			}

			return ReplyAsync(EmoteHelper.Cross + " I barely recognize myself. *Invalid Emote.*");
		});

		[Command("GenerateColor")]
		[Name("GenerateColor")]
		[Description("Generate a color in chat")]
		[Usage("generatecolor 0 150 255")]
		public Task GenerateColor(
			[Name("Red")]
			[Description("Number from 0 to 255")]
			int? red = null,
			[Name("Green")]
			[Description("Number from 0 to 255")]
			int? green = null,
			[Name("Blue")]
			[Description("Number from 0 to 255")]
			int? blue = null)
		{
			var validRed = red.GetValidColorNumber(Random.Next(255));
			var validGreen = green.GetValidColorNumber(Random.Next(255));
			var validBlue = blue.GetValidColorNumber(Random.Next(255));

			return ReplyAsync(embed: new EmbedBuilder()
				.WithAuthor(Context.User)
				.WithDescription("red: `" + validRed + "` green: `" + validGreen + "` blue: `" + validBlue + "`")
				.WithColor(new Color(validRed, validGreen, validBlue))
				.Build());
		}

		[Command("Kadaka")]
		[Name("Kadaka")]
		[Description("Praise our lord and savior, Kadaka!")]
		[Usage("kadaka")]
		public Task Kadaka() => ReplyAsync(
			"░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░\n"
			+ "░░░PRAISE░░░░░░▄▀▀█▀▀▄░░░░░░░░░░░░\n"
			+ "░░░OUR░░░░░░░▐░▀░░░▀░▌░░░░░░░░░░░\n"
			+ "░░░LORD░░░░░░▐░█▀░░▀█░▌░░░░░░░░░░\n"
			+ "░░░AND░░░░░░░▐█░░█░░░░▌░░░░░░░░░░\n"
			+ "░░░SAVIOUR░░░░▐▀░░░█░░░▌░░░░░░░░░\n"
			+ "░░░░░░░░░░░░░▐░░█▀▀█░█▌░░░░░░░░░░\n"
			+ "░░░░░░░░░░░░░▐█░░▀▀░░░▌░░░░░░░░░░\n"
			+ "░░░KADAKA!░░░░▐░░█░░░░█▌░░░░░░░░░\n"
			+ "░░░░░░░░░░░░░▐░█░░▀░░░▌░░░░░░░░░░\n"
			+ "░░░░░░░░░░░░░▐█░▀░░█░█▌░░░░░░░░░░");

		[Command("Kuduku")]
		[Name("Kuduku")]
		[Description("Kuduku has arrived!")]
		[Usage("kuduku")]
		public Task Kuduku() => ReplyAsync(
			"░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░░▄▄▄▄▄▄▄▄░░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░▐░░░░░░░░▌░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░▌░▐▄░░▄▌░▐░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░▌░░░░░░░░▐░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░▐░▀▀▄▄▀▀░▌░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░▌░░░▀▀░░░▐░░░░\n"
			+ "░░░░░░░░░░░░░░░░░░░░▌░░░░░░░░▐░░░░\n"
			+ "░░░░Kuduku░░░░░░░░░░░▌░░░░░░░░▐░░░░\n"
			+ "░░░░░░has░░░░░░░░░░░░▌░░░░░░░░▐░░░░\n"
			+ "░░░░░arrived░░░░░░░░░░░▌░░░░░░░░▐░░░░");

		[Command("Mock")]
		[Name("Mock")]
		[Description("Turns text into Spongebob Mocking Meme")]
		[Usage("mock well would ya look at that")]
		public async Task MockAsync(
			[Name("Text")]
			[Description("Text to turn into the Spongebob mock meme")]
			[Remainder]string text)
		{
			string meme = string.Concat(text.ToLower().AsEnumerable().Select((c, i) => i % 2 is 0 ? c : char.ToUpper(c)));
			var image = ImageService.CreateMockImage(meme);
			await Context.Channel.SendFileAsync(image, "sToP-tHAt-RiGhT-nOw-" + DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + ".png", "");
		}

		[Command("Nut")]
		[Name("Nut")]
		[Description("Nut on the chat")]
		[Usage("nut")]
		public Task Nut() => ReplyAsync("█▀█ █▄█ ▀█▀");

		[Command("Rate")]
		[Name("Rate")]
		[Description("Rates something for you out of 10")]
		[Usage("rate this bot")]
		public Task Rate(
			[Name("Thing to Rate")]
			[Description("The thing you want to rate")]
			[Remainder] string thingToRate) => ReplyAsync(":thinking: Must I do everything myself? *I would rate '" + thingToRate + "' a solid " + Random.Next(11) + "/10*");

		[Command("Scare")]
		[Name("Scare")]
		[Description("Scares a user")]
		[Usage("scare @user")]
		public Task Scare(
			[Name("User")]
			[Description("The user whose profile you want to get, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user) => ReplyAsync(user.Mention + " Boo!\n\nhttps://i.imgur.com/mHb3SH1.png");

		[Command("Toucan")]
		[Name("Toucan")]
		[Description("Le Toucan Has Arrive")]
		[Usage("toucan")]
		public Task Toucan() => ReplyAsync(
			"░░░░░░░░▄▄▄▀▀▀▄▄███▄░░░░░░░░░░░░░░\n"
			+ "░░░░░▄▀▀░░░░░░░▐░▀██▌░░░░░░░░░░░░░\n"
			+ "░░░▄▀░░░░▄▄███░▌▀▀░▀█░░░░░░░░░░░░░\n"
			+ "░░▄█░░▄▀▀▒▒▒▒▒▄▐░░░░█▌░░░░░░░░░░░░\n"
			+ "░▐█▀▄▀▄▄▄▄▀▀▀▀▌░░░░░▐█▄░░░░░░░░░░░\n"
			+ "░▌▄▄▀▀░░░░░░░░▌░░░░▄███████▄░░░░░░\n"
			+ "░░░░░░░░░░░░░▐░░░░▐███████████▄░░░\n"
			+ "░░░░░le░░░░░░░▐░░░░▐█████████████▄\n"
			+ "░░░░toucan░░░░░░▀▄░░░▐█████████████▄ \n"
			+ "░░░░░░has░░░░░░░░▀▄▄███████████████ \n"
			+ "░░░░░arrived░░░░░░░░░░░░█▀██████░░");

		[Command("Yeah")]
		[Name("Yeah")]
		[Description("YEEEEAAAHHH")]
		[Usage("yeah")]
		[RunMode(RunMode.Parallel)]
		public async Task YeahAsync()
		{
			var message = await ReplyAsync("( •_•)");
			await Task.Delay(1000);
			await message.ModifyAsync(x => x.Content = "( •_•)>⌐■-■");
			await Task.Delay(1200);
			await message.ModifyAsync(x => x.Content = "(⌐■_■)\n**YYYYYYEEEEEEEAAAAAHHHHHHH**");
		}
	}
}