namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using Newtonsoft.Json.Linq;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Http;
	using System.Threading.Tasks;
	using TwitchLib.Api;

	public class MixerAPI
	{
		private readonly string _endpoint = "https://mixer.com/api/v1/";
		private readonly HttpClient _httpClient;

		public MixerAPI(HttpClient httpClient) => _httpClient = httpClient;

		public async Task<string> GetChannel(uint id)
		{
			using (var response = await _httpClient.GetAsync(_endpoint + "channels/" + id.ToString() + "/details", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
			{
				if (response.IsSuccessStatusCode)
					return await response.Content.ReadAsStringAsync();
			}

			return null;
		}

		public string GetChannelGame(string json)
		{
			var jo = JObject.Parse(json);
			return (string)jo["type"]["name"];
		}

		public string GetChannelGameCover(string json)
		{
			var jo = JObject.Parse(json);
			return (string)jo["type"]["coverUrl"];
		}

		public async Task<uint> GetChannelId(string username)
		{
			uint id = 0;
			using (var response = await _httpClient.GetAsync(_endpoint + "channels/" + username, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
			{
				if (response.IsSuccessStatusCode)
				{
					var jo = JObject.Parse(await response.Content.ReadAsStringAsync());
					id = (uint)jo["id"];
				}
			}

			return id;
		}

		public string GetChannelThumbnail(string json)
		{
			var jo = JObject.Parse(json);
			if (jo["thumbnail"].HasValues)
				return (string)jo["thumbnail"]["url"];
			else
				return (string)jo["bannerUrl"];
		}

		public string GetChannelTitle(string json)
		{
			var jo = JObject.Parse(json);
			return (string)jo["name"];
		}

		public async Task<string> GetUser(uint id)
		{
			using (var response = await _httpClient.GetAsync(_endpoint + "users/" + id.ToString(), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
			{
				if (response.IsSuccessStatusCode)
					return await response.Content.ReadAsStringAsync();
			}

			return null;
		}

		public string GetUserAvatar(uint id)
		{
			string json = GetUser(id).GetAwaiter().GetResult();
			var jo = JObject.Parse(json);
			return (string)jo["avatarUrl"];
		}

		public async Task<uint> GetUserId(string username)
		{
			uint id = 0;
			using (var response = await _httpClient.GetAsync(_endpoint + "users/search?query=" + username, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
			{
				if (response.IsSuccessStatusCode)
				{
					var ja = JArray.Parse(await response.Content.ReadAsStringAsync());
					var jo = JObject.Parse(ja[0].ToString());
					id = (uint)jo["id"];
				}
			}

			return id;
		}

		public int GetViewerCount(string json)
		{
			var jo = JObject.Parse(json);
			return (int)jo["viewersCurrent"];
		}

		public bool IsChannelLive(string json)
		{
			var jo = JObject.Parse(json);
			return (bool)jo["online"];
		}
	}

	[Service]
	public class StreamService
	{
		private readonly DiscordSocketClient _client;
		private readonly DatabaseContext _database;
		private readonly HttpClient _httpClient;
		private readonly LogService _log;

		public StreamService(DiscordSocketClient client, DatabaseContext database, HttpClient httpClient, LogService log)
		{
			_client = client;
			_database = database;
			_httpClient = httpClient;
			_log = log;
		}

		public async Task ProcessStreams()
		{
			foreach (var guild in await _database.Guilds.Include(x => x.Streams).Where(x => (x.EnableTwitchFeed || x.EnableMixerFeed) && x.Streams.Count > 0).ToListAsync())
			{
				foreach (var stream in guild.Streams)
					await BuildStreamAsync(stream, _client.GetGuild(Convert.ToUInt64(guild.GuildId)), guild);
			}
		}

		private async Task BuildStreamAsync(Stream stream, SocketGuild socketGuild, Guild guild)
		{
			try
			{
				bool streamWasLive = stream.IsLive;

				switch (stream.StreamType)
				{
					case StreamType.Mixer:
						if (!guild.EnableMixerFeed)
							return;

						var mixer = new MixerAPI(_httpClient);
						string chanJson = await mixer.GetChannel(stream.MixerChannelId);
						bool chanIsLive = mixer.IsChannelLive(chanJson);

						if (!chanIsLive)
							stream.IsLive = false;

						if (chanIsLive && !stream.IsLive)
						{
							var embed = EmbedHelper.Embed(EmbedHelper.Mixer)
								.WithTitle(mixer.GetChannelTitle(chanJson))
								.WithDescription("\n**" + stream.Username + "** is playing **" + mixer.GetChannelGame(chanJson) + "** for " + mixer.GetViewerCount(chanJson).ToString()
									+ " viewers!\n\n**https://mixer.com/" + stream.Username + "**")
								.WithAuthor(stream.Username, mixer.GetUserAvatar(stream.MixerUserId), $"https://mixer.com/{stream.Username}")
								.WithThumbnailUrl(mixer.GetChannelGameCover(chanJson))
								.WithImageUrl(mixer.GetChannelThumbnail(chanJson))
								.Build();

							var channel = socketGuild.GetChannel(stream.ChannelId) as SocketTextChannel;
							await channel.SendMessageAsync(embed: embed);

							stream.IsLive = true;
							await _database.SaveChangesAsync();
						}
						break;

					case StreamType.Twitch:
						if (!guild.EnableTwitchFeed)
							return;

						var twitch = new TwitchAPI();
						twitch.Settings.ClientId = (await _database.BotConfigs.AsNoTracking().FirstAsync()).TwitchClientId;
						var user = (await twitch.Helix.Users.GetUsersAsync(new List<string>(new string[] { stream.TwitchUserId.ToString() }))).Users[0];
						var _stream = (await twitch.V5.Streams.GetStreamByUserAsync(stream.TwitchUserId.ToString(), "live")).Stream;

						if (_stream is null)
						{
							stream.IsLive = false;
						}
						else
						{
							if (!stream.IsLive)
							{
								var games = (await twitch.Helix.Games.GetGamesAsync(null, new List<string>(new string[] { _stream.Game })));

								if (games.Games.Length > 0)
								{
									var game = games.Games[0];
									var embed = EmbedHelper.Embed(EmbedHelper.Twitch)
												.WithTitle(_stream.Channel.Status)
												.WithDescription("\n**" + user.DisplayName + "** is playing **" + game.Name + "** for " + _stream.Viewers + " viewers!\n\n**http://www.twitch.tv/"
													+ user.DisplayName + "**")
												.WithAuthor(user.DisplayName, user.ProfileImageUrl, "http://www.twitch.tv/" + user.DisplayName)
												.WithThumbnailUrl(game.BoxArtUrl.Replace("{width}x{height}", "285x380"))
												.WithImageUrl(_stream.Preview.Large)
												.Build();

									var channel = socketGuild.GetChannel(stream.ChannelId) as SocketTextChannel;
									await channel.SendMessageAsync(embed: embed);

									stream.IsLive = true;
									await _database.SaveChangesAsync();
								}
							}
						}
						break;
				}

				if (streamWasLive && !stream.IsLive)
					await _database.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				await _log.LogMessage(new LogMessage(LogSeverity.Error, "Stream", string.Empty, ex));
				return;
			}
		}
	}
}