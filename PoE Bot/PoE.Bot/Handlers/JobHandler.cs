namespace PoE.Bot.Handlers
{
    using System;
    using PoE.Bot.Helpers;
    using FluentScheduler;
    using Discord.WebSocket;
    using PoE.Bot.Handlers.Objects;

    public class JobHandler : Registry
    {
        DBHandler DB { get; }
        DiscordSocketClient Client { get; }
        public JobHandler(DBHandler dB, DiscordSocketClient client)
        {
            DB = dB;
            Client = client;
            JobManager.UseUtcTime();
            JobManager.JobEnd += (Info)
                => LogHandler.Write(Source.EVT, $"Finished {Info.Name} in {Info.Duration}");
            JobManager.JobException += (Info)
                => LogHandler.Write(Source.EXC, $"Exception ocurred in {Info.Name} job.\n{Info.Exception.Message}\n{Info.Exception.StackTrace}");
        }

        public void Initialize()
        {
            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (!Server.Muted.IsEmpty || Server.Muted.Count != 0)
                        foreach (var Mute in Server.Muted)
                        {
                            if (Mute.Value < DateTime.UtcNow)
                            {
                                Server.Muted.TryRemove(Mute.Key, out _);
                                JobHelper.UnmuteUser(Mute.Key, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server);
                                DB.Execute<GuildObject>(Operation.SAVE, Server, Server.Id);
                            }
                        }
            }).WithName("timed mute").ToRunEvery(1).Minutes();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (!Server.Reminders.IsEmpty || Server.Reminders.Count != 0)
                        foreach (var Reminder in Server.Reminders)
                        {
                            if (Reminder.Value.ExpiryDate < DateTime.UtcNow)
                            {
                                var Guild = Client.GetGuild(Convert.ToUInt64(Server.Id));
                                var User = Client.GetUser(Reminder.Key);
                                if (User == null && Guild == null) Server.Reminders.TryRemove(Reminder.Key, out _);
                                else if (Guild == null && User != null)
                                {
                                    MethodHelper.RunSync(User.GetOrCreateDMChannelAsync())
                                .SendMessageAsync(Reminder.Value.Message);
                                    Server.Reminders.TryRemove(Reminder.Key, out _);
                                }
                                else
                                {
                                    var Channel = Guild.GetChannel(Reminder.Value.TextChannel);
                                    if (Channel == null)
                                        MethodHelper.RunSync(User.GetOrCreateDMChannelAsync()).SendMessageAsync(
                                        $"{StringHelper.FormatTimeSpan(DateTime.UtcNow - Reminder.Value.RequestedDate)} ago you asked me to remind you about {Reminder.Value.Message}");
                                    else (Channel as SocketTextChannel).SendMessageAsync($"{User.Mention}, " +
                                        $"{StringHelper.FormatTimeSpan(DateTime.UtcNow - Reminder.Value.RequestedDate)} ago you asked me to remind you about {Reminder.Value.Message}");
                                    Server.Reminders.TryRemove(Reminder.Key, out _);
                                }
                                DB.Execute<GuildObject>(Operation.SAVE, Server, Server.Id);
                            }
                        }
            }).WithName("reminders").ToRunEvery(1).Minutes();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (Server.LeaderboardFeed)
                        if (Server.Leaderboards.Count != 0)
                            foreach (var Leaderboard in Server.Leaderboards)
                                if (Leaderboard.Enabled)
                                    LeaderboardHelper.BuildAndSend(Leaderboard, Client.GetGuild(Convert.ToUInt64(Server.Id)));
            }).WithName("leaderboards").ToRunEvery(1).Hours();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (Server.MixerFeed)
                        if (Server.MixerStreams.Count != 0)
                            foreach (var Mixer in Server.MixerStreams)
                                MixerHelper.BuildAndSend(Mixer, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, DB);
            }).WithName("mixer streams").ToRunEvery(15).Minutes();

            Schedule(() =>
            {
                var Config = DB.Execute<ConfigObject>(Operation.LOAD, Id: "Config");
                foreach (var Server in DB.Servers())
                    if (Server.TwitchFeed)
                        if (Server.TwitchStreams.Count != 0)
                            foreach (var Twitch in Server.TwitchStreams)
                                TwitchHelper.BuildAndSend(Twitch, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, Config, DB);
            }).WithName("twitch streams").ToRunEvery(15).Minutes();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (Server.RssFeed)
                        if (Server.RssFeeds.Count != 0)
                            foreach (var Feed in Server.RssFeeds)
                                RssHelper.BuildAndSend(Feed, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, DB);
            }).WithName("rss feeds").ToRunEvery(10).Minutes();

            JobManager.Initialize(this);
        }

        public void RunJob(Action action, string Name, int Interval, Time Time = Time.MINUTES)
            => JobManager.AddJob(() => action(), Sch =>
            {
                var Set = Sch.WithName(Name).ToRunEvery(Interval);
                switch (Time)
                {
                    case Time.HOURS: Set.Hours(); break;
                    case Time.MINUTES: Set.Minutes(); break;
                }
            });

        public void RemoveJob(string Name) => JobManager.RemoveJob(Name);

        public void ClearJobs() => JobManager.StopAndBlock();
    }

    public enum Time
    {
        HOURS,
        MINUTES,
    }
}
