namespace PoE.Bot.Services
{
	using Discord;
	using HtmlAgilityPack;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using PoE.Bot.Attributes;
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;
	using System.Web;

	[Service]
	public class WikiService
	{
		private readonly string _endpoint = "https://pathofexile.gamepedia.com/api.php";
		private readonly HttpClient _httpClient;

		public WikiService(HttpClient httpClient) => _httpClient = httpClient;

		public async Task<Embed> BuildWikiItemEmbed(string item)
		{
			var openSearchArray = JArray.Parse(await _httpClient.GetStringAsync(_endpoint + $"?action=opensearch&search={WebUtility.UrlEncode(item)}&suggest=true&redirects=resolve").ConfigureAwait(false));
			dynamic openSearch = JsonConvert.DeserializeObject(openSearchArray[1].ToString());
			var cargoQuery = await _httpClient.GetStringAsync(_endpoint + $"?action=cargoquery&tables=items&fields=name,flavour_text,rarity,inventory_icon&where=name=%22{item}%22&format=json").ConfigureAwait(false);
			var cargo = JsonConvert.DeserializeObject<CargoRoot>(cargoQuery);

			if (openSearch.Count > 0)
			{
				var index = 0;

				if (openSearch.Count > 1)
				{
					for (var i = 0; i < openSearch.Count; i++)
					{
						if (string.Equals(openSearchArray[1][i].ToString(), item, StringComparison.CurrentCultureIgnoreCase))
						{
							index = i;
							break;
						}
					}
				}

				string pageUrl = openSearchArray[3][index].ToString();
				Color rarity = Color.LightGrey;

				var embed = new EmbedBuilder()
					.WithTitle(openSearchArray[1][index].ToString())
					.WithUrl(pageUrl)
					.WithCurrentTimestamp();

				if (cargo.CargoResults.Count > 0)
				{
					switch (cargo.CargoResults[0].CargoItem.Rarity)
					{
						case "Normal":
							rarity = Color.LightGrey;
							break;

						case "Magic":
							rarity = Color.Blue;
							break;

						case "Rare":
							rarity = Color.Gold;
							break;

						case "Unique":
							rarity = Color.Red;
							break;
					}

					var imagePath = "https://pathofexile.gamepedia.com/" + cargo.CargoResults[0].CargoItem.Image;
					var htmlDoc = new HtmlDocument();
					htmlDoc.LoadHtml(await _httpClient.GetStringAsync(imagePath).ConfigureAwait(false));
					var image = htmlDoc.DocumentNode.SelectSingleNode("//div[@class=\"fullImageLink\"]//a").Attributes[0].Value;

					embed.WithImageUrl(image)
						.WithTitle(cargo.CargoResults[0].CargoItem.Name)
						.WithDescription(StripTagsCharArray(HttpUtility.HtmlDecode(cargo.CargoResults[0].CargoItem.FlavourText).Replace("<br>", " ")))
						.WithColor(rarity);
				}

				return embed.Build();
			}
			else
			{
				var builder = new EmbedBuilder();

				builder.WithTitle("Oops! No items were found.")
					.WithDescription("Please make sure you type in the name correctly, and try again.")
					.WithColor(new Color(255, 127, 0))
					.WithCurrentTimestamp();

				return builder.Build();
			}
		}

		private string StripTagsCharArray(string source)
		{
			char[] array = new char[source.Length];
			var arrayIndex = 0;
			var inside = false;

			for (int i = 0; i < source.Length; i++)
			{
				var let = source[i];
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

	public class CargoRoot
	{
		[JsonProperty("cargoquery")]
		public List<CargoResult> CargoResults { get; set; }
	}

	public class CargoResult
	{
		[JsonProperty("title")]
		public CargoItem CargoItem { get; set; }
	}

	public class CargoItem
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("flavour text")]
		public string FlavourText { get; set; }

		[JsonProperty("rarity")]
		public string Rarity { get; set; }

		[JsonProperty("inventory icon")]
		public string Image { get; set; }
	}
}