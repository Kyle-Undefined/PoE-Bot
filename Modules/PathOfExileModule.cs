namespace PoE.Bot.Modules
{
	using PoE.Bot.Attributes;
	using PoE.Bot.Helpers;
	using PoE.Bot.ModuleBases;
	using PoE.Bot.Checks;
	using PoE.Bot.Services;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Path of Exile Module")]
	[Description("Path of Exile Specific Commands")]
	public class PathOfExileModule : PoEBotBase
	{
		public PathOfBuildingService PathOfBuilding { get; set; }
		public WikiService Wiki { get; set; }

		[Command("Lab")]
		[Name("Lab")]
		[Description("Get the links for PoE Lab Notes website")]
		[Usage("lab")]
		public Task Lab() => ReplyAsync(embed: EmbedHelper.Embed(EmbedHelper.Info)
			.WithTitle("Please turn off any Ad Blockers you have to help the team keep doing Izaros work.")
			.WithDescription("[Homepage](https://www.poelab.com/) - [Support](https://www.poelab.com/support/)")
			.AddField("Labrynth Info", "[Guide](https://www.poelab.com/new-to-labyrinth/) - [Enchantments](https://www.poelab.com/all-enchantments/)"
				+ " - [Darkshrines](https://www.poelab.com/hidden-darkshrines/) - [Izaro's Phases](https://www.poelab.com/izaro-phases/) - [Izaro's Attacks](https://www.poelab.com/izaro-weapon-and-attacks/)"
				+ " - [Traps](https://www.poelab.com/traps_and_trials/) - [Doors & Keys](https://www.poelab.com/doors-and-keys/) - [Puzzles](https://www.poelab.com/gauntlets-puzzles/)"
				+ " - [Lore](https://www.poelab.com/lore/) - [Tips](https://www.poelab.com/labyrinth-tips/) - [Puzzle Solutions](https://www.poelab.com/puzzle-solutions/)"
				+ " - [Trial Locations](https://www.poelab.com/trial-locations/) - [Trial Tracker](https://www.poelab.com/trial-tracker/) - [What makes a Good Labyrinth?](https://www.poelab.com/what-makes-a-good-labyrinth/)")
			.AddField("Leveling Guides", "[Act 1](https://www.poelab.com/act-1-leveling-guide/) - [Act 2](https://www.poelab.com/act-2-leveling-guide/)"
				+ " - [Act 3](https://www.poelab.com/act-3-leveling-guide/) - [Act 4](https://www.poelab.com/act-4-leveling-guide/) - [Act 5](https://www.poelab.com/act-5-leveling-guide/)"
				+ " - [Act 6](https://www.poelab.com/act-6-leveling-guide/) - [Act 7](https://www.poelab.com/act7-leveling-guide/) - [Act 8](https://www.poelab.com/act-8-leveling-guide/)"
				+ " - [Act 9](https://www.poelab.com/act-9-leveling-guide/) - [Act 10](https://www.poelab.com/act-10-leveling-guide/)")
			.AddField("League Cheat sheets", "[Azurite Mine Rooms](https://www.poelab.com/azurite-mine-legend-rooms/) - [Incursion Rooms](https://www.poelab.com/incursion-rooms/)")
			.AddField("League Mechanics", "[Incursion](https://www.poelab.com/incursion/)")
			.AddField("Endgame Goals", "[Crafting an Item](https://www.poelab.com/crafting-an-item/) - [Road to Uber Elder](https://www.poelab.com/road-to-uber-elder/)"
				+ " - [Zana and the Map Device](https://www.poelab.com/zana-and-the-map-device/)")
			.AddField("Endgame Boss Guides", "[Ahuatotli](https://www.poelab.com/ahuatotli-the-blind/) - [Atziri](https://www.poelab.com/atziri-queen-of-the-vaal/)"
				+ " - [Aul](https://www.poelab.com/aul-the-crystal-king/) - [Chimera](https://www.poelab.com/guardian-of-the-chimera/) - [Constrictor](https://www.poelab.com/the-constrictor/)"
				+ " - [Elder](https://www.poelab.com/the-elder/) - [Enslaver](https://www.poelab.com/the-enslaver/) - [Eradicator](https://www.poelab.com/the-eradicator/)"
				+ " - [Hydra](https://www.poelab.com/guardian-of-the-hydra/) - [Minotaur](https://www.poelab.com/guardian-of-the-minotaur/) - [Phoenix](https://www.poelab.com/guardian-of-the-phoenix/)"
				+ " - [Purifier](https://www.poelab.com/the-purifier/) - [Shaper](https://www.poelab.com/the-shaper-master-of-the-void/) - [Uber Elder](https://www.poelab.com/the-uber-elder/)"
				+ " - [Vessel of the Vaal](https://www.poelab.com/the-vessel-of-the-vaals/)")
			.AddField("Breach Boss Guides", "[Chayula](https://www.poelab.com/chayula-who-dreamt/) - [Esh](https://www.poelab.com/esh-forked-thought/)"
				+ " - [Tul](https://www.poelab.com/tul-creeping-avalanche/) - [Uul-Netol](https://www.poelab.com/uul-netol-unburdened-flesh/) - [Xoph](https://www.poelab.com/xoph-dark-embers/)")
			.AddField("Misc", "[Defensive Layers](https://www.poelab.com/defensive-layers/) - [Mapping Etique](https://www.poelab.com/mapping-etiquette/)")
			.Build());

		[Command("PoB")]
		[Name("PoB")]
		[Description("Parses the PasteBin export from Path of Building and shows the information about the build")]
		[Usage("pob https://pastebin.com/cwQVpT8J")]
		public async Task PoBAsync(
			[Name("Pastebin Url")]
			[Description("Url of the Path of Building export")]
			[Remainder] string pasteBinURL) => await ReplyAsync(embed: await PathOfBuilding.BuildPathOfBuildingEmbed(pasteBinURL, Context.User));

		[Command("Price")]
		[Name("Price")]
		[Description("Pulls the price for the requested currency, in the chosen league, all values based on Chaos")]
		[Usage("price exalt standard")]
		[BanChannel("price-checkers")]
		public Task Price(
			[Name("Name")]
			[Description("Name of the currency you want to lookup")]
			string name = null,
			[Name("League")]
			[Description("The league you want to lookup the currency in, defaults to Challenge League. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			string league = null
			//Leagues league = Leagues.Challenge)
			)
		{
			return ReplyAsync("We have discontinued price checks until further notice, sorry for the inconvenience.");
			//var guild = Database.Read<GuildObject>(Context.Guild.Id);
			//if (!guild.CurrencyItems.Any(x => x.Alias.Contains(name.ToLower()) && x.League == league))
			//    return ReplyAsync($"{EmoteHelper.Cross} What in God's name is that smell? *`{name}` is not in the `{league}` list.*");

			//var item = guild.CurrencyItems.FirstOrDefault(x => x.Alias.Contains(name.ToLower()) && x.League == league);
			//Embed embed = EmbedHelper.Embed(EmbedHelper.Info)
			//    .AddField($"{item.Name.Replace("_", " ")} in {league} league", $"```{item.Alias}```")
			//    .AddField("Ratio", $"```{item.Quantity}:{item.Price}c```")
			//    .AddField("Last Updated", $"```{item.LastUpdated}```")
			//    .WithFooter("Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.")
			//    .Build();

			//return ReplyAsync(embed: embed);
		}

		[Command("PriceList")]
		[Name("PriceList")]
		[Description("Pulls the price for the all currency, in the specified league")]
		[Usage("pricelist standard")]
		public Task PriceList(
			[Name("League")]
			[Description("The league you want to list the currency from, defaults to Challenge League. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			string league = null
			//Leagues league = Leagues.Challenge)
			)
		{
			return ReplyAsync("We have discontinued price checks until further notice, sorry for the inconvenience.");
			//var guild = Database.Read<GuildObject>(Context.Guild.Id);
			//var items = guild.CurrencyItems.Where(x => x.League == league).Select(x =>
			//    $"**{x.Name.Replace("_", " ")}**\n"
			//    + $"*{x.Alias}*\n"
			//    + $"Ratio: {x.Quantity}:{x.Price}c\n"
			//    + $"Last Updated: {x.LastUpdated}\n");
			//return PagedReplyAsync(items, $"{league} Price List");
		}

		[Command("Trial")]
		[Name("Trial")]
		[Description("Announce a Trial of Ascendancy that you have come across")]
		[Usage("trial burning")]
		[RequireChannel("lab-and-trials")]
		public Task Trial(
			[Name("Trial")]
			[Description("Name of the trial you want to announce, any part of the trial name")]
			[Remainder] string trial)
		{
			var trialRoles = Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")).Select(x => x.Name.ToLower());

			if (!trialRoles.Any(x => x.Contains(trial.ToLower())))
				return ReplyAsync(EmoteHelper.Cross + " An emperor is only as efficient as those he commands. *Not a proper Trial name*");

			return ReplyAsync("The essence of an empire must be shared equally amongst all of its citizens."
				+ " *" + Context.User.Mention + " has found the " + Context.Guild.Roles.FirstOrDefault(x => x.Name.IndexOf(trial, StringComparison.CurrentCultureIgnoreCase) >= 0)?.Mention + "*");
		}

		[Command("TrialAdd")]
		[Name("TrialAdd")]
		[Description("Adds a trial to your list")]
		[Usage("trialadd burning")]
		[RequireChannel("lab-and-trials")]
		public Task TrialAdd(
			[Name("Trial")]
			[Description("Name of the trial you want to announce, any part of the trial name")]
			[Remainder] string trial)
		{
			var trialRoles = Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")).Select(x => x.Name.ToLower());

			if (!trialRoles.Any(x => x.Contains(trial.ToLower())) && !(trial.ToLower() is "all"))
				return ReplyAsync(EmoteHelper.Cross + " An emperor is only as efficient as those he commands. *Not a proper Trial name*");

			if (trial.ToLower() is "all")
				Context.User.AddRolesAsync(Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")));
			else
				Context.User.AddRoleAsync(Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")).FirstOrDefault(x => x.Name.IndexOf(trial, StringComparison.CurrentCultureIgnoreCase) >= 0));

			return ReplyAsync("Some things that slumber should never be awoken. *Trial" + (trial.ToLower() is "all" ? "s were" : " was") + " added to your list.* " + EmoteHelper.OkHand);
		}

		[Command("TrialDelete")]
		[Name("TrialDelete")]
		[Description("Deletes a trial from your list")]
		[Usage("trialdelete burning")]
		[RequireChannel("lab-and-trials")]
		public Task TrialDelete(
			[Name("Trial")]
			[Description("Name of the trial you want to announce, any part of the trial name")]
			[Remainder] string trial)
		{
			var trialRoles = Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")).Select(x => x.Name.ToLower());

			if (!trialRoles.Any(x => x.Contains(trial.ToLower())) && !(trial.ToLower() is "all"))
				return ReplyAsync(EmoteHelper.Cross + " An emperor is only as efficient as those he commands. *Not a proper Trial name*");

			if (trial.ToLower() is "all")
				Context.User.RemoveRolesAsync(Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")));
			else
				Context.User.RemoveRoleAsync(Context.Guild.Roles.Where(x => x.Name.Contains("Trial of")).FirstOrDefault(x => x.Name.IndexOf(trial, StringComparison.CurrentCultureIgnoreCase) >= 0));
			return ReplyAsync("Woooooah, the weary traveller draws close to the end of the path.. *Trial" + (trial.ToLower() is "all" ? "s were" : " was") + " removed from your list.* " + EmoteHelper.OkHand);
		}

		[Command("Trials")]
		[Name("Trials")]
		[Description("Lists the trials you are looking for")]
		[Usage("trials")]
		[RequireChannel("lab-and-trials")]
		public Task Trials()
		{
			if (Context.User.Roles.Where(x => x.Name.Contains("Trial of")).Count() > 0)
				return ReplyAsync("The Emperor beckons, and the world attends. *" + string.Join(", ", Context.User.Roles.Where(x => x.Name.Contains("Trial of")).Select(x => x.Name)) + "*");
			else
				return ReplyAsync(EmoteHelper.Cross + " It is the sovereign who empowers the sceptre. Not the other way round. *You've not added any Trials*");
		}

		[Command("Wiki")]
		[Name("Wiki")]
		[Description("Searches an item on the Path of Exile Wiki")]
		[Usage("wiki the sun")]
		public async Task WikiAsync(
			[Name("Item")]
			[Description("Item to search the Path of Exile Wiki for")]
			[Remainder] string item) => await ReplyAsync(embed: await Wiki.BuildWikiItemEmbed(item));
	}
}