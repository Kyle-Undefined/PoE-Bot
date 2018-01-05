using System.Collections.Generic;
using Discord;

namespace PoE.Bot.Commands
{
    public class CommandContext
    {
        /// <summary>
        /// Gets the message that invoked the command.
        /// </summary>
        public IMessage Message { get; internal set; }

        /// <summary>
        /// Gets the command that was invoked.
        /// </summary>
        public Command Command { get; internal set; }

        /// <summary>
        /// Gets the collection of raw arguments passed to the command.
        /// </summary>
        public IReadOnlyList<string> RawArguments { get; internal set; }

        /// <summary>
        /// Gets the user who invoked the command.
        /// </summary>
        public IGuildUser User { get { return this.Message.Author as IGuildUser; } }

        /// <summary>
        /// Gets the channel the command was invoked in.
        /// </summary>
        public IMessageChannel Channel { get { return this.Message.Channel; } }

        /// <summary>
        /// Gets the guild that the command was invoked in.
        /// </summary>
        public IGuild Guild { get { var ch = this.Channel as IGuildChannel; if (ch != null) return ch.Guild; return null; } }
    }
}
