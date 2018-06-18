namespace PoE.Bot.Helpers
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Handlers;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using Raven.Client.Extensions;
    using PoE.Bot.Objects;
    using System.Collections.Concurrent;

    public class EventHelper
    {
        DatabaseHandler DB { get; }
        Random Random { get; }
        GuildHelper GuildHelper { get; }
        public TimeSpan GlobalTimeout { get; }
        ConcurrentDictionary<ulong, DateTime> WaitList { get; }

        public EventHelper(DatabaseHandler dB, Random random, GuildHelper helper)
        {
            DB = dB;
            Random = random;
            GuildHelper = helper;
            GlobalTimeout = TimeSpan.FromSeconds(30);
            WaitList = new ConcurrentDictionary<ulong, DateTime>();
        }

        internal async Task CheckStateAsync(DiscordSocketClient Client)
        {
            if (Client.ConnectionState is ConnectionState.Connected)
                return;
            var Timeout = Task.Delay(GlobalTimeout);
            var Connect = Client.StartAsync();
            var LocalTask = await Task.WhenAny(Timeout, Connect);
            if (LocalTask == Timeout || Connect.IsFaulted)
                return;
            else if (Connect.IsCompletedSuccessfully)
                return;
            else Environment.Exit(1);
        }

        internal void RunTasks(SocketUserMessage Message, GuildObject Server)
            => Task.Run(async ()
                =>
            {
                await AFKHandler(Message, Server).WithCancellation(MethodHelper.Cancellation(TimeSpan.FromSeconds(10)));
                await ModeratorAsync(Message, Server).WithCancellation(MethodHelper.Cancellation(TimeSpan.FromSeconds(10)));
            });

        Task AFKHandler(SocketMessage Message, GuildObject Server)
        {
            if (!Message.MentionedUsers.Any(x => Server.AFK.ContainsKey(x.Id)))
                return Task.CompletedTask;
            string Reason = null;
            var User = Message.MentionedUsers.FirstOrDefault(u => Server.AFK.TryGetValue(u.Id, out Reason));
            return User is null ? Task.CompletedTask : Message.Channel.SendMessageAsync($"**{User.Username} has left an AFK Message:**  {Reason}");
        }

        Task ModeratorAsync(SocketUserMessage Message, GuildObject Server)
        {
            if (GuildHelper.ProfanityMatch(Message.Content, Server.ProfanityList) && Server.AntiProfanity)
                return GuildHelper.WarnUserAsync(Message, Server, DB, $"{Message.Author.Mention}, Refrain from using profanity. You've been warned.");
            return Task.CompletedTask;
        }
    }
}
