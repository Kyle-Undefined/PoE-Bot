namespace PoE.Bot.Handlers
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using PoE.Bot.Helpers;
    using FluentScheduler;
    using Discord.WebSocket;
    using PoE.Bot.Objects;

    public class JobHandler : Registry
    {
        DatabaseHandler DB { get; }
        DiscordSocketClient Client { get; }
        public JobHandler(DatabaseHandler dB, DiscordSocketClient client)
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
                foreach (var Server in DB.Servers().Where(x => !x.Muted.IsEmpty))
                    foreach (var Mute in Server.Muted.Where(x => x.Value < DateTime.Now))
                    {
                        Server.Muted.TryRemove(Mute.Key, out _);
                        MethodHelper.RunSync(JobHelper.UnmuteUser(Mute.Key, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server));
                        DB.Execute<GuildObject>(Operation.SAVE, Server, Server.Id);
                    }
            }).WithName("timed mute").ToRunEvery(1).Minutes().DelayFor(2).Seconds();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers().Where(x => !x.Reminders.IsEmpty))
                {
                    var RemindersCount = Server.Reminders.Count;
                    foreach (var Reminder in Server.Reminders.Where(x => x.Value.Any()))
                    {
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
                foreach (var Server in DB.Servers().Where(x => x.LeaderboardFeed && x.Leaderboards.Any()))
                    foreach (var Leaderboard in Server.Leaderboards.Where(l => l.Enabled))
                        MethodHelper.RunSync(LeaderboardHelper.BuildAndSend(Leaderboard, Client.GetGuild(Convert.ToUInt64(Server.Id))));
            }).WithName("leaderboards").ToRunEvery(30).Minutes();

            Schedule(() =>
            {
                var Config = DB.Execute<ConfigObject>(Operation.LOAD, Id: "Config");
                foreach (var Server in DB.Servers().Where(s => (s.TwitchFeed || s.MixerFeed) && s.Streams.Any()))
                    foreach (var Stream in Server.Streams)
                        MethodHelper.RunSync(StreamHelper.BuildAndSend(Stream, Client.GetGuild(Convert.ToUInt64(Server.Id)), Server, Config, DB));
            }).WithName("streams").ToRunEvery(5).Minutes().DelayFor(10).Seconds();

            Schedule(() =>
            {
                foreach (var Server in DB.Servers().Where(x => x.RssFeed && x.RssFeeds.Any()))
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
