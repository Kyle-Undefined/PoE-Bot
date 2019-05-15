namespace PoE.Bot.Services
{
	using Discord;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.DependencyInjection;
	using PoE.Bot.Attributes;
	using System;
	using System.Threading.Tasks;

	[Service(ServiceLifetime.Transient)]
	public class LogService
	{
		private readonly ILogger<LogService> _logger;

		public LogService(ILogger<LogService> logger) => _logger = logger;

		public Task LogMessage(LogMessage message)
		{
			_logger.Log(LogLevelFromSeverity(message.Severity), 0, message, message.Exception, (_, __) => message.ToString().Substring(9));
			return Task.CompletedTask;
		}

		private static LogLevel LogLevelFromSeverity(LogSeverity severity) => (LogLevel)(Math.Abs((int)severity - 5));
	}
}