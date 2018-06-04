namespace PoE.Bot.Helpers
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Handlers;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Handlers.Objects;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class GuildHelper
    {
        string[] ProfanityList { get => new string[] { "bellend", "belend", "bellends", "belends", "bollocks", "bollucks", "bullocks", "bulucks", "bolocks", "bolucks", "bulocks", "bulucks", "buluck", "bolock", "boluck", "bulock", "buluck", "cocksuckers", "cocksucker", "cockmunchers", "cockmuncher", "cockface", "cockhead", "coon", "cunts", "cunt", "cuntwhit", "cuntswhit", "cuntwit", "cuntswit", "doushes", "douches", "doushe", "douche", "dooshes", "dooshe", "doosh", "dykes", "dyke", "dikes", "dike", "faggots", "phaggots", "fagots", "phagots", "faggot", "phaggot", "fagot", "phagot", "faggity", "fagging", "phagity", "fagget", "faggit", "faggat", "faggets", "faggits", "faggats", "faget", "fagit", "fagat", "fagets", "fagits", "fagats", "phagget", "phaggit", "phaggat", "phaggets", "phaggits", "phaggats", "phaget", "phagit", "phagat", "phagets", "phagits", "phagats", "fuckwhits", "fuckwhit", "fuckwits", "fuckwit", "knobends", "knobend", "knobheads", "knobhead", "niggers", "nigger", "niggahs", "niggah", "nigga", "niggas", "niggaz", "nigers", "niger", "nigahs", "nigah", "niga", "nigas", "nigaz", "niggggers", "nigggger", "niggggahs", "niggggah", "nigggga", "niggggas", "niggggaz", "niggers", "nigger", "niggahs", "niggah", "nigga", "niggas", "niggaz", "niggggggggers", "nigggggggger", "niggggggggahs", "niggggggggah", "nigggggggga", "niggggggggas", "niggggggggaz", "niggggers", "nigggger", "niggggahs", "niggggah", "nigggga", "niggggas", "niggggaz", "retards", "retard", "retarded", "retarts", "retarted", "retart", "shitheads", "shithead", "shitfucks", "shitfuck", "twats", "twat", "twatheads", "twathead", "tossers", "tosser", "wankers", "wanker" }; }
        string InviteRegex { get => @"^(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?(d+i+s+c+o+r+d+|a+p+p)+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$"; }
        public bool InviteMatch(string Message) => CheckMatch(InviteRegex).Match(Message).Value != string.Empty;
        public bool ProfanityMatch(string Message) => DoesStringHaveProfanity(Message, ProfanityList);
        public Regex CheckMatch(string Pattern = null) => new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        public IMessageChannel DefaultChannel(IGuild guild)
        {
            var Guild = guild as SocketGuild;
            var ValidNames = new[] { "general", "chat", "lobby", "discussion", "lobby" };
            return Guild.TextChannels.Where(
                x => Guild.CurrentUser.GetPermissions(x).SendMessages).FirstOrDefault(
                x => ValidNames.Contains(x.Name) || x.Id == Guild.Id) ?? Guild.DefaultChannel;
        }

        public IMessageChannel DefaultStreamChannel(IGuild guild)
        {
            var Guild = guild as SocketGuild;
            var ValidNames = new[] { "streams", "streamers", "live" };
            return Guild.TextChannels.Where(
                x => Guild.CurrentUser.GetPermissions(x).SendMessages).FirstOrDefault(
                x => ValidNames.Contains(x.Name)) ?? Guild.DefaultChannel;
        }

        public async Task LogAsync(DBHandler DB, IGuild Guild, IUser User, IUser Mod, CaseType CaseType, string Reason)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: Guild.Id);
            Reason = Reason ?? $"*Responsible moderator, please type `{Server.Prefix}Reason {Server.UserCases.Count + 1} <Reason>`*";
            var ModChannel = await Guild.GetTextChannelAsync(Server.ModLog);
            IUserMessage Message = null;
            if (ModChannel != null)
                Message = await ModChannel.SendMessageAsync($"**{CaseType}** | Case {Server.UserCases.Count + 1}\n**User:** {User} ({User.Id})\n**Reason:** {Reason}\n" +
                       $"**Responsible Moderator:** {Mod}");
            Server.UserCases.Add(new CaseObject
            {
                UserId = User.Id,
                Reason = Reason,
                CaseType = CaseType,
                Username = $"{User}",
                Moderator = $"{Mod}",
                ModeratorId = Mod.Id,
                CaseDate = DateTime.UtcNow,
                Number = Server.UserCases.Count + 1,
                MessageId = Message == null ? 0 : Message.Id
            });
            DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
        }

        public bool HierarchyCheck(IGuild IGuild, IGuildUser User)
        {
            var Guild = IGuild as SocketGuild;
            var HighestRole = Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).FirstOrDefault().Position;
            return (User as SocketGuildUser).Roles.Any(x => x.Position >= HighestRole);
        }

        public ProfileObject GetProfile(DBHandler DB, ulong GuildId, ulong UserId)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: GuildId);
            if (!Server.Profiles.ContainsKey(UserId))
            {
                Server.Profiles.Add(UserId, new ProfileObject());
                DB.Execute<GuildObject>(Operation.SAVE, Server, GuildId);
                return Server.Profiles[UserId];
            }
            return Server.Profiles[UserId];
        }

        public void SaveProfile(DBHandler DB, ulong GuildId, ulong UserId, ProfileObject Profile)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: GuildId);
            Server.Profiles[UserId] = Profile;
            DB.Execute<GuildObject>(Operation.SAVE, Server, GuildId);
        }

        public ulong ParseUlong(string Value)
        {
            if (string.IsNullOrWhiteSpace(Value)) return 0;
            return Convert.ToUInt64(string.Join("", CheckMatch("[0-9]").Matches(Value).Select(x => x.Value)));
        }

        public IList<string> Pages<T>(IEnumerable<T> Collection)
        {
            var BuildPages = new List<string>(Collection.Count());
            for (int i = 0; i <= Collection.Count(); i += 10) BuildPages.Add(string.Join("\n", Collection.Skip(i).Take(10)));
            return BuildPages;
        }

        public (bool, string) CalculateResponse(SocketMessage Message)
            => (Message == null || string.IsNullOrWhiteSpace(Message.Content)) ?
            (false, $"{Extras.Cross} Request cancelled. Either timed out or no response was provided.")
            : Message.Content.ToLower().Equals("c") ? (false, $"Got it {Extras.OkHand}") : (true, Message.Content);

        public bool DoesStringHaveProfanity(string data, string[] badWords)
        {
            foreach (var word in badWords)
            {
                var expword = ExpandBadWordToIncludeIntentionalMisspellings(word);
                Regex r = new Regex(expword, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                var match = r.Match(data);
                if (match.Success) return match.Success;
            }
            return false;
        }

        public string ExpandBadWordToIncludeIntentionalMisspellings(string word)
        {
            var chars = word
                .ToCharArray();

            var op = "[" + string.Join("][", chars) + "]";

            return op
                .Replace("[a]", "[aA@]")
                .Replace("[b]", "[bBI3l3i3]")
                .Replace("[c]", "(?:[cC\\(]|[kK])")
                .Replace("[d]", "[dD]")
                .Replace("[e]", "[eE3]")
                .Replace("[f]", "(?:[fF]|[phpHPhPH])")
                .Replace("[g]", "[gG6]")
                .Replace("[h]", "[hH]")
                .Replace("[i]", "[iIl!1]")
                .Replace("[j]", "[jJ]")
                .Replace("[k]", "(?:[cC\\(]|[kK])")
                .Replace("[l]", "[lL1!i]")
                .Replace("[m]", "[mM]")
                .Replace("[n]", "[nN]")
                .Replace("[o]", "[oO0]")
                .Replace("[p]", "[pP]")
                .Replace("[q]", "[qQ9]")
                .Replace("[r]", "[rR]")
                .Replace("[s]", "[sS$5]")
                .Replace("[t]", "[tT7]")
                .Replace("[u]", "[uUvV]")
                .Replace("[v]", "[vVuU]")
                .Replace("[w]", "[wWvvVV]")
                .Replace("[x]", "[xX]")
                .Replace("[y]", "[yY]")
                .Replace("[z]", "[zZ2]")
                ;
        }
    }
}
