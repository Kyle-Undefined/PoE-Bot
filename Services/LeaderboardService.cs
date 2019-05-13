namespace PoE.Bot.Services
{
	using CsvHelper;
	using CsvHelper.Configuration;
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using PoE.Bot.Helpers;
	using PoE.Bot.Models;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Text;
	using System.Threading.Tasks;

	public enum AscendancyClass
	{
		Ascendant,
		Assassin,
		Berserker,
		Champion,
		Chieftain,
		Deadeye,
		Duelist,
		Elementalist,
		Gladiator,
		Guardian,
		Hierophant,
		Inquisitor,
		Juggernaut,
		Marauder,
		Necromancer,
		Occultist,
		Pathfinder,
		Raider,
		Ranger,
		Saboteur,
		Scion,
		Shadow,
		Slayer,
		Templar,
		Trickster,
		Witch
	}

	public class LeaderboardData
	{
		public SortedSet<LeaderboardDataItem> Ascendants = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Assassins = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Berserkers = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Champions = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Chieftains = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Deadeyes = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Discordians = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Duelists = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Elementalists = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Gladiators = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Guardians = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Hierophants = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Inquisitors = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Juggernauts = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Marauders = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Necromancers = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Occultists = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Pathfinders = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Raiders = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Rangers = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Saboteurs = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Scions = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Shadows = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Slayers = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Templars = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Tricksters = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> Witches = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));
		public SortedSet<LeaderboardDataItem> AllRecords = new SortedSet<LeaderboardDataItem>(Comparer<LeaderboardDataItem>.Create((x, y) => x.Rank.CompareTo(y.Rank)));

		public void Add(LeaderboardDataItem dataItem)
		{
			AllRecords.Add(dataItem);

			switch (dataItem.Character.IndexOf("discord", StringComparison.CurrentCultureIgnoreCase) >= 0)
			{
				case true:
					Discordians.Add(dataItem);
					break;
			}

			switch (dataItem.Class)
			{
				case AscendancyClass.Ascendant:
					Ascendants.Add(dataItem);
					break;

				case AscendancyClass.Assassin:
					Assassins.Add(dataItem);
					break;

				case AscendancyClass.Berserker:
					Berserkers.Add(dataItem);
					break;

				case AscendancyClass.Champion:
					Champions.Add(dataItem);
					break;

				case AscendancyClass.Chieftain:
					Chieftains.Add(dataItem);
					break;

				case AscendancyClass.Deadeye:
					Deadeyes.Add(dataItem);
					break;

				case AscendancyClass.Duelist:
					Duelists.Add(dataItem);
					break;

				case AscendancyClass.Elementalist:
					Elementalists.Add(dataItem);
					break;

				case AscendancyClass.Gladiator:
					Gladiators.Add(dataItem);
					break;

				case AscendancyClass.Guardian:
					Guardians.Add(dataItem);
					break;

				case AscendancyClass.Hierophant:
					Hierophants.Add(dataItem);
					break;

				case AscendancyClass.Inquisitor:
					Inquisitors.Add(dataItem);
					break;

				case AscendancyClass.Juggernaut:
					Juggernauts.Add(dataItem);
					break;

				case AscendancyClass.Marauder:
					Marauders.Add(dataItem);
					break;

				case AscendancyClass.Necromancer:
					Necromancers.Add(dataItem);
					break;

				case AscendancyClass.Occultist:
					Occultists.Add(dataItem);
					break;

				case AscendancyClass.Pathfinder:
					Pathfinders.Add(dataItem);
					break;

				case AscendancyClass.Raider:
					Raiders.Add(dataItem);
					break;

				case AscendancyClass.Ranger:
					Rangers.Add(dataItem);
					break;

				case AscendancyClass.Saboteur:
					Saboteurs.Add(dataItem);
					break;

				case AscendancyClass.Scion:
					Scions.Add(dataItem);
					break;

				case AscendancyClass.Shadow:
					Shadows.Add(dataItem);
					break;

				case AscendancyClass.Slayer:
					Slayers.Add(dataItem);
					break;

				case AscendancyClass.Templar:
					Templars.Add(dataItem);
					break;

				case AscendancyClass.Trickster:
					Tricksters.Add(dataItem);
					break;

				case AscendancyClass.Witch:
					Witches.Add(dataItem);
					break;
			}
		}
	}

	public class LeaderboardDataItem
	{
		public LeaderboardDataItem()
		{
		}

		public LeaderboardDataItem(int rank, string character, AscendancyClass ascendancyClass, int level, ulong experience, bool dead)
		{
			Rank = rank;
			Character = character;
			Class = ascendancyClass;
			Level = level;
			Experience = experience;
			Dead = dead;
		}

		public string Character { get; set; }
		public AscendancyClass Class { get; set; }
		public bool Dead { get; set; }
		public ulong Experience { get; set; }
		public int Level { get; set; }
		public int Rank { get; set; }
	}

	public sealed class LeaderboardDataItemMap : ClassMap<LeaderboardDataItem>
	{
		public LeaderboardDataItemMap()
		{
			Map(x => x.Rank).Name("Rank");
			Map(x => x.Character).Name("Character");
			Map(x => x.Class).Name("Class");
			Map(x => x.Level).Name("Level");
			Map(x => x.Experience).Name("Experience");
			Map(x => x.Dead).Name("Dead")
				.TypeConverterOption.BooleanValues(true, true, "Dead")
				.TypeConverterOption.BooleanValues(false, true, string.Empty)
				.Default(false);
		}
	}

	[Service]
	public class LeaderboardService
	{
		private readonly DiscordSocketClient _client;
		private readonly DatabaseContext _database;
		private readonly HttpClient _httpClient;
		private readonly LogService _log;

		public LeaderboardService(DiscordSocketClient client, DatabaseContext database, HttpClient httpClient, LogService log)
		{
			_client = client;
			_database = database;
			_httpClient = httpClient;
			_log = log;
		}

		public async Task ProcessLeaderboards()
		{
			var boards = new List<Task>();
			foreach (var guild in await _database.Guilds.AsNoTracking().Include(x => x.Leaderboards).Where(x => x.EnableLeaderboardFeed && x.Leaderboards.Count > 0).ToListAsync())
			{
				foreach (var leaderboard in guild.Leaderboards.Where(x => x.Enabled))
					boards.Add(Task.Run(async () =>
					{
						await BuildLeaderboardAsync(leaderboard, _client.GetGuild(Convert.ToUInt64(guild.GuildId)));
					}));
			}
			Task isComplete = Task.WhenAll(boards);
			await isComplete;

			return;
		}

		private Embed BuildDiscordOnlyEmbed(LeaderboardData data, string leaderboard)
		{
			var sb = new StringBuilder();
			var embed = EmbedHelper.Embed(EmbedHelper.Leaderboard)
				.WithTitle("Discordians Only " + leaderboard.Replace("_", " ") + " Leaderboard")
				.WithDescription("Retrieved " + data.Discordians.Count.ToString("##,##0") + " users with Discord in their name.")
				.WithCurrentTimestamp()
				.AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Duelist || x.Class is AscendancyClass.Slayer || x.Class is AscendancyClass.Champion
				|| x.Class is AscendancyClass.Gladiator))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Duelist || x.Class is AscendancyClass.Slayer
					|| x.Class is AscendancyClass.Champion || x.Class is AscendancyClass.Gladiator).Take(10))
				{
					sb = FormatData(sb, item);
				}

				embed.AddField("Duelists, Slayers, Champions, Gladiators", "```" + sb + "```");
			}

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Marauder || x.Class is AscendancyClass.Juggernaut || x.Class is AscendancyClass.Chieftain
				|| x.Class is AscendancyClass.Berserker))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Marauder || x.Class is AscendancyClass.Juggernaut
					|| x.Class is AscendancyClass.Chieftain || x.Class is AscendancyClass.Berserker).Take(10))
				{
					sb = FormatData(sb, item);
				}
				embed.AddField("Marauders, Juggernauts, Chieftains, Berserkers", "```" + sb + "```");
			}

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Ranger || x.Class is AscendancyClass.Pathfinder || x.Class is AscendancyClass.Deadeye
				|| x.Class is AscendancyClass.Raider))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Ranger || x.Class is AscendancyClass.Pathfinder
					|| x.Class is AscendancyClass.Deadeye || x.Class is AscendancyClass.Raider).Take(10))
				{
					sb = FormatData(sb, item);
				}
				embed.AddField("Rangers, Pathfinders, Raiders, Deadeyes", "```" + sb + "```");
			}

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Scion || x.Class is AscendancyClass.Ascendant))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Scion || x.Class is AscendancyClass.Ascendant).Take(10))
				{
					sb = FormatData(sb, item);
				}
				embed.AddField("Scions, Ascendants", "```" + sb + "```");
			}

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Shadow || x.Class is AscendancyClass.Saboteur || x.Class is AscendancyClass.Assassin
				|| x.Class is AscendancyClass.Trickster))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Shadow || x.Class is AscendancyClass.Saboteur
					|| x.Class is AscendancyClass.Assassin || x.Class is AscendancyClass.Trickster).Take(10))
				{
					sb = FormatData(sb, item);
				}
				embed.AddField("Shadows, Saboteurs, Assassins, Tricksters", "```" + sb + "```");
			}

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Templar || x.Class is AscendancyClass.Guardian || x.Class is AscendancyClass.Inquisitor
				|| x.Class is AscendancyClass.Hierophant))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Templar || x.Class is AscendancyClass.Guardian
					|| x.Class is AscendancyClass.Inquisitor || x.Class is AscendancyClass.Hierophant).Take(10))
				{
					sb = FormatData(sb, item);
				}
				embed.AddField("Templars, Guardians, Inquisitors, Hierophants", "```" + sb + "```");
			}

			if (data.Discordians.Any(x => x.Class is AscendancyClass.Witch || x.Class is AscendancyClass.Necromancer || x.Class is AscendancyClass.Occultist
				|| x.Class is AscendancyClass.Elementalist))
			{
				sb = new StringBuilder();
				foreach (var item in data.Discordians.Where(x => x.Class is AscendancyClass.Witch || x.Class is AscendancyClass.Necromancer
					|| x.Class is AscendancyClass.Occultist || x.Class is AscendancyClass.Elementalist).Take(10))
				{
					sb = FormatData(sb, item);
				}
				embed.AddField("Witches, Necromancers, Occultists, Elementalists", "```" + sb + "```");
			}

			return embed.Build();
		}

		private Embed BuildFirstTopAscendancyEmbed(LeaderboardData data)
		{
			var sb = new StringBuilder();
			var embed = EmbedHelper.Embed(EmbedHelper.Leaderboard)
					.WithTitle("Top 10 Characters of each Ascendancy")
					.WithDescription("Rank is overall and not by Ascendancy.")
					.WithCurrentTimestamp();

			if (data.Ascendants.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Ascendants.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Ascendants", "```" + sb + "```");
			}

			if (data.Assassins.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Assassins.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Assassins", "```" + sb + "```");
			}

			if (data.Berserkers.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Berserkers.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Berserkers", "```" + sb + "```");
			}

			if (data.Champions.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Champions.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Champions", "```" + sb + "```");
			}

			if (data.Chieftains.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Chieftains.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Chieftains", "```" + sb + "```");
			}

			if (data.Deadeyes.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Deadeyes.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Deadeyes", "```" + sb + "```");
			}

			if (data.Elementalists.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Elementalists.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Elementalists", "```" + sb + "```");
			}

			if (data.Gladiators.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Gladiators.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Gladiators", "```" + sb + "```");
			}

			if (data.Guardians.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Guardians.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Guardians", "```" + sb + "```");
			}

			if (data.Hierophants.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Hierophants.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Hierophants", "```" + sb + "```");
			}

			return embed.Build();
		}

		private async Task BuildLeaderboardAsync(Leaderboard leaderboard, SocketGuild guild)
		{
			LeaderboardData data = null;
			try
			{
				data = await GetLeaderboardDataAsync(leaderboard.Console, leaderboard.Variant);

				var channel = guild.GetChannel(leaderboard.ChannelId) as SocketTextChannel;
				foreach (var msg in await channel.GetMessagesAsync().FlattenAsync())
				{
					await msg.DeleteAsync();
					await Task.Delay(1000);
				}

				await channel.SendMessageAsync(embed: BuildTopClassEmbed(data, leaderboard.Variant));
				await channel.SendMessageAsync(embed: BuildFirstTopAscendancyEmbed(data));
				await channel.SendMessageAsync(embed: BuildSecondTopAscendancyEmbed(data));
				await channel.SendMessageAsync(embed: BuildDiscordOnlyEmbed(data, leaderboard.Variant));

				await Task.Delay(15000);
			}
			catch (Exception ex)
			{
				await _log.LogMessage(new LogMessage(LogSeverity.Error, "Leaderboard", string.Empty, ex));
				return;
			}
		}

		private Embed BuildSecondTopAscendancyEmbed(LeaderboardData data)
		{
			var sb = new StringBuilder();
			var embed = EmbedHelper.Embed(EmbedHelper.Leaderboard)
					.WithTitle("Top 10 Characters of each Ascendancy")
					.WithDescription("Rank is overall and not by Ascendancy.")
					.WithCurrentTimestamp();

			if (data.Inquisitors.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Inquisitors.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Inquisitors", "```" + sb + "```");
			}

			if (data.Juggernauts.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Juggernauts.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Juggernauts", "```" + sb + "```");
			}

			if (data.Necromancers.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Necromancers.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Necromancers", "```" + sb + "```");
			}

			if (data.Occultists.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Occultists.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Occultists", "```" + sb + "```");
			}

			if (data.Pathfinders.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Pathfinders.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Pathfinders", "```" + sb + "```");
			}

			if (data.Raiders.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Raiders.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Raiders", "```" + sb + "```");
			}

			if (data.Saboteurs.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Saboteurs.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Saboteurs", "```" + sb + "```");
			}

			if (data.Slayers.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Slayers.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Slayers", "```" + sb + "```");
			}

			if (data.Tricksters.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in data.Tricksters.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Tricksters", "```" + sb + "```");
			}
			return embed.Build();
		}

		private Embed BuildTopClassEmbed(LeaderboardData data, string leaderboard)
		{
			var sb = new StringBuilder();
			var embed = EmbedHelper.Embed(EmbedHelper.Leaderboard)
					.WithTitle(WebUtility.UrlDecode(leaderboard).Replace("_", " ") + " Leaderboard")
					.WithDescription("Retrieved " + data.AllRecords.Count.ToString("##,##0") + " records.")
					.WithCurrentTimestamp()
					.AddField("Top 10 Characters of each Class", "Rank is overall and not by Class.");

			var duelists = data.AllRecords.Where(x => x.Class is AscendancyClass.Duelist || x.Class is AscendancyClass.Slayer || x.Class is AscendancyClass.Gladiator
				|| x.Class is AscendancyClass.Champion).ToList();
			var marauders = data.AllRecords.Where(x => x.Class is AscendancyClass.Marauder || x.Class is AscendancyClass.Juggernaut || x.Class is AscendancyClass.Chieftain
				|| x.Class is AscendancyClass.Berserker).ToList();
			var rangers = data.AllRecords.Where(x => x.Class is AscendancyClass.Ranger || x.Class is AscendancyClass.Raider || x.Class is AscendancyClass.Deadeye
				|| x.Class is AscendancyClass.Pathfinder).ToList();
			var scions = data.AllRecords.Where(x => x.Class is AscendancyClass.Scion || x.Class is AscendancyClass.Ascendant).ToList();
			var shadows = data.AllRecords.Where(x => x.Class is AscendancyClass.Shadow || x.Class is AscendancyClass.Saboteur || x.Class is AscendancyClass.Assassin
				|| x.Class is AscendancyClass.Trickster).ToList();
			var templars = data.AllRecords.Where(x => x.Class is AscendancyClass.Templar || x.Class is AscendancyClass.Inquisitor || x.Class is AscendancyClass.Hierophant
				|| x.Class is AscendancyClass.Guardian).ToList();
			var witches = data.AllRecords.Where(x => x.Class is AscendancyClass.Witch || x.Class is AscendancyClass.Necromancer || x.Class is AscendancyClass.Occultist
				|| x.Class is AscendancyClass.Elementalist).ToList();

			if (duelists.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in duelists.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Duelists, Slayers, Champions, Gladiators", "```" + sb + "```");
			}

			if (marauders.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in marauders.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Marauders, Juggernauts, Chieftains, Berserkers", "```" + sb + "```");
			}

			if (rangers.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in rangers.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Rangers, Pathfinders, Raiders, Deadeyes", "```" + sb + "```");
			}

			if (scions.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in scions.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Scions, Ascendants", "```" + sb + "```");
			}

			if (shadows.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in shadows.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Shadows, Saboteurs, Assassins, Tricksters", "```" + sb + "```");
			}

			if (templars.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in templars.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Templars, Guardians, Inquisitors, Hierophants", "```" + sb + "```");
			}

			if (witches.Count > 0)
			{
				sb = new StringBuilder();
				foreach (var item in witches.Take(10))
					sb = FormatData(sb, item);

				embed.AddField("Witches, Necromancers, Occultists, Elementalists", "```" + sb + "```");
			}

			return embed.Build();
		}

		private async Task<LeaderboardData> GetLeaderboardDataAsync(string console, string variant)
		{
			LeaderboardData board = new LeaderboardData();
			using (var response = await _httpClient.GetAsync("https://www.pathofexile.com/ladder/export-csv/league/" + variant + "/realm/" + console + "/index/1", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
			{
				if (response.IsSuccessStatusCode)
				{
					using (var stream = await response.Content.ReadAsStreamAsync())
					using (var reader = new StreamReader(stream))
					using (var csv = new CsvReader(reader))
					{
						csv.Configuration.RegisterClassMap<LeaderboardDataItemMap>();
						await csv.ReadAsync();
						csv.ReadHeader();

						while (await csv.ReadAsync())
							board.Add(csv.GetRecord<LeaderboardDataItem>());
					}
				}
				else
				{
					await _log.LogMessage(new LogMessage(LogSeverity.Error, "Leaderboard", "Fetch Failed: https://www.pathofexile.com/ladder/export-csv/league/" + variant + "/realm/" + console + "/index/1"));
				}
			}
			return board;
		}

		private StringBuilder FormatData(StringBuilder sb, LeaderboardDataItem item)
		{
			return sb.Append(item.Character.PadRight(24))
				.Append("R:")
				.AppendFormat("{0,4}", item.Rank)
				.Append(" | L:")
				.AppendFormat("{0,3}", item.Level)
				.Append(" | ")
				.AppendFormat("{0,14}", item.Class.ToString())
				.AppendLine();
		}
	}
}