using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PoE.Bot.Migrations
{
	public partial class Initial : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "BlacklistedUsers",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					UserId = table.Column<ulong>(nullable: false),
					BlacklistedWhen = table.Column<DateTime>(nullable: false),
					Reason = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_BlacklistedUsers", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "BotConfigs",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					BotToken = table.Column<string>(nullable: true),
					Prefix = table.Column<string>(nullable: true),
					TwitchClientId = table.Column<string>(nullable: true),
					ProjectChannel = table.Column<ulong>(nullable: false),
					SupportChannel = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_BotConfigs", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Guilds",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					EnableAntiProfanity = table.Column<bool>(nullable: false),
					EnableDeletionLog = table.Column<bool>(nullable: false),
					EnableLeaderboardFeed = table.Column<bool>(nullable: false),
					EnableMixerFeed = table.Column<bool>(nullable: false),
					EnableRssFeed = table.Column<bool>(nullable: false),
					EnableTwitchFeed = table.Column<bool>(nullable: false),
					MaxWarnings = table.Column<int>(nullable: false),
					AnnouncementRole = table.Column<ulong>(nullable: false),
					BotChangeChannel = table.Column<ulong>(nullable: false),
					CaseLogChannel = table.Column<ulong>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false),
					LotteryRole = table.Column<ulong>(nullable: false),
					MessageLogChannel = table.Column<ulong>(nullable: false),
					MuteRole = table.Column<ulong>(nullable: false),
					PlaystationRole = table.Column<ulong>(nullable: false),
					ReportLogChannel = table.Column<ulong>(nullable: false),
					RulesChannel = table.Column<ulong>(nullable: false),
					XboxRole = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Guilds", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Cases",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					CaseType = table.Column<int>(nullable: false),
					CaseDate = table.Column<DateTime>(nullable: false),
					Number = table.Column<int>(nullable: false),
					Reason = table.Column<string>(nullable: true),
					GuildId = table.Column<ulong>(nullable: false),
					MessageId = table.Column<ulong>(nullable: false),
					ModeratorId = table.Column<ulong>(nullable: false),
					UserId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Cases", x => x.Id);
					table.ForeignKey(
						name: "FK_Cases_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "CurrencyItems",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					LastUpdated = table.Column<DateTime>(nullable: false),
					Price = table.Column<double>(nullable: false),
					Quantity = table.Column<double>(nullable: false),
					League = table.Column<int>(nullable: false),
					Alias = table.Column<string>(nullable: true),
					Name = table.Column<string>(nullable: true),
					GuildId = table.Column<ulong>(nullable: false),
					UserId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CurrencyItems", x => x.Id);
					table.ForeignKey(
						name: "FK_CurrencyItems_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Leaderboards",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Enabled = table.Column<bool>(nullable: false),
					Variant = table.Column<string>(nullable: true),
					Console = table.Column<string>(nullable: true),
					ChannelId = table.Column<ulong>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Leaderboards", x => x.Id);
					table.ForeignKey(
						name: "FK_Leaderboards_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Profanities",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Word = table.Column<string>(nullable: true),
					GuildId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Profanities", x => x.Id);
					table.ForeignKey(
						name: "FK_Profanities_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RssFeeds",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					FeedUrl = table.Column<string>(nullable: true),
					Tag = table.Column<string>(nullable: true),
					ChannelId = table.Column<ulong>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RssFeeds", x => x.Id);
					table.ForeignKey(
						name: "FK_RssFeeds_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RuleFields",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Title = table.Column<string>(nullable: true),
					Content = table.Column<string>(nullable: true),
					Order = table.Column<int>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RuleFields", x => x.Id);
					table.ForeignKey(
						name: "FK_RuleFields_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Rules",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					TotalFields = table.Column<int>(nullable: false),
					Description = table.Column<string>(nullable: true),
					GuildId = table.Column<ulong>(nullable: false),
					MessageId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Rules", x => x.Id);
					table.ForeignKey(
						name: "FK_Rules_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Shops",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					League = table.Column<int>(nullable: false),
					Item = table.Column<string>(nullable: true),
					GuildId = table.Column<ulong>(nullable: false),
					UserId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Shops", x => x.Id);
					table.ForeignKey(
						name: "FK_Shops_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Streams",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					IsLive = table.Column<bool>(nullable: false),
					StreamType = table.Column<int>(nullable: false),
					Username = table.Column<string>(nullable: true),
					MixerChannelId = table.Column<uint>(nullable: false),
					MixerUserId = table.Column<uint>(nullable: false),
					ChannelId = table.Column<ulong>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false),
					TwitchUserId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Streams", x => x.Id);
					table.ForeignKey(
						name: "FK_Streams_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Tags",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					CreationDate = table.Column<DateTime>(nullable: false),
					Uses = table.Column<int>(nullable: false),
					Content = table.Column<string>(nullable: true),
					Name = table.Column<string>(nullable: true),
					GuildId = table.Column<ulong>(nullable: false),
					UserId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Tags", x => x.Id);
					table.ForeignKey(
						name: "FK_Tags_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Muted = table.Column<bool>(nullable: false),
					MutedUntil = table.Column<DateTime>(nullable: false),
					Warnings = table.Column<int>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false),
					UserId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
					table.ForeignKey(
						name: "FK_Users_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RssRecentUrls",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					RecentUrl = table.Column<string>(nullable: true),
					RssFeedId = table.Column<ulong>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RssRecentUrls", x => x.Id);
					table.ForeignKey(
						name: "FK_RssRecentUrls_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_RssRecentUrls_RssFeeds_RssFeedId",
						column: x => x.RssFeedId,
						principalTable: "RssFeeds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RssRoles",
				columns: table => new
				{
					Id = table.Column<ulong>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					RoleId = table.Column<ulong>(nullable: false),
					RssFeedId = table.Column<ulong>(nullable: false),
					GuildId = table.Column<ulong>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RssRoles", x => x.Id);
					table.ForeignKey(
						name: "FK_RssRoles_Guilds_GuildId",
						column: x => x.GuildId,
						principalTable: "Guilds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_RssRoles_RssFeeds_RssFeedId",
						column: x => x.RssFeedId,
						principalTable: "RssFeeds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Cases_GuildId",
				table: "Cases",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_CurrencyItems_GuildId",
				table: "CurrencyItems",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_Leaderboards_GuildId",
				table: "Leaderboards",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_Profanities_GuildId",
				table: "Profanities",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_RssFeeds_GuildId",
				table: "RssFeeds",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_RssRecentUrls_GuildId",
				table: "RssRecentUrls",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_RssRecentUrls_RssFeedId",
				table: "RssRecentUrls",
				column: "RssFeedId");

			migrationBuilder.CreateIndex(
				name: "IX_RssRoles_GuildId",
				table: "RssRoles",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_RssRoles_RssFeedId",
				table: "RssRoles",
				column: "RssFeedId");

			migrationBuilder.CreateIndex(
				name: "IX_RuleFields_GuildId",
				table: "RuleFields",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_Rules_GuildId",
				table: "Rules",
				column: "GuildId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Shops_GuildId",
				table: "Shops",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_Streams_GuildId",
				table: "Streams",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_Tags_GuildId",
				table: "Tags",
				column: "GuildId");

			migrationBuilder.CreateIndex(
				name: "IX_Users_GuildId",
				table: "Users",
				column: "GuildId");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "BlacklistedUsers");

			migrationBuilder.DropTable(
				name: "BotConfigs");

			migrationBuilder.DropTable(
				name: "Cases");

			migrationBuilder.DropTable(
				name: "CurrencyItems");

			migrationBuilder.DropTable(
				name: "Leaderboards");

			migrationBuilder.DropTable(
				name: "Profanities");

			migrationBuilder.DropTable(
				name: "RssRecentUrls");

			migrationBuilder.DropTable(
				name: "RssRoles");

			migrationBuilder.DropTable(
				name: "RuleFields");

			migrationBuilder.DropTable(
				name: "Rules");

			migrationBuilder.DropTable(
				name: "Shops");

			migrationBuilder.DropTable(
				name: "Streams");

			migrationBuilder.DropTable(
				name: "Tags");

			migrationBuilder.DropTable(
				name: "Users");

			migrationBuilder.DropTable(
				name: "RssFeeds");

			migrationBuilder.DropTable(
				name: "Guilds");
		}
	}
}
