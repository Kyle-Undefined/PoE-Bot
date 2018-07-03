namespace PoE.Bot.Helpers
{
    using Addons;
    using CsvHelper;
    using CsvHelper.Configuration;
    using Discord;
    using Discord.WebSocket;
    using Objects;
    using System;
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
        public static async Task<IAsyncResult> BuildAndSend(LeaderboardObject leaderboard, SocketGuild guild)
        {
            List<LeaderboardData> racers = new List<LeaderboardData>();

            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync($"https://www.pathofexile.com/public/ladder/Path_of_Exile_Xbox_{leaderboard.Variant}_league_export.csv", HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.IsSuccessStatusCode)
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    using (TextReader reader = new StreamReader(stream))
                    using (CsvReader csv = new CsvReader(reader))
                    {
                        csv.Configuration.RegisterClassMap<LeaderboardDataMap>();
                        await csv.ReadAsync();
                        csv.ReadHeader();

                        while (await csv.ReadAsync())
                            racers.Add(csv.GetRecord<LeaderboardData>());
                    }
            }

            if (racers.Any())
            {
                StringBuilder sb = new StringBuilder();
                var ascendants = racers.FindAll(x => x.Class is AscendancyClass.Ascendant);
                var assassins = racers.FindAll(x => x.Class is AscendancyClass.Assassin);
                var berserkers = racers.FindAll(x => x.Class is AscendancyClass.Berserker);
                var champions = racers.FindAll(x => x.Class is AscendancyClass.Champion);
                var chieftains = racers.FindAll(x => x.Class is AscendancyClass.Chieftain);
                var deadeyes = racers.FindAll(x => x.Class is AscendancyClass.Deadeye);
                var duelists = racers.FindAll(x => x.Class is AscendancyClass.Duelist);
                var elementalists = racers.FindAll(x => x.Class is AscendancyClass.Elementalist);
                var gladiators = racers.FindAll(x => x.Class is AscendancyClass.Gladiator);
                var guardians = racers.FindAll(x => x.Class is AscendancyClass.Guardian);
                var hierophants = racers.FindAll(x => x.Class is AscendancyClass.Hierophant);
                var inquisitors = racers.FindAll(x => x.Class is AscendancyClass.Inquisitor);
                var juggernauts = racers.FindAll(x => x.Class is AscendancyClass.Juggernaut);
                var marauders = racers.FindAll(x => x.Class is AscendancyClass.Marauder);
                var necromancers = racers.FindAll(x => x.Class is AscendancyClass.Necromancer);
                var occultists = racers.FindAll(x => x.Class is AscendancyClass.Occultist);
                var pathfinders = racers.FindAll(x => x.Class is AscendancyClass.Pathfinder);
                var raiders = racers.FindAll(x => x.Class is AscendancyClass.Raider);
                var rangers = racers.FindAll(x => x.Class is AscendancyClass.Ranger);
                var saboteurs = racers.FindAll(x => x.Class is AscendancyClass.Saboteur);
                var scions = racers.FindAll(x => x.Class is AscendancyClass.Scion);
                var shadows = racers.FindAll(x => x.Class is AscendancyClass.Shadow);
                var slayers = racers.FindAll(x => x.Class is AscendancyClass.Slayer);
                var templars = racers.FindAll(x => x.Class is AscendancyClass.Templar);
                var tricksters = racers.FindAll(x => x.Class is AscendancyClass.Trickster);
                var witchs = racers.FindAll(x => x.Class is AscendancyClass.Witch);

                if (ascendants.Any())
                    sb.AppendLine($"Ascendants   : {ascendants.Count().ToString("##,##0")}");
                if (assassins.Any())
                    sb.AppendLine($"Assassins    : {assassins.Count().ToString("##,##0")}");
                if (berserkers.Any())
                    sb.AppendLine($"Berserkers   : {berserkers.Count().ToString("##,##0")}");
                if (champions.Any())
                    sb.AppendLine($"Champions    : {champions.Count().ToString("##,##0")}");
                if (chieftains.Any())
                    sb.AppendLine($"Chieftains   : {chieftains.Count().ToString("##,##0")}");
                if (deadeyes.Any())
                    sb.AppendLine($"Deadeyes     : {deadeyes.Count().ToString("##,##0")}");
                if (duelists.Any())
                    sb.AppendLine($"Duelists     : {duelists.Count().ToString("##,##0")}");
                if (elementalists.Any())
                    sb.AppendLine($"Elementalists: {elementalists.Count().ToString("##,##0")}");
                if (gladiators.Any())
                    sb.AppendLine($"Gladiators   : {gladiators.Count().ToString("##,##0")}");
                if (guardians.Any())
                    sb.AppendLine($"Guardians    : {guardians.Count().ToString("##,##0")}");
                if (hierophants.Any())
                    sb.AppendLine($"Hierophants  : {hierophants.Count().ToString("##,##0")}");
                if (inquisitors.Any())
                    sb.AppendLine($"Inquisitors  : {inquisitors.Count().ToString("##,##0")}");
                if (juggernauts.Any())
                    sb.AppendLine($"Juggernauts  : {juggernauts.Count().ToString("##,##0")}");
                if (marauders.Any())
                    sb.AppendLine($"Marauders    : {marauders.Count().ToString("##,##0")}");
                if (necromancers.Any())
                    sb.AppendLine($"Necromancers : {necromancers.Count().ToString("##,##0")}");
                if (occultists.Any())
                    sb.AppendLine($"Occultists   : {occultists.Count().ToString("##,##0")}");
                if (pathfinders.Any())
                    sb.AppendLine($"Pathfinders  : {pathfinders.Count().ToString("##,##0")}");
                if (raiders.Any())
                    sb.AppendLine($"Raiders      : {raiders.Count().ToString("##,##0")}");
                if (rangers.Any())
                    sb.AppendLine($"Rangers      : {rangers.Count().ToString("##,##0")}");
                if (saboteurs.Any())
                    sb.AppendLine($"Saboteurs    : {saboteurs.Count().ToString("##,##0")}");
                if (scions.Any())
                    sb.AppendLine($"Scions       : {scions.Count().ToString("##,##0")}");
                if (shadows.Any())
                    sb.AppendLine($"Shadows      : {shadows.Count().ToString("##,##0")}");
                if (slayers.Any())
                    sb.AppendLine($"Slayers      : {slayers.Count().ToString("##,##0")}");
                if (templars.Any())
                    sb.AppendLine($"Templars     : {templars.Count().ToString("##,##0")}");
                if (tricksters.Any())
                    sb.AppendLine($"Tricksters   : {tricksters.Count().ToString("##,##0")}");
                if (witchs.Any())
                    sb.AppendLine($"Witchs       : {witchs.Count().ToString("##,##0")}");

                EmbedBuilder embed = Extras.Embed(Extras.Leaderboard)
                    .WithTitle($"{WebUtility.UrlDecode(leaderboard.Variant).Replace("_", " ")} Leaderboard")
                    .WithDescription($"Retrieved {racers.Count().ToString("##,##0")} records, Rank is overall and not by Ascendancy, below is the total of Ascendancy classes:\n```{sb.ToString()}```")
                    .WithCurrentTimestamp()
                    .AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

                var totalDuelists = duelists.Concat(slayers).Concat(gladiators).Concat(champions).ToList();
                var totalMarauders = marauders.Concat(juggernauts).Concat(chieftains).Concat(berserkers).ToList();
                var totalRangers = rangers.Concat(raiders).Concat(deadeyes).Concat(pathfinders).ToList();
                var totalScions = scions.Concat(ascendants).ToList();
                var totalShadows = shadows.Concat(saboteurs).Concat(assassins).Concat(tricksters).ToList();
                var totalTemplars = templars.Concat(inquisitors).Concat(hierophants).Concat(guardians).ToList();
                var totalWitchs = witchs.Concat(necromancers).Concat(occultists).Concat(elementalists).ToList();

                totalDuelists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                totalMarauders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                totalRangers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                totalScions.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                totalShadows.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                totalTemplars.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                totalWitchs.Sort((p, q) => p.Rank.CompareTo(q.Rank));

                if (totalDuelists.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalDuelists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Duelists, Slayers, Champions, Gladiators", $"```{sb.ToString()}```");
                }

                if (totalMarauders.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalMarauders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Marauders, Juggernauts, Chieftains, Berserkers", $"```{sb.ToString()}```");
                }

                if (totalRangers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalRangers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Rangers, Pathfinders, Raiders, Deadeyes", $"```{sb.ToString()}```");
                }

                if (totalScions.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalScions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Scions, Ascendants", $"```{sb.ToString()}```");
                }

                if (totalShadows.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalShadows.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Shadows, Saboteurs, Assassins, Tricksters", $"```{sb.ToString()}```");
                }

                if (totalTemplars.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalTemplars.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Templars, Guardians, Inquisitors, Hierophants", $"```{sb.ToString()}```");
                }

                if (totalWitchs.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalWitchs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Witches, Necromancers, Occultists, Elemantalists", $"```{sb.ToString()}```");
                }

                EmbedBuilder embedClasses = Extras.Embed(Extras.Leaderboard)
                    .WithTitle("Top 10 Characters of each Class")
                    .WithDescription("Rank is overall and not by Class.")
                    .WithCurrentTimestamp();

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
                    foreach (LeaderboardData racer in totalDuelists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Duelists", $"```{sb.ToString()}```");
                }

                if (shadows.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalShadows.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Shadows", $"```{sb.ToString()}```");
                }

                if (marauders.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalMarauders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Marauders", $"```{sb.ToString()}```");
                }

                if (witchs.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalWitchs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Witches", $"```{sb.ToString()}```");
                }

                if (rangers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalRangers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Rangers", $"```{sb.ToString()}```");
                }

                if (templars.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalTemplars.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Templars", $"```{sb.ToString()}```");
                }

                if (scions.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalScions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Scions", $"```{sb.ToString()}```");
                }

                EmbedBuilder embedAscendancy = Extras.Embed(Extras.Leaderboard)
                    .WithTitle("Top 10 Characters of each Ascendancy")
                    .WithDescription("Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp();

                ascendants.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                assassins.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                berserkers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                champions.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                chieftains.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                deadeyes.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                elementalists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                gladiators.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                guardians.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                hierophants.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                inquisitors.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                juggernauts.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                necromancers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                occultists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                pathfinders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                raiders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                saboteurs.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                slayers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                tricksters.Sort((p, q) => p.Rank.CompareTo(q.Rank));

                if (slayers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in slayers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Slayers", $"```{sb.ToString()}```");
                }

                if (champions.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in champions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Champions", $"```{sb.ToString()}```");
                }

                if (gladiators.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in gladiators.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Gladiators", $"```{sb.ToString()}```");
                }

                if (assassins.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in assassins.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Assassins", $"```{sb.ToString()}```");
                }

                if (saboteurs.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in saboteurs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Saboteurs", $"```{sb.ToString()}```");
                }

                if (tricksters.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in tricksters.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Tricksters", $"```{sb.ToString()}```");
                }

                if (juggernauts.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in juggernauts.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Juggernauts", $"```{sb.ToString()}```");
                }

                if (berserkers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in berserkers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Berserkers", $"```{sb.ToString()}```");
                }

                if (chieftains.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in chieftains.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Chieftains", $"```{sb.ToString()}```");
                }

                if (necromancers.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in necromancers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Necromancers", $"```{sb.ToString()}```");
                }

                EmbedBuilder embedAscendancyCont = Extras.Embed(Extras.Leaderboard)
                    .WithTitle("Top 10 Characters of each Ascendancy")
                    .WithDescription("Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp();

                if (elementalists.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in elementalists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Elemantalists", $"```{sb.ToString()}```");
                }

                if (occultists.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in occultists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Occultists", $"```{sb.ToString()}```");
                }

                if (deadeyes.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in deadeyes.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Deadeyes", $"```{sb.ToString()}```");
                }

                if (raiders.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in raiders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Raiders", $"```{sb.ToString()}```");
                }

                if (pathfinders.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in pathfinders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Pathfinders", $"```{sb.ToString()}```");
                }

                if (inquisitors.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in inquisitors.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Inquisitors", $"```{sb.ToString()}```");
                }

                if (hierophants.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in hierophants.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Hierophants", $"```{sb.ToString()}```");
                }

                if (guardians.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in guardians.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Guardians", $"```{sb.ToString()}```");
                }

                if (ascendants.Any())
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in ascendants.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Ascendants", $"```{sb.ToString()}```");
                }

                EmbedBuilder discordians = Extras.Embed(Extras.Leaderboard)
                    .WithTitle($"Discordians Only {leaderboard.Variant.Replace("_", " ")} Leaderboard")
                    .WithDescription($"Retrieved {racers.Count(r => r.Character.ToLower().Contains("discord")).ToString("##,##0")} users with Discord in their name.")
                    .WithCurrentTimestamp()
                    .AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

                if (totalDuelists.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalDuelists.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Duelists, Slayers, Champions, Gladiators", $"```{sb.ToString()}```");
                }

                if (totalShadows.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalShadows.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Shadows, Saboteurs, Assassins, Tricksters", $"```{sb.ToString()}```");
                }

                if (totalMarauders.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalMarauders.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Marauders, Juggernauts, Chieftains, Berserkers", $"```{sb.ToString()}```");
                }

                if (totalWitchs.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalWitchs.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Witches, Necromancers, Occultists, Elemantalists", $"```{sb.ToString()}```");
                }

                if (totalRangers.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalRangers.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Rangers, Pathfinders, Raiders, Deadeyes", $"```{sb.ToString()}```");
                }

                if (totalTemplars.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalTemplars.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Templars, Guardians, Inquisitors, Hierophants", $"```{sb.ToString()}```");
                }

                if (totalScions.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (LeaderboardData racer in totalScions.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    discordians.AddField("Scions, Ascendants", $"```{sb.ToString()}```");
                }

                SocketTextChannel channel = guild.GetTextChannel(leaderboard.ChannelId);
                var messages = channel.GetMessagesAsync().FlattenAsync().GetAwaiter().GetResult();
                foreach (IMessage msg in messages)
                    await msg.DeleteAsync();

                await channel.SendMessageAsync(embed: embed.Build());

                if (embedClasses.Fields.Any())
                    await channel.SendMessageAsync(embed: embedClasses.Build());
                if (embedAscendancy.Fields.Any())
                    await channel.SendMessageAsync(embed: embedAscendancy.Build());
                if (embedAscendancyCont.Fields.Any())
                    await channel.SendMessageAsync(embed: embedAscendancyCont.Build());
                if (discordians.Fields.Any())
                    await channel.SendMessageAsync(embed: discordians.Build());

                await Task.Delay(30000);
            }

            return Task.CompletedTask;
        }
    }
}