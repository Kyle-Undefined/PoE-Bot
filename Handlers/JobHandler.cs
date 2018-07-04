namespace PoE.Bot.Handlers
{
    using Discord.WebSocket;
    using FluentScheduler;
    using Helpers;
    using Objects;
    using System;
    using System.Linq;

    public class JobHandler : Registry
    {
        public JobHandler(DatabaseHandler databaseHandler, DiscordSocketClient client)
        {
            DatabaseHandler = databaseHandler;
            Client = client;
            JobManager.JobException += (Info)
                => LogHandler.Write(Source.Exception, $"Exception ocurred in {Info.Name} job.\n{Info.Exception.Message}\n{Info.Exception.StackTrace}");
        }

        private DiscordSocketClient Client { get; }
        private DatabaseHandler DatabaseHandler { get; }

        public void Initialize()
        {
            Schedule(() => LogHandler.ForceGC()).ToRunEvery(10).Minutes();

            Schedule(() =>
            {
                foreach (GuildObject server in DatabaseHandler.Servers().Where(x => !x.Muted.IsEmpty))
                    foreach (var mute in server.Muted.Where(x => x.Value < DateTime.Now))
                    {
                        server.Muted.TryRemove(mute.Key, out _);
                        MethodHelper.RunSync(GuildHelper.UnmuteUserAsync(mute.Key, Client.GetGuild(Convert.ToUInt64(server.Id)), server));
                        DatabaseHandler.Save<GuildObject>(server, server.Id);
                    }
            }).WithName("mute").ToRunEvery(1).Minutes();

            Schedule(() =>
            {
                foreach (GuildObject server in DatabaseHandler.Servers().Where(x => !x.Reminders.IsEmpty))
                {
                    foreach (var kvpReminder in server.Reminders.Where(x => x.Value.Any()))
                    {
                        server.Reminders.TryGetValue(kvpReminder.Key, out var reminders);
                        foreach (RemindObject reminder in reminders.ToList())
                        {
                            if (!(reminder.ExpiryDate <= DateTime.Now))
                                continue;

                            SocketGuild guild = Client.GetGuild(Convert.ToUInt64(server.Id));
                            SocketUser user = guild.GetUser(kvpReminder.Key) ?? Client.GetUser(kvpReminder.Key);

                            switch (guild)
                            {
                                case null when user is null:
                                    server.Reminders.TryRemove(kvpReminder.Key, out _);
                                    break;

                                case null when !(user is null):
                                    MethodHelper.RunSync(user.GetOrCreateDMChannelAsync()).SendMessageAsync($"({StringHelper.FormatTimeSpan(reminder.ExpiryDate - reminder.RequestedDate)}) {reminder.Message}");
                                    reminders.Remove(reminder);
                                    server.Reminders.TryUpdate(kvpReminder.Key, reminders, kvpReminder.Value);
                                    break;

                                default:
                                    SocketTextChannel Channel = guild.GetTextChannel(reminder.TextChannel);
                                    if (Channel is null)
                                        MethodHelper.RunSync(user.GetOrCreateDMChannelAsync()).SendMessageAsync($"({StringHelper.FormatTimeSpan(reminder.ExpiryDate - reminder.RequestedDate)}) {reminder.Message}");
                                    else
                                        MethodHelper.RunSync(Channel.SendMessageAsync($"{user.Mention}, {StringHelper.FormatTimeSpan(reminder.ExpiryDate - reminder.RequestedDate)} ago you asked me to remind you about {reminder.Message}"));

                                    reminders.Remove(reminder);
                                    server.Reminders.TryUpdate(kvpReminder.Key, reminders, kvpReminder.Value);
                                    break;
                            }
                            if (!kvpReminder.Value.Any())
                                server.Reminders.TryRemove(kvpReminder.Key, out _);
                        }
                        DatabaseHandler.Save<GuildObject>(server, server.Id);
                    }
                }
            }).WithName("reminders").ToRunEvery(1).Minutes();

            Schedule(() =>
            {
                foreach (GuildObject server in DatabaseHandler.Servers().Where(x => x.LeaderboardFeed && x.Leaderboards.Any()))
                    foreach (LeaderboardObject leaderboard in server.Leaderboards.Where(l => l.Enabled))
                        MethodHelper.RunSync(LeaderboardHelper.BuildAndSend(leaderboard, Client.GetGuild(Convert.ToUInt64(server.Id))));
            }).WithName("leaderboards").ToRunEvery(30).Minutes();

            Schedule(() =>
            {
                ConfigObject Config = DatabaseHandler.Execute<ConfigObject>(Operation.Load, id: "Config");
                foreach (GuildObject server in DatabaseHandler.Servers().Where(s => (s.TwitchFeed || s.MixerFeed) && s.Streams.Any()))
                    foreach (StreamObject stream in server.Streams)
                        MethodHelper.RunSync(StreamHelper.BuildAndSend(stream, Client.GetGuild(Convert.ToUInt64(server.Id)), server, Config, DatabaseHandler));
            }).WithName("streams").ToRunEvery(5).Minutes();

            Schedule(() =>
            {
                foreach (GuildObject server in DatabaseHandler.Servers().Where(x => x.RssFeed && x.RssFeeds.Any()))
                    foreach (RssObject feed in server.RssFeeds)
                        MethodHelper.RunSync(RssHelper.BuildAndSend(feed, Client.GetGuild(Convert.ToUInt64(server.Id)), server, DatabaseHandler));
            }).WithName("rss").ToRunEvery(5).Minutes();

            JobManager.Initialize(this);
        }
    }
}