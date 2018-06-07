namespace PoE.Bot.Handlers
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
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
                            if (Mute.Value < DateTime.Now)
                            {
                                Server.Muted.TryRemove(Mute.Key, out _);
                                MethodHelper.RunSync(JobHelper.UnmuteUser(Mute.Key, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server));
                                DB.Execute<GuildObject>(Operation.SAVE, Server, Server.Id);
                            }
                        }
            }).WithName("timed mute").ToRunEvery(1).Minutes().DelayFor(2).Seconds();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                {
                    if (Server.Reminders.IsEmpty)
                        continue;
                    var RemindersCount = Server.Reminders.Count;
                    foreach (var Reminder in Server.Reminders)
                    {
                        if (!Reminder.Value.Any())
                            continue;
                        var Reminders = new List<RemindObject>();
                        Server.Reminders.TryGetValue(Reminder.Key, out Reminders);
                        for (int i = 0; i < Reminders.Count; i++)
                        {
                            if (!(Reminders[i].ExpiryDate <= DateTime.Now))
                                continue;
                            var Guild = Client.GetGuild(Convert.ToUInt64(Server.Id));
                            var User = Guild.GetUser(Reminder.Key) ?? Client.GetUser(Reminder.Key);
                            if (Guild is null && User is null)
                                Server.Reminders.TryRemove(Reminder.Key, out _);
                            else if(Guild is null && !(User is null))
                            {
                                MethodHelper.RunSync(User.GetOrCreateDMChannelAsync()).SendMessageAsync($"({StringHelper.FormatTimeSpan(Reminders[i].ExpiryDate - Reminders[i].RequestedDate)}) {Reminders[i].Message}");
                                Reminders.Remove(Reminders[i]);
                                Server.Reminders.TryUpdate(Reminder.Key, Reminders, Reminder.Value);
                            }
                            else
                            {
                                var Channel = Guild.GetTextChannel(Reminders[i].TextChannel);
                                if(Channel is null)
                                    MethodHelper.RunSync(User.GetOrCreateDMChannelAsync()).SendMessageAsync($"({StringHelper.FormatTimeSpan(Reminders[i].ExpiryDate - Reminders[i].RequestedDate)}) {Reminders[i].Message}");
                                else
                                    MethodHelper.RunSync(Channel.SendMessageAsync($"{User.Mention}, {StringHelper.FormatTimeSpan(Reminders[i].ExpiryDate - Reminders[i].RequestedDate)} ago you asked me to remind you about {Reminders[i].Message}"));
                                Reminders.Remove(Reminders[i]);
                                Server.Reminders.TryUpdate(Reminder.Key, Reminders, Reminder.Value);
                            }
                            if(!Reminder.Value.Any())
                                Server.Reminders.TryRemove(Reminder.Key, out _);

                        }
                        if (RemindersCount != Server.Reminders.Count)
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
                                    MethodHelper.RunSync(LeaderboardHelper.BuildAndSend(Leaderboard, Client.GetGuild(Convert.ToUInt64(Server.Id))));
            }).WithName("leaderboards").ToRunEvery(30).Minutes();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (Server.MixerFeed)
                        if (Server.MixerStreams.Count != 0)
                            foreach (var Mixer in Server.MixerStreams)
                                MethodHelper.RunSync(MixerHelper.BuildAndSend(Mixer, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, DB));
            }).WithName("mixer streams").ToRunEvery(5).Minutes().DelayFor(20).Seconds();

            Schedule(() =>
            {
                var Config = DB.Execute<ConfigObject>(Operation.LOAD, Id: "Config");
                foreach (var Server in DB.Servers())
                    if (Server.TwitchFeed)
                        if (Server.TwitchStreams.Count != 0)
                            foreach (var Twitch in Server.TwitchStreams)
                                MethodHelper.RunSync(TwitchHelper.BuildAndSend(Twitch, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, Config, DB));
            }).WithName("twitch streams").ToRunEvery(5).Minutes().DelayFor(10).Seconds();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers())
                    if (Server.RssFeed)
                        if (Server.RssFeeds.Count != 0)
                            foreach (var Feed in Server.RssFeeds)
                                MethodHelper.RunSync(RssHelper.BuildAndSend(Feed, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, DB));
            }).WithName("rss feeds").ToRunEvery(5).Minutes();

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
