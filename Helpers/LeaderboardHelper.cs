namespace PoE.Bot.Helpers
{
    using Addons;
    using CsvHelper;
    using CsvHelper.Configuration;
    using Discord;
    using Discord.WebSocket;
    using Objects;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public enum AscendancyClass
    {
        Ascendant,
        Assassin,
        Berserker,
        Champion,
        Chieftain,
        Deadeye,
        Duelist,
        Elementalist,
        Gladiator,
        Guardian,
        Hierophant,
        Inquisitor,
        Juggernaut,
        Marauder,
        Necromancer,
        Occultist,
        Pathfinder,
        Raider,
        Ranger,
        Saboteur,
        Scion,
        Shadow,
        Slayer,
        Templar,
        Trickster,
        Witch
    }

    public partial class LeaderboardData
    {
        public LeaderboardData()
        {
        }

        public LeaderboardData(int rank, string character, AscendancyClass ascendancyClass, int level, ulong experience, bool dead)
        {
            Rank = rank;
            Character = character;
            Class = ascendancyClass;
            Level = level;
            Experience = experience;
            Dead = dead;
        }

        public string Character { get; set; }
        public AscendancyClass Class { get; set; }
        public bool Dead { get; set; }
        public ulong Experience { get; set; }
        public int Level { get; set; }
        public int Rank { get; set; }
    }

    public sealed partial class LeaderboardDataMap : ClassMap<LeaderboardData>
    {
        public LeaderboardDataMap()
        {
            AutoMap();
            Map(m => m.Dead)
                .TypeConverterOption.BooleanValues(true, true, "Dead")
                .TypeConverterOption.BooleanValues(false, true, string.Empty)
                .Default(false);
        }
    }

    public partial class LeaderboardHelper
    {
        public static async Task BuildAndSend(LeaderboardObject leaderboard, SocketGuild guild, HttpClient httpClient)
        {
            List<LeaderboardData> racers = new List<LeaderboardData>();

            using (HttpResponseMessage response = await httpClient.GetAsync($"https://www.pathofexile.com/public/ladder/Path_of_Exile_Xbox_{leaderboard.Variant}_league_export.csv", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (TextReader reader = new StreamReader(stream))
                    using (CsvReader csv = new CsvReader(reader))
                    {
                        csv.Configuration.RegisterClassMap<LeaderboardDataMap>();
                        await csv.ReadAsync().ConfigureAwait(false);
                        csv.ReadHeader();

                        while (await csv.ReadAsync().ConfigureAwait(false))
                            racers.Add(csv.GetRecord<LeaderboardData>());
                    }
            }

            if (racers.Any())
            {
                StringBuilder sb = new StringBuilder();
                var duelists = racers.Where(r => r.Class is AscendancyClass.Duelist || r.Class is AscendancyClass.Slayer || r.Class is AscendancyClass.Gladiator || r.Class is AscendancyClass.Champion).ToList();
                var marauders = racers.Where(r => r.Class is AscendancyClass.Marauder || r.Class is AscendancyClass.Juggernaut || r.Class is AscendancyClass.Chieftain || r.Class is AscendancyClass.Berserker).ToList();
                var rangers = racers.Where(r => r.Class is AscendancyClass.Ranger || r.Class is AscendancyClass.Raider || r.Class is AscendancyClass.Deadeye || r.Class is AscendancyClass.Pathfinder).ToList();
                var scions = racers.Where(r => r.Class is AscendancyClass.Scion || r.Class is AscendancyClass.Ascendant).ToList();
                var shadows = racers.Where(r => r.Class is AscendancyClass.Shadow || r.Class is AscendancyClass.Saboteur || r.Class is AscendancyClass.Assassin || r.Class is AscendancyClass.Trickster).ToList();
                var templars = racers.Where(r => r.Class is AscendancyClass.Templar || r.Class is AscendancyClass.Inquisitor || r.Class is AscendancyClass.Hierophant || r.Class is AscendancyClass.Guardian).ToList();
                var witchs = racers.Where(r => r.Class is AscendancyClass.Witch || r.Class is AscendancyClass.Necromancer || r.Class is AscendancyClass.Occultist || r.Class is AscendancyClass.Elementalist).ToList();

                EmbedBuilder embed = Extras.Embed(Extras.Leaderboard)
                    .WithTitle($"{WebUtility.UrlDecode(leaderboard.Variant).Replace("_", " ")} Leaderboard")
                    .WithDescription($"Retrieved {racers.Count().ToString("##,##0")} records, Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp()
                    .AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

                duelists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                marauders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rangers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                scions.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                shadows.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                templars.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                witchs.Sort((p, q) => p.Rank.CompareTo(q.Rank));

                if (duelists.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in duelists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Duelists, Slayers, Champions, Gladiators", $"```{sb.ToString()}```");
                }

                if (marauders.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in marauders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Marauders, Juggernauts, Chieftains, Berserkers", $"```{sb.ToString()}```");
                }

                if (rangers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in rangers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Rangers, Pathfinders, Raiders, Deadeyes", $"```{sb.ToString()}```");
                }

                if (scions.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in scions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Scions, Ascendants", $"```{sb.ToString()}```");
                }

                if (shadows.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in shadows.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Shadows, Saboteurs, Assassins, Tricksters", $"```{sb.ToString()}```");
                }

                if (templars.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in templars.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Templars, Guardians, Inquisitors, Hierophants", $"```{sb.ToString()}```");
                }

                if (witchs.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in witchs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Witches, Necromancers, Occultists, Elemantalists", $"```{sb.ToString()}```");
                }

                EmbedBuilder embedClasses = Extras.Embed(Extras.Leaderboard)
                    .WithTitle("Top 10 Characters of each Class")
                    .WithDescription("Rank is overall and not by Class.")
                    .WithCurrentTimestamp();

                if (duelists.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in duelists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Duelists", $"```{sb.ToString()}```");
                }

                if (shadows.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in shadows.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Shadows", $"```{sb.ToString()}```");
                }

                if (marauders.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in marauders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Marauders", $"```{sb.ToString()}```");
                }

                if (witchs.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in witchs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Witches", $"```{sb.ToString()}```");
                }

                if (rangers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in rangers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Rangers", $"```{sb.ToString()}```");
                }

                if (templars.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in templars.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Templars", $"```{sb.ToString()}```");
                }

                if (scions.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in scions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Scions", $"```{sb.ToString()}```");
                }

                EmbedBuilder embedAscendancy = Extras.Embed(Extras.Leaderboard)
                    .WithTitle("Top 10 Characters of each Ascendancy")
                    .WithDescription("Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp();

                if (duelists.Any(r => r.Class is AscendancyClass.Slayer))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in duelists.Where(r => r.Class is AscendancyClass.Slayer).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Slayers", $"```{sb.ToString()}```");
                }

                if (duelists.Any(r => r.Class is AscendancyClass.Champion))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in duelists.Where(r => r.Class is AscendancyClass.Champion).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Champions", $"```{sb.ToString()}```");
                }

                if (duelists.Any(r => r.Class is AscendancyClass.Gladiator))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in duelists.Where(r => r.Class is AscendancyClass.Gladiator).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Gladiators", $"```{sb.ToString()}```");
                }

                if (shadows.Any(r => r.Class is AscendancyClass.Assassin))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in shadows.Where(r => r.Class is AscendancyClass.Assassin).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Assassins", $"```{sb.ToString()}```");
                }

                if (shadows.Any(r => r.Class is AscendancyClass.Saboteur))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in shadows.Where(r => r.Class is AscendancyClass.Saboteur).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Saboteurs", $"```{sb.ToString()}```");
                }

                if (shadows.Any(r => r.Class is AscendancyClass.Trickster))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in shadows.Where(r => r.Class is AscendancyClass.Trickster).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Tricksters", $"```{sb.ToString()}```");
                }

                if (marauders.Any(r => r.Class is AscendancyClass.Juggernaut))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in marauders.Where(r => r.Class is AscendancyClass.Juggernaut).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Juggernauts", $"```{sb.ToString()}```");
                }

                if (marauders.Any(r => r.Class is AscendancyClass.Berserker))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in marauders.Where(r => r.Class is AscendancyClass.Berserker).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Berserkers", $"```{sb.ToString()}```");
                }

                if (marauders.Any(r => r.Class is AscendancyClass.Chieftain))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in marauders.Where(r => r.Class is AscendancyClass.Chieftain).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Chieftains", $"```{sb.ToString()}```");
                }

                if (witchs.Any(r => r.Class is AscendancyClass.Necromancer))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in witchs.Where(r => r.Class is AscendancyClass.Necromancer).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Necromancers", $"```{sb.ToString()}```");
                }

                EmbedBuilder embedAscendancyCont = Extras.Embed(Extras.Leaderboard)
                    .WithTitle("Top 10 Characters of each Ascendancy")
                    .WithDescription("Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp();

                if (witchs.Any(r => r.Class is AscendancyClass.Elementalist))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in witchs.Where(r => r.Class is AscendancyClass.Elementalist).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Elemantalists", $"```{sb.ToString()}```");
                }

                if (witchs.Any(r => r.Class is AscendancyClass.Occultist))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in witchs.Where(r => r.Class is AscendancyClass.Occultist).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Occultists", $"```{sb.ToString()}```");
                }

                if (rangers.Any(r => r.Class is AscendancyClass.Deadeye))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in rangers.Where(r => r.Class is AscendancyClass.Deadeye).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Deadeyes", $"```{sb.ToString()}```");
                }

                if (rangers.Any(r => r.Class is AscendancyClass.Raider))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in rangers.Where(r => r.Class is AscendancyClass.Raider).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Raiders", $"```{sb.ToString()}```");
                }

                if (rangers.Any(r => r.Class is AscendancyClass.Pathfinder))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in rangers.Where(r => r.Class is AscendancyClass.Pathfinder).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Pathfinders", $"```{sb.ToString()}```");
                }

                if (templars.Any(r => r.Class is AscendancyClass.Inquisitor))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in templars.Where(r => r.Class is AscendancyClass.Inquisitor).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Inquisitors", $"```{sb.ToString()}```");
                }

                if (templars.Any(r => r.Class is AscendancyClass.Hierophant))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in templars.Where(r => r.Class is AscendancyClass.Hierophant).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Hierophants", $"```{sb.ToString()}```");
                }

                if (templars.Any(r => r.Class is AscendancyClass.Guardian))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in templars.Where(r => r.Class is AscendancyClass.Guardian).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Guardians", $"```{sb.ToString()}```");
                }

                if (scions.Any(r => r.Class is AscendancyClass.Ascendant))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in scions.Where(r => r.Class is AscendancyClass.Ascendant).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Ascendants", $"```{sb.ToString()}```");
                }

                EmbedBuilder discordians = Extras.Embed(Extras.Leaderboard)
                    .WithTitle($"Discordians Only {leaderboard.Variant.Replace("_", " ")} Leaderboard")
                    .WithDescription($"Retrieved {racers.Count(r => r.Character.ToLower().Contains("discord")).ToString("##,##0")} users with Discord in their name.")
                    .WithCurrentTimestamp()
                    .AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

                if (duelists.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in duelists.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Duelists, Slayers, Champions, Gladiators", $"```{sb.ToString()}```");
                }

                if (marauders.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in marauders.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Marauders, Juggernauts, Chieftains, Berserkers", $"```{sb.ToString()}```");
                }

                if (rangers.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in rangers.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Rangers, Pathfinders, Raiders, Deadeyes", $"```{sb.ToString()}```");
                }

                if (scions.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in scions.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Scions, Ascendants", $"```{sb.ToString()}```");
                }

                if (shadows.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in shadows.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Shadows, Saboteurs, Assassins, Tricksters", $"```{sb.ToString()}```");
                }

                if (templars.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in templars.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Templars, Guardians, Inquisitors, Hierophants", $"```{sb.ToString()}```");
                }

                if (witchs.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in witchs.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,4} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Witches, Necromancers, Occultists, Elemantalists", $"```{sb.ToString()}```");
                }

                SocketTextChannel channel = guild.GetTextChannel(leaderboard.ChannelId);
                var messages = await channel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
                foreach (IMessage msg in messages)
                    await msg.DeleteAsync().ConfigureAwait(false);

                await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);

                if (embedClasses.Fields.Any())
                    await channel.SendMessageAsync(embed: embedClasses.Build()).ConfigureAwait(false);
                if (embedAscendancy.Fields.Any())
                    await channel.SendMessageAsync(embed: embedAscendancy.Build()).ConfigureAwait(false);
                if (embedAscendancyCont.Fields.Any())
                    await channel.SendMessageAsync(embed: embedAscendancyCont.Build()).ConfigureAwait(false);
                if (discordians.Fields.Any())
                    await channel.SendMessageAsync(embed: discordians.Build()).ConfigureAwait(false);

                await Task.Delay(30000).ConfigureAwait(false);
            }
        }
    }
}