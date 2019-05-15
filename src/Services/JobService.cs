namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using FluentScheduler;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using System;
	using System.Linq;

	[Service]
	public class JobService
	{
		private readonly DiscordSocketClient _client;
		private readonly DatabaseContext _database;
		private readonly LeaderboardService _leaderboard;
		private readonly LogService _log;
		private readonly RssService _rss;
		private readonly StreamService _stream;

		public JobService(DiscordSocketClient client, DatabaseContext database, LeaderboardService leaderboard, LogService log, RssService rss, StreamService stream)
		{
			_client = client;
			_database = database;
			_leaderboard = leaderboard;
			_log = log;
			_rss = rss;
			_stream = stream;

			JobManager.Initialize();
		}

		public void Initialize()
		{

			_log.LogMessage(new LogMessage(LogSeverity.Info, "Jobs", "Starting Server Job Timers"));

			_log.LogMessage(new LogMessage(LogSeverity.Info, "Jobs", "Starting Role Manager"));
			JobManager.AddJob(async () =>
			{
				try
				{
					foreach (var mute in await _database.Users.Include(x => x.Guild).Where(x => x.Muted && x.MutedUntil < DateTime.Now).ToListAsync())
					{
						mute.Muted = false;
						mute.MutedUntil = default;
						await _database.SaveChangesAsync();

						var guild = _client.GetGuild(mute.Guild.GuildId);
						var user = guild.GetUser(mute.UserId);
						var role = guild.GetRole(mute.Guild.MuteRole) ?? guild.Roles.FirstOrDefault(x => x.Name is "Muted");

						if (user is null)
							return;

						if (!user.Roles.Contains(role))
							return;

						if (user.Roles.Contains(role))
							await user.RemoveRoleAsync(role);
					}
				}
				catch (Exception ex)
				{
					await _log.LogMessage(new LogMessage(LogSeverity.Error, "Mute", string.Empty, ex));
					return;
				}
			}, x => x.ToRunEvery(1).Minutes());

			_log.LogMessage(new LogMessage(LogSeverity.Info, "Jobs", "Starting Leaderboards"));
			JobManager.AddJob(async () => await _leaderboard.ProcessLeaderboards(), x => x.ToRunEvery(60).Minutes());

			_log.LogMessage(new LogMessage(LogSeverity.Info, "Jobs", "Starting Streams"));
			JobManager.AddJob(async () => await _stream.ProcessStreams(), x => x.ToRunEvery(5).Minutes());

			_log.LogMessage(new LogMessage(LogSeverity.Info, "Jobs", "RSS Feeds"));
			JobManager.AddJob(async () => await _rss.ProcessRssFeeds(), x => x.ToRunEvery(15).Minutes());

			_log.LogMessage(new LogMessage(LogSeverity.Info, "Jobs", "4 jobs running..."));
		}
	}
}