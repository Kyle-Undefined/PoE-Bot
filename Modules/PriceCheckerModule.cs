namespace PoE.Bot.Modules
{
    using Addons;
    using Addons.Preconditions;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using Objects;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [Name("Price Checker Commands"), RequireRole("Price Checker"), Ratelimit]
    public class PriceCheckerModule : BotBase
    {
        [Command("Mute", RunMode = RunMode.Async), Remarks("Mutes a user for the specified time and reason."), Summary("Mute <@user> <time> <reason>"), RequireChannel("trade-board")]
        public Task MuteAsync(IGuildUser user, TimeSpan time, [Remainder] string reason)
            => GuildHelper.MuteUserAsync(Context, MuteType.Trade, user, time, reason);

        [Command("Price"), Remarks("Adds, Deletes, or Updates the price for the currency."), Summary("Price <action> <league: Standard, Hardcore, Challenge, ChallengeHC> <name: Replace spaces with _ OR The Alias you're Updating or Deleting> <quantity> <price> <alias>"), RequireChannel("price-checkers")]
        public Task PriceAsync(CommandAction action, Leagues league, string name, double quantity = double.NaN, double price = double.NaN, [Remainder] string alias = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (Context.Server.Prices.Any(p => p.Name == name && p.League == league))
                        return ReplyAsync($"{Extras.Cross} The throne is the most devious trap of them all. *`{name}` is already in the `{league}` list.*");
                    if (double.IsNaN(quantity))
                        quantity = 0;
                    if (double.IsNaN(price))
                        price = 0;

                    Context.Server.Prices.Add(new PriceObject
                    {
                        League = league,
                        Name = name,
                        Quantity = quantity,
                        Price = price,
                        Alias = string.Join(", ", alias.Split(" ")).ToLower(),
                        LastUpdated = DateTime.Now,
                        UserId = Context.User.Id
                    });

                    Embed embed = Extras.Embed(Extras.Added)
                        .AddField("Leage", league)
                        .AddField("Name", name.Replace("_", " "))
                        .AddField("Alias", string.Join(", ", alias.Split(" ")).ToLower())
                        .AddField("Quantity", quantity)
                        .AddField("Price", price)
                        .AddField("Last Updated", DateTime.Now)
                        .WithAuthor(Context.User)
                        .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                        .Build();

                    return ReplyAsync(embed: embed, save: DocumentType.Server);

                case CommandAction.Delete:
                    if (!Context.Server.Prices.Any(p => p.Name.ToLower() == name.ToLower() && p.League == league))
                        return ReplyAsync($"{Extras.Cross} I'm not smart enough for that ... yet. *`{name}` is not in the `{league}` list.*");

                    Context.Server.Prices.Remove(Context.Server.Prices.FirstOrDefault(p => p.Name.ToLower() == name.ToLower() && p.League == league));
                    return ReplyAsync($"The very land heeds to my command. *`{name}` was deleted from the `{league}` list.* {Extras.OkHand}", save: DocumentType.Server);

                case CommandAction.Update:
                    if (!Context.Server.Prices.Any(p => p.Alias.Contains(name.ToLower()) && p.League == league))
                        return ReplyAsync($"{Extras.Cross} The throne is the most devious trap of them all. *`{name}` is not in the `{league}` list.*");
                    if (double.IsNaN(quantity))
                        quantity = 0;
                    if (double.IsNaN(price))
                        price = 0;

                    PriceObject priceObject = Context.Server.Prices.FirstOrDefault(p => p.Alias.Contains(name.ToLower()) && p.League == league);
                    Context.Server.Prices.Remove(priceObject);

                    priceObject.Quantity = quantity;
                    priceObject.Price = price;
                    priceObject.LastUpdated = DateTime.Now;
                    priceObject.UserId = Context.User.Id;
                    if (!(alias is null))
                        priceObject.Alias = string.Join(", ", alias.Split(" ")).ToLower();

                    Context.Server.Prices.Add(priceObject);

                    Embed embedUpdate = Extras.Embed(Extras.Added)
                        .AddField("Leage", league)
                        .AddField("Name", priceObject.Name.Replace("_", " "))
                        .AddField("Alias", priceObject.Alias)
                        .AddField("Quantity", quantity)
                        .AddField("Price", price)
                        .AddField("Last Updated", DateTime.Now)
                        .WithAuthor(Context.User)
                        .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                        .Build();

                    return ReplyAsync(embed: embedUpdate, save: DocumentType.Server);

                default:
                    return ReplyAsync($"{Extras.Cross} action is either `Add`, `Delete`, or `Update`.");
            }
        }

        [Command("Purge"), Alias("Prune"), Remarks("Deletes Messages, and can specify a user"), Summary("Purge [amount] [@user]"), RequireChannels(new string[] { "trade-board", "price-checks" })]
        public Task PurgeAsync(int amount = 20, IGuildUser user = null)
        {
            if (user is null)
            {
                (Context.Channel as SocketTextChannel).DeleteMessagesAsync(MethodHelper.RunSync(Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync()))
                .ContinueWith(x => ReplyAndDeleteAsync($"Beauty will grow from your remains. *Deleted `{amount}` messages.* {Extras.OkHand}", TimeSpan.FromSeconds(5)));
                return GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, Context.User, Context.User, CaseType.Purge, $"Purged {amount} Messages in #{Context.Channel.Name}");
            }
            else
            {
                (Context.Channel as SocketTextChannel).DeleteMessagesAsync(MethodHelper.RunSync(Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync()).Where(x => x.Author.Id == user.Id))
                .ContinueWith(x => ReplyAndDeleteAsync($"Beauty will grow from your remains. *Deleted `{amount}` of `{user}`'s messages.* {Extras.OkHand}", TimeSpan.FromSeconds(5)));
                return GuildHelper.LogAsync(Context.DatabaseHandler, Context.Guild, Context.User, Context.User, CaseType.Purge, $"Purged {amount} of {user}'s Messages #{Context.Channel.Name}");
            }
        }

        [Command("PriceReset"), Remarks("Resets all the prices for items to 0 for specified league reset."), Summary("PriceReset <league: Defaults to Challenge>"), RequireChannel("price-checkers")]
        public Task ResetAsync(Leagues league = Leagues.Challenge)
        {
            foreach (PriceObject price in Context.Server.Prices.Where(x => x.League == league).ToArray())
            {
                Context.Server.Prices.Remove(price);
                price.Quantity = 0;
                price.Price = 0;
                price.LastUpdated = DateTime.Now;
                price.UserId = Context.User.Id;
                Context.Server.Prices.Add(price);
            }

            return ReplyAsync($"For Tukohama! *All prices have been reset for the {league} League.* {Extras.OkHand}", save: DocumentType.Server);
        }

        [Command("Warn", RunMode = RunMode.Async), Remarks("Warns a user with a specified reason."), Summary("Warn <@user> <reason>"), RequireChannel("trade-board")]
        public Task WarnAysnc(IGuildUser user, [Remainder] string reason)
            => GuildHelper.WarnUserAsync(Context, user, reason, MuteType.Trade);
    }
}