namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using HtmlAgilityPack;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net.Http;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Serialization;

	public class RssData
	{
		[XmlElement("item")]
		public List<RssItem> Items { get; set; }
	}

	[XmlRoot("rss")]
	public class RssDataObject
	{
		[XmlElement("channel")]
		public RssData Data { get; set; }
	}

	public class RssItem
	{
		[XmlElement(ElementName = "commentRss", Namespace = "http://wellformedweb.org/CommentAPI/")]
		public string CommentRss { get; set; }

		[XmlElement(ElementName = "comments", Namespace = "http://purl.org/rss/1.0/modules/slash/")]
		public int Comments { get; set; }

		[XmlElement("description")]
		public string Description { get; set; }

		[XmlElement("link")]
		public string Link { get; set; }

		[XmlElement("pubDate")]
		public string PubDate { get; set; }

		[XmlElement("title")]
		public string Title { get; set; }
	}

	[Service]
	public class RssService
	{
		private readonly DiscordSocketClient _client;
		private readonly DatabaseContext _database;
		private readonly HttpClient _httpClient;
		private readonly LogService _log;

		public RssService(DiscordSocketClient client, DatabaseContext database, HttpClient httpClient, LogService log)
		{
			_client = client;
			_database = database;
			_httpClient = httpClient;
			_log = log;
		}

		public async Task ProcessRssFeeds()
		{
			List<Task> feeds = new List<Task>();
			foreach (var guild in await _database.Guilds.Include(x => x.RssFeeds).Include(x => x.RssRecentUrls).Include(x => x.RssRoles).Where(x => x.EnableRssFeed && x.RssFeeds.Count > 0).ToListAsync())
			{
				foreach (var feed in guild.RssFeeds)
					feeds.Add(new Task(async () => {
						RssDataObject rssData = await GetRssAsync(feed.FeedUrl);
						List<RssItem> rssPosts = await excludeRecentPosts(feed, rssData);
						await BuildRssFeedAsync(feed, rssPosts, _client.GetGuild(Convert.ToUInt64(guild.GuildId)));
						await saveRecentUrls(feed, rssPosts);
					}));
			}
			Task isComplete = Task.WhenAll(feeds);
			await isComplete;
		}

		private async Task<RssDataObject> GetRssAsync(string feedUrl)
		{
			var response = await _httpClient.GetAsync(feedUrl).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
				return null;

			var serializer = new XmlSerializer(typeof(RssDataObject));
			var xml = await response.Content.ReadAsStringAsync();
			var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
			return serializer.Deserialize(xmlStream) as RssDataObject;
		}

		private async Task<List<RssItem>> excludeRecentPosts(RssFeed feed, RssDataObject rssData)
		{
			var recentUrls = feed.Guild.RssRecentUrls;
			List<RssItem> posts = new List<RssItem>();
			foreach (var item in rssData.Data.Items.Take(10).Reverse())
			{
				if (!(recentUrls.Select(x => x.RecentUrl).Contains(item.Link)))
					posts.Add(item);
			}
			return posts;
		}

		private async Task BuildRssFeedAsync(RssFeed feed, List<RssItem> posts, SocketGuild socketGuild)
		{
			try
			{
				var recentUrls = feed.Guild.RssRecentUrls;
				var channel = socketGuild.GetChannel(feed.ChannelId) as SocketTextChannel;

				posts.ForEach(async item =>
				{
					var embed = EmbedHelper.Embed(EmbedHelper.RSS);
					var sb = new StringBuilder();

					string description = StripTagsCharArray(RoughStrip(HtmlEntity.DeEntitize(item.Description)));
					description = description.Length > 800 ? $"{description.Substring(0, 800)} [...]" : description;
					var feedUri = new Uri(feed.FeedUrl);

					switch (feedUri)
					{
						case Uri uri when feedUri.Host is "www.gggtracker.com":
							sb.AppendLine("-----------------------------------------------------------")
								.Append(":newspaper: ***").Append(CleanTitle(item.Title)).AppendLine("***\n")
								.AppendLine(item.Link)
								.Append("```").Append(description).AppendLine("```");
							break;

						case Uri uri when feedUri.Host is "www.poelab.com":
							sb.AppendLine("-----------------------------------------------------------")
								.Append("***").Append(CleanTitle(item.Title)).AppendLine("***")
								.AppendLine("*Please turn off any Ad Blockers you have to help the team keep doing Izaros work.*")
								.AppendLine(item.Link);

							string labDescription = "Lab notes not added.";

							if (item.Comments > 0 && item.Title.Contains("Uber"))
							{
								var commentRSS = await GetRssAsync(item.CommentRss);
								var comment = commentRSS.Data.Items.Find(x => x.Title is "By: SuitSizeSmall");
								if (!(comment is null))
									labDescription = comment.Description;
							}

							sb.Append("```").Append(labDescription).AppendLine("```");
							break;

						case Uri uri when feedUri.Host is "www.pathofexile.com":
							embed.WithTitle(CleanTitle(item.Title))
								.WithDescription(description)
								.WithUrl(item.Link)
								.WithTimestamp(new DateTimeOffset(Convert.ToDateTime(item.PubDate).ToUniversalTime()));

							string newsImage = await GetAnnouncementImageAsync(item.Link);
							if (!string.IsNullOrWhiteSpace(newsImage))
								embed.WithImageUrl(newsImage);
							break;

						default:
							sb.Append("***").Append(CleanTitle(item.Title)).AppendLine("***\n")
								.AppendLine(item.Link)
								.Append("```").Append(description).AppendLine("```");
							break;
					}

					IRole roleToMention = null;
					if (feed.Guild.RssRoles.Count > 0)
					{
						foreach (var roleId in feed.Guild.RssRoles)
						{
							var role = socketGuild.GetRole(roleId.RoleId);
							if (role.Name.IndexOf("everyone", StringComparison.CurrentCultureIgnoreCase) >= 0 && !string.IsNullOrEmpty(feed.Tag))
							{
								if (embed.Title.IndexOf(feed.Tag, StringComparison.CurrentCultureIgnoreCase) >= 0)
								{
									roleToMention = role;
									break;
								}
							}
							else if (!(role.Name.IndexOf("everyone", StringComparison.CurrentCultureIgnoreCase) >= 0))
							{
								roleToMention = role;
								break;
							}
						}
					}

					if (!string.IsNullOrEmpty(embed.Title))
						await channel.SendMessageAsync(roleToMention?.Mention, embed: embed.Build());
					else if (!string.IsNullOrEmpty(sb.ToString()))
						await channel.SendMessageAsync(sb.ToString());


				});

				await _database.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				await _log.LogMessage(new LogMessage(LogSeverity.Error, "Rss", string.Empty, ex));
				return;
			}
		}

		private async Task saveRecentUrls(RssFeed feed, List<RssItem> posts)
		{
			await Task.Run(() => 
			{
				posts.ForEach(async item =>
				{
					await _database.RssRecentUrls.AddAsync(new RssRecentUrl
					{
						RecentUrl = item.Link,
						RssFeedId = feed.Id,
						GuildId = feed.Guild.Id
					});
				});
			});
		}

		private async Task<string> GetAnnouncementImageAsync(string url)
		{
			var imageURL = string.Empty;
			var doc = new HtmlDocument();

			using (var response = await _httpClient.GetAsync(url).ConfigureAwait(false))
			using (var content = response.Content)
			{
				string result = await content.ReadAsStringAsync();
				doc.LoadHtml(result);
			}

			try
			{
				foreach (var node in doc.DocumentNode.SelectNodes("//img"))
				{
					if (node.Attributes["src"].Value.Contains("/news/"))
					{
						imageURL = node.Attributes["src"].Value;
						break;
					}
				}
			}
			catch
			{
			}

			return imageURL;
		}

		private string CleanTitle(string title) => title.Replace("*", string.Empty);

		private string RoughStrip(string source)
		{
			var val = source.Replace("<ul>", "")
				.Replace("</ul><br/>", "</ul>")
				.Replace("</ul>", "")
				.Replace("<li>", " * ")
				.Replace("</li>", "\n")
				.Replace("<br/>\n<br/>\n", "\n\n");

			if (val.StartsWith("<style>"))
				val = val.Substring(val.IndexOf("</style>") + 8);

			return val;
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
}