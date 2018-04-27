using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Commands
{
    /// <summary>
    /// Represents a command.
    /// </summary>
    public sealed class Command
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the aliases of the command.
        /// </summary>
        public ReadOnlyCollection<string> Aliases { get; private set; }

        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the command's parameter definitions.
        /// </summary>
        public ReadOnlyCollection<CommandParameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the permission checker for the command.
        /// </summary>
        internal IPermissionChecker Checker { get; private set; }

        /// <summary>
        /// Gets the function to execute when this command is executed.
        /// </summary>
        internal Delegate Function { get; private set; }

        /// <summary>
        /// Gets the command's registering module.
        /// </summary>
        internal ICommandModule Module { get; private set; }

        /// <summary>
        /// Gets or sets the required permissions.
        /// </summary>
        internal Permission RequiredPermission { get; set; }

        /// <summary>
        /// Creates a new instance of a command.
        /// </summary>
        /// <param name="name">Name of the command.</param>
        /// <param name="aliases">Aliases of the command.</param>
        /// <param name="description">Command's description.</param>
        /// <param name="checker">Command's permissions checker.</param>
        /// <param name="method">Method executed when command is called.</param>
        /// <param name="handler">Command's registering handler.</param>
        /// <param name="permission">Command's required permission.</param>
        public Command(string name, string[] aliases, string description, IPermissionChecker checker, Delegate function, ICommandModule module, Permission permission, IList<CommandParameter> @params)
        {
            this.Name = name;
            this.Aliases = new ReadOnlyCollection<string>(aliases);
            this.Description = description;
            this.Checker = checker;
            this.Function = function;
            this.Module = module;
            this.RequiredPermission = permission;
            this.Parameters = new ReadOnlyCollection<CommandParameter>(@params);
        }

        internal async Task Execute(CommandContext context)
        {
            var error = null as string;
            var canrun = false;
            if (this.Checker == null)
                canrun = true;
            else
                canrun = this.Checker.CanRun(this, context.User, context.Message, context.Channel, context.Guild, out error);
            if (canrun)
                await (Task)this.Function.DynamicInvoke(PrepareArguments(context));
            else
                throw new UnauthorizedAccessException(error);
        }

        private object[] PrepareArguments(CommandContext ctx)
        {
            var prms = this.Parameters.Where(xp => xp.IsFunctionArgument).OrderBy(xp => xp.Order);
            var args = new object[prms.Count() + 1];
            args[0] = ctx;

            foreach (var prm in prms)
            {
                if (prm.IsCatchAll)
                {
                    if (!prm.ParameterType.IsArray)
                        throw new InvalidOperationException("Parameter is catchall but not an array.");

                    var ags = ctx.RawArguments
                        .Skip(prm.Order)
                        .Select(xa => PoE_Bot.CommandManager.ParameterParser.Parse(ctx, xa, prm.ParameterType.GetElementType()))
                        .ToArray();
                    var agt = Array.CreateInstance(prm.ParameterType.GetElementType(), ags.Length);
                    Array.Copy(ags, agt, agt.Length);
                    args[prm.Order + 1] = agt;
                    break;
                }
                else
                {
                    if (prm.ParameterType.IsArray)
                        throw new InvalidOperationException("Parameter is not catchall but an array.");

                    if (prm.IsRequired && ctx.RawArguments.Count < prm.Order + 1)
                        throw new ArgumentException(string.Concat("Parameter ", prm.Name, " is required."));
                    else if (!prm.IsRequired && ctx.RawArguments.Count < prm.Order + 1)
                        break;

                    var arg = (ctx.Command.Name.ToLower() == "wiki") ? string.Join(" ", ctx.RawArguments) : ctx.RawArguments[prm.Order];
                    var val = PoE_Bot.CommandManager.ParameterParser.Parse(ctx, arg, prm.ParameterType);
                    args[prm.Order + 1] = val;
                }
            }

            return args;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Command))
                return false;
            return this == (obj as Command);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Command cmd1, Command cmd2)
        {
            var ocmd1 = cmd1 as object;
            var ocmd2 = cmd2 as object;
            if (ocmd1 == null && ocmd2 != null)
                return false;
            if (ocmd1 != null && ocmd2 == null)
                return false;
            if (ocmd1 == null && ocmd2 == null)
                return true;

            return cmd1.Name == cmd2.Name && cmd1.Module.Name == cmd2.Module.Name;
        }

        public static bool operator !=(Command cmd1, Command cmd2)
        {
            return !(cmd1 == cmd2);
        }
    }
}
