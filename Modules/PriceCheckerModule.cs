namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using Discord.Commands;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;

    [Name("Price Checker Commands"), Group("Price"), RequireRole("Price Checker"), Ratelimit]
    public class PriceCheckerModule : BotBase
    {
        [Command("Add"), Remarks("Adds the price for the currency."), Summary("Price Add <League: Standard, Hardcore, Challenge, ChallengeHC> <Name: Replace spaces with _> <Quantity> <Price> <Alias>"), RequireChannel("price-checkers")]
        public Task AddAsync(Leagues League, string Name, Double Quantity, Double Price, [Remainder] string Alias)
        {
            if (Context.Server.Prices.Where(p => p.Name == Name && p.League == League).Any())
                return ReplyAsync($"{Extras.Cross} The throne is the most devious trap of them all. *`{Name}` is already in the `{League}` list.*");
            Context.Server.Prices.Add(new PriceObject
            {
                League = League,
                Name = Name,
                Quantity = Quantity,
                Price = Price,
                Alias = string.Join(", ", Alias.Split(" ")).ToLower(),
                LastUpdated = DateTime.Now,
                UserId = Context.User.Id
            });

            var Embed = Extras.Embed(Drawing.Green)
                .AddField("Leage", League)
                .AddField("Name", Name.Replace("_", " "))
                .AddField("Alias", string.Join(", ", Alias.Split(" ")).ToLower())
                .AddField("Quantity", Quantity)
                .AddField("Price", Price)
                .AddField("Last Updated", DateTime.Now)
                .WithAuthor(Context.User)
                .WithThumbnailUrl(Context.User.GetAvatarUrl())
                .Build();

            return ReplyAsync(Embed: Embed, Save: 's');
        }

        [Command("Update"), Remarks("Updates the price for the currency."), Summary("Price Update <League: Standard, Hardcore, Challenge, ChallengeHC> <Name: Any Alias> <Quantity> <Price> [Aliases]"), RequireChannel("price-checkers")]
        public Task UpdateAsync(Leagues League, string Name, Double Quantity, Double Price, [Remainder] string Aliases = null)
        {
            if (!Context.Server.Prices.Where(p => p.Alias.Contains(Name.ToLower()) && p.League == League).Any())
                return ReplyAsync($"{Extras.Cross} The throne is the most devious trap of them all. *`{Name}` is not in the `{League}` list.*");

            var price = Context.Server.Prices.FirstOrDefault(p => p.Alias.Contains(Name.ToLower()) && p.League == League);
            Context.Server.Prices.Remove(price);

            price.Quantity = Quantity;
            price.Price = Price;
            price.LastUpdated = DateTime.Now;
            price.UserId = Context.User.Id;
            if (!(Aliases is null))
                price.Alias = string.Join(", ", Aliases.Split(" ")).ToLower();

            Context.Server.Prices.Add(price);

            var Embed = Extras.Embed(Drawing.Green)
                .AddField("Leage", League)
                .AddField("Name", price.Name.Replace("_", " "))
                .AddField("Alias", price.Alias)
                .AddField("Quantity", Quantity)
                .AddField("Price", Price)
                .AddField("Last Updated", DateTime.Now)
                .WithAuthor(Context.User)
                .WithThumbnailUrl(Context.User.GetAvatarUrl())
                .Build();

            return ReplyAsync(Embed: Embed, Save: 's');
        }

        [Command("Reset"), Remarks("Resets all the prices for items to 0 for league reset."), Summary("Price Reset"), RequireChannel("price-checkers")]
        public Task ResetAsync()
        {
            foreach (var Price in Context.Server.Prices.ToArray())
            {
                Context.Server.Prices.Remove(Price);
                Price.Quantity = 0;
                Price.Price = 0;
                Price.LastUpdated = DateTime.Now;
                Price.UserId = Context.User.Id;
                Context.Server.Prices.Add(Price);
            }

            return ReplyAsync($"For Tukohama! *All prices have been reset.* {Extras.OkHand}", Save: 's');
        }

        [Command("Delete"), Remarks("Deletes a currency from the system, by alias, should only be used if one is added in wrong"), Summary("Price Delete <League: Standard, Hardcore, Challenge, ChallengeHC> <Name: Any Alias>"), RequireChannel("price-checkers")]
        public Task DeleteAsync(Leagues League, string Name)
        {
            if (!Context.Server.Prices.Where(p => p.Alias.Contains(Name.ToLower()) && p.League == League).Any())
                return ReplyAsync($"{Extras.Cross} I'm not smart enough for that ... yet. *`{Name}` is not in the `{League}` list.*");
            Context.Server.Prices.Remove(Context.Server.Prices.FirstOrDefault(p => p.Alias.Contains(Name.ToLower()) && p.League == League));
            return ReplyAsync($"The very land heeds to my command. *`{Name}` was deleted from the `{League}` list.* {Extras.OkHand}", Save: 's');
        }

        [Command("Mute", RunMode = RunMode.Async), Remarks("Mutes a user for the specified time and reason."), Summary("Mute <@User> <Time> <Reason>"), RequireChannel("trade-board")]
        public Task MuteAsync(IGuildUser User, TimeSpan Time, [Remainder] string Reason)
            => Context.GuildHelper.MuteUserAsync(Context, MuteType.TRADE, User, Time, Reason);
    }
}
