using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;

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

            var chn = ctx.Channel;
            var alias = string.Join(", ", Alias);

            PricePlugin.Instance.AddCurrency(Name, alias, Quantity, Price, DateTime.Now);
            var embed = this.PrepareEmbed("Success", "Currency was added successfully.", EmbedType.Success);
            var displayName = Name;

            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Name";
                x.Value = displayName.Replace("_", " ");
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Alias";
                x.Value = Alias;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Quantity";
                x.Value = Quantity;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Price";
                x.Value = Price;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Last Updated";
                x.Value = DateTime.Now;
            });

            await chn.SendMessageAsync("", false, embed);
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

            var chn = ctx.Channel;
            var currName = PricePlugin.Instance.GetCurrencyName(Alias);
            var aliases = Alias;

            if (Aliases.Count() > 0)
                aliases = string.Join(", ", Aliases);

            PricePlugin.Instance.RemoveCurrency(currName, Alias);
            PricePlugin.Instance.AddCurrency(currName, aliases, Quantity, Price, DateTime.Now);

            var embed = this.PrepareEmbed("Success", "Currency was updated successfully.", EmbedType.Success);

            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Name";
                x.Value = currName.Replace("_", " ");
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Alias";
                x.Value = aliases;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Quantity";
                x.Value = Quantity;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Price";
                x.Value = Price;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Last Updated";
                x.Value = DateTime.Now;
            });

            await chn.SendMessageAsync("", false, embed);
        }

        [Command("price", "Pulls the price for the requested currency, all values based on Chaos.", Aliases = "", CheckPermissions = false)]
        public async Task Price(CommandContext ctx,
            [ArgumentParameter("Currency to lookup, based on alias name, use pricelist to see all currency prices and aliases.", true)] string Alias)
        {
            if (string.IsNullOrWhiteSpace(Alias))
                throw new ArgumentException("You must enter an alias.");

            var chn = ctx.Channel;
            var alias = (Alias.Contains(":") ? Alias.Replace(":", "") : Alias);
            var curr = PricePlugin.Instance.GetCurrency(alias);
            var embed = this.PrepareEmbed(curr.Name.Replace("_", " "), curr.Alias, EmbedType.Info);

            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Ratio";
                x.Value = String.Concat("Current going rate is ", curr.Quantity, " for ", curr.Price, " Chaos.");
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Last Updated";
                x.Value = curr.LastUpdated;
            });
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Disclaimer";
                x.Value = "Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.";
            });

            await chn.SendMessageAsync("", false, embed);
        }

        [Command("pricelist", "Pulls the price for the all currency", Aliases = "prices;plist", CheckPermissions = false)]
        public async Task Price(CommandContext ctx)
        {
            var chn = ctx.Channel;
            var currency = PricePlugin.Instance.GetCurrency();

            var embed = this.PrepareEmbed("Disclaimer", "Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.", EmbedType.Info);

            foreach (var curr in currency.Reverse().Skip(currency.Count()/2))
            {
                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = curr.Name.Replace("_", " ");
                    x.Value = String.Concat(curr.Alias, "\nCurrent going rate is ", curr.Quantity, " for ", curr.Price, " Chaos.\n", "Last Updated: ", curr.LastUpdated);
                });
            }

            var embed2 = this.PrepareEmbed(EmbedType.Info);
            embed2.Timestamp = DateTime.Now;

            foreach (var curr in currency.Skip(currency.Count() / 2))
            {
                embed2.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = curr.Name.Replace("_", " ");
                    x.Value = String.Concat(curr.Alias, "\nCurrent going rate is ", curr.Quantity, " for ", curr.Price, " Chaos.\n", "Last Updated: ", curr.LastUpdated);
                });
            }

            embed2.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Disclaimer";
                x.Value = "Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.";
            });

            await chn.SendMessageAsync("", false, embed);
            await chn.SendMessageAsync("", false, embed2);
        }

        private EmbedBuilder PrepareEmbed(EmbedType type)
        {
            var embed = new EmbedBuilder();
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
            embed.Timestamp = DateTime.Now;
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
    }
}
