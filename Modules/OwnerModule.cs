namespace PoE.Bot.Modules
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Addons;
    using PoE.Bot.Helpers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Drawing = System.Drawing.Color;

    [Name("Owner Commands"), RequireOwner]
    public class OwnerModule : BotBase
    {
        [Command("Update", RunMode = RunMode.Async), Remarks("Updates PoE Bots Information."), Summary("Update <Action: a, u, s, n, r, t, p> [Value]")]
        public async Task UpdateAsync(char Action, [Remainder] string Value = null)
        {
            char Save = 'n';
            switch (Action)
            {
                case 'a':
                    var ImagePath = string.IsNullOrWhiteSpace(Value) ?
                        await StringHelper.DownloadImageAsync(Context.HttpClient, (await Context.Client.GetApplicationInfoAsync()).IconUrl) : Value;
                    await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(ImagePath));
                    break;
                case 'u': await Context.Client.CurrentUser.ModifyAsync(x => x.Username = Value); break;
                case 's':
                    var Split = Value.Split(':');
                    await (Context.Client as DiscordSocketClient).SetActivityAsync(new Game(Split[1], (ActivityType)Enum.Parse(typeof(ActivityType), Split[0]))).ConfigureAwait(false);
                    break;
                case 'n':
                    await (await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload)).ModifyAsync(x => x.Nickname = string.IsNullOrWhiteSpace(Value) ? Context.Client.CurrentUser.Username : Value);
                    break;
                case 'r':
                    Context.Config.ReportChannel = string.IsNullOrWhiteSpace(Value) ? 0 : Context.GuildHelper.ParseUlong(Value);
                    Save = 'c';
                    break;
                case 't':
                    var Tokens = Value.Split(':');
                    Context.Config.APIKeys["TC"] = Tokens[0];
                    Context.Config.APIKeys["TA"] = Tokens[1];
                    Save = 'c';
                    break;
                case 'p': Context.Config.Prefix = Value; Save = 'c'; break;
            }
            await ReplyAsync($"Bot has been updated {Extras.OkHand}", Save: Save);
        }

        [Command("Blacklist"), Remarks("Adds or removes a user from the blacklist."), Summary("Blacklist <Action: a, r> <@User>")]
        public Task BlaclistAsync(char Action, IUser User)
        {
            switch (Action)
            {
                case 'a':
                    if (Context.Config.Blacklist.Contains(User.Id))
                        return ReplyAsync($"{Extras.Cross} {User} is already in blacklisted.");
                    Context.Config.Blacklist.Add(User.Id);
                    return ReplyAsync($"`{User}` has been added to global blacklist.", Save: 'c');
                case 'r':
                    if (!Context.Config.Blacklist.Contains(User.Id))
                        return ReplyAsync($"{Extras.Cross} {User} isn't blacklisted.");
                    Context.Config.Blacklist.Remove(User.Id);
                    return ReplyAsync($"`{User}` has been removed from global blacklist.", Save: 'c');
                default: return Task.CompletedTask;
            }
        }

        [Command("Eval", RunMode = RunMode.Async), Remarks("Evaluates C# code."), Summary("Eval <Code>")]
        public async Task EvalAsync([Remainder] string Code)
        {
            var Message = await ReplyAsync("Debugging ...");
            var Imports = Context.Config.Namespaces.Any() ? Context.Config.Namespaces :
                new[] { "System", "System.Linq", "System.Collections.Generic", "System.IO", "System.Threading.Tasks" }.ToList();
            var Options = ScriptOptions.Default.AddReferences(MethodHelper.Assemblies).AddImports(Imports);
            var Globals = new EvalObject
            {
                Context = Context,
                Guild = Context.Guild as SocketGuild,
                User = Context.User as SocketGuildUser,
                Client = Context.Client as DiscordSocketClient,
                Channel = Context.Channel as SocketGuildChannel
            };
            try
            {
                var Eval = await CSharpScript.EvaluateAsync(Code.Replace("```", ""), Options, Globals, typeof(EvalObject));
                await Message.ModifyAsync(x => x.Content = $"{Eval ?? "No Result Produced."}");
            }
            catch (CompilationErrorException Ex)
            {
                await Message.ModifyAsync(x => x.Content = Ex.Message ?? Ex.StackTrace);
            }
        }

        [Command("Namespace", RunMode = RunMode.Async), Remarks("Shows a list of all namespaces in PoE Bots config."), Summary("Namespace")]
        public Task NamespaceAsync()
            => !Context.Config.Namespaces.Any() ? ReplyAsync($"Uhm.. I couldn't find any namespaces.") :
            PagedReplyAsync(Context.GuildHelper.Pages(Context.Config.Namespaces), "Current Namespaces");

        [Command("Namespace"), Remarks("Shows a list of all namespaces in PoE Bots config."), Summary("Namespace <Action: a, r> <Namespace>")]
        public Task NamespaceAsync(char Action, string Namespace)
        {
            switch (Action)
            {
                case 'a':
                    if (Context.Config.Namespaces.Contains(Namespace))
                        return ReplyAsync($"{Extras.Cross} {Namespace} namespace already exists.");
                    Context.Config.Namespaces.Add(Namespace);
                    return ReplyAsync($"`{Namespace}` has been added.", Save: 'c');
                case 'r':
                    if (!Context.Config.Namespaces.Contains(Namespace))
                        return ReplyAsync($"{Extras.Cross} {Namespace} namespace doesn't exist.");
                    Context.Config.Namespaces.Remove(Namespace);
                    return ReplyAsync($"`{Namespace}` has been removed.", Save: 'c');
                default: return Task.CompletedTask;
            }
        }

        [Command("Stats", RunMode = RunMode.Async), Remarks("Displays information about PoE Bot and its stats."), Summary("Stats")]
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
                $"Bot: {Client.Guilds.Sum(x => x.Users.Where(z => z.IsBot is true).Count())}\n" +
                $"Human: { Client.Guilds.Sum(x => x.Users.Where(z => z.IsBot is false).Count())}\n" +
                $"Total: {Client.Guilds.Sum(x => x.Users.Count)}", true)
                .AddField("Database",
                $"Tags: {Servers.Sum(x => x.Tags.Count)}\n" +
                $"Currencies: {Servers.Sum(x => x.Prices.Count)}\n" +
                $"Shop Items: {Servers.Sum(x => x.Shops.Count)}", true)
                .AddField("Mixer", $"Streams: {Context.Server.Streams.Count(s => s.StreamType is StreamType.MIXER)}", true)
                .AddField("Twitch", $"Streams: {Context.Server.Streams.Count(s => s.StreamType is StreamType.MIXER)}", true)
                .AddField("Leaderboard", $"Variants: {Context.Server.Leaderboards.Count}", true)
                .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}", true)
                .AddField("Memory", $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true)
                .AddField("Programmer", $"[{(await Context.Client.GetApplicationInfoAsync()).Owner}](https://discord.me/poe_xbox)", true)
                .Build();
            await ReplyAsync(Embed: Embed);
        }
    }
}
