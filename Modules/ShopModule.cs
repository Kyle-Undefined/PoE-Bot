namespace PoE.Bot.Modules
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Addons.Interactive;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Extensions;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using PoE.Bot.ModuleBases;
	using PoE.Bot.Checks;
	using Qmmands;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	[Name("Shop Module")]
	[Description("Shop Commands")]
	[Group("Shop")]
	[RequireChannel("shops")]
	public class ShopModule : PoEBotBase
	{
		public DatabaseContext Database { get; set; }

		[Command("Add")]
		[Name("Shop Add")]
		[Description("Adds the item to your shop")]
		[Usage("shop add challenge kaom's roots")]
		public async Task ShopAddAsync(
			[Name("League")]
			[Description("The league to add your shop item to. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league,
			[Name("Item")]
			[Description("The item you are adding to your shop")]
			[Remainder] string item)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Shops).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (TryParseShop(league, item, guild, out var shop))
			{
				await ReplyAsync(EmoteHelper.Cross + " You chose the road, old friend. God put me at the end of it. *This item is already in your shop.*");
				return;
			}

			await Database.Shops.AddAsync(new Shop
			{
				Item = item,
				League = league,
				UserId = Context.User.Id,
				GuildId = guild.Id
			});

			await Database.SaveChangesAsync();
			await ReplyAsync("Your humble servant thanks you, my God. *`" + item + "` has been added to your shop.* " + EmoteHelper.OkHand);
		}

		[Command("Clear")]
		[Name("Shop Clear")]
		[Description("Clears all items from your shop")]
		[Usage("shop clear")]
		public async Task ShopClearAsync()
		{
			var items = await Database.Shops.Where(x => x.UserId == Context.User.Id).ToListAsync();

			foreach (var item in items)
				Database.Shops.Remove(item);

			await Database.SaveChangesAsync();
			await ReplyAsync("Your trust is the only reward I need, my Lord of light. *All items have been removed from your shop.* " + EmoteHelper.OkHand);
		}

		[Command("Delete")]
		[Name("Shop Delete")]
		[Description("Deletes the item from your shop. Item has to be an exact match to what you want to delete")]
		[Usage("shop delete kaom's roots")]
		public async Task ShopDeleteAsync(
			[Name("League")]
			[Description("The league to delete your shop item from. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league,
			[Name("Item")]
			[Description("The item you are deleting from your shop")]
			[Remainder] string item)
		{
			var guild = await Database.Guilds.AsNoTracking().Include(x => x.Shops).FirstAsync(x => x.GuildId == Context.Guild.Id);

			if (!TryParseShop(league, item, guild, out var shop))
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm no beast of burden. *`" + item + "` was not found in your shop.*");
				return;
			}

			Database.Shops.Remove(shop);
			await Database.SaveChangesAsync();
			await ReplyAsync("Your trust is the only reward I need, my Lord of light. *`" + shop.Item + "` has been removed from your shop.* " + EmoteHelper.OkHand);
		}

		[Command("Search")]
		[Name("Shop Search")]
		[Description("Searches all shops for the item")]
		[Usage("shop search kaom's roots")]
		public async Task SearchAsync(
			[Name("Item")]
			[Description("The item you are searching for")]
			[Remainder] string item)
		{
			var items = await Database.Shops.AsNoTracking().Include(x => x.Guild).Where(x => x.Item.IndexOf(item, StringComparison.CurrentCultureIgnoreCase) >= 0 && x.Guild.Id == Context.Guild.Id)
				.Select(x => "League: **" + x.League + "**\n" + x.Item + "\nOwned By: " + Context.Guild.GetUser(x.UserId).Mention + "\n").ToListAsync();

			if (items.Count is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm no beast of burden. *`" + item + "` was not found in any shop.*");
				return;
			}
			else
			{
				var pages = new List<string>();

				foreach (var shop in items.ToList().SplitList())
					pages.Add(string.Join("\n", shop));

				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = "Shop Search Results",
					Author = new EmbedAuthorBuilder
					{
						Name = "Shop Items",
						IconUrl = Context.Client.CurrentUser.GetAvatar()
					}
				});
			}
		}

		[Command("SearchLeague")]
		[Name("Shop SearchLeague")]
		[Description("Searches all shops for the item, in the specified League")]
		[Usage("shop searchleague hardcore kaom's roots")]
		public async Task SearchLeagueAsync(
			[Name("League")]
			[Description("The league to search. Valid leagues are Standard, Hardcore, Challenge, ChallengeHC")]
			League league,
			[Name("Item")]
			[Description("The item you are searching for")]
			[Remainder] string item)
		{
			var items = await Database.Shops.AsNoTracking().Include(x => x.Guild).Where(x => x.Item.IndexOf(item, StringComparison.CurrentCultureIgnoreCase) >= 0 && x.Guild.Id == Context.Guild.Id)
				.Select(x => x.Item + "\nOwned By: " + Context.Guild.GetUser(x.UserId).Mention + "\n").ToListAsync();

			if (items.Count is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm no beast of burden. *`" + item + "` was not found in any " + league + " shops.*");
				return;
			}
			else
			{
				var pages = new List<string>();

				foreach (var shop in items.ToList().SplitList())
					pages.Add(string.Join("\n", shop));

				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = "League " + league + " Search Results",
					Author = new EmbedAuthorBuilder
					{
						Name = "Shop Items",
						IconUrl = Context.Client.CurrentUser.GetAvatar()
					}
				});
			}
		}

		[Command("User")]
		[Name("Shop User")]
		[Description("Gets all shops for a user")]
		[Usage("shop user @user")]
		public async Task UserAsync(
			[Name("User")]
			[Description("The user whose shop you want to view, if no user is set it shows yours, can either be @user, user id, or user/nick name (wrapped in quotes if it contains a space)")]
			SocketGuildUser user = null)
		{
			user = user ?? Context.User;
			var items = await Database.Shops.AsNoTracking().Include(x => x.Guild).Where(x => x.UserId == user.Id && x.Guild.Id == Context.Guild.Id).Select(x => "League: **" + x.League + "**\nItem:\n"
				+ x.Item + "\n").ToListAsync();

			if (items.Count is 0)
			{
				await ReplyAsync(EmoteHelper.Cross + " I'm no beast of user. *`" + user + "` doesn't have any items in their shop.*");
				return;
			}
			else
			{
				var pages = new List<string>();

				foreach (var item in items.ToList().SplitList())
					pages.Add(string.Join("\n", item));

				await PagedReplyAsync(new PaginatedMessage
				{
					Pages = pages,
					Color = new Color(0, 255, 255),
					Title = user.GetDisplayName() + "'s Personal Shop",
					Author = new EmbedAuthorBuilder
					{
						Name = "Shop Items",
						IconUrl = user.GetAvatar()
					}
				});
			}
		}

		private bool TryParseShop(League league, string shopItem, Guild guild, out Shop shop)
		{
			shop = guild.Shops.FirstOrDefault(x => string.Equals(x.Item, shopItem, StringComparison.CurrentCultureIgnoreCase) && x.League == league);
			return !(shop is null);
		}
	}
}