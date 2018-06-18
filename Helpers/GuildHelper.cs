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

    public class GuildHelper
    {
        public bool ProfanityMatch(string Message, IList<string> ProfanityList)
            => DoesStringHaveProfanity(Message, ProfanityList);

        public Regex CheckMatch(string Pattern = null)
            => new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

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
            {
                var UserCases = Server.UserCases.Where(x => x.UserId == User.Id);
                var Embed = Extras.Embed(Extras.Case)
                    .WithAuthor($"Case Number: {Server.UserCases.Count + 1}")
                    .WithTitle(CaseType.ToString())
                    .AddField("User", $"{User.Mention} `{User}` ({User.Id})")
                    .AddField("History", $"Cases: {UserCases.Count()}\nWarnings: {UserCases.Where(x => x.CaseType == CaseType.WARNING).Count()}\n" +
                        $"Mutes: {UserCases.Where(x => x.CaseType == CaseType.MUTE).Count()}\nAuto Mutes: {UserCases.Where(x => x.CaseType == CaseType.AUTOMODMUTE).Count()}\n" +
                        $"Auto Perm Mutes: {UserCases.Where(x => x.CaseType == CaseType.AUTOMODPERMMUTE).Count()}")
                    .AddField("Reason", Reason)
                    .AddField("Moderator", $"{Mod}")
                    .WithCurrentTimestamp()
                    .Build();
                Message = await ModChannel.SendMessageAsync(embed: Embed);
            }
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
            DB.Save<GuildObject>(Server, Guild.Id);
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
                DB.Save<GuildObject>(Server, GuildId);
                return Server.Profiles[UserId];
            }
            return Server.Profiles[UserId];
        }

        public void SaveProfile(DatabaseHandler DB, ulong GuildId, ulong UserId, ProfileObject Profile)
        {
            var Server = DB.Execute<GuildObject>(Operation.LOAD, Id: GuildId);
            Server.Profiles[UserId] = Profile;
            DB.Save<GuildObject>(Server, GuildId);
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

        public bool DoesStringHaveProfanity(string Data, IList<string> BadWords)
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
                .Replace("[b]", "(?:(I3)|(l3)|(i3)|(13)|[bB])+")
                .Replace("[c]", "(?:[cC\\(]|[kK])+")
                .Replace("[d]", "[dD]+")
                .Replace("[e]", "[eE3]+")
                .Replace("[f]", "(?:[fF]|[pPhH])+")
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

        public async Task MuteUserAsync(IContext Context, MuteType MuteType, IGuildUser User, TimeSpan? Time, string Message, bool LogMute = true)
        {
            await Context.Message.DeleteAsync();

            if((Context.Server.MuteRole is 0 && MuteType == MuteType.MOD) || (Context.Server.TradeMuteRole is 0 && MuteType == MuteType.TRADE))
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} I'm baffled by this at the moment. *No Mute Role Configured*");
                return;
            }
            if (User.RoleIds.Contains(Context.Server.MuteRole) || User.RoleIds.Contains(Context.Server.TradeMuteRole))
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{User}` is already muted.*");
                return;
            }
            if (Context.GuildHelper.HierarchyCheck(Context.Guild, User))
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} Oops, clumsy me! *`{User}` is higher than I.*");
                return;
            }
            switch (MuteType)
            {
                case MuteType.MOD:
                    await User.AddRoleAsync(Context.Guild.GetRole(Context.Server.MuteRole));
                    Context.Server.Muted.TryAdd(User.Id, DateTime.Now.Add((TimeSpan)Time));
                    Context.DBHandler.Save<GuildObject>(Context.Server, Context.Guild.Id);
                    break;
                case MuteType.TRADE:
                    await User.AddRoleAsync(Context.Guild.GetRole(Context.Server.TradeMuteRole));
                    Context.Server.Muted.TryAdd(User.Id, DateTime.Now.Add((TimeSpan)Time));
                    Context.DBHandler.Save<GuildObject>(Context.Server, Context.Guild.Id);
                    break;
            }
            if (LogMute)
                await LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.MUTE, $"{Message} ({StringHelper.FormatTimeSpan((TimeSpan)Time)})");
            await Context.Channel.SendMessageAsync($"Rest now, tormented soul. *`{User}` has been muted for {StringHelper.FormatTimeSpan((TimeSpan)Time)}* {Extras.OkHand}");

            var Embed = Extras.Embed(Extras.Info)
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

        public async Task MuteUserAsync(DatabaseHandler DB, SocketUserMessage Message, GuildObject Server, IGuildUser User, CaseType CaseType, TimeSpan Time, string Reason)
        {
            var Guild = (Message.Author as SocketGuildUser).Guild;

            await User.AddRoleAsync(Guild.GetRole(Server.MuteRole));
            Server.Muted.TryAdd(User.Id, DateTime.Now.Add(Time));
            DB.Save<GuildObject>(Server, Guild.Id);

            await LogAsync(DB, Guild, User, Guild.CurrentUser, CaseType, $"{Reason} ({StringHelper.FormatTimeSpan(Time)})");

            var Embed = Extras.Embed(Extras.Info)
                .WithAuthor(Guild.CurrentUser)
                .WithTitle("Mod Action")
                .WithDescription($"You were muted in the {Guild.Name} server.")
                .WithThumbnailUrl(Guild.CurrentUser.GetAvatarUrl())
                .WithFooter($"You can PM any Moderator directly to resolve the issue.")
                .AddField("Reason", Reason)
                .AddField("Duration", StringHelper.FormatTimeSpan(Time))
                .Build();

            await (await User.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: Embed);
        }

        public async Task UnmuteUserAsync(IContext Context, IGuildUser User)
        {
            var MuteRole = Context.Guild.GetRole(Context.Server.MuteRole) ?? Context.Guild.Roles.FirstOrDefault(x => x.Name is "Muted");
            var TradeMuteRole = Context.Guild.GetRole(Context.Server.MuteRole) ?? Context.Guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute");
            if (!User.RoleIds.Contains(MuteRole.Id) && !User.RoleIds.Contains(TradeMuteRole.Id))
            {
                await Context.Channel.SendMessageAsync($"{Extras.Cross} I'm no fool, but this one's got me beat. *`{User}` doesn't have any mute role.*");
                return;
            }
            if (User.RoleIds.Contains(MuteRole.Id))
                await User.RemoveRoleAsync(MuteRole);
            else if (User.RoleIds.Contains(TradeMuteRole.Id))
                await User.RemoveRoleAsync(TradeMuteRole);
            if (Context.Server.Muted.ContainsKey(User.Id))
                Context.Server.Muted.TryRemove(User.Id, out _);
            Context.DBHandler.Save<GuildObject>(Context.Server, Context.Guild.Id);
            await Context.Channel.SendMessageAsync($"It seems there's still glory in the old Empire yet! *`{User}` has been unmuted.* {Extras.OkHand}");
        }

        public static async Task UnmuteUserAsync(ulong UserId, SocketGuild Guild, GuildObject Server)
        {
            var User = Guild.GetUser(UserId);
            var MuteRole = Guild.GetRole(Server.MuteRole) ?? Guild.Roles.FirstOrDefault(x => x.Name is "Muted");
            var TradeMuteRole = Guild.GetRole(Server.TradeMuteRole) ?? Guild.Roles.FirstOrDefault(x => x.Name is "Trade Mute");
            if (!User.Roles.Contains(MuteRole) && !User.Roles.Contains(TradeMuteRole))
                return;
            if (User.Roles.Contains(MuteRole))
                await User.RemoveRoleAsync(MuteRole);
            else if (User.Roles.Contains(TradeMuteRole))
                await User.RemoveRoleAsync(TradeMuteRole);
        }

        public async Task WarnUserAsync(IContext Context, IGuildUser User, string Reason, MuteType MuteType = MuteType.MOD)
        {
            if (Context.Server.MaxWarningsToMute is 0 || Context.Server.MaxWarningsToPermMute is 0 || User.Id == Context.Guild.OwnerId ||
                User.GuildPermissions.Administrator || User.GuildPermissions.ManageGuild || User.GuildPermissions.ManageChannels ||
                User.GuildPermissions.ManageRoles || User.GuildPermissions.BanMembers || User.GuildPermissions.KickMembers)
                return;
            var Profile = GetProfile(Context.DBHandler, Context.Guild.Id, User.Id);
            Profile.Warnings++;
            if (Profile.Warnings >= Context.Server.MaxWarningsToPermMute)
            {
                DateTime Now = DateTime.Now;
                TimeSpan Span = Now.AddYears(999) - Now;
                await MuteUserAsync(Context, MuteType, User, Span, $"Muted permanently for reaching Max number of warnings. {Reason}", false);
                await LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.AUTOMODPERMMUTE, $"Muted permanently due to reaching max number of warnings. {Reason}");
            }
            else if (Profile.Warnings >= Context.Server.MaxWarningsToMute)
            {
                await MuteUserAsync(Context, MuteType, User, TimeSpan.FromDays(1), $"Muted for 1 day due to reaching Max number of warnings. {Reason}", false);
                await LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.AUTOMODMUTE, $"Muted for 1 day due to reaching max number of warnings. {Reason}");
            }
            else
            {
                await LogAsync(Context.DBHandler, Context.Guild, User, Context.User, CaseType.WARNING, Reason);
                await Context.Channel.SendMessageAsync($"Purity will prevail! *`{User}` has been warned.* {Extras.Warning}");
            }
            SaveProfile(Context.DBHandler, Context.Guild.Id, User.Id, Profile);
        }

        public async Task WarnUserAsync(SocketUserMessage Message, GuildObject Server, DatabaseHandler DB, string Warning)
        {
            var Guild = (Message.Author as SocketGuildUser).Guild;
            var User = Message.Author as SocketGuildUser;
            if (Server.MaxWarningsToMute is 0 || Server.MaxWarningsToPermMute is 0 || User.Id == Guild.OwnerId ||
                User.GuildPermissions.Administrator || User.GuildPermissions.ManageGuild || User.GuildPermissions.ManageChannels ||
                User.GuildPermissions.ManageRoles || User.GuildPermissions.BanMembers || User.GuildPermissions.KickMembers)
                return;
            await Message.DeleteAsync();
            var Profile = GetProfile(DB, Guild.Id, User.Id);
            Profile.Warnings++;
            if (Profile.Warnings >= Server.MaxWarningsToPermMute)
            {
                DateTime Now = DateTime.Now;
                TimeSpan Span = Now.AddYears(999) - Now;
                await MuteUserAsync(DB, Message, Server, User, CaseType.AUTOMODPERMMUTE, Span, $"Muted by AutoMod. {Warning} For saying: `{Message.Content}`");
            }
            else if (Profile.Warnings >= Server.MaxWarningsToMute)
                await MuteUserAsync(DB, Message, Server, User, CaseType.AUTOMODMUTE, TimeSpan.FromDays(1), $"Muted by AutoMod. {Warning} For saying: `{Message.Content}`");
            else
                await LogAsync(DB, Guild, User, Guild.CurrentUser, CaseType.WARNING, $"{Warning} For saying: `{Message.Content}`");
            SaveProfile(DB, Guild.Id, User.Id, Profile);
            await Message.Channel.SendMessageAsync(Warning);
        }
    }
}
