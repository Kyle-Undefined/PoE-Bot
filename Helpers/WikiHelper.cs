namespace PoE.Bot.Helpers
{
    using Discord;
    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class WikiHelper
    {
        public static async Task<Embed> WikiGetItemAsync(string item)
        {
            string searchURL = "http://pathofexile.gamepedia.com/api.php?action=opensearch&search=";
            string parseURL = "http://pathofexile.gamepedia.com/api.php?action=parse&prop=text&format=json&page=";
            string sectionsURL = "http://pathofexile.gamepedia.com/api.php?action=parse&prop=sections&format=json&page=";
            string parseSectionsURL = "http://pathofexile.gamepedia.com/api.php?action=parse&prop=text&format=json&section={0}&page={1}";

            using (HttpClient httpClient = new HttpClient())
            {
                string jsonSearch = await httpClient.GetStringAsync(searchURL + WebUtility.UrlEncode(item));

                JArray jsonSearchArray = JArray.Parse(jsonSearch);
                dynamic jsonObj = JsonConvert.DeserializeObject(jsonSearchArray[1].ToString());

                if (jsonObj.Count > 0)
                {
                    int wikiMultiIndex = 0;

                    if (jsonObj.Count > 1)
                    {
                        for (int i = 0; i < jsonObj.Count; i++)
                        {
                            if (jsonSearchArray[1][i].ToString().ToLower() == item.ToLower())
                            {
                                wikiMultiIndex = i;
                                break;
                            }
                        }
                    }

                    string itemName = wikiMultiIndex > 0 ? jsonSearchArray[1][wikiMultiIndex].ToString() : jsonSearchArray[1].First.ToString();
                    string itemURL = wikiMultiIndex > 0 ? jsonSearchArray[3][wikiMultiIndex].ToString() : jsonSearchArray[3].First.ToString();
                    string wikiPage = itemURL.Remove(0, itemURL.LastIndexOf('/') + 1);
                    string jsonParse = await httpClient.GetStringAsync(parseURL + wikiPage);

                    if (jsonParse.Contains("Redirect"))
                    {
                        JObject redirectJson = JObject.Parse(jsonParse);
                        HtmlDocument redirectHtmlDoc = new HtmlDocument();
                        redirectHtmlDoc.LoadHtml(redirectJson["parse"]["text"]["*"].ToString());
                        wikiPage = redirectHtmlDoc.DocumentNode.SelectSingleNode("//ul[@class=\"redirectText\"]//a").InnerText.Replace(" ", "_");
                        jsonParse = await httpClient.GetStringAsync(parseURL + wikiPage);
                    }

                    JObject json = JObject.Parse(jsonParse);
                    EmbedBuilder builder = new EmbedBuilder();
                    string endstring = json["parse"]["text"]["*"].ToString();
                    int itemType = 0;

                    string[] rarityList = new string[]
                    {
                    "item-box -normal",
                    "item-box -unique",
                    "item-box -divicard",
                    "item-box -gem",
                    "item-box -currency"
                    };

                    Color[] rarityColors = new Color[]
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

                    HtmlDocument htmlDoc = new HtmlDocument();
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
                            description = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-flavour text-color -flavour\"]").InnerHtml.Replace("<br>", " ").Replace("<span>", string.Empty).Replace("</span>", string.Empty);
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

                    if (itemType is 2)
                    {
                        string stackNumber = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-stack\"]").InnerHtml.Replace("<br>", " ");
                        string reward = htmlDoc.DocumentNode.SelectSingleNode("//span[@class=\"divicard-reward\"]").InnerText;
                        image = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"divicard-art\"]//img");

                        builder.AddField("Stack", $"```{stackNumber.Trim()}```")
                            .AddField("Reward", $"```{AddSpacesToSentence(reward.Trim())}```");

                        string jsonParseSections = await httpClient.GetStringAsync(sectionsURL + wikiPage);
                        JObject jsonSectionsObj = JObject.Parse(jsonParseSections);
                        string sectionNumber = string.Empty;

                        for (int i = 0; i < jsonSectionsObj["parse"]["sections"].Count(); i++)
                        {
                            if (jsonSectionsObj["parse"]["sections"][i]["line"].ToString().Contains("Drop restriction"))
                                sectionNumber = jsonSectionsObj["parse"]["sections"][i]["index"].ToString();
                        }

                        if (!string.IsNullOrEmpty(sectionNumber))
                        {
                            string jsonSection = await httpClient.GetStringAsync(string.Format(parseSectionsURL, sectionNumber, wikiPage));
                            JObject jsonObjectSection = JObject.Parse(jsonSection);

                            string sectionstring = jsonObjectSection["parse"]["text"]["*"].ToString();
                            HtmlDocument htmlSectionDoc = new HtmlDocument();
                            bool dropOverride = false;
                            htmlSectionDoc.LoadHtml(sectionstring);

                            HtmlNodeCollection dropAreas = htmlSectionDoc.DocumentNode.SelectNodes("//ul//li");

                            if (dropAreas is null)
                            {
                                dropAreas = htmlSectionDoc.DocumentNode.SelectNodes("//p");
                                dropOverride = true;
                            }

                            StringBuilder dropSB = new StringBuilder();

                            foreach (HtmlNode drop in dropAreas)
                            {
                                HtmlDocument htmlDropSectionDoc = new HtmlDocument();
                                string dropStr = drop.InnerHtml;
                                string d;

                                htmlDropSectionDoc.LoadHtml(dropStr);

                                try
                                {
                                    d = htmlDropSectionDoc.DocumentNode.SelectSingleNode("//span[@class=\"c-item-hoverbox\"]//a").InnerText;
                                }
                                catch
                                {
                                    d = dropOverride ? drop.InnerText : drop.InnerText.Substring(1);
                                }

                                dropSB.AppendLine(d);
                            }

                            builder.AddField("Drop Restrictions", $"```{dropSB.ToString().Trim()}```");
                        }
                        else
                        {
                            HtmlNodeCollection acquisition = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"group -textwrap\"]//a");
                            StringBuilder dropSB = new StringBuilder();

                            foreach (HtmlNode drop in acquisition)
                                dropSB.AppendLine(drop.InnerText);

                            builder.AddField("Drop Restrictions", $"```{dropSB.ToString().Trim()}```");
                        }
                    }
                    else
                    {
                        HtmlNodeCollection itemMods = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"infobox-page-container\"]//em[@class=\"tc -default\"]");
                        string vaalsouls = string.Empty;

                        if (itemType is 3 && title.Contains("Vaal"))
                        {
                            HtmlNodeCollection vaal = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"infobox-page-container\"]//em[@class=\"tc -value\"]");
                            vaalsouls = vaal[1].InnerText + vaal[2].InnerText;
                        }

                        HtmlNodeCollection modRolls = htmlDoc.DocumentNode.SelectNodes("//span[@class=\"infobox-page-container\"]//span[@class=\"group -textwrap tc -mod\"]");
                        image = htmlDoc.DocumentNode.SelectNodes("//a[@class=\"image\"]//img");
                        List<string> itemModsArray = new List<string>();

                        if (!(itemMods is null))
                        {
                            foreach (HtmlNode mod in itemMods)
                            {
                                if (!mod.InnerText.Contains("uality") && !mod.InnerText.StartsWith("Level"))
                                {
                                    itemModsArray.Add(mod.InnerText + vaalsouls);
                                    vaalsouls = string.Empty;
                                }
                            }

                            foreach (string imod in itemModsArray)
                            {
                                string[] s = imod.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                                if (s.Length > 1)
                                    builder.AddField(s[0], $"```{s[1].Replace("&#8211;", "-").Trim()}```");
                                else
                                    builder.AddField(s[0], "------------------------------------------");
                            }
                        }

                        StringBuilder sb = new StringBuilder();

                        if (!(modRolls is null))
                        {
                            foreach (HtmlNode modRoll in modRolls)
                            {
                                string str = modRoll.InnerHtml;
                                str = str.Replace("<br>", Environment.NewLine);
                                str = StripTagsCharArray(str);
                                sb.AppendLine(str);
                            }

                            builder.AddField("Mods", $"```{sb.ToString().Trim()}```");
                        }

                        string jsonParseSections = await httpClient.GetStringAsync(sectionsURL + wikiPage);
                        JObject jsonSectionsObj = JObject.Parse(jsonParseSections);
                        string sectionNumber = string.Empty;
                        bool hasDivCards = false;
                        bool hasVendorRecipe = false;

                        for (int i = 0; i < jsonSectionsObj["parse"]["sections"].Count(); i++)
                        {
                            if (jsonSectionsObj["parse"]["sections"][i]["line"].ToString().Contains("Divination card"))
                            {
                                hasDivCards = true;
                                sectionNumber = jsonSectionsObj["parse"]["sections"][i]["index"].ToString();
                            }
                            else if (jsonSectionsObj["parse"]["sections"][i]["line"].ToString().Contains("Vendor recipe"))
                                hasVendorRecipe = true;
                        }

                        if (hasDivCards)
                        {
                            string jsonSection = await httpClient.GetStringAsync(string.Format(parseSectionsURL, sectionNumber, wikiPage));
                            JObject jsonObjectSection = JObject.Parse(jsonSection);

                            string sectionstring = jsonObjectSection["parse"]["text"]["*"].ToString();
                            HtmlDocument htmlSectionDoc = new HtmlDocument();
                            htmlSectionDoc.LoadHtml(sectionstring);

                            HtmlNodeCollection divCards = htmlSectionDoc.DocumentNode.SelectNodes("//ul//li");

                            StringBuilder cardSB = new StringBuilder();

                            foreach (HtmlNode card in divCards)
                            {
                                HtmlDocument htmlDivSectionDoc = new HtmlDocument();
                                string fullString = card.InnerHtml;
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

                    string imageURL = image[0].OuterHtml.Substring(0, image[0].OuterHtml.IndexOf("width") - 2);
                    imageURL = imageURL.Substring(image[0].OuterHtml.IndexOf("src") + 5);
                    builder.WithImageUrl(imageURL);

                    return builder.Build();
                }
                else
                {
                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle("Oops! No items were found.")
                        .WithDescription("Please make sure you type in the name correctly, and try again.")
                        .WithColor(new Color(255, 127, 0))
                        .WithCurrentTimestamp();

                    return builder.Build();
                }
            }
        }

        private static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && !(text[i - 1] is ' '))
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        private static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let is '<')
                {
                    inside = true;
                    continue;
                }
                if (let is '>')
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
    }
}