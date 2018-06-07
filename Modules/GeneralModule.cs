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
    using System.Collections.Generic;
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
            var Message = Context.GuildHelper.CalculateResponse(await WaitAsync("Please provide your feedback in a couple sentences.", Timeout: TimeSpan.FromMinutes(1)));
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

        [Command("About", RunMode = RunMode.Async), Remarks("Displays information about the Server stats, and PoE Bot stats."), Summary("About")]
        public async Task AboutAsync()
        {
            var Client = Context.Client as DiscordSocketClient;
            var TextChannels = await Context.Guild.GetTextChannelsAsync();
            var VoiceChannels = await Context.Guild.GetVoiceChannelsAsync();
            var Users = await Context.Guild.GetUsersAsync();
            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor($"{Context.Client.CurrentUser.Username} Statistics 🤖", Context.Client.CurrentUser.GetAvatarUrl())
                .WithDescription((await Client.GetApplicationInfoAsync()).Description)
                .AddField("Server Info", $"This server was created on {Context.Guild.CreatedAt.Date.ToLongDateString()} @ {Context.Guild.CreatedAt.Date.ToLongTimeString()}")
                .AddField("Discord Owner", (await Context.Guild.GetUserAsync(Context.Guild.OwnerId)).Mention, true)
                .AddField("Voice Region", Context.Guild.VoiceRegionId, true)
                .AddField("Verification Level", Context.Guild.VerificationLevel.ToString(), true)
                .AddField($"Channels [{TextChannels.Count + VoiceChannels.Count}]",
                    $"Text: {TextChannels.Count}\n" +
                    $"Voice: {VoiceChannels.Count}\n", true)
                .AddField($"Members [{Users.Count()}]",
                    $"Human: {Users.Count(x => x.IsBot == false)}\n" +
                    $"Bot: {Users.Count(x => x.IsBot == true)}\n", true)
                .AddField($"Roles [{Context.Guild.Roles.Count}]",
                    $"Separated: {Context.Guild.Roles.Count(x => x.IsHoisted == true)}\n" +
                    $"Mentionable: {Context.Guild.Roles.Count(x => x.IsMentionable == true)}", true)
                .AddField("Server Stats", "This server has the following data logged:")
                .AddField("Items",
                    $"Tags: {Context.Server.Tags.Count}\n" +
                    $"Currencies: {Context.Server.Prices.Count}\n" +
                    $"Shop Items: {Context.Server.Shops.Count}", true)
                .AddField("Streams", 
                    $"Mixer: {Context.Server.MixerStreams.Count}\n" +
                    $"Twitch: {Context.Server.TwitchStreams.Count}", true)
                .AddField("Leaderboard", $"Variants: {Context.Server.Leaderboards.Count(x => x.Enabled == true)}", true)
                .AddField("Bot Info", "Info about myself:")
                .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}", true)
                .AddField("Memory", $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true)
                .AddField("Programmer", $"[@{(await Context.Client.GetApplicationInfoAsync()).Owner}](https://discord.me/poe_xbox)", true)
                .Build();
            await ReplyAsync(Embed: Embed);
        }

        [Command("Remind"), Remarks("Set a reminder for later."), Summary("Remind <Time: Number(d/h/m/s) Example: 5h for 5 Hours> <Message>")]
        public async Task RemindAsync(TimeSpan Time, [Remainder] string Message)
        {
            var Reminders = new List<RemindObject>();
            if (Context.Server.Reminders.ContainsKey(Context.User.Id))
                Context.Server.Reminders.TryGetValue(Context.User.Id, out Reminders);
            Reminders.Add(new RemindObject{
                Message = Message,
                TextChannel = Context.Channel.Id,
                RequestedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.Add(Time)
            });
            Context.Server.Reminders.AddOrUpdate(Context.User.Id, Reminders, (key, value) => value = Reminders);
            await ReplyAsync($"Alright {Context.User.Mention}, I'll remind you in {StringHelper.FormatTimeSpan(Time)}.", Save: 's');
        }

        [Command("Reminders"), Remarks("Shows all of your reminders."), Summary("Reminders")]
        public Task RemindersAsync()
        {
            if (!Context.Server.Reminders.Any(x => x.Key == Context.User.Id))
                return ReplyAsync($"Uhm, you don't have reminders {Extras.Cross}");
            var Reminder = Context.Server.Reminders.First(x => x.Key == Context.User.Id);
            var Reminders = new List<string>();
            for (int i = 0; i < Reminder.Value.Count; i++)
                Reminders.Add($"Reminder #**{i}** | Expires on: **{Reminder.Value[i].ExpiryDate.ToUniversalTime()}**\n**Message:** {Reminder.Value[i].Message}");
            return PagedReplyAsync(Reminders, "Your Current Reminders");
        }

        [Command("Reminder Remove"), Remarks("Removes a reminder."), Summary("Reminder Remove <Number>")]
        public Task ReminderRemove(int Number)
        {
            if (!Context.Server.Reminders.Any(x => x.Key == Context.User.Id))
                return ReplyAsync($"Uhm, you don't have reminders {Extras.Cross}");
            var Reminders = new List<RemindObject>();
            Context.Server.Reminders.TryGetValue(Context.User.Id, out Reminders);
            try { Reminders.RemoveAt(Number); } catch { return ReplyAsync($"{Extras.Cross} Invalid reminder number was provided."); }
            if (Reminders.Any()) Context.Server.Reminders.TryRemove(Context.User.Id, out _);
            else
                Context.Server.Reminders.TryUpdate(Context.User.Id, Reminders, Context.Server.Reminders.FirstOrDefault(x => x.Key == Context.User.Id).Value);
            return ReplyAsync($"Reminder #{Number} deleted 🗑️", Save: 's');
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

        [Command("Trials Add"), Summary("Trials Add <Trial: Any part of the Trial Name or All for all Trials>"), RequireChannel("lab-and-trials"), Remarks("Add a Trial of Ascendancy that you're looking for to be notified when someone has found it.")]
        public Task TrialAddAsync([Remainder] string Trial)
        {
            if (Trial.ToLower() == "all") { (Context.User as SocketGuildUser).AddRolesAsync(Context.Guild.Roles.Where(r => r.Name.Contains("Trial of"))); }
            else (Context.User as SocketGuildUser).AddRoleAsync(Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault());
            return ReplyAsync($"Trial{(Trial.ToLower() == "all" ? "s were" : " was")} added to your list {Extras.OkHand}");
        }

        [Command("Trials Delete"), Summary("Trials Delete <Trial: Any part of the Trial Name or All for all Trials>"), RequireChannel("lab-and-trials"), Remarks("Delete a Trial of Ascendancy that you have completed.")]
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
