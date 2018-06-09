namespace PoE.Bot.Helpers
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Handlers;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Drawing = System.Drawing.Color;

    public class GuildHelper
    {
        string[] ProfanityList { get => new string[] { "belend", "belends", "bulucks", "bolocks", "bolucks", "bulocks", "bulucks", "buluck", "bolock", "boluck", "bulock", "buluck", "cocksuckers", "cocksucker", "cockmunchers", "cockmuncher", "cockface", "cockhead", "coon", "coons", "cunts", "cunt", "cuntwhit", "cuntswhit", "cuntwit", "cuntswit", "doushes", "douches", "doushe", "douche", "dooshes", "dooshe", "doosh", "dykes", "dyke", "dikes", "dike", "fagots", "fagot", "fagity", "faging", "faget", "fagit", "fagat", "fagets", "fagits", "fagats", "fuckwhits", "fuckwhit", "fuckwits", "fuckwit", "knobends", "knobend", "knobheads", "knobhead", "nigers", "niger", "nigahs", "nigah", "niga", "nigas", "nigaz", "retards", "retard", "retarded", "retarts", "retarted", "retart", "shitheads", "shithead", "shitfucks", "shitfuck", "twats", "twat", "twatheads", "twathead", "tossers", "tosser", "wankers", "wanker" }; }
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

        public async Task LogAsync(DatabaseHandler DB, IGuild Guild, IUser User, IUser Mod, CaseType CaseType, string Reason)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: Guild.Id);
            Reason = string.IsNullOrWhiteSpace(Reason) ? $"*Exile, please type `{Server.Prefix}Reason {Server.UserCases.Count + 1} <Reason>`*" : Reason;
            var ModChannel = await Guild.GetTextChannelAsync(Server.ModLog);
            IUserMessage Message = null;
            if (!(ModChannel is null))
                Message = await ModChannel.SendMessageAsync($"**{CaseType}** | Case {Server.UserCases.Count + 1}\n**User:** {User} ({User.Id})\n**Reason:** {Reason}\n" +
                       $"**Moderator:** {Mod}");
            Server.UserCases.Add(new CaseObject
            {
                UserId = User.Id,
                Reason = Reason,
                CaseType = CaseType,
                Username = $"{User}",
                Moderator = $"{Mod}",
                ModeratorId = Mod.Id,
                CaseDate = DateTime.Now,
                Number = Server.UserCases.Count + 1,
                MessageId = Message is null ? 0 : Message.Id
            });
            DB.Execute<GuildObject>(Operation.SAVE, Server, Guild.Id);
        }

        public bool HierarchyCheck(IGuild IGuild, IGuildUser User)
        {
            var Guild = IGuild as SocketGuild;
            var HighestRole = Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).FirstOrDefault().Position;
            return (User as SocketGuildUser).Roles.Any(x => x.Position >= HighestRole);
        }

        public ProfileObject GetProfile(DatabaseHandler DB, ulong GuildId, ulong UserId)
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

        public void SaveProfile(DatabaseHandler DB, ulong GuildId, ulong UserId, ProfileObject Profile)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: GuildId);
            Server.Profiles[UserId] = Profile;
            DB.Execute<GuildObject>(Operation.SAVE, Server, GuildId);
        }

        public ulong ParseUlong(string Value)
        {
            if (string.IsNullOrWhiteSpace(Value))
                return 0;
            return Convert.ToUInt64(string.Join("", CheckMatch("[0-9]").Matches(Value).Select(x => x.Value)));
        }

        public IList<string> Pages<T>(IEnumerable<T> Collection)
        {
            var BuildPages = new List<string>(Collection.Count());
            for (int i = 0; i <= Collection.Count(); i += 10)
                BuildPages.Add(string.Join("\n", Collection.Skip(i).Take(10)));
            return BuildPages;
        }

        public (bool, string) CalculateResponse(SocketMessage Message)
            => (Message is null || string.IsNullOrWhiteSpace(Message.Content)) ?
            (false, $"{Extras.Cross} There is a fine line between consideration and hesitation. The former is wisdom, the latter is fear. *Request Timed Out*")
            : Message.Content.ToLower().Equals("c") ? (false, $"Understood, Exile {Extras.OkHand}") : (true, Message.Content);

        public bool DoesStringHaveProfanity(string Data, string[] BadWords)
        {
            foreach (var Word in BadWords)
            {
                var Expword = ExpandBadWordToIncludeIntentionalMisspellings(Word);
                Regex r = new Regex(Expword, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                var Match = r.Match(Data);
                if (Match.Success)
                    return Match.Success;
            }
            return false;
        }

        public string ExpandBadWordToIncludeIntentionalMisspellings(string Word)
        {
            var Chars = Word.ToCharArray();
            var op = @"(^|\s)[" + string.Join("][", Chars) + @"](\s|$)";

            return op
                .Replace("[a]", "[aA@]+")
                .Replace("[b]", "[bBI3l3i3]+")
                .Replace("[c]", "(?:[cC\\(]|[kK])+")
                .Replace("[d]", "[dD]+")
                .Replace("[e]", "[eE3]+")
                .Replace("[f]", "(?:[fF]|[phpHPhPH])+")
                .Replace("[g]", "[gG6]+")
                .Replace("[h]", "[hH]+")
                .Replace("[i]", "[iIl!1]+")
                .Replace("[j]", "[jJ]+")
                .Replace("[k]", "(?:[cC\\(]|[kK])+")
                .Replace("[l]", "[lL1!i]+")
                .Replace("[m]", "[mM]+")
                .Replace("[n]", "[nN]+")
                .Replace("[o]", "[oO0]+")
                .Replace("[p]", "[pP]+")
                .Replace("[q]", "[qQ9]+")
                .Replace("[r]", "[rR]+")
                .Replace("[s]", "[sS$5]+")
                .Replace("[t]", "[tT7]+")
                .Replace("[u]", "[uUvV]+")
                .Replace("[v]", "[vVuU]+")
                .Replace("[w]", "[wWvvVV]+")
                .Replace("[x]", "[xX]+")
                .Replace("[y]", "[yY]+")
                .Replace("[z]", "[zZ2]+")
                ;
        }

        public async Task MuteUserAsync(IContext Context, IGuildUser User, TimeSpan? Time, string Message)
        {
            await Context.Message.DeleteAsync();

            if(Context.Server.MuteRole is 0)
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} I'm baffled by this at the moment. *No Mute Role Configured*");
                return;
            }
            if (User.RoleIds.Contains(Context.Server.MuteRole))
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{User}` is already muted.*");
                return;
            }
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} Oops, clumsy me! *`{User}` is higher than I.*");
                return;
            }
            await User.AddRoleAsync(Context.Guild.GetRole(Context.Server.MuteRole));
            await LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.MUTE, $"{Message} ({StringHelper.FormatTimeSpan((TimeSpan)Time)})");
            await Context.Channel.SendMessageAsync($"Rest now, tormented soul. *`{User}` has been muted for {StringHelper.FormatTimeSpan((TimeSpan)Time)}* {Extras.OkHand}");

            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor(Context.User)
                .WithTitle("Mod Action")
                .WithDescription($"You were muted in the {Context.Guild.Name} server.")
                .WithThumbnailUrl(Context.User.GetAvatarUrl())
                .WithFooter($"You can PM {Context.User.Username} directly to resolve the issue.")
                .AddField("Reason", Message)
                .AddField("Duration", StringHelper.FormatTimeSpan((TimeSpan)Time))
                .Build();

            await (await User.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: Embed);
        }
    }
}
