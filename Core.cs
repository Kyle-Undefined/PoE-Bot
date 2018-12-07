namespace PoE.Bot
{
    using Discord;
    using Discord.WebSocket;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using NLog.Extensions.Logging;
    using PoE.Bot.Attributes;
    using PoE.Bot.Contexts;
    using PoE.Bot.Extensions;
    using PoE.Bot.Services;
    using Qmmands;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;

    public static class Core
    {
        private static async Task Main()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var assembly = Assembly.GetEntryAssembly();
            var types = assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(ServiceAttribute), true).Length > 0);

            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 100
                }))
                .AddSingleton(new CommandService(new CommandServiceConfiguration
                {
                    CaseSensitive = false
                })
                .AddTypeParsers(assembly))
                .AddSingleton<HttpClient>()
                .AddSingleton(new Random())
                .AddServices(types)
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                .AddDbContext<DatabaseContext>(options => options.UseSqlite(configuration.GetConnectionString("Sqlite")), ServiceLifetime.Transient)
                .BuildServiceProvider();

            await services.GetRequiredService<BotStartService>().InitializeAsync();
        }
    }
}