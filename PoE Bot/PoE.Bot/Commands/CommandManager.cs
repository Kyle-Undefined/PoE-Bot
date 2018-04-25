using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PoE.Bot.Attributes;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Commands
{
    /// <summary>
    /// Handles all commands.
    /// </summary>
    public class CommandManager
    {
        internal ParameterParser ParameterParser { get; private set; }
        private Dictionary<string, Command> RegisteredCommands { get; set; }
        private Dictionary<string, IPermissionChecker> RegisteredCheckers { get; set; }
        public int CommandCount { get { return this.GetCommands().Count(); } }
        public int CheckerCount { get { return this.RegisteredCheckers.Count; } }

        /// <summary>
        /// Initializes the command handler.
        /// </summary>
        internal void Initialize()
        {
            Log.W("Comms Mgr", "Initializing commands");
            this.ParameterParser = new ParameterParser();
            this.RegisterCheckers();
            this.RegisterCommands();
            this.InitCommands();
            Log.W("Comms Mgr", "Initialized");
        }

        /// <summary>
        /// Gets all registered commands.
        /// </summary>
        /// <returns>All registered commands.</returns>
        public IEnumerable<Command> GetCommands()
        {
            foreach (var cmd in this.RegisteredCommands.GroupBy(xkvp => xkvp.Value))
                yield return cmd.Key;
        }

        public string GetPrefix(ulong guildid)
        {
            var prefix = "!";
            var gconf = PoE_Bot.ConfigManager.GetGuildConfig(guildid);
            if (gconf != null && gconf.CommandPrefix != null)
                prefix = gconf.CommandPrefix;

            return prefix;
        }

        internal Command GetCommand(string name)
        {
            if (this.RegisteredCommands.ContainsKey(name))
                return this.RegisteredCommands[name];
            return null;
        }

        private void RegisterCheckers()
        {
            Log.W("Comms Mgr", "Registering permission checkers");
            this.RegisteredCheckers = new Dictionary<string, IPermissionChecker>();
            var @as = PoE_Bot.PluginManager.PluginAssemblies;
            var ts = @as.SelectMany(xa => xa.DefinedTypes);
            var ct = typeof(IPermissionChecker);
            foreach (var t in ts)
            {
                if (!ct.IsAssignableFrom(t.AsType()) || !t.IsClass || t.IsAbstract)
                    continue;

                var ipc = (IPermissionChecker)Activator.CreateInstance(t.AsType());
                this.RegisteredCheckers.Add(ipc.Id, ipc);
                Log.W("Comms Mgr", "Registered checker '{0}' for type {1}", ipc.Id, t.ToString());
            }
            Log.W("Comms Mgr", "Registered {0:#,##0} checkers", this.RegisteredCheckers.Count);
        }

        private void RegisterCommands()
        {
            Log.W("Comms Mgr", "Registering commands");
            this.RegisteredCommands = new Dictionary<string, Command>();
            var @as = PoE_Bot.PluginManager.PluginAssemblies;
            var ts = @as.SelectMany(xa => xa.DefinedTypes);
            var ht = typeof(ICommandModule);
            var ct = typeof(PoE.Bot.Attributes.CommandAttribute);
            var pt = typeof(MethodParameterAttribute);
            foreach (var t in ts)
            {
                if (!ht.IsAssignableFrom(t.AsType()) || !t.IsClass || t.IsAbstract)
                    continue;

                var ch = (ICommandModule)Activator.CreateInstance(t.AsType());
                Log.W("Comms Mgr", "Found module handler '{0}' in type {1}", ch.Name, t.ToString());
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    var xct = m.GetCustomAttribute<PoE.Bot.Attributes.CommandAttribute>();
                    if (xct == null)
                        continue;

                    var prs = m.GetParameters();
                    var xps = m.GetCustomAttributes<MethodParameterAttribute>().ToArray();
                    if (prs.Length > 1 && xps.Length > 0)
                    {
                        Log.W("Comms Mgr", "Command '{0}' has invalid parameter specification, skipping", xct.Name);
                        continue;
                    }

                    var ats = new List<CommandParameter>();
                    if (xps.Length > 0)
                    {
                        foreach (var xp in xps)
                            ats.Add(new CommandParameter(xp.Order, xp.Name, xp.Description, xp.IsRequired, xp.IsCatchAll, false));
                    }
                    else if (prs.Length > 1)
                    {
                        var prn = 0;
                        foreach (var prm in prs.Skip(1))
                        {
                            var pmi = prm.GetCustomAttribute<ArgumentParameterAttribute>();
                            var isp = prm.GetCustomAttribute<ParamArrayAttribute>();

                            var all = false; // catchall
                            if (isp != null)
                                all = true;

                            if (pmi == null)
                                pmi = new ArgumentParameterAttribute("UNSPECIFIED.", true);

                            ats.Add(new CommandParameter(prn++, prm.Name, pmi.Description, pmi.IsRequired, all, true) { ParameterType = prm.ParameterType });
                        }
                    }

                    var prms = m.GetParameters();
                    var args = new ParameterExpression[prms.Length];
                    var i = 0;
                    foreach (var prm in prms)
                        args[i++] = Expression.Parameter(prm.ParameterType, prm.Name);
                    var func = Expression.Lambda(Expression.Call(Expression.Constant(ch), m, args), args).Compile();

                    var aliases = xct.Aliases != null ? xct.Aliases.Split(';') : new string[] { };
                    var cmd = new Command(xct.Name, aliases, xct.Description, xct.CheckPermissions && this.RegisteredCheckers.ContainsKey(xct.CheckerId) ? this.RegisteredCheckers[xct.CheckerId] : null, func, ch, xct.RequiredPermission, ats);
                    var names = new string[1 + aliases.Length];
                    names[0] = cmd.Name;
                    if (aliases.Length > 0)
                        Array.Copy(aliases, 0, names, 1, aliases.Length);
                    if (!this.RegisteredCommands.ContainsKey(cmd.Name))
                    {
                        foreach (var name in names)
                        {
                            if (!this.RegisteredCommands.ContainsKey(name))
                                this.RegisteredCommands.Add(name, cmd);
                            else
                                Log.W("Comms Mgr", "Alias '{0}' for command '{1}' already taken, skipping", name, cmd.Name);
                        }
                        Log.W("Comms Mgr", "Registered command '{0}' for module '{1}'", cmd.Name, ch.Name);
                    }
                    else
                        Log.W("Comms Mgr", "Command name '{0}' is already registered, skipping", cmd.Name);
                }
                Log.W("Comms Mgr", "Registered command module '{0}' for type {1}", ch.Name, t.ToString());
            }
            Log.W("Comms Mgr", "Registered {0:#,##0} commands", this.RegisteredCommands.GroupBy(xkvp => xkvp.Value).Count());
        }

        private void InitCommands()
        {
            Log.W("Comms Mgr", "Registering command handler");
            PoE_Bot.Client.DiscordClient.MessageReceived += HandleCommand;
            Log.W("Comms Mgr", "Done");
        }

        private async Task HandleCommand(SocketMessage arg)
        {
            await Task.Delay(1);

            var msg = arg as SocketUserMessage;
            if (msg == null || msg.Author == null || msg.Author.IsBot || msg.Content.Length == 1)
                return;

            var chn = msg.Channel as SocketTextChannel;
            if (chn == null && !IsPrivateMessage(msg))
                return;

            var gld = IsPrivateMessage(msg) ? null : chn.Guild;
            if (gld == null && !IsPrivateMessage(msg))
                return;

            var client = PoE_Bot.Client.DiscordClient;
            var argpos = 0;
            var gconf = IsPrivateMessage(msg) ? null : PoE_Bot.ConfigManager.GetGuildConfig(gld.Id);
            var cprefix = "!";
            if (gconf != null && gconf.CommandPrefix != null)
                cprefix = gconf.CommandPrefix;

            if (msg.Content.Contains("[["))
            {
                string message = msg.Content;
                string item = message.Split('[', ']')[2];
                var cmd = this.GetCommand("wiki");

                if (cmd == null)
                    return;

                var ctx = new CommandContext();
                ctx.Message = msg;
                ctx.Command = cmd;
                ctx.RawArguments = this.ParseArgumentList(item);
                var t = Task.Run(async () =>
                {
                    try
                    {
                        if (!IsPrivateMessage(msg))
                            if (gconf.DeleteCommands != null && gconf.DeleteCommands.Value)
                                await msg.DeleteAsync();
                        await cmd.Execute(ctx);
                        this.CommandExecuted(ctx);
                    }
                    catch (Exception ex)
                    {
                        this.CommandError(new CommandErrorContext { Context = ctx, Exception = ex });
                    }
                });
            }
            else if (msg.HasStringPrefix(cprefix, ref argpos) || msg.HasMentionPrefix(client.CurrentUser, ref argpos))
            {
                if (msg.Content.IndexOf(" ") == 1 && msg.Content.StartsWith(cprefix))
                    return;

                var cmdn = msg.Content.Substring(argpos);
                var argi = cmdn.IndexOf(' ');
                if (argi == -1)
                    argi = cmdn.Length;
                var args = cmdn.Substring(argi).Trim();
                cmdn = cmdn.Substring(0, argi);
                var cmd = this.GetCommand(cmdn);
                if (cmd == null)
                    return;

                var ctx = new CommandContext();
                ctx.Message = msg;
                ctx.Command = cmd;
                ctx.RawArguments = this.ParseArgumentList(args);
                var t = Task.Run(async () =>
                {
                    try
                    {
                        if(!IsPrivateMessage(msg))
                            if (gconf.DeleteCommands != null && gconf.DeleteCommands.Value)
                                await msg.DeleteAsync();
                        await cmd.Execute(ctx);
                        this.CommandExecuted(ctx);
                    }
                    catch (Exception ex)
                    {
                        this.CommandError(new CommandErrorContext { Context = ctx, Exception = ex });
                    }
                });
            }
        }

        private void CommandError(CommandErrorContext ctxe)
        {
            var ctx = ctxe.Context;
            Log.W("DSC CMD", "User '{0}#{1}' failed to execute command '{2}' in guild '{3}' ({4}); reason: {5} ({6})", ctx.User.Username, ctx.User.Discriminator, ctx.Command != null ? ctx.Command.Name : "<unknown>", ctx.Guild.Name, ctx.Guild.IconId, ctxe.Exception != null ? ctxe.Exception.GetType().ToString() : "<unknown exception type>", ctxe.Exception != null ? ctxe.Exception.Message : "N/A");
            if (ctxe.Exception != null)
                Log.X("DSC CMD", ctxe.Exception);

            var embed = new EmbedBuilder();
            embed.Title = "Error executing command";
            embed.Description = string.Format("User {0} failed to execute command **{1}**.", ctx.User.Mention, ctx.Command != null ? ctx.Command.Name : "<unknown>");
            embed.Color = new Color(255, 127, 0);

            embed.AddField(x =>
            {
                x.IsInline = false;
                x.Name = "Reason";
                x.Value = ctxe.Exception != null ? ctxe.Exception.Message : "<unknown>";
            });

            if (ctxe.Exception != null)
            {
                embed.AddField(x =>
                {
                    x.IsInline = false;
                    x.Name = "Exception details";
                    x.Value = string.Format("**{0}**: {1}", ctxe.Exception.GetType().ToString(), ctxe.Exception.Message);
                });
            }

            ctx.Channel.SendMessageAsync("", false, embed.Build()).GetAwaiter().GetResult();
        }

        private void CommandExecuted(CommandContext ctx)
        {
            Log.W("DSC CMD", "User '{0}#{1}' executed command '{2}' on server '{3}' ({4})", ctx.User.Username, ctx.User.Discriminator, ctx.Command.Name, ctx.Guild.Name, ctx.Guild.Id);
        }

        private IReadOnlyList<string> ParseArgumentList(string argstring)
        {
            if (string.IsNullOrWhiteSpace(argstring))
                return new List<string>().AsReadOnly();

            var arglist = new List<string>();
            var argsraw = argstring.Split(' ');
            var sb = new StringBuilder();
            var building_arg = false;
            foreach (var argraw in argsraw)
            {
                if (!building_arg && !argraw.StartsWith("\""))
                    arglist.Add(argraw);
                else if (!building_arg && argraw.StartsWith("\"") && argraw.EndsWith("\""))
                    arglist.Add(argraw.Substring(1, argraw.Length - 2));
                else if (!building_arg && argraw.StartsWith("\"") && !argraw.EndsWith("\""))
                {
                    building_arg = true;
                    sb.Append(argraw.Substring(1)).Append(' ');
                }
                else if (building_arg && !argraw.EndsWith("\""))
                    sb.Append(argraw).Append(' ');
                else if (building_arg && argraw.EndsWith("\"") && !argraw.EndsWith("\\\""))
                {
                    sb.Append(argraw.Substring(0, argraw.Length - 1));
                    arglist.Add(sb.ToString());
                    building_arg = false;
                    sb = new StringBuilder();
                }
                else if (building_arg && argraw.EndsWith("\\\""))
                    sb.Append(argraw.Remove(argraw.Length - 2, 1)).Append(' ');
            }

            return arglist.AsReadOnly();
        }

        private bool IsPrivateMessage(SocketMessage msg)
        {
            return (msg.Channel.GetType() == typeof(SocketDMChannel));
        }
    }
}
