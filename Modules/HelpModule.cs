namespace PoE.Bot.Modules
{
    using System;
    using System.Linq;
    using PoE.Bot.Addons;
    using Discord.Commands;
    using System.Threading.Tasks;
    using PoE.Bot.Addons.Preconditions;
    using Drawing = System.Drawing.Color;

    [Name("Help"), Ratelimit]
    public class HelpModule : BotBase
    {
        IServiceProvider Provider { get; }
        public CommandService CommandService { get; set; }

        [Command("Command"), Summary("Display how you can use a command.")]
        public Task CommandAsync([Remainder] string CommandName)
        {
            var Command = CommandService.Search(Context, CommandName).Commands?.FirstOrDefault(x => x.CheckPreconditionsAsync(Context, Provider).GetAwaiter().GetResult().IsSuccess).Command;
            if (Command is null)
                return ReplyAsync($"{Extras.Cross} What in God's name is that smell? *`{CommandName}` command doesn't exist.*");
            string Name = Command.Name.Contains("Async") ? Command.Module.Group : Command.Name;
            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor("Detailed Command Information", Context.Client.CurrentUser.GetAvatarUrl())
                .AddField("Name", Name, true)
                .AddField("Aliases", string.Join(", ", Command.Aliases), true)
                .AddField("Arguments", Command.Parameters.Any() ? string.Join(", ", Command.Parameters.Select(x => $"`{(x.Type.IsValueType ? Nullable.GetUnderlyingType(x.Type).Name : x.Type.Name)}` {x.Name}")) : "No arguments.")
                .AddField("Usage", $"{Context.Server.Prefix}{Command.Summary}")
                .AddField("Summary", Command.Remarks)
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithFooter("<> = Required | [] = Optional | Need help? Tag @Server Nerd");
            var GetChar = Command.Parameters.Where(x => x.Type == typeof(char));
            if (GetChar.Any())
                Embed.AddField($"{GetChar.FirstOrDefault()?.Name} Values", "a, r, m. a = Add, r = remove, m = Modify.");
            var GetEnum = Command.Parameters.Where(x => x.Type.IsEnum is true);
            if (GetEnum.Any())
                Embed.AddField($"{GetEnum.FirstOrDefault()?.Name} Values", string.Join(", ", GetEnum?.FirstOrDefault().Type.GetEnumNames()));
            return ReplyAsync(Embed: Embed.Build());
        }

        [Command("Commands"), Summary("Displays all commands.")]
        public Task CommandsAsync()
        {
            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor("List of all commands", Context.Client.CurrentUser.GetAvatarUrl())
                .WithFooter($"For More Information On A Command's Usage: {Context.Config.Prefix}Command CommandName");

            foreach (ModuleInfo mi in CommandService.Modules.OrderBy(x => x.Name)) // list all modules by name
                if (!mi.IsSubmodule)
                    if (!(mi.Name is "Help")) // we don't want to list our help command
                    {
                        bool ok = true;
                        foreach (PreconditionAttribute precondition in mi.Preconditions) // check preconditions before showing it
                            if (!(precondition.CheckPermissionsAsync(Context, null, Provider)).Result.IsSuccess)
                            {
                                ok = false;
                                break;
                            }
                        if (ok)
                        {
                            var cmds = mi.Commands.Where(x => !x.Name.Contains("Async")).ToList<object>(); // this part will check preconditions for each command, it has to be an object because groups are Modules
                            cmds.AddRange(mi.Submodules);
                            for (int i = cmds.Count - 1; i >= 0; i--)
                            {
                                object o = cmds[i];
                                foreach (PreconditionAttribute precondition in ((o as CommandInfo)?.Preconditions ?? (o as ModuleInfo)?.Preconditions))
                                    if (!(precondition.CheckPermissionsAsync(Context, o as CommandInfo, Provider)).Result.IsSuccess)
                                        cmds.Remove(o);
                            }
                            if (!(cmds.Count is 0)) // add to the embed if the list has anything
                            {
                                var list = cmds.Select(x => $"{(!string.IsNullOrEmpty(mi.Group) ? mi.Group + " " : "")}{(x as CommandInfo)?.Name ?? (x as ModuleInfo)?.Name}").OrderBy(x => x);
                                Embed.AddField(mi.Name, String.Join(", ", list));
                            }
                        }
                    }

            return ReplyAsync(Embed: Embed.Build());
        }
    }
}
