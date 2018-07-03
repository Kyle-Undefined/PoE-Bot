namespace PoE.Bot.Modules
{
    using Addons;
    using Addons.Preconditions;
    using Discord;
    using Discord.Commands;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [Name("Help"), Ratelimit]
    public class HelpModule : BotBase
    {
        public CommandService CommandService { get; set; }
        private IServiceProvider Provider { get; }

        [Command("Command"), Summary("Display how you can use a command.")]
        public Task CommandAsync([Remainder] string commandName)
        {
            CommandInfo command = CommandService.Search(Context, commandName).Commands?.FirstOrDefault(x => x.CheckPreconditionsAsync(Context, Provider).GetAwaiter().GetResult().IsSuccess).Command;
            if (command is null)
                return ReplyAsync($"{Extras.Cross} What in God's name is that smell? *`{commandName}` command doesn't exist.*");

            string name = command.Name.Contains("Async") ? command.Module.Group : command.Name;
            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithAuthor("Detailed Command Information", Context.Client.CurrentUser.GetAvatarUrl())
                .AddField("Name", name, true)
                .AddField("Aliases", string.Join(", ", command.Aliases), true)
                .AddField("Arguments", command.Parameters.Any()
                    ? string.Join(", ", command.Parameters.Select(x => $"`{(x.Type.IsValueType ? (Nullable.GetUnderlyingType(x.Type) is null ? x.Type.Name : Nullable.GetUnderlyingType(x.Type).Name) : x.Type.Name)}` {x.Name}"))
                    : "No arguments.")
                .AddField("Usage", $"{Context.Server.Prefix}{command.Summary}")
                .AddField("Summary", command.Remarks)
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithFooter("<> = Required | [] = Optional | Need help? Tag @Server Nerd");
            var enums = command.Parameters.Where(x => x.Type.IsEnum);
            if (enums.Any())
                embed.AddField($"{enums.FirstOrDefault()?.Name} Values", string.Join(", ", enums.FirstOrDefault().Type.GetEnumNames()));

            return ReplyAsync(embed: embed.Build());
        }

        [Command("Commands"), Summary("Displays all commands.")]
        public Task CommandsAsync()
        {
            EmbedBuilder embed = Extras.Embed(Extras.Info)
                .WithAuthor("List of all commands", Context.Client.CurrentUser.GetAvatarUrl())
                .WithFooter($"For More Information On A Command's Usage: {Context.Config.Prefix}Command CommandName");

            foreach (ModuleInfo mi in CommandService.Modules.OrderBy(x => x.Name))
                if (!mi.IsSubmodule)
                    if (!(mi.Name is "Help"))
                    {
                        bool ok = true;
                        foreach (PreconditionAttribute precondition in mi.Preconditions)
                            if (!precondition.CheckPermissionsAsync(Context, null, Provider).Result.IsSuccess)
                            {
                                ok = false;
                                break;
                            }
                        if (ok)
                        {
                            var cmds = mi.Commands.Where(x => !x.Name.Contains("Async")).ToList<object>();
                            cmds.AddRange(mi.Submodules);
                            for (int i = cmds.Count - 1; i >= 0; i--)
                            {
                                object o = cmds[i];
                                foreach (PreconditionAttribute precondition in (o as CommandInfo)?.Preconditions ?? (o as ModuleInfo)?.Preconditions)
                                    if (!precondition.CheckPermissionsAsync(Context, o as CommandInfo, Provider).Result.IsSuccess)
                                        cmds.Remove(o);
                            }
                            if (!(cmds.Count is 0))
                            {
                                var list = cmds.Select(x => $"{(!string.IsNullOrEmpty(mi.Group) ? mi.Group + " " : "")}{(x as CommandInfo)?.Name ?? (x as ModuleInfo)?.Name}").OrderBy(x => x);
                                embed.AddField(mi.Name, string.Join(", ", list));
                            }
                        }
                    }

            return ReplyAsync(embed: embed.Build());
        }
    }
}