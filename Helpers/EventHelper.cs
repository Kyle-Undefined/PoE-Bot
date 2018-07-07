namespace PoE.Bot.Helpers
{
    using Addons;
    using Discord;
    using Discord.WebSocket;
    using Handlers;
    using Objects;
    using Raven.Client.Extensions;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventHelper
    {
        public EventHelper(DatabaseHandler databaseHandler, Random random)
        {
            DatabaseHandler = databaseHandler;
            Random = random;
            GlobalTimeout = TimeSpan.FromSeconds(30);
        }

        public TimeSpan GlobalTimeout { get; }
        private DatabaseHandler DatabaseHandler { get; }
        private Random Random { get; }

        internal async Task CheckStateAsync(DiscordSocketClient client)
        {
            if (client.ConnectionState is ConnectionState.Connected)
                return;

            Task timeout = Task.Delay(GlobalTimeout);
            Task connect = client.StartAsync();
            Task localTask = await Task.WhenAny(timeout, connect);
            if (localTask == timeout || connect.IsFaulted)
                return;
            if (connect.IsCompletedSuccessfully)
                return;

            Environment.Exit(1);
        }

        internal void RunTasks(SocketUserMessage message, Context context)
            => Task.Run(async ()
                =>
            {
                await AFKHandler(message, context.Server).WithCancellation(MethodHelper.Cancellation(TimeSpan.FromSeconds(10)));
                await ModeratorAsync(message, context.Server).WithCancellation(MethodHelper.Cancellation(TimeSpan.FromSeconds(10)));
            });

        private Task AFKHandler(SocketMessage message, GuildObject server)
        {
            if (!message.MentionedUsers.Any(x => server.AFK.ContainsKey(x.Id)))
                return Task.CompletedTask;

            string reason = null;
            SocketUser user = message.MentionedUsers.FirstOrDefault(u => server.AFK.TryGetValue(u.Id, out reason));
            return user is null
                ? Task.CompletedTask
                : message.Channel.SendMessageAsync($"**{user.Username} has left an AFK Message:**  {reason}");
        }

        private Task ModeratorAsync(SocketMessage message, GuildObject server)
        {
            SocketGuild guild = (message.Author as SocketGuildUser).Guild;
            SocketGuildUser user = message.Author as SocketGuildUser;
            if (server.MaxWarningsToMute is 0 || user.Id == guild.OwnerId || user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageChannels ||
                user.GuildPermissions.ManageRoles || user.GuildPermissions.BanMembers || user.GuildPermissions.KickMembers)
                return Task.CompletedTask;
            if (message.Content.ProfanityMatch(server.ProfanityList) && server.AntiProfanity)
                return GuildHelper.WarnUserAsync(message, server, DatabaseHandler, $"{message.Author.Mention}, Refrain from using profanity. You've been warned.");

            return Task.CompletedTask;
        }
    }
}