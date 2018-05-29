using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;

namespace PoE.Bot.Plugin.Price
{
    public class PriceCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.Price Module"; } }

        [Command("priceadd", "Adds the price for the currency.", Aliases = "addprice;padd;addp", CheckerId = "CorePriceChecker", CheckPermissions = true)]
        public async Task PriceAdd(CommandContext ctx,
            [ArgumentParameter("Name of the currency, spaces should be filled with \"_\". Ex: Orb_of_Alchemy, Gemcutter's_Prism", true)] string Name,
            [ArgumentParameter("Quanity of the currency, per Chaos.", true)] Double Quantity,
            [ArgumentParameter("Price of the currency, in Chaos.", true)] Double Price,
            [ArgumentParameter("Aliases of the currency, everything it can be known for.", true)] params string[] Alias)

        {
            if(string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("You must enter a name.");
            if(Double.IsNaN(Quantity))
                throw new ArgumentException("You must add a quantity.");
            if(Double.IsNaN(Price))
                throw new ArgumentException("You must add a price.");
            if (Alias.Count() == 0)
                throw new ArgumentException("You must add aliases this currency is known as.");

            var alias = string.Join(", ", Alias);
            alias = alias.ToLower();

            PricePlugin.Instance.AddCurrency(Name, alias, Quantity, Price, DateTime.Now);
            var embed = this.PrepareEmbed("Success", "Currency was added successfully.", EmbedType.Success);
            var displayName = Name;

            embed.AddField("Name", $"```{displayName.Replace("_", " ")}```")
                .AddField("Alias", $"```{alias.ToLower()}```")
                .AddField("Quantity", $"```{Quantity}```")
                .AddField("Price", $"```{Price}```")
                .AddField("Last Updated", $"```{DateTime.Now}```")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("priceupdate", "Updates the price for the currency.", Aliases = "updateprice;pupdate;updatep", CheckerId = "CorePriceChecker", CheckPermissions = true)]
        public async Task PriceUpdate(CommandContext ctx,
            [ArgumentParameter("Alias of the currency to update.", true)] string Alias,
            [ArgumentParameter("Quanity of the currency, per Chaos.", true)] Double Quantity,
            [ArgumentParameter("Price of the currency, in Chaos.", true)] Double Price,
            [ArgumentParameter("Aliases of the currency, everything it can be known for.", false)] params string[] Aliases)
        {
            if (string.IsNullOrWhiteSpace(Alias))
                throw new ArgumentException("You must enter an alias.");
            if (Double.IsNaN(Quantity))
                throw new ArgumentException("You must add a quantity.");
            if (Double.IsNaN(Price))
                throw new ArgumentException("You must add a price.");

            Alias = Alias.ToLower();
            var currName = PricePlugin.Instance.GetCurrencyName(Alias);
            var aliases = Alias;

            if (Aliases.Count() > 0)
                aliases = string.Join(", ", Aliases);

            aliases = aliases.ToLower();
            PricePlugin.Instance.RemoveCurrency(currName, Alias);
            PricePlugin.Instance.AddCurrency(currName, aliases, Quantity, Price, DateTime.Now);

            var embed = this.PrepareEmbed("Success", "Currency was updated successfully.", EmbedType.Success);
            embed.AddField("Name", $"```{currName.Replace("_", " ")}```")
                .AddField("Alias", $"```{aliases.ToLower()}```")
                .AddField("Quantity", $"```{Quantity}```")
                .AddField("Price", $"```{Price}```")
                .AddField("Last Updated", $"```{DateTime.Now}```")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("price", "Pulls the price for the requested currency, all values based on Chaos.", Aliases = "", CheckPermissions = false)]
        public async Task Price(CommandContext ctx,
            [ArgumentParameter("Currency to lookup, based on alias name, use pricelist to see all currency prices and aliases.", true)] string Alias)
        {
            if (string.IsNullOrWhiteSpace(Alias))
                throw new ArgumentException("You must enter an alias.");

            var alias = Alias.ToLower();

            if (alias.Contains(":"))
            {
                var s = alias.Split(':');
                alias = s[1];
            }

            var curr = PricePlugin.Instance.GetCurrency(alias);

            if (curr == null)
                throw new ArgumentException("Sorry, there was an issue getting the price. Please make sure it's spelled correctly.");

            var embed = this.PrepareEmbed(EmbedType.Info);
            embed.AddField(curr.Name.Replace("_", " "), $"```{curr.Alias}```")
                .AddField("Ratio", $"```{curr.Quantity}:{curr.Price}c```")
                .AddField("Last Updated", $"```{curr.LastUpdated}```")
                .WithFooter("Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.");

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("pricelist", "Pulls the price for the all currency", Aliases = "prices;plist", CheckPermissions = false)]
        public async Task Price(CommandContext ctx)
        {
            var currency = PricePlugin.Instance.GetCurrency();
            var sb = new StringBuilder();

            foreach (var curr in currency)
            {
                sb.Append("```");
                sb.AppendFormat("Name: {0}\nAlias: {1}\nRatio: {2}:{3}c\nLast Updated: {4}", curr.Name.Replace("_", " "), curr.Alias, curr.Quantity, curr.Price, curr.LastUpdated).AppendLine();
                sb.Append("```");
                sb.AppendLine("---------");
            }

            var embedChunks = ChunkString(sb.ToString(), 1024);
            foreach (var chunk in embedChunks)
            {
                var chunkedEmbed = this.PrepareEmbed(EmbedType.Info);
                chunkedEmbed.AddField("Currency List", chunk)
                    .WithFooter("Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.");
                await ctx.Channel.SendMessageAsync("", false, chunkedEmbed.Build());
            }
        }

        [Command("pricereset", "Resets all the prices for items to 0 for league reset", CheckerId = "CorePriceChecker", CheckPermissions = true)]
        public async Task PriceReset(CommandContext ctx)
        {
            var currency = PricePlugin.Instance.GetCurrency();

            foreach (var curr in currency.ToArray())
            {
                PricePlugin.Instance.RemoveCurrency(curr.Name, curr.Alias);
                PricePlugin.Instance.AddCurrency(curr.Name, curr.Alias, 0, 0, DateTime.Now);
            }

            await ctx.Message.DeleteAsync();
        }

        [Command("pricedelete", "Deletes a currency from the system, by alias, should only be used if one is added in wrong", Aliases = "pricedel;pdel", CheckerId = "CorePriceChecker", CheckPermissions = true)]
        public async Task PriceDelete(CommandContext ctx,
            [ArgumentParameter("Currency to delete, based on alias name, use pricelist to see all currency aliases.", true)] string Alias)
        {
            if (string.IsNullOrWhiteSpace(Alias))
                throw new ArgumentException("You must enter an alias.");

            var alias = Alias.ToLower();
            var curr = PricePlugin.Instance.GetCurrency(alias);

            PricePlugin.Instance.RemoveCurrency(curr.Name, curr.Alias);

            var embed = this.PrepareEmbed("Success", "Currency was deleted successfully.", EmbedType.Success);
            embed.AddField("Name", $"```{curr.Name.Replace("_", " ")}```")
                .AddField("Alias", $"```{curr.Alias}```")
                .AddField("Quantity", $"```{curr.Quantity}```")
                .AddField("Price", $"```{curr.Price}```")
                .AddField("Last Updated", $"```{curr.LastUpdated}```")
                .WithAuthor(ctx.User)
                .WithThumbnailUrl(string.IsNullOrEmpty(ctx.User.GetAvatarUrl()) ? ctx.User.GetDefaultAvatarUrl() : ctx.User.GetAvatarUrl());

            await ctx.Channel.SendMessageAsync("", false, embed.Build());
        }

        private EmbedBuilder PrepareEmbed(EmbedType type)
        {
            var embed = new EmbedBuilder();
            embed.WithCurrentTimestamp();
            switch (type)
            {
                case EmbedType.Info:
                    embed.Color = new Color(0, 127, 255);
                    break;

                case EmbedType.Success:
                    embed.Color = new Color(127, 255, 0);
                    break;

                case EmbedType.Warning:
                    embed.Color = new Color(255, 255, 0);
                    break;

                case EmbedType.Error:
                    embed.Color = new Color(255, 127, 0);
                    break;

                default:
                    embed.Color = new Color(255, 255, 255);
                    break;
            }
            return embed;
        }

        private EmbedBuilder PrepareEmbed(string title, string desc, EmbedType type)
        {
            var embed = this.PrepareEmbed(type);
            embed.Title = title;
            embed.Description = desc;
            embed.WithCurrentTimestamp();
            return embed;
        }

        private enum EmbedType : uint
        {
            Unknown,
            Success,
            Error,
            Warning,
            Info
        }

        static IEnumerable<string> ChunkString(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}
