﻿using System;
using System.Collections.Generic;
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
                x.Value = alias.ToLower();
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

            await chn.SendMessageAsync("", false, embed.Build());
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
                x.Value = aliases.ToLower();
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

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("price", "Pulls the price for the requested currency, all values based on Chaos.", Aliases = "", CheckPermissions = false)]
        public async Task Price(CommandContext ctx,
            [ArgumentParameter("Currency to lookup, based on alias name, use pricelist to see all currency prices and aliases.", true)] string Alias)
        {
            if (string.IsNullOrWhiteSpace(Alias))
                throw new ArgumentException("You must enter an alias.");

            var chn = ctx.Channel;
            var alias = Alias.ToLower();

            if (alias.Contains(":"))
            {
                var s = alias.Split(':');
                alias = s[1];
            }

            var curr = PricePlugin.Instance.GetCurrency(alias);

            if (curr == null)
                throw new ArgumentException("Sorry, there was an issue getting the price. Please make sure it's spelled correctly.");

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

            await chn.SendMessageAsync("", false, embed.Build());
        }

        [Command("pricelist", "Pulls the price for the all currency", Aliases = "prices;plist", CheckPermissions = false)]
        public async Task Price(CommandContext ctx)
        {
            var chn = ctx.Channel;
            var currency = PricePlugin.Instance.GetCurrency();
            var sb = new StringBuilder();

            foreach (var curr in currency)
            {
                sb.AppendFormat("**Name**: {0}", curr.Name.Replace("_", " ")).AppendLine();
                sb.AppendFormat("**Alias**: {0}", curr.Alias).AppendLine();
                sb.AppendFormat("**Going Rate**: {0}", curr.Quantity + " for " + curr.Price + " Chaos.").AppendLine();
                sb.AppendFormat("**Last Updated**: {0}", curr.LastUpdated).AppendLine();
                sb.AppendLine("---------");
            }

            var embedChunks = ChunkString(sb.ToString(), 1024);
            foreach (var chunk in embedChunks)
            {
                var chunkedEmbed = this.PrepareEmbed("Disclaimer", "Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.", EmbedType.Info);
                chunkedEmbed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Currency List";
                    x.Value = chunk;
                });
                await chn.SendMessageAsync("", false, chunkedEmbed.Build());
            }
        }

        [Command("pricereset", "Resets all the prices for items to 0 for league reset", CheckerId = "CoreAdminChecker", CheckPermissions = true, RequiredPermission = Permission.Administrator)]
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

        static IEnumerable<string> ChunkString(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}
