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
            [ArgumentParameter("Name of the currency.", true)] string Name,
            [ArgumentParameter("Alias of the currency.", true)] string Alias,
            [ArgumentParameter("Quanity of the currency, per Chaos.", true)] Double Quantity,
            [ArgumentParameter("Price of the currency, in Chaos.", true)] Double Price)
        {
            var chn = ctx.Channel;

            PricePlugin.Instance.AddCurrency(Name, Alias, Quantity, Price, DateTime.Now);
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
            [ArgumentParameter("Alias of the currency.", true)] string Alias,
            [ArgumentParameter("Quanity of the currency, per Chaos.", true)] Double Quantity,
            [ArgumentParameter("Price of the currency, in Chaos.", true)] Double Price)
        {
            var chn = ctx.Channel;
            var currName = PricePlugin.Instance.GetCurrencyName(Alias);
            var displayName = currName;
            PricePlugin.Instance.RemoveCurrency(currName, Alias, Quantity, Price);
            PricePlugin.Instance.AddCurrency(currName, Alias, Quantity, Price, DateTime.Now);
            var embed = this.PrepareEmbed("Success", "Currency was updated successfully.", EmbedType.Success);

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

        [Command("price", "Pulls the price for the requested currency, all values based on Chaos.", Aliases = "", CheckPermissions = false)]
        public async Task Price(CommandContext ctx,
            [ArgumentParameter("Currency to lookup, based on alias name, use pricelist to see all currency prices and aliases.", true)] string Alias)
        {
            var chn = ctx.Channel;
            var curr = PricePlugin.Instance.GetCurrency(Alias);
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
            var embed = this.PrepareEmbed("Current list of currencies and prices", "------------------------------------------", EmbedType.Info);

            foreach (var curr in currency)
            {
                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = curr.Name.Replace("_", " ");
                    x.Value = String.Concat(curr.Alias, "\nCurrent going rate is ", curr.Quantity, " for ", curr.Price, " Chaos.\n", "Last Updated: ", curr.LastUpdated);
                });
            }
            
            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Disclaimer";
                x.Value = "Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.";
            });

            await chn.SendMessageAsync("", false, embed);
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
