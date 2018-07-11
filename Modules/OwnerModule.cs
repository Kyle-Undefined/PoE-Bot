namespace PoE.Bot.Modules
{
    using Addons;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Helpers;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.Scripting;
    using Objects;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    [Name("Owner Commands"), RequireOwner]
    public class OwnerModule : BotBase
    {
        public enum Setting
        {
            Activity,
            Avatar,
            FeedbackChannel,
            Nickname,
            Prefix,
            TwitchToken,
            Username,
        }

        [Command("Blacklist"), Remarks("Adds or Deletes a user from the blacklist."), Summary("Blacklist <action> <@user>")]
        public Task BlaclistAsync(CommandAction action, IUser user)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (Context.Config.Blacklist.Contains(user.Id))
                        return ReplyAsync($"{Extras.Cross} {user} is already in blacklisted.");
                    Context.Config.Blacklist.Add(user.Id);
                    return ReplyAsync($"`{user}` has been added to global blacklist.", save: DocumentType.Config);

                case CommandAction.Delete:
                    if (!Context.Config.Blacklist.Contains(user.Id))
                        return ReplyAsync($"{Extras.Cross} {user} isn't blacklisted.");
                    Context.Config.Blacklist.Remove(user.Id);
                    return ReplyAsync($"`{user}` has been removed from global blacklist.", save: DocumentType.Config);

                default:
                    return ReplyAsync($"{Extras.Cross} Action is either `Add` or `Delete`.");
            }
        }

        [Command("Eval", RunMode = RunMode.Async), Remarks("Evaluates C# code."), Summary("Eval <code>")]
        public async Task EvalAsync([Remainder] string code)
        {
            IUserMessage message = await ReplyAsync("Debugging ...").ConfigureAwait(false);
            var imports = Context.Config.Namespaces.Any()
                ? Context.Config.Namespaces
                : new[] { "System", "System.Linq", "System.Collections.Generic", "System.IO", "System.Threading.Tasks" }.ToList();
            ScriptOptions options = ScriptOptions.Default.AddReferences(MethodHelper.Assemblies).AddImports(imports);
            EvalObject globals = new EvalObject
            {
                Context = Context,
                Guild = Context.Guild as SocketGuild,
                User = Context.User as SocketGuildUser,
                Client = Context.Client as DiscordSocketClient,
                Channel = Context.Channel as SocketGuildChannel
            };

            try
            {
                object eval = await CSharpScript.EvaluateAsync(code.Replace("```", string.Empty), options, globals, typeof(EvalObject)).ConfigureAwait(false);
                await message.ModifyAsync(x => x.Content = $"{eval ?? "No Result Produced."}").ConfigureAwait(false);
            }
            catch (CompilationErrorException ex)
            {
                await message.ModifyAsync(x => x.Content = ex.Message ?? ex.StackTrace).ConfigureAwait(false);
            }
        }

        [Command("Namespace"), Remarks("Shows a list of all namespaces in PoE Bots config."), Summary("Namespace <action> <namespace>")]
        public Task NamespaceAsync(CommandAction action, string namespaceName = null)
        {
            switch (action)
            {
                case CommandAction.Add:
                    if (Context.Config.Namespaces.Contains(namespaceName))
                        return ReplyAsync($"{Extras.Cross} {namespaceName} namespace already exists.");

                    Context.Config.Namespaces.Add(namespaceName);
                    return ReplyAsync($"`{namespaceName}` has been added.", save: DocumentType.Config);

                case CommandAction.Delete:
                    if (!Context.Config.Namespaces.Contains(namespaceName))
                        return ReplyAsync($"{Extras.Cross} {namespaceName} namespace doesn't exist.");

                    Context.Config.Namespaces.Remove(namespaceName);
                    return ReplyAsync($"`{namespaceName}` has been removed.", save: DocumentType.Config);

                case CommandAction.List:
                    return !Context.Config.Namespaces.Any()
                        ? ReplyAsync("Couldn't find any Namespaces")
                        : PagedReplyAsync(MethodHelper.Pages(Context.Config.Namespaces), "Current Namespaces");

                default:
                    return ReplyAsync($"{Extras.Cross} Action is either `Add`, `Delete` or `List`.");
            }
        }

        [Command("Stats", RunMode = RunMode.Async), Remarks("Displays information about PoE Bot and its stats."), Summary("Stats")]
        public async Task StatsAsync()
        {
            DiscordSocketClient client = Context.Client as DiscordSocketClient;
            GuildObject[] servers = Context.DatabaseHandler.Servers();
            Embed embed = Extras.Embed(Extras.Info)
                .WithAuthor($"{Context.Client.CurrentUser.Username} Statistics 🤖", Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl())
                .WithDescription((await client.GetApplicationInfoAsync().ConfigureAwait(false)).Description)
                .AddField("Channels",
                    $"Categories: {client.Guilds.Sum(x => x.CategoryChannels.Count)}\n" +
                    $"Text: {client.Guilds.Sum(x => x.TextChannels.Count - x.CategoryChannels.Count)}\n" +
                    $"Voice: {client.Guilds.Sum(x => x.VoiceChannels.Count)}\n" +
                    $"Total: {client.Guilds.Sum(x => x.Channels.Count)}", true)
                .AddField("Members",
                    $"Bot: {client.Guilds.Sum(x => x.Users.Count(z => z.IsBot is true))}\n" +
                    $"Human: { client.Guilds.Sum(x => x.Users.Count(z => z.IsBot is false))}\n" +
                    $"Total: {client.Guilds.Sum(x => x.Users.Count)}", true)
                .AddField("Database",
                    $"Tags: {servers.Sum(x => x.Tags.Count)}\n" +
                    $"Currencies: {servers.Sum(x => x.Prices.Count)}\n" +
                    $"Shop Items: {servers.Sum(x => x.Shops.Count)}", true)
                .AddField("Mixer", $"Streams: {servers.Sum(x => x.Streams.Count(s => s.StreamType is StreamType.Mixer))}", true)
                .AddField("Twitch", $"Streams: {servers.Sum(x => x.Streams.Count(s => s.StreamType is StreamType.Twitch))}", true)
                .AddField("Leaderboard", $"Variants: {servers.Sum(x => x.Leaderboards.Count)}", true)
                .AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}", true)
                .AddField("Memory", $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true)
                .AddField("Programmer", $"[{(await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner}](https://discord.me/poe_xbox)", true)
                .Build();
            await ReplyAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("Update", RunMode = RunMode.Async), Remarks("Updates PoE Bots Information."), Summary("Update <setting> <value>")]
        public async Task UpdateAsync(Setting setting, [Remainder] string value = null)
        {
            DocumentType save = DocumentType.None;
            switch (setting)
            {
                case Setting.Avatar:
                    string imagePath = string.IsNullOrWhiteSpace(value)
                        ? await StringHelper.DownloadImageAsync(Context.HttpClient, (await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false)).IconUrl).ConfigureAwait(false)
                        : value;
                    await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(imagePath)).ConfigureAwait(false);
                    break;

                case Setting.Username:
                    await Context.Client.CurrentUser.ModifyAsync(x => x.Username = value).ConfigureAwait(false);
                    break;

                case Setting.Activity:
                    string[] split = value.Split(':');
                    await (Context.Client as DiscordSocketClient).SetActivityAsync(new Game(split[1], (ActivityType)Enum.Parse(typeof(ActivityType), split[0]))).ConfigureAwait(false);
                    break;

                case Setting.Nickname:
                    await (await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false)).ModifyAsync(x => x.Nickname = string.IsNullOrWhiteSpace(value) ? Context.Client.CurrentUser.Username : value).ConfigureAwait(false);
                    break;

                case Setting.FeedbackChannel:
                    Context.Config.FeedbackChannel = string.IsNullOrWhiteSpace(value) ? 0 : value.ParseULong();
                    save = DocumentType.Config;
                    break;

                case Setting.TwitchToken:
                    string[] tokens = value.Split(':');
                    Context.Config.APIKeys["TC"] = tokens[0];
                    Context.Config.APIKeys["TA"] = tokens[1];
                    save = DocumentType.Config;
                    break;

                case Setting.Prefix:
                    Context.Config.Prefix = value;
                    save = DocumentType.Config;
                    break;
            }
            await ReplyAsync($"Bot has been updated {Extras.OkHand}", save: save).ConfigureAwait(false);
        }
    }
}