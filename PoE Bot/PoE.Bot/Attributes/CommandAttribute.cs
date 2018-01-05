using System;
using PoE.Bot.Commands.Permissions;

namespace PoE.Bot.Attributes
{
    /// <summary>
    /// Indicates that this method is a discord command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Gets the command's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the command's description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets or sets whether to check permissions for the command.
        /// </summary>
        public bool CheckPermissions { get; set; }

        /// <summary>
        /// Gets or sets the ID of the permissions checker.
        /// </summary>
        public string CheckerId { get; set; }

        /// <summary>
        /// Gets or sets the command's aliases. Format like so:
        /// alias1;alias2;alias3
        /// </summary>
        public string Aliases { get; set; }

        /// <summary>
        /// Gets or sets the permission required to run this command.
        /// </summary>
        public Permission RequiredPermission { get; set; }

        /// <summary>
        /// Creates a new instance of the Command attriubte.
        /// </summary>
        /// <param name="name">Command's name.</param>
        /// <param name="description">Command's description.</param>
        public CommandAttribute(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }
    }
}
