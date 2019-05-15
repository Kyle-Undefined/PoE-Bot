namespace PoE.Bot.Services
{
	using Discord;
	using Discord.WebSocket;
	using Microsoft.EntityFrameworkCore;
	using PoE.Bot.Attributes;
	using PoE.Bot.Contexts;
	using Qmmands;
	using System.Reflection;
	using System.Threading.Tasks;

	[Service]
	public class BotStartService
	{
		private readonly CommandService _commands;
		private readonly DatabaseContext _database;
		private readonly DiscordSocketClient _client;
		private readonly EventService _events;
		private readonly JobService _jobs;
		private readonly ReactionService _reaction;

		public BotStartService(CommandService commands, DatabaseContext database, DiscordSocketClient client, EventService events, JobService jobs, ReactionService reaction)
		{
			_commands = commands;
			_database = database;
			_client = client;
			_events = events;
			_jobs = jobs;
			_reaction = reaction;
		}

		public async Task InitializeAsync()
		{
			await _client.LoginAsync(TokenType.Bot, (await _database.BotConfigs.AsNoTracking().FirstAsync()).BotToken);
			await _client.StartAsync();
			_commands.AddModules(Assembly.GetEntryAssembly());

			_events.Initialize();
			_reaction.Initialize();
			_jobs.Initialize();

			await Task.Delay(-1);
		}
	}
}