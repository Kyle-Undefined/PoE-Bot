namespace PoE.Bot.Contexts
{
	using Microsoft.EntityFrameworkCore;
	using Microsoft.EntityFrameworkCore.Design;
	using Microsoft.Extensions.Configuration;
	using PoE.Bot.Models;

	public class DatabaseContext : DbContext
	{
		public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
		{
		}

		public DbSet<BlacklistedUser> BlacklistedUsers { get; set; }
		public DbSet<BotConfig> BotConfigs { get; set; }
		public DbSet<Case> Cases { get; set; }
		public DbSet<CurrencyItem> CurrencyItems { get; set; }
		public DbSet<Guild> Guilds { get; set; }
		public DbSet<Leaderboard> Leaderboards { get; set; }
		public DbSet<Profanity> Profanities { get; set; }
		public DbSet<RssFeed> RssFeeds { get; set; }
		public DbSet<RssRecentUrl> RssRecentUrls { get; set; }
		public DbSet<RssRole> RssRoles { get; set; }
		public DbSet<RuleField> RuleFields { get; set; }
		public DbSet<Rule> Rules { get; set; }
		public DbSet<Shop> Shops { get; set; }
		public DbSet<Stream> Streams { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<User> Users { get; set; }
	}

	public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
	{
		public DatabaseContext CreateDbContext(string[] args)
		{
			var configuration = new ConfigurationBuilder()
			   .SetBasePath(System.IO.Directory.GetCurrentDirectory())
			   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			   .Build();

			var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(configuration.GetConnectionString("Sqlite"));

			return new DatabaseContext(optionsBuilder.Options);
		}
	}
}