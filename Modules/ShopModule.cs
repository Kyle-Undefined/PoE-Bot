namespace PoE.Bot.Modules
{
    using Addons;
    using Addons.Preconditions;
    using Discord;
    using Discord.Commands;
    using Helpers;
    using Objects;
    using System.Linq;
    using System.Threading.Tasks;

    [Name("Shop Commands"), Group("Shop"), RequireChannel("shops"), Ratelimit]
    public class ShopModule : BotBase
    {
        [Command("Add"), Summary("Adds the item to your shop."), Remarks("Shop Add <league> <item>")]
        public Task AddAsync(Leagues league, [Remainder] string item)
        {
            Context.Server.Shops.Add(new ShopObject
            {
                UserId = Context.User.Id,
                League = league,
                Item = item
            });

            return ReplyAsync($"Your humble servant thanks you, my God. *`{item}` has been added to your shop.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Clear"), Summary("Clears all items from your shop."), Remarks("Shop Clear")]
        public Task ClearAsync()
        {
            foreach (ShopObject item in Context.Server.Shops.Where(s => s.UserId == Context.User.Id).ToList())
                Context.Server.Shops.Remove(item);
            return ReplyAsync($"Your trust is the only reward I need, my Lord of light. *All items have been removed from your shop.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Delete"), Summary("Deletes the item from your shop. Item has to be an exact match to what you want to delete"), Remarks("Shop Delete <league> <item>")]
        public Task DeleteAsync(Leagues league, [Remainder] string item)
        {
            if (!Context.Server.Shops.Any(s => s.Item == item && s.League == league && s.UserId == Context.User.Id))
                return ReplyAsync($"{Extras.Cross} I'm no beast of burden. *`{item}` was not found in your shop.*");

            ShopObject shop = Context.Server.Shops.FirstOrDefault(s => s.Item == item && s.League == league && s.UserId == Context.User.Id);
            Context.Server.Shops.Remove(shop);
            return ReplyAsync($"Your trust is the only reward I need, my Lord of light. *`{item}` has been removed from your shop.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Search"), Summary("Searches all shops for the item"), Remarks("Shop Search <item>")]
        public Task SearchAsync([Remainder] string item)
        {
            var shopItems = Context.Server.Shops.Where(x => x.Item.ToLower().Contains(item.ToLower())).Select(x => $"League: **{x.League}**\n{x.Item}\nOwned By: {(Context.Guild.GetUserAsync(x.UserId).GetAwaiter().GetResult()).Mention}\n");
            if (!shopItems.Any())
                return ReplyAsync($"{Extras.Cross} I'm no beast of burden. *`{item}` was not found in any shop.*");
            return PagedReplyAsync(MethodHelper.Pages(shopItems), $"Search Results");
        }

        [Command("SearchLeague"), Summary("Searches all shops for the item, in specified League"), Remarks("Shop SearchLeague <league> <item>")]
        public Task SearchLeagueAsync(Leagues league, [Remainder] string item)
        {
            var shopItems = Context.Server.Shops.Where(x => x.Item.ToLower().Contains(item.ToLower()) && x.League == league).Select(x => $"{x.Item}\nOwned By: {(Context.Guild.GetUserAsync(x.UserId).GetAwaiter().GetResult()).Mention}\n");
            if (!shopItems.Any())
                return ReplyAsync($"{Extras.Cross} I'm no beast of burden. *`{item}` was not found in any {league} shops.*");
            return PagedReplyAsync(MethodHelper.Pages(shopItems), $"League {league} Search Results");
        }

        [Command("User"), Summary("Gets all shops for a user"), Remarks("Shop User [@user]")]
        public Task UserAsync(IGuildUser user = null)
        {
            user = user ?? Context.User as IGuildUser;
            string[] shopItems = Context.Server.Shops.Where(x => x.UserId == user.Id).Select(x => $"League: **{x.League}**\nItem:\n{x.Item}\n").ToArray();
            if (!Context.Server.Shops.Any() || !shopItems.Any())
                return ReplyAsync($"{Extras.Cross} I'm no beast of user. *`{user}` doesn't have any items in their shop.*");
            return PagedReplyAsync(MethodHelper.Pages(shopItems), $"{user.Nickname ?? user.Username}'s Personal Shop");
        }
    }
}