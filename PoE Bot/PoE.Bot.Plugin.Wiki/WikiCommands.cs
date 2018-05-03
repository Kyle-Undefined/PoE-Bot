using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using PoE.Bot.Attributes;
using PoE.Bot.Commands;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Plugin.Wiki
{
    public class WikiCommands : ICommandModule
    {
        public string Name { get { return "PoE.Bot.Plugin.Wiki Module"; } }
        private string SearchURL = "http://pathofexile.gamepedia.com/api.php?action=opensearch&search=";
        private string ParseURL = "http://pathofexile.gamepedia.com/api.php?action=parse&prop=text&format=json&page=";
        private string SectionsURL = "http://pathofexile.gamepedia.com/api.php?action=parse&prop=sections&format=json&page=";
        private string ParseSectionsURL = "http://pathofexile.gamepedia.com/api.php?action=parse&prop=text&format=json&section={0}&page={1}";

        [Command("wiki", "Searches an item on the PoE Wiki", CheckPermissions = false)]
        public async Task wiki(CommandContext ctx,
            [ArgumentParameter("Item to search for", true)] string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                throw new ArgumentException("You must enter a item.");

            EmbedBuilder builder = await GetWikiItem(item);

            await ctx.Channel.SendMessageAsync("", false, builder.Build());
        }

        private async Task<EmbedBuilder> GetWikiItem(string item)
        {
            using (var httpClient = new HttpClient())
            {
                var jsonSearch = await httpClient.GetStringAsync(SearchURL + WebUtility.UrlEncode(item));

                JArray jsonSearchArray = JArray.Parse(jsonSearch);
                dynamic jObj = JsonConvert.DeserializeObject(jsonSearchArray[1].ToString());

                if (jObj.Count > 0)
                {
                    int wikiMultiIndex = 0;

                    if (jObj.Count > 1)
                    {
                        for (int i = 0; i < jObj.Count; i++)
                        {
                            if(jsonSearchArray[1][i].ToString().ToLower() == item.ToLower())
                            {
                                wikiMultiIndex = i;
                                break;
                            }
                        }
                    }

                    string itemName = (wikiMultiIndex > 0 ? jsonSearchArray[1][wikiMultiIndex].ToString() : jsonSearchArray[1].First.ToString());
                    string itemURL = (wikiMultiIndex > 0 ? jsonSearchArray[3][wikiMultiIndex].ToString() : jsonSearchArray[3].First.ToString());
                    string wikiPage = itemURL.Remove(0, itemURL.LastIndexOf('/') + 1);

                    var jsonParse = await httpClient.GetStringAsync(ParseURL + wikiPage);

                    if (jsonParse.Contains("Redirect"))
                    {
                        JObject rJson = JObject.Parse(jsonParse);
                        var rHtmlDoc = new HtmlDocument();
                        rHtmlDoc.LoadHtml(rJson["parse"]["text"]["*"].ToString());
                        wikiPage = rHtmlDoc.DocumentNode.SelectSingleNode("//ul[@class=\"redirectText\"]//a").InnerText.Replace(" ", "_");
                        jsonParse = await httpClient.GetStringAsync(ParseURL + wikiPage);
                    }

                    JObject json = JObject.Parse(jsonParse);

                    EmbedBuilder builder = new EmbedBuilder();

                    var endstring = json["parse"]["text"]["*"].ToString();
                    int itemType = 0;

                    var rarityList = new string[]
                    {
                    "item-box -normal",
                    "item-box -unique",
                    "item-box -divicard",
                    "item-box -gem",
                    "item-box -currency"
                    };

                    var rarityColors = new Color[]
                    {
                    Color.LightGrey,
                    Color.Red,
                    Color.Blue,
                    Color.Teal,
                    Color.Gold
                    };

                    for (int i = 0; i < rarityList.Length; i++)
                    {
                        if (endstring.IndexOf(rarityList[i]) > 0 && (endstring.IndexOf(rarityList[i]) < 60))
                        {
                            builder.WithColor(rarityColors[i]);
                            itemType = i;
                        }
                    }

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(endstring);

                    string title = string.Empty;
                    string description = string.Empty;

                    switch (itemType)
                    {
                        case 0:
                            title = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"header -single\"]").InnerHtml;
                            break;
                        case 1:
                            title = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"header -double\"]").InnerHtml.Replace("<br>", " ");
                            description = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"group -textwrap tc -flavour\"]").InnerHtml.Replace("<br>", " ");
                            break;
                        case 2:
                            title = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-header\"]").InnerHtml.Replace("<br>", " ");
                            description = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-flavour text-color -flavour\"]").InnerHtml.Replace("<br>", " ").Replace("<span>", "").Replace("</span>", "");
                            break;
                        case 3:
                            title = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"header -single\"]").InnerHtml;
                            description = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"group -textwrap tc -gemdesc\"]").InnerHtml.Replace("&#39;", "'");
                            break;
                        case 4:
                            title = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"header -single\"]").InnerHtml;
                            break;
                    }

                    HtmlNodeCollection image = null;

                    if (itemType == 2)
                    {
                        var stackNumber = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-stack\"]").InnerHtml.Replace("<br>", " ");
                        var reward = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-reward\"]").InnerText;
                        image = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"divicard-art\"]//img");

                        builder.AddField("Stack", $"```{stackNumber.Trim()}```")
                            .AddField("Reward", $"```{AddSpacesToSentence(reward.Trim())}```");

                        var jsonParseSections = await httpClient.GetStringAsync(SectionsURL + wikiPage);
                        JObject jSectionsObj = JObject.Parse(jsonParseSections);
                        string sectionNumber = string.Empty;

                        for (var i = 0; i < jSectionsObj["parse"]["sections"].Count(); i++)
                        {
                            if (jSectionsObj["parse"]["sections"][i]["line"].ToString().Contains("Drop restriction"))
                                sectionNumber = jSectionsObj["parse"]["sections"][i]["index"].ToString();
                        }

                        if (!string.IsNullOrEmpty(sectionNumber))
                        {
                            var jsonSection = await httpClient.GetStringAsync(String.Format(ParseSectionsURL, sectionNumber, wikiPage));
                            JObject jSection = JObject.Parse(jsonSection);

                            var sectionstring = jSection["parse"]["text"]["*"].ToString();
                            var htmlSectionDoc = new HtmlDocument();
                            bool dropOverride = false;
                            htmlSectionDoc.LoadHtml(sectionstring);

                            var dropAreas = htmlSectionDoc.DocumentNode.SelectNodes("//ul//li");

                            if (dropAreas == null)
                            {
                                dropAreas = htmlSectionDoc.DocumentNode.SelectNodes("//p");
                                dropOverride = true;
                            }

                            StringBuilder dropSB = new StringBuilder();

                            foreach (var drop in dropAreas)
                            {
                                var htmlDropSectionDoc = new HtmlDocument();
                                string dropStr = drop.InnerHtml;
                                string d;

                                htmlDropSectionDoc.LoadHtml(dropStr);

                                try
                                {
                                    d = htmlDropSectionDoc.DocumentNode.SelectSingleNode("//span[@class=\"c-item-hoverbox\"]//a").InnerText;
                                }
                                catch
                                {
                                    d = (dropOverride) ? drop.InnerText : drop.InnerText.Substring(1);
                                }

                                dropSB.AppendLine(d);
                            }

                            builder.AddField("Drop Restrictions", $"```{dropSB.ToString().Trim()}```");
                        }
                        else
                        {
                            var acquisition = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"group -textwrap\"]//a");
                            StringBuilder dropSB = new StringBuilder();

                            foreach (var drop in acquisition)
                            {
                                dropSB.AppendLine(drop.InnerText);
                            }

                            builder.AddField("Drop Restrictions", $"```{dropSB.ToString().Trim()}```");
                        }
                    }
                    else
                    {
                        var itemMods = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"infobox-page-container\"]//em[@class=\"tc -default\"]");
                        string vaalsouls = string.Empty;

                        if (itemType == 3 && title.Contains("Vaal"))
                        {
                            var vaal = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"infobox-page-container\"]//em[@class=\"tc -value\"]");
                            vaalsouls = vaal[1].InnerText + vaal[2].InnerText;
                        }

                        var modRolls = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"infobox-page-container\"]//span[@class=\"group -textwrap tc -mod\"]");
                        image = htmlDoc.DocumentNode.SelectNodes("//a[@class=\"image\"]//img");
                        List<string> itemModsArray = new List<string>();
                        List<string> Mods = new List<string>();

                        if(itemMods != null)
                        {
                            foreach (var mod in itemMods)
                            {
                                if (!mod.InnerText.Contains("uality") && !mod.InnerText.StartsWith("Level"))
                                {
                                    itemModsArray.Add(mod.InnerText + vaalsouls);
                                    vaalsouls = string.Empty;
                                }
                            }

                            foreach (var imod in itemModsArray)
                            {
                                string[] s = imod.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                                if (s.Length > 1)
                                {
                                    builder.AddField(s[0], $"```{s[1].Replace("&#8211;", "-").Trim()}```");
                                }
                                else
                                {
                                    builder.AddField(s[0], "------------------------------------------");
                                }
                            }
                        }

                        StringBuilder sb = new StringBuilder();

                        if (modRolls != null)
                        {
                            foreach (var modRoll in modRolls)
                            {
                                var str = modRoll.InnerHtml;
                                str = str.Replace("<br>", Environment.NewLine);
                                str = StripTagsCharArray(str);
                                sb.AppendLine(str);
                            }

                            builder.AddField("Mods", $"```{sb.ToString().Trim()}```");
                        }

                        var jsonParseSections = await httpClient.GetStringAsync(SectionsURL + wikiPage);
                        JObject jSectionsObj = JObject.Parse(jsonParseSections);
                        string sectionNumber = string.Empty;
                        bool hasDivCards = false;
                        bool hasVendorRecipe = false;

                        for (var i = 0; i < jSectionsObj["parse"]["sections"].Count(); i++)
                        {
                            if (jSectionsObj["parse"]["sections"][i]["line"].ToString().Contains("Divination card"))
                            {
                                hasDivCards = true;
                                sectionNumber = jSectionsObj["parse"]["sections"][i]["index"].ToString();
                            }
                            else if (jSectionsObj["parse"]["sections"][i]["line"].ToString().Contains("Vendor recipe"))
                                hasVendorRecipe = true;
                        }

                        if (hasDivCards)
                        {
                            var jsonSection = await httpClient.GetStringAsync(String.Format(ParseSectionsURL, sectionNumber, wikiPage));
                            JObject jSection = JObject.Parse(jsonSection);

                            var sectionstring = jSection["parse"]["text"]["*"].ToString();
                            var htmlSectionDoc = new HtmlDocument();
                            htmlSectionDoc.LoadHtml(sectionstring);

                            var divCards = htmlSectionDoc.DocumentNode.SelectNodes("//ul//li");

                            StringBuilder cardSB = new StringBuilder();

                            foreach (var card in divCards)
                            {
                                var htmlDivSectionDoc = new HtmlDocument();
                                var fullString = card.InnerHtml;
                                string beginningString;
                                string cardName;

                                htmlDivSectionDoc.LoadHtml(fullString);

                                try
                                {
                                    beginningString = fullString.Substring(0, fullString.IndexOf("<span")).Substring(1);
                                    cardName = htmlDivSectionDoc.DocumentNode.SelectSingleNode("//span[@class=\"c-item-hoverbox\"]//a").InnerText;
                                }
                                catch
                                {
                                    beginningString = fullString.Substring(0, fullString.IndexOf("<a")).Substring(1);
                                    beginningString = beginningString.Substring(0, beginningString.LastIndexOf(" ") - 2);
                                    cardName = htmlDivSectionDoc.DocumentNode.SelectSingleNode("//a").InnerText;
                                }

                                cardSB.AppendLine(beginningString + cardName);
                            }

                            builder.AddField("Divination Cards", $"```{cardSB.ToString().Trim()}```");

                        }

                        if (hasVendorRecipe)
                            builder.AddField("Vendor Recipes", "You can view the vendor recipes for this item here: https://pathofexile.gamepedia.com/Vendor_recipe_system", true);
                    }

                    builder.WithTitle(title)
                        .WithDescription(description)
                        .WithCurrentTimestamp()
                        .WithUrl(itemURL);

                    var imageURL = image[0].OuterHtml.Substring(0, image[0].OuterHtml.IndexOf("width") - 2);
                    imageURL = imageURL.Substring(image[0].OuterHtml.IndexOf("src") + 5);
                    builder.WithImageUrl(imageURL);

                    return builder;
                }
                else
                {
                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle("Oops! No items were found.")
                        .WithDescription("Please make sure you type in the name correctly, and try again.")
                        .WithColor(new Color(255, 127, 0))
                        .WithCurrentTimestamp();

                    return builder;
                }
            }
        }

        private static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        private string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
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
    }
}
