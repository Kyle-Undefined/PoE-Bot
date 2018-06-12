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
    using PoE.Bot.Objects;
    using PoE.Bot.Addons.Preconditions;

    [Name("General Commands"), Ratelimit]
    public class GeneralModule : BotBase
    {
        IServiceProvider Provider { get; }
        public CommandService CommandService { get; set; }

        [Command("Ping"), Remarks("Replies back with a pong?"), Summary("Ping")]
        public async Task PingAsync() => await ReplyAsync(embed: Extras.Embed(Extras.Info)
            .WithTitle("Wisdom is the offspring of Suffering and Time.")
            .AddField("Gateway", $"{(Context.Client as DiscordSocketClient).Latency} ms").Build());

        [Command("Report"), Remarks("Reports a user to guild moderators."), Summary("Report <@User> <Reason>")]
        public async Task ReportAsync(IUser user, [Remainder] string Reason)
        {
            await Context.Message.DeleteAsync();

            var rep = await Context.Guild.GetTextChannelAsync(Context.Server.RepLog);
            var Embed = Extras.Embed(Extras.Report)
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
            var Message = Context.GuildHelper.CalculateResponse(await WaitAsync("Wisdom is the offspring of Suffering and Time. *Please provide your feedback in a couple sentences.*", Timeout: TimeSpan.FromMinutes(1)));
            if (!Message.Item1)
            {
                await ReplyAsync(Message.Item2);
                return;
            }
            var Channel = (Context.Client as DiscordSocketClient).GetChannel(Context.Config.ReportChannel) as IMessageChannel;
            await Channel.SendMessageAsync(Message.Item2);
            await ReplyAsync($"Behold the machinery at maximum efficiency! {Extras.OkHand}");
        }

        [Command("AFK"), Remarks("Adds Or Removes you from AFK list."), Summary("AFK <Action> <AFKMessage>")]
        public Task AFKAsync(char Action = 'a', [Remainder] string AFKMessage = "Running around Wraeclast, slaying monsters. Shoot me a DM.")
        {
            switch (Action)
            {
                case 'a':
                    if (Context.Server.AFK.ContainsKey(Context.User.Id))
                        return ReplyAsync($"{Extras.Cross} Exile, it seems you are already in Wraeclast.");
                    Context.Server.AFK.Add(Context.User.Id, AFKMessage);
                    return ReplyAsync($"Exiles will be notified that you are in Wraeclast when you are mentioned {Extras.OkHand}", Save: 's');
                case 'r':
                    if (!Context.Server.AFK.ContainsKey(Context.User.Id))
                        return ReplyAsync($"{Extras.Cross} Exile, it seems you are not in Wraeclast.");
                    Context.Server.AFK.Remove(Context.User.Id);
                    return ReplyAsync($"You've returned from Wraeclast {Extras.OkHand}", Save: 's');
                case 'm':
                    if (!Context.Server.AFK.ContainsKey(Context.User.Id))
                        return ReplyAsync($"{Extras.Cross} Exile, it seems you are not in Wraeclast.");
                    Context.Server.AFK[Context.User.Id] = AFKMessage;
                    return ReplyAsync($"Your Wraeclast messages has been modified {Extras.OkHand}", Save: 's');
                default:
                    return ReplyAsync($"{Extras.Cross} Invalid Action! Possible Actions:\n`a` Add To AFK\n`r` Remove From AFK\n`m` Modify AFK Message");
            }
        }

        [Command("About", RunMode = RunMode.Async), Remarks("Displays information about the Server stats, and PoE Bot stats."), Summary("About")]
        public async Task AboutAsync()
        {
            var Client = Context.Client as DiscordSocketClient;
            var TextChannels = await Context.Guild.GetTextChannelsAsync();
            var VoiceChannels = await Context.Guild.GetVoiceChannelsAsync();
            var Users = await Context.Guild.GetUsersAsync();
            var Embed = Extras.Embed(Extras.Info)
                .WithAuthor($"{Context.Client.CurrentUser.Username} Statistics 🤖", Context.Client.CurrentUser.GetAvatarUrl())
                .WithDescription((await Client.GetApplicationInfoAsync()).Description)
                .AddField("Wraeclast Info", $"Wraeclast was created on {Context.Guild.CreatedAt.DateTime.ToLongDateString()} @ {Context.Guild.CreatedAt.DateTime.ToLongTimeString()}")
                .AddField("Kitava", (await Context.Guild.GetUserAsync(Context.Guild.OwnerId)).Mention, true)
                .AddField("Map", Context.Guild.VoiceRegionId, true)
                .AddField("Mod", Context.Guild.VerificationLevel.ToString(), true)
                .AddField($"Chat [{TextChannels.Count + VoiceChannels.Count}]",
                    $"Text: {TextChannels.Count}\n" +
                    $"Voice: {VoiceChannels.Count}\n", true)
                .AddField($"Characters [{Users.Count()}]",
                    $"Exiles: {Users.Count(x => x.IsBot is false)}\n" +
                    $"Lieutenants: {Users.Count(x => x.IsBot is true)}\n", true)
                .AddField($"Roles [{Context.Guild.Roles.Count}]",
                    $"Separated: {Context.Guild.Roles.Count(x => x.IsHoisted is true)}\n" +
                    $"Mentionable: {Context.Guild.Roles.Count(x => x.IsMentionable is true)}", true)
                .AddField("Wraeclast Stats", "Wraeclast has the following data logged:")
                .AddField("Items",
                    $"Tags: {Context.Server.Tags.Count}\n" +
                    $"Currencies: {Context.Server.Prices.Count}\n" +
                    $"Shop Items: {Context.Server.Shops.Count}", true)
                .AddField("Streams", 
                    $"Mixer: {Context.Server.Streams.Count(s => s.StreamType is StreamType.MIXER)}\n" +
                    $"Twitch: {Context.Server.Streams.Count(s => s.StreamType is StreamType.TWITCH)}", true)
                .AddField("Leaderboard", $"Variants: {Context.Server.Leaderboards.Count(x => x.Enabled is true)}", true)
                .AddField("Lieutenant Info", "Info about myself:")
                .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}", true)
                .AddField("Memory", $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true)
                .AddField("Izaro", $"[@{(await Context.Client.GetApplicationInfoAsync()).Owner}](https://discord.me/poe_xbox)", true)
                .Build();
            await ReplyAsync(Embed: Embed);
        }

        [Command("Show"), Remarks("Shows the League sections you are interested in."), Summary("Show <Name>"), RequireChannel("role-setup")]
        public Task ShowAsync(IRole Role)
        {
            Context.Message.DeleteAsync();
            var User = Context.User as SocketGuildUser;
            if (!Context.Server.SelfRoles.Contains(Role.Id))
                return ReplyAndDeleteAsync($"{Extras.Cross} Exile, you can't assign this role.");
            else if (User.Roles.Contains(Role))
                return ReplyAndDeleteAsync($"{Extras.Cross} Exile, you already have this role.");
            User.AddRoleAsync(Role);
            return ReplyAndDeleteAsync($"It seems this new arena suits me! *Role has been added to you.* {Extras.OkHand}");
        }

        [Command("Hide"), Remarks("Hides the League sections you aren't interested in."), Summary("Hide <Name>"), RequireChannel("role-setup")]
        public Task HideAsync(IRole Role)
        {
            Context.Message.DeleteAsync();
            var User = Context.User as SocketGuildUser;
            if (!Context.Server.SelfRoles.Contains(Role.Id))
                return ReplyAndDeleteAsync($"{Extras.Cross} Exile, you can't assign this role.");
            else if (!User.Roles.Contains(Role))
                return ReplyAndDeleteAsync($"{Extras.Cross} Exile, you don't have this role.");
            User.RemoveRoleAsync(Role);
            return ReplyAndDeleteAsync($"Hm. How fascinating. *Role has been removed from you.* {Extras.OkHand}");
        }

        [Command("Remind"), Remarks("Set a reminder for later. Time is formatted like: Number(d/h/m/s) Example: 5h for 5 Hours."), Summary("Remind <Time> <Message>")]
        public async Task RemindAsync(TimeSpan Time, [Remainder] string Message)
        {
            var Reminders = new List<RemindObject>();
            if (Context.Server.Reminders.ContainsKey(Context.User.Id))
                Context.Server.Reminders.TryGetValue(Context.User.Id, out Reminders);
            Reminders.Add(new RemindObject{
                Message = Message,
                TextChannel = Context.Channel.Id,
                RequestedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.Add(Time)
            });
            Context.Server.Reminders.AddOrUpdate(Context.User.Id, Reminders, (key, value) => value = Reminders);
            await ReplyAsync($"This land has forgotten Karui strength, {Context.User.Mention}. I will remind it. ({StringHelper.FormatTimeSpan(Time)})", Save: 's');
        }

        [Command("Reminders"), Remarks("Shows all of your reminders."), Summary("Reminders")]
        public Task RemindersAsync()
        {
            if (!Context.Server.Reminders.Any(x => x.Key == Context.User.Id))
                return ReplyAsync($"{Extras.Cross} Exile, you don't have reminders.");
            var Reminder = Context.Server.Reminders.First(x => x.Key == Context.User.Id);
            var Reminders = new List<string>();
            for (int i = 0; i < Reminder.Value.Count; i++)
                Reminders.Add($"Reminder #**{i}** | Expires on: **{Reminder.Value[i].ExpiryDate}**\n**Message:** {Reminder.Value[i].Message}");
            return PagedReplyAsync(Reminders, "Your Current Reminders");
        }

        [Command("Reminder Remove"), Remarks("Removes a reminder."), Summary("Reminder Remove <Number>")]
        public Task ReminderRemove(int Number)
        {
            if (!Context.Server.Reminders.Any(x => x.Key == Context.User.Id))
                return ReplyAsync($"{Extras.Cross} Exile, you don't have reminders.");
            var Reminders = new List<RemindObject>();
            Context.Server.Reminders.TryGetValue(Context.User.Id, out Reminders);
            try
            {
                Reminders.RemoveAt(Number);
            }
            catch
            {
                return ReplyAsync($"{Extras.Cross} Exile, invalid reminder number was provided.");
            }
            if (!Reminders.Any())
                Context.Server.Reminders.TryRemove(Context.User.Id, out _);
            else
                Context.Server.Reminders.TryUpdate(Context.User.Id, Reminders, Context.Server.Reminders.FirstOrDefault(x => x.Key == Context.User.Id).Value);
            return ReplyAsync($"Reminder #{Number} deleted {Extras.Trash}", Save: 's');
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
                var embed = Extras.Embed(Extras.Info)
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

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error fetching build data. Reason: {ex.Message}");
            }
        }

        [Command("Wiki"), Remarks("Searches an item on the Path of Exile Wiki."), Summary("Wiki <Item>")]
        public async Task WikiAsync([Remainder] string Item)
            => await ReplyAsync(embed: await WikiHelper.WikiGetItemAsync(Item));

        [Command("Trials Add"), Summary("Trials Add <Trial>"), RequireChannel("lab-and-trials"), Remarks("Add a Trial of Ascendancy that you're looking for to be notified when someone has found it. Any part of the Trial Name or All for all Trials.")]
        public Task TrialAddAsync([Remainder] string Trial)
        {
            if (Trial.ToLower() is "all")
                (Context.User as SocketGuildUser).AddRolesAsync(Context.Guild.Roles.Where(r => r.Name.Contains("Trial of")));
            else
                (Context.User as SocketGuildUser).AddRoleAsync(Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault());
            return ReplyAsync($"Some things that slumber should never be awoken. *Trial{(Trial.ToLower() is "all" ? "s were" : " was")} added to your list.* {Extras.OkHand}");
        }

        [Command("Trials Delete"), Summary("Trials Delete <Trial>"), RequireChannel("lab-and-trials"), Remarks("Delete a Trial of Ascendancy that you have completed. Any part of the Trial Name or All for all Trials.")]
        public Task TrialDeleteAsync([Remainder] string Trial)
        {
            if (Trial.ToLower() is "all")
                (Context.User as SocketGuildUser).RemoveRolesAsync(Context.Guild.Roles.Where(r => r.Name.Contains("Trial of")));
            else
                (Context.User as SocketGuildUser).RemoveRoleAsync(Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault());
            return ReplyAsync($"Woooooah, the weary traveller draws close to the end of the path.. *Trial{(Trial.ToLower() is "all" ? "s were" : " was")} removed from your list.* {Extras.OkHand}");
        }

        [Command("Trials List"), Summary("Trials"), RequireChannel("lab-and-trials"), Remarks("Shows a list of Trials you have left.")]
        public Task TrialListAsync()
            => ReplyAsync($"The Emperor beckons, and the world attends. *{String.Join(", ", (Context.User as SocketGuildUser).Roles.Where(r => r.Name.Contains("Trial of")).Select(r => r.Name))}*");

        [Command("Trial"), Summary("Trial <Trial>"), RequireChannel("lab-and-trials"), Remarks("Announce a Trial of Ascendancy that you have come across. Any part of the Trial Name.")]
        public Task TrialAsync([Remainder] string Trial)
            => ReplyAsync($"The essence of an empire must be shared equally amongst all of its citizens. *{Context.User.Mention} has found the {Context.Guild.Roles.Where(r => r.Name.ToLower().Contains(Trial.ToLower())).FirstOrDefault().Mention}*");

        [Command("Price"), Summary("Price <Name: Any Alias> [League: Defaults to Challenge]"), Remarks("Pulls the price for the requested currency, in the chosen league, all values based on Chaos."), BanChannel("price-checkers")]
        public Task PriceAsync(string Name, Leagues League = Leagues.Challenge)
        {
            if (!Context.Server.Prices.Where(p => p.Alias.Contains(Name.ToLower()) && p.League == League).Any())
                return ReplyAsync($"{Extras.Cross} What in God's name is that smell? *`{Name}` is not in the `{League}` list.*");
            var Price = Context.Server.Prices.FirstOrDefault(p => p.Alias.Contains(Name.ToLower()) && p.League == League);
            var User = (Context.Guild.GetUserAsync(Price.UserId).GetAwaiter().GetResult() as IUser);
            var Embed = Extras.Embed(Extras.Info)
                .AddField($"{Price.Name.Replace("_", " ")} in {League} league", $"```{Price.Alias}```")
                .AddField("Ratio", $"```{Price.Quantity}:{Price.Price}c```")
                .AddField("Last Updated", $"```{Price.LastUpdated}```")
                .AddField("Updated By", User.Mention)
                .WithFooter("Please be mindful of the Last Updated date and time, as these prices are gathered through community feedback. As you do your trades, if you could kindly report your ratios to a @Price Checker, we would greatly appreciate it as it keeps the prices current.")
                .Build();

            return ReplyAsync(embed: Embed);
        }

        [Command("PriceList"), Summary("PriceList [League: Defaults to Challenge]"), Remarks("Pulls the price for the all currency, in the specified league, defaults to Challenge")]
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
