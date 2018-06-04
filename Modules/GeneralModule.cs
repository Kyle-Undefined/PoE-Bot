namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using PoE.Bot.Handlers.Objects;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;

    [Name("General Commands"), Ratelimit]
    public class GeneralModule : Base
    {
        IServiceProvider Provider { get; }
        public CommandService CommandService { get; set; }

        [Command("Ping"), Remarks("Replies back with a pong?"), Summary("Ping")]
        public async Task PingAsync() => await ReplyAsync(string.Empty, Extras.Embed(Drawing.Aqua)
            .WithTitle("Beep Boop, Boop Beep!")
            .AddField("Gateway", $"{(Context.Client as DiscordSocketClient).Latency} ms").Build());

        [Command("Report"), Remarks("Reports a user to guild moderators."), Summary("Report <@User> <Reason>")]
        public async Task ReportAsync(IUser user, [Remainder] string Reason)
        {
            await Context.Message.DeleteAsync();

            var rep = await Context.Guild.GetTextChannelAsync(Context.Server.RepLog);
            var Embed = Extras.Embed(Drawing.Goldenrod)
                .WithAuthor(Context.User.Username, Context.User.GetAvatarUrl())
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithTitle($"Report for {user.Username}")
                .WithDescription($"**Reason:**\n{Reason}")
                .Build();

            await rep.SendMessageAsync(embed: Embed);
        }

        [Command("Feedback", RunMode = RunMode.Async), Remarks("Give feedback on my performance or suggest new features!"), Summary("Feedback")]
        public async Task FeedbackAsync()
        {
            var Wait = await WaitAsync("Please provide your feedback in couple sentences. (c to cancel)", Timeout: TimeSpan.FromMinutes(1));
            var Message = Context.GuildHelper.CalculateResponse(Wait);
            if (!Message.Item1) { await ReplyAsync(Message.Item2); return; }
            var Channel = (Context.Client as DiscordSocketClient).GetChannel(Context.Config.ReportChannel) as IMessageChannel;
            await Channel.SendMessageAsync(Message.Item2);
            await ReplyAsync($"Thank you for submitting your feedback.");
        }

        [Command("AFK"), Remarks("Adds Or Removes you from AFK list. Actions: Add/Remove/Modify"), Summary("AFK <Action: a, r, m> <AFKMessage>")]
        public Task AFKAsync(char Action = 'a', [Remainder] string AFKMessage = "Hey I'm AFK. Leave a DM?")
        {
            switch (Action)
            {
                case 'a':
                    if (Context.Server.AFK.ContainsKey(Context.User.Id)) return ReplyAsync("Whoops, it seems you are already AFK.");
                    Context.Server.AFK.Add(Context.User.Id, AFKMessage);
                    return ReplyAsync("Users will be notified that you are AFK when you are mentioned.", Save: 's');
                case 'r':
                    if (!Context.Server.AFK.ContainsKey(Context.User.Id)) return ReplyAsync("Whoops, it seems you are not AFK.");
                    Context.Server.AFK.Remove(Context.User.Id);
                    return ReplyAsync("You've been removed from AFK.", Save: 's');
                case 'm':
                    if (!Context.Server.AFK.ContainsKey(Context.User.Id)) return ReplyAsync("Whoops, it seems you are not AFK.");
                    Context.Server.AFK[Context.User.Id] = AFKMessage;
                    return ReplyAsync("Your AFK messages has been modified.", Save: 's');
                default: return ReplyAsync($"{Extras.Cross} Invalid Action! Possible Actions:\n`a` Add To AFK\n`r` Remove From AFK\n`m` Modify AFK Message");
            }
        }

        [Command("GuildInfo"), Remarks("Displays information about guild."), Summary("GuildInfo")]
        public Task GuildInfoAsync()
        {
            var Guild = Context.Guild as SocketGuild;
            return ReplyAsync(string.Empty, Extras.Embed(Drawing.Aqua)
                .WithAuthor($"{Context.Guild.Name}'s Information | {Context.Guild.Id}", Context.Guild.IconUrl)
                .WithFooter($"Created On: {Guild.CreatedAt}")
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .AddField("Owner", Guild.Owner, true)
                .AddField("Default Channel", Guild.DefaultChannel.Name ?? "No Default Channel", true)
                .AddField("Message Notifications", Guild.DefaultMessageNotifications, true)
                .AddField("Verification Level", Guild.VerificationLevel, true)
                .AddField("MFA Level", Guild.MfaLevel, true)
                .AddField("Text Channels", Guild.TextChannels.Count, true)
                .AddField("Voice Channels", Guild.VoiceChannels.Count, true)
                .AddField("Bot Members", Guild.Users.Count(x => x.IsBot == true), true)
                .AddField("Human Members", Guild.Users.Count(x => x.IsBot == false), true)
                .AddField("Total Members", Guild.MemberCount, true)
                .AddField("Roles", string.Join(", ", Guild.Roles.OrderByDescending(x => x.Position).Select(x => x.Name))).Build());
        }

        [Command("RoleInfo"), Remarks("Displays information about a role."), Summary("RoleInfo <@Role>")]
        public Task RoleInfoAsync(IRole Role)
            => ReplyAsync(string.Empty, Extras.Embed(Drawing.Aqua)
                .WithTitle($"{Role.Name} Information")
                .WithFooter($"Created On: {Role.CreatedAt}")
                .AddField("ID", Role.Id, true)
                .AddField("Color", Role.Color, true)
                .AddField("Role Position", Role.Position, true)
                .AddField("Shows Separately?", Role.IsHoisted ? "Yep" : "Nope", true)
                .AddField("Managed By Discord?", Role.IsManaged ? "Yep" : "Nope", true)
                .AddField("Can Mention?", Role.IsMentionable ? "Yep" : "Nope", true)
                .AddField("Permissions", string.Join(", ", Role.Permissions)).Build());

        [Command("UserInfo"), Remarks("Displays information about a user."), Summary("UserInfo [@User]")]
        public Task UserInfoAsync(SocketGuildUser User = null)
        {
            User = User ?? Context.User as SocketGuildUser;
            return ReplyAsync(string.Empty, Extras.Embed(Drawing.Aqua)
                .WithAuthor($"{User.Username} Information | {User.Id}", User.GetAvatarUrl())
                .WithThumbnailUrl(User.GetAvatarUrl()).
                AddField("Muted?", User.IsMuted ? "Yep" : "Nope", true)
                .AddField("Is Bot?", User.IsBot ? "Yep" : "Nope", true)
                .AddField("Creation Date", User.CreatedAt, true)
                .AddField("Join Date", User.JoinedAt, true)
                .AddField("Status", User.Status, true)
                .AddField("Permissions", string.Join(", ", User.GuildPermissions.ToList()), true)
                .AddField("Roles", string.Join(", ", (User as SocketGuildUser).Roles.OrderBy(x => x.Position).Select(x => x.Name)), true).Build());
        }

        [Command("Case"), Remarks("Shows information about a specific case."), Summary("Case [CaseNumber]")]
        public Task CaseAsync(int CaseNumber = 0)
        {
            if (CaseNumber == 0 && Context.Server.UserCases.Any()) CaseNumber = Context.Server.UserCases.LastOrDefault().Number;
            var Case = Context.Server.UserCases.FirstOrDefault(x => x.Number == CaseNumber);
            if (Case == null) return ReplyAsync($"Case #{CaseNumber} doesn't exist.");
            return ReplyAsync(string.Empty, Extras.Embed(Drawing.Aqua)
                .AddField("User", $"{Case.Username} ({Case.UserId})", true)
                .AddField("Case Type", Case.CaseType, true)
                .AddField("Responsible Moderator", $"{Case.Moderator} ({Case.ModeratorId})", true)
                .AddField("Reason", Case.Reason).Build());
        }

        [Command("Stats", RunMode = RunMode.Async), Alias("About", "Info"), Remarks("Displays information about PoE Bot and its stats."), Summary("Stats")]
        public async Task StatsAsync()
        {
            var Client = Context.Client as DiscordSocketClient;
            var Servers = Context.DBHandler.Servers();
            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor($"{Context.Client.CurrentUser.Username} Statistics 🤖", Context.Client.CurrentUser.GetAvatarUrl())
                .WithDescription((await Client.GetApplicationInfoAsync()).Description)
                .AddField("Channels",
                $"Text: {Client.Guilds.Sum(x => x.TextChannels.Count)}\n" +
                $"Voice: {Client.Guilds.Sum(x => x.VoiceChannels.Count)}\n" +
                $"Total: {Client.Guilds.Sum(x => x.Channels.Count)}", true)
                .AddField("Members",
                $"Bot: {Client.Guilds.Sum(x => x.Users.Where(z => z.IsBot == true).Count())}\n" +
                $"Human: { Client.Guilds.Sum(x => x.Users.Where(z => z.IsBot == false).Count())}\n" +
                $"Total: {Client.Guilds.Sum(x => x.Users.Count)}", true)
                .AddField("Database",
                $"Tags: {Servers.Sum(x => x.Tags.Count)}\n" +
                $"Currencies: {Servers.Sum(x => x.Prices.Count)}\n" +
                $"Shop Items: {Servers.Sum(x => x.Shops.Count)}", true);
            Embed.AddField("Mixer", $"Streams: {Context.Server.MixerStreams.Count}", true);
            Embed.AddField("Twitch", $"Streams: {Context.Server.TwitchStreams.Count}", true);
            Embed.AddField("Leaderboard", $"Variants: {Context.Server.Leaderboards.Count}", true);
            Embed.AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}", true);
            Embed.AddField("Memory", $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true);
            Embed.AddField("Programmer", $"[{(await Context.Client.GetApplicationInfoAsync()).Owner}](https://discord.me/poe_xbox)", true);
            await ReplyAsync(Embed: Embed.Build());
        }

        [Command("Remind"), Remarks("Set a reminder for later."), Summary("Remind <Time: Number(d/h/m/s) Example: 5h for 5 Hours> <Message>")]
        public async Task RemindAsync(TimeSpan Time, [Remainder] string Message)
        {
            Context.Server.Reminders.TryAdd(Context.User.Id, new RemindObject
            {
                Message = Message,
                TextChannel = Context.Channel.Id,
                RequestedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.Add(Time)
            });
            await ReplyAsync($"Alright {Context.User.Mention}, I'll remind you in {StringHelper.FormatTimeSpan(Time)}.", Save: 's');
        }

        [Command("PoB"), Remarks("Parses the PasteBin export from Path of Building and shows the information about the build."), Summary("PoB <PasteBinURL>")]
        public async Task PoBAsync([Remainder] string PasteBinURL)
        {
            Parser _parser = new Parser();
            PasteBinFetcher _pastebinFetcher = new PasteBinFetcher();

            try
            {
                var base64 = await _pastebinFetcher.GetRawCode(PasteBinURL);
                var character = _parser.ParseCode(base64);
                var embed = Extras.Embed(Drawing.Aqua)
                    .AddField(PathOfBuildingHelper.GenerateDefenseField(character))
                    .AddField(PathOfBuildingHelper.GenerateOffenseField(character))
                    .WithFooter($"Pastebin: {PasteBinURL}")
                    .WithTitle($"{(PathOfBuildingHelper.IsSupport(character) ? "Support" : character.Skills.MainSkillGroup.Gems.Where(e => e.Enabled).Select(e => e.Name).First())} - {(character.Ascendancy == string.Empty ? character.Class : character.Ascendancy)} (Lvl: {character.Level})")
                    .WithThumbnailUrl($"https://raw.githubusercontent.com/Kyle-Undefined/PoE.Bot/master/Path-Of-Building/Images/Classes/{(character.Ascendancy == string.Empty ? character.Class : character.Ascendancy)}.png")
                    .WithAuthor(Context.User)
                    .WithCurrentTimestamp();

                if (PathOfBuildingHelper.ShowCharges(character.Config) && (character.Summary.EnduranceCharges > 0 || character.Summary.PowerCharges > 0 || character.Summary.FrenzyCharges > 0))
                    embed.AddField(PathOfBuildingHelper.GenerateChargesField(character));

                embed.AddField(PathOfBuildingHelper.GenerateSkillsField(character));

                if (character.Config.Contains("Input"))
                    embed.AddField(PathOfBuildingHelper.GenerateConfigField(character));

                embed.AddField("**Info**:", $"[Tree]({PathOfBuildingHelper.GenerateTreeURL(character.Tree)}) - powered by [Path of Building](https://github.com/Openarl/PathOfBuilding) - Help from [Faust](https://github.com/FWidm/discord-pob) and [thezensei](https://github.com/andreandersen/LiftDiscord/).");

                await ReplyAsync(string.Empty, embed.Build());
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error fetching build data. Reason: {ex.Message}");
            }
        }

        [Command("Wiki"), Remarks("Searches an item on the PoE Wiki."), Summary("Wiki <Item>")]
        public async Task WikiAsync([Remainder] string Item)
            => await ReplyAsync(string.Empty, await WikiHelper.WikiGetItemAsync(Item));

        [Command("TrialAdd"), Summary("TrialAdd <Trial: Any part of the Trial Name or All for all Trials>"), RequireChannel("lab-and-trials"), Remarks("Add a Trial of Ascendancy that you're looking for to be notified when someone has found it.")]
        public Task TrialAddAsync([Remainder] string Trial)
        {
            if (Trial.ToLower() == "all") { (Context.User as SocketGuildUser).AddRolesAsync(Context.Guild.Roles.Where(r => r.Name.Contains("Trial of"))); }
            else (Context.User as SocketGuildUser).AddRoleAsync(Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault());
            return ReplyAsync($"Trial{(Trial.ToLower() == "all" ? "s were" : " was")} added to your list {Extras.OkHand}");
        }

        [Command("TrialDelete"), Summary("TrialDelete <Trial: Any part of the Trial Name or All for all Trials>"), RequireChannel("lab-and-trials"), Remarks("Delete a Trial of Ascendancy that you have completed.")]
        public Task TrialDeleteAsync([Remainder] string Trial)
        {
            if (Trial.ToLower() == "all") { (Context.User as SocketGuildUser).RemoveRolesAsync(Context.Guild.Roles.Where(r => r.Name.Contains("Trial of"))); }
            else (Context.User as SocketGuildUser).RemoveRoleAsync(Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault());
            return ReplyAsync($"Trial{(Trial.ToLower() == "all" ? "s were" : " was")} removed to your list {Extras.OkHand}");
        }

        [Command("Trial"), Summary("Trial <Trial: Any part of the Trial Name>"), RequireChannel("lab-and-trials"), Remarks("Announce a Trial of Ascendancy that you have come across.")]
        public Task TrialAsync([Remainder] string Trial)
            => ReplyAsync($"{Context.User.Mention} has found the {Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault().Mention}");

        [Command("Price"), Summary("Price <Name: Any Alias> [League: Defaults to Challenge | Standard, Hardcore, Challenge, ChallengeHC]"), Remarks("Pulls the price for the requested currency, in the chosen league, all values based on Chaos."), BanChannel("price-checkers")]
        public Task PriceAsync(string Name, Leagues League = Leagues.Challenge)
        {
            if (!Context.Server.Prices.Where(p => p.Alias.Contains(Name.ToLower()) && p.League == League).Any()) return ReplyAsync($"`{Name}` is not in the `{League}` list {Extras.Cross}");
            var Price = Context.Server.Prices.FirstOrDefault(p => p.Alias.Contains(Name.ToLower()) && p.League == League);
            var User = (Context.Guild.GetUserAsync(Price.UserId).GetAwaiter().GetResult() as IUser);
            var Embed = Extras.Embed(Drawing.Aqua)
                .AddField($"{Price.Name.Replace("_", " ")} in {League} league", $"```{Price.Alias}```")
                .AddField("Ratio", $"```{Price.Quantity}:{Price.Price}c```")
                .AddField("Last Updated", $"```{Price.LastUpdated}```")
                .AddField("Updated By", User.Mention)
                .WithFooter("Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.")
                .Build();

            return ReplyAsync(embed: Embed);
        }

        [Command("PriceList"), Summary("PriceList [League Defaults to Challenge | Standard, Hardcore, Challenge, ChallengeHC]"), Remarks("Pulls the price for the all currency, in the specified league, defaults to Challenge")]
        public Task PriceListAsync(Leagues League = Leagues.Challenge)
        {
            var Prices = Context.Server.Prices.Where(x => x.League == League).Select(x => 
                $"**{x.Name.Replace("_", " ")}**\n" +
                $"*{x.Alias}*\n" +
                $"Ratio: {x.Quantity}:{x.Price}c\n" +
                $"Last Updated: {x.LastUpdated}\n" +
                $"Updated By: { (Context.Guild.GetUserAsync(x.UserId).GetAwaiter().GetResult() as IUser).Mention}\n");
            return PagedReplyAsync(Context.GuildHelper.Pages(Prices), $"{League} Price List");
        }
    }
}
