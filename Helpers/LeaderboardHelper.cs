namespace PoE.Bot.Helpers
{
    using Discord;
    using System.Net.Http;
    using System.Net;
    using System.IO;
    using System.Linq;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using PoE.Bot.Addons;
    using Drawing = System.Drawing.Color;
    using System.Collections.Generic;
    using System.Text;
    using CsvHelper;
    using CsvHelper.Configuration;

    public class LeaderboardHelper
    {
        public static async Task BuildAndSend(LeaderboardObject Leaderboard, SocketGuild Guild)
        {
            List<LeaderboardData> racers = new List<LeaderboardData>();

            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync($"https://www.pathofexile.com/public/ladder/Path_of_Exile_Xbox_{Leaderboard.Variant}_league_export.csv", HttpCompletionOption.ResponseHeadersRead))
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
                var sb = new StringBuilder();
                var rSlayers = racers.FindAll(x => x.Class == AscendancyClass.Slayer);
                var rGladiators = racers.FindAll(x => x.Class == AscendancyClass.Gladiator);
                var rChampions = racers.FindAll(x => x.Class == AscendancyClass.Champion);
                var rAssassins = racers.FindAll(x => x.Class == AscendancyClass.Assassin);
                var rSaboteurs = racers.FindAll(x => x.Class == AscendancyClass.Saboteur);
                var rTricksters = racers.FindAll(x => x.Class == AscendancyClass.Trickster);
                var rJuggernauts = racers.FindAll(x => x.Class == AscendancyClass.Juggernaut);
                var rBerserkers = racers.FindAll(x => x.Class == AscendancyClass.Berserker);
                var rChieftains = racers.FindAll(x => x.Class == AscendancyClass.Chieftain);
                var rNecromancers = racers.FindAll(x => x.Class == AscendancyClass.Necromancer);
                var rElementalists = racers.FindAll(x => x.Class == AscendancyClass.Elementalist);
                var rOccultists = racers.FindAll(x => x.Class == AscendancyClass.Occultist);
                var rDeadeyes = racers.FindAll(x => x.Class == AscendancyClass.Deadeye);
                var rRaiders = racers.FindAll(x => x.Class == AscendancyClass.Raider);
                var rPathfinders = racers.FindAll(x => x.Class == AscendancyClass.Pathfinder);
                var rInquisitors = racers.FindAll(x => x.Class == AscendancyClass.Inquisitor);
                var rHierophants = racers.FindAll(x => x.Class == AscendancyClass.Hierophant);
                var rGuardians = racers.FindAll(x => x.Class == AscendancyClass.Guardian);
                var rAscendants = racers.FindAll(x => x.Class == AscendancyClass.Ascendant);
                var rDuelists = racers.FindAll(x => x.Class == AscendancyClass.Duelist);
                var rShadows = racers.FindAll(x => x.Class == AscendancyClass.Shadow);
                var rMarauders = racers.FindAll(x => x.Class == AscendancyClass.Marauder);
                var rWitchs = racers.FindAll(x => x.Class == AscendancyClass.Witch);
                var rRangers = racers.FindAll(x => x.Class == AscendancyClass.Ranger);
                var rTemplars = racers.FindAll(x => x.Class == AscendancyClass.Templar);
                var rScions = racers.FindAll(x => x.Class == AscendancyClass.Scion);

                if (rSlayers.Any())
                    sb.AppendLine($"Slayers      : {rSlayers.Count().ToString("##,##0")}");
                if (rGladiators.Any())
                    sb.AppendLine($"Gladiators   : {rGladiators.Count().ToString("##,##0")}");
                if (rChampions.Any())
                    sb.AppendLine($"Champions    : {rChampions.Count().ToString("##,##0")}");
                if (rAssassins.Any())
                    sb.AppendLine($"Assassins    : {rAssassins.Count().ToString("##,##0")}");
                if (rSaboteurs.Any())
                    sb.AppendLine($"Saboteurs    : {rSaboteurs.Count().ToString("##,##0")}");
                if (rTricksters.Any())
                    sb.AppendLine($"Tricksters   : {rTricksters.Count().ToString("##,##0")}");
                if (rJuggernauts.Any())
                    sb.AppendLine($"Juggernauts  : {rJuggernauts.Count().ToString("##,##0")}");
                if (rBerserkers.Any())
                    sb.AppendLine($"Berserkers   : {rBerserkers.Count().ToString("##,##0")}");
                if (rChieftains.Any())
                    sb.AppendLine($"Chieftains   : {rChieftains.Count().ToString("##,##0")}");
                if (rNecromancers.Any())
                    sb.AppendLine($"Necromancers : {rNecromancers.Count().ToString("##,##0")}");
                if (rElementalists.Any())
                    sb.AppendLine($"Elementalists: {rElementalists.Count().ToString("##,##0")}");
                if (rOccultists.Any())
                    sb.AppendLine($"Occultists   : {rOccultists.Count().ToString("##,##0")}");
                if (rDeadeyes.Any())
                    sb.AppendLine($"Deadeyes     : {rDeadeyes.Count().ToString("##,##0")}");
                if (rRaiders.Any())
                    sb.AppendLine($"Raiders      : {rRaiders.Count().ToString("##,##0")}");
                if (rPathfinders.Any())
                    sb.AppendLine($"Pathfinders  : {rPathfinders.Count().ToString("##,##0")}");
                if (rInquisitors.Any())
                    sb.AppendLine($"Inquisitors  : {rInquisitors.Count().ToString("##,##0")}");
                if (rHierophants.Any())
                    sb.AppendLine($"Hierophants  : {rHierophants.Count().ToString("##,##0")}");
                if (rGuardians.Any())
                    sb.AppendLine($"Guardians    : {rGuardians.Count().ToString("##,##0")}");
                if (rAscendants.Any())
                    sb.AppendLine($"Ascendants   : {rAscendants.Count().ToString("##,##0")}");
                if (rDuelists.Any())
                    sb.AppendLine($"Duelists     : {rDuelists.Count().ToString("##,##0")}");
                if (rShadows.Any())
                    sb.AppendLine($"Shadows      : {rShadows.Count().ToString("##,##0")}");
                if (rMarauders.Any())
                    sb.AppendLine($"Marauders    : {rMarauders.Count().ToString("##,##0")}");
                if (rWitchs.Any())
                    sb.AppendLine($"Witchs       : {rWitchs.Count().ToString("##,##0")}");
                if (rRangers.Any())
                    sb.AppendLine($"Rangers      : {rRangers.Count().ToString("##,##0")}");
                if (rTemplars.Any())
                    sb.AppendLine($"Templars     : {rTemplars.Count().ToString("##,##0")}");
                if (rScions.Any())
                    sb.AppendLine($"Scions       : {rScions.Count().ToString("##,##0")}");

                var embed = Extras.Embed(Drawing.Aqua)
                    .WithTitle($"{WebUtility.UrlDecode(Leaderboard.Variant).Replace("_", " ")} Leaderboard")
                    .WithDescription($"Retrieved {racers.Count().ToString("##,##0")} records, Rank is overall and not by Ascendancy, below is the total of Ascendancy classes:\n```{sb.ToString()}```")
                    .WithCurrentTimestamp()
                    .AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

                var cDuelists = racers.FindAll(x => x.Class == AscendancyClass.Duelist || x.Class == AscendancyClass.Slayer || x.Class == AscendancyClass.Gladiator || x.Class == AscendancyClass.Champion);
                var cShadows = racers.FindAll(x => x.Class == AscendancyClass.Shadow || x.Class == AscendancyClass.Saboteur || x.Class == AscendancyClass.Assassin || x.Class == AscendancyClass.Trickster);
                var cMarauders = racers.FindAll(x => x.Class == AscendancyClass.Marauder || x.Class == AscendancyClass.Juggernaut || x.Class == AscendancyClass.Chieftain || x.Class == AscendancyClass.Berserker);
                var cWitchs = racers.FindAll(x => x.Class == AscendancyClass.Witch || x.Class == AscendancyClass.Necromancer || x.Class == AscendancyClass.Occultist || x.Class == AscendancyClass.Elementalist);
                var cRangers = racers.FindAll(x => x.Class == AscendancyClass.Ranger || x.Class == AscendancyClass.Raider || x.Class == AscendancyClass.Deadeye || x.Class == AscendancyClass.Pathfinder);
                var cTemplars = racers.FindAll(x => x.Class == AscendancyClass.Templar || x.Class == AscendancyClass.Inquisitor || x.Class == AscendancyClass.Hierophant || x.Class == AscendancyClass.Guardian);
                var cScions = racers.FindAll(x => x.Class == AscendancyClass.Scion || x.Class == AscendancyClass.Ascendant);

                cDuelists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                cShadows.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                cMarauders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                cWitchs.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                cRangers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                cTemplars.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                cScions.Sort((p, q) => p.Rank.CompareTo(q.Rank));

                if (cDuelists.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cDuelists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Duelists, Slayers, Champions, Gladiators", $"```{sb.ToString()}```");
                }

                if (cShadows.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cShadows.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Shadows, Saboteurs, Assassins, Tricksters", $"```{sb.ToString()}```");
                }

                if (cMarauders.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cMarauders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Marauders, Juggernauts, Chieftains, Berserkers", $"```{sb.ToString()}```");
                }

                if (cWitchs.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cWitchs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Witches, Necromancers, Occultists, Elemantalists", $"```{sb.ToString()}```");
                }

                if (cRangers.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cRangers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Rangers, Pathfinders, Raiders, Deadeyes", $"```{sb.ToString()}```");
                }

                if (cTemplars.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cTemplars.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Templars, Guardians, Inquisitors, Hierophants", $"```{sb.ToString()}```");
                }

                if (cScions.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cScions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    embed.AddField("Scions, Ascendants", $"```{sb.ToString()}```");
                }

                var embedClasses = Extras.Embed(Drawing.Aqua)
                    .WithTitle("Top 10 Characters of each Class")
                    .WithDescription("Rank is overall and not by Class.")
                    .WithCurrentTimestamp();

                rDuelists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rShadows.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rMarauders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rWitchs.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rRangers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rTemplars.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rScions.Sort((p, q) => p.Rank.CompareTo(q.Rank));

                if (rDuelists.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cDuelists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Duelists", $"```{sb.ToString()}```");
                }

                if (rShadows.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cShadows.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Shadows", $"```{sb.ToString()}```");
                }

                if (rMarauders.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cMarauders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Marauders", $"```{sb.ToString()}```");
                }

                if (rWitchs.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cWitchs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Witches", $"```{sb.ToString()}```");
                }

                if (rRangers.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cRangers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Rangers", $"```{sb.ToString()}```");
                }

                if (rTemplars.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cTemplars.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Templars", $"```{sb.ToString()}```");
                }

                if (rScions.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in cScions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedClasses.AddField("Scions", $"```{sb.ToString()}```");
                }

                var embedAscendancy = Extras.Embed(Drawing.Aqua)
                    .WithTitle("Top 10 Characters of each Ascendancy")
                    .WithDescription("Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp();

                rSlayers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rGladiators.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rChampions.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rAssassins.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rSaboteurs.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rTricksters.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rJuggernauts.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rBerserkers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rChieftains.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rNecromancers.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rElementalists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rOccultists.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rDeadeyes.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rRaiders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rPathfinders.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rInquisitors.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rHierophants.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rGuardians.Sort((p, q) => p.Rank.CompareTo(q.Rank));
                rAscendants.Sort((p, q) => p.Rank.CompareTo(q.Rank));

                if (rSlayers.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rSlayers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Slayers", $"```{sb.ToString()}```");
                }

                if (rChampions.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rChampions.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Champions", $"```{sb.ToString()}```");
                }

                if (rGladiators.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rGladiators.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Gladiators", $"```{sb.ToString()}```");
                }

                if (rAssassins.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rAssassins.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Assassins", $"```{sb.ToString()}```");
                }

                if (rSaboteurs.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rSaboteurs.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Saboteurs", $"```{sb.ToString()}```");
                }

                if (rTricksters.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rTricksters.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Tricksters", $"```{sb.ToString()}```");
                }

                if (rJuggernauts.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rJuggernauts.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Juggernauts", $"```{sb.ToString()}```");
                }

                if (rBerserkers.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rBerserkers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Berserkers", $"```{sb.ToString()}```");
                }

                if (rChieftains.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rChieftains.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Chieftains", $"```{sb.ToString()}```");
                }

                if (rNecromancers.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rNecromancers.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancy.AddField("Necromancers", $"```{sb.ToString()}```");
                }

                var embedAscendancyCont = Extras.Embed(Drawing.Aqua)
                    .WithTitle("Top 10 Characters of each Ascendancy")
                    .WithDescription("Rank is overall and not by Ascendancy.")
                    .WithCurrentTimestamp();

                if (rElementalists.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rElementalists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Elemantalists", $"```{sb.ToString()}```");
                }

                if (rOccultists.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rOccultists.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Occultists", $"```{sb.ToString()}```");
                }

                if (rDeadeyes.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rDeadeyes.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Deadeyes", $"```{sb.ToString()}```");
                }

                if (rRaiders.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rRaiders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Raiders", $"```{sb.ToString()}```");
                }

                if (rPathfinders.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rPathfinders.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Pathfinders", $"```{sb.ToString()}```");
                }

                if (rInquisitors.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rInquisitors.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Inquisitors", $"```{sb.ToString()}```");
                }

                if (rHierophants.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rHierophants.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Hierophants", $"```{sb.ToString()}```");
                }

                if (rGuardians.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rGuardians.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Guardians", $"```{sb.ToString()}```");
                }

                if (rAscendants.Any())
                {
                    sb = new StringBuilder();
                    foreach (var racer in rAscendants.Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | E:{racer.Experience,10}{(racer.Dead ? " | X" : null)}");
                    embedAscendancyCont.AddField("Ascendants", $"```{sb.ToString()}```");
                }

                var Discordians = Extras.Embed(Drawing.Aqua)
                    .WithTitle($"Discordians Only {Leaderboard.Variant.Replace("_", " ")} Leaderboard")
                    .WithDescription($"Retrieved {racers.Where(r => r.Character.ToLower().Contains("discord")).Count().ToString("##,##0")} users with Discord in their name.")
                    .WithCurrentTimestamp()
                    .AddField("Top 10 Characters of each Class Ascendancy", "Rank is overall and not by Ascendancy.");

                if (cDuelists.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cDuelists.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Duelists, Slayers, Champions, Gladiators", $"```{sb.ToString()}```");
                }

                if (cShadows.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cShadows.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Shadows, Saboteurs, Assassins, Tricksters", $"```{sb.ToString()}```");
                }

                if (cMarauders.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cMarauders.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Marauders, Juggernauts, Chieftains, Berserkers", $"```{sb.ToString()}```");
                }

                if (cWitchs.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cWitchs.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Witches, Necromancers, Occultists, Elemantalists", $"```{sb.ToString()}```");
                }

                if (cRangers.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cRangers.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Rangers, Pathfinders, Raiders, Deadeyes", $"```{sb.ToString()}```");
                }

                if (cTemplars.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cTemplars.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Templars, Guardians, Inquisitors, Hierophants", $"```{sb.ToString()}```");
                }

                if (cScions.Any(r => r.Character.ToLower().Contains("discord")))
                {
                    sb = new StringBuilder();
                    foreach (var racer in cScions.Where(r => r.Character.ToLower().Contains("discord")).Take(10))
                        sb.AppendLine($"{racer.Character.PadRight(24)}R:{racer.Rank,5} | L:{racer.Level,3} | {racer.Class.ToString(),14}{(racer.Dead ? " | X" : null)}");
                    Discordians.AddField("Scions, Ascendants", $"```{sb.ToString()}```");
                }

                var Channel = Guild.GetTextChannel(Leaderboard.ChannelId);
                var Messages = Channel.GetMessagesAsync().FlattenAsync().GetAwaiter().GetResult();
                foreach (var Msg in Messages)
                    await Msg.DeleteAsync();

                await Channel.SendMessageAsync(embed: embed.Build());

                if (embedClasses.Fields.Any())
                    await Channel.SendMessageAsync(embed: embedClasses.Build());
                if (embedAscendancy.Fields.Any())
                    await Channel.SendMessageAsync(embed: embedAscendancy.Build());
                if (embedAscendancyCont.Fields.Any())
                    await Channel.SendMessageAsync(embed: embedAscendancyCont.Build());
                if (Discordians.Fields.Any())
                    await Channel.SendMessageAsync(embed: Discordians.Build());

                await Task.Delay(30000);
            }
        }
    }

    public class LeaderboardData
    {
        public int Rank { get; set; }
        public string Character { get; set; }
        public AscendancyClass Class { get; set; }
        public int Level { get; set; }
        public ulong Experience { get; set; }
        public bool Dead { get; set; }

        public LeaderboardData() { }

        public LeaderboardData(int Rank, string Character, AscendancyClass Class, int Level, ulong Experience, bool Dead)
        {
            this.Rank = Rank;
            this.Character = Character;
            this.Class = Class;
            this.Level = Level;
            this.Experience = Experience;
            this.Dead = Dead;
        }
    }

    public sealed class LeaderboardDataMap : ClassMap<LeaderboardData>
    {
        public LeaderboardDataMap()
        {
            AutoMap();
            Map(m => m.Dead)
                .TypeConverterOption.BooleanValues(true, true, "Dead")
                .TypeConverterOption.BooleanValues(false, true, "")
                .Default(false);
        }
    }

    public enum AscendancyClass
    {
        Slayer,
        Gladiator,
        Champion,
        Assassin,
        Saboteur,
        Trickster,
        Juggernaut,
        Berserker,
        Chieftain,
        Necromancer,
        Elementalist,
        Occultist,
        Deadeye,
        Raider,
        Pathfinder,
        Inquisitor,
        Hierophant,
        Guardian,
        Ascendant,
        Duelist,
        Shadow,
        Marauder,
        Witch,
        Ranger,
        Templar,
        Scion
    }
}
