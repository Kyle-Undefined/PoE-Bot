namespace PoE.Bot.Modules
{
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.ModuleBases;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Support Module")]
	[Description("Bot Support Commands")]
	[Group("Support")]
	public class SupportModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }

		[Command("Bug")]
		[Name("Support Bug")]
		[Description("Submit a bug report")]
		[Usage("support bug mock command is broken, it isn't posting the image")]
		public async Task BugAsync(
			[Name("Bug")]
			[Description("The bug you are experiencing, please be as descriptive as possible")]
			[Remainder] string bug)
		{
			var config = await Database.BotConfigs.AsNoTracking().FirstAsync();
			var channel = Context.Client.GetChannel(config.SupportChannel) as SocketTextChannel;
			var embed = EmbedHelper.Embed(EmbedHelper.Deleted)
				.WithAuthor(Context.User.GetDisplayName(), Context.User.GetAvatar())
				.WithTitle("Bug Report")
				.WithDescription(bug)
				.AddField("Info", "User: " + Context.User.Mention + " *(" + Context.User.Username + "#" + Context.User.Discriminator + ")*\nGuild: " + Context.Guild.Name + " - "
					+ "*" + Context.Guild.Id + "*")
				.WithCurrentTimestamp()
				.Build();
			await channel.SendMessageAsync(embed: embed);
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Bugs")]
		[Name("Support Bugs")]
		[Description("The bugs logged for the bot")]
		[Usage("support bugs")]
		public Task Bugs() => ReplyAsync("https://github.com/Kyle-Undefined/PoE-Bot/labels/bug");

		[Command("Feature")]
		[Name("Support Feature")]
		[Description("Suggest a feature")]
		[Usage("support feature price ticketing system")]
		public async Task FeatureAsync(
			[Name("Feature")]
			[Description("The new feature you'd like to see")]
			[Remainder] string feature)
		{
			var config = await Database.BotConfigs.AsNoTracking().FirstAsync();
			var channel = Context.Client.GetChannel(config.SupportChannel) as SocketTextChannel;
			var embed = EmbedHelper.Embed(EmbedHelper.Added)
				.WithAuthor(Context.User.GetDisplayName(), Context.User.GetAvatar())
				.WithTitle("Feature Request")
				.WithDescription(feature)
				.AddField("Info", "User: " + Context.User.Mention + " *(" + Context.User.Username + "#" + Context.User.Discriminator + ")*\nGuild: " + Context.Guild.Name + " - "
					+ "*" + Context.Guild.Id + "*")
				.WithCurrentTimestamp()
				.Build();
			await channel.SendMessageAsync(embed: embed);
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Features")]
		[Name("Support Features")]
		[Description("The features requested for the bot")]
		[Usage("support features")]
		public Task Features() => ReplyAsync("https://github.com/Kyle-Undefined/PoE-Bot/labels/Feature");

		[Command("SourceCode")]
		[Name("Support SourceCode")]
		[Description("The bots source code")]
		[Usage("support sourcecode")]
		public Task Source() => ReplyAsync("https://github.com/Kyle-Undefined/PoE-Bot");

		[Command("Guild")]
		[Name("Support Guild")]
		[Description("The support guild for the bot")]
		[Usage("support guild")]
		public Task SupportGuild() => ReplyAsync("https://discord.gg/94WWV48");
	}
}