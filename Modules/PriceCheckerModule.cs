namespace PoE.Bot.Modules
{
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using PoE.Bot.ModuleBases;
	using PoE.Bot.Checks;
	using Qmmands;
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Price Checker Module")]
	[Description("Price Checker Commands")]
	[Group("Price")]
	[RequireRole("Price Checker")]
	[RequireChannel("price-checkers")]
	public class PriceCheckerModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }

		[Command("Add")]
		[Name("Price Add")]
		[Description("Adds a price for a currency item")]
		[Usage("price add challenge Exalted_Orb 1 55 ex exalts")]
		public async Task CurrencyItemAddAsync(
			[Name("League")]
			[Description("The league to add the currency item for. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league,
			[Name("Name")]
			[Description("The name of the currency item. Replace spaces with _")]
			string name,
			[Name("Quantity")]
			[Description("The number of the item")]
			double quantity,
			[Name("Price")]
			[Description("How much chaos it takes to buy the currency for")]
			double price,
			[Name("Alias")]
			[Description("The names the currency is known as")]
			[Remainder] string alias)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.CurrencyItems).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (TryParseCurrencyItem(league, name, guild, out var currencyItem))
			{
				await ReplyAsync(EmoteHelper.Cross + " The throne is the most devious trap of them all. *`" + currencyItem.Name + "` is already in the `" + currencyItem.League + "` list.*");
				return;
			}

			await Database.CurrencyItems.AddAsync(new CurrencyItem
			{
				Alias = string.Join(", ", alias.Split(" ")).ToLower(),
				LastUpdated = DateTime.Now,
				League = league,
				Name = name,
				Price = price,
				Quantity = quantity,
				UserId = Context.User.Id,
				GuildId = guild.Id
			});

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Delete")]
		[Name("Price Delete")]
		[Description("Deletes a price for a currency item")]
		[Usage("price delete challenge Exalted_Orb")]
		public async Task CurrencyItemDeleteAsync(
			[Name("League")]
			[Description("The league to delete the currency from. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league,
			[Name("Name")]
			[Description("The name of the currency item. Replace spaces with _")]
			string name)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.CurrencyItems).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseCurrencyItem(league, name, guild, out var currencyItem))
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm not smart enough for that ... yet. *`" + name + "` is not in the `" + league + "` list.*");
				return;
			}

			Database.CurrencyItems.Remove(currencyItem);
			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Reset")]
		[Name("Price Reset")]
		[Description("Resets all the prices for items to 0 for specified league reset.")]
		[Usage("price reset challenge")]
		public async Task CurrencyItemResetAsync(
			[Name("League")]
			[Description("The league to reset currency prices. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league = League.Challenge)
		{
			var items = await Database.CurrencyItems.Include(x => x.Guild).Where(x => x.League == league && x.Guild.Id == Context.Guild.Id).ToListAsync();

			foreach (var item in items)
			{
				item.Price = 0;
				item.Quantity = 0;
				item.UserId = Context.User.Id;
				item.LastUpdated = DateTime.Now;

				Database.CurrencyItems.Update(item);
			}

			await Database.SaveChangesAsync();
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		[Command("Update")]
		[Name("Price Update")]
		[Description("Updates a price for a currency item")]
		[Usage("price update challenge exalts 1 65")]
		public async Task CurrencyItemUpdateAsync(
			[Name("League")]
			[Description("The league to add the currency item for. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league,
			[Name("Name")]
			[Description("The name of the currency item. Can be any alias")]
			string name,
			[Name("Quantity")]
			[Description("The number of the item you can get per chaos")]
			double quantity,
			[Name("Price")]
			[Description("How much chaos it takes to buy one of the currency for")]
			double price,
			[Name("Alias")]
			[Description("The names the currency is known as. Can be left empty if the alias isn't being updated")]
			[Remainder] string alias = null)
		{
			var guild = await Database.Guilds.Include(x => x.CurrencyItems).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseCurrencyItem(league, name, guild, out var currencyItem))
			{
				await ReplyAsync(EmoteHelper.Cross + " The throne is the most devious trap of them all. *`" + name + "` is not in the `" + league + "` list.*");
				return;
			}

			if (!(alias is null))
				currencyItem.Alias = string.Join(", ", alias.Split(" ")).ToLower();

			currencyItem.LastUpdated = DateTime.Now;
			currencyItem.Price = price;
			currencyItem.Quantity = quantity;
			currencyItem.UserId = Context.User.Id;

			Database.CurrencyItems.Update(currencyItem);
			await ReplyWithEmoteAsync(EmoteHelper.OkHand);
		}

		private bool TryParseCurrencyItem(League league, string item, Guild guild, out CurrencyItem currencyItem)
		{
			currencyItem = guild.CurrencyItems.FirstOrDefault(x => string.Equals(x.Name, item, StringComparison.CurrentCultureIgnoreCase) && x.League == league);
			return !(currencyItem is null);
		}
	}
}