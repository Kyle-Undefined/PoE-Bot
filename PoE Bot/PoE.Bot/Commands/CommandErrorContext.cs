using System;

namespace PoE.Bot.Commands
{
    public class CommandErrorContext
    {
        /// <summary>
        /// Gets the context of this command's execution.
        /// </summary>
        public CommandContext Context { get; internal set; }

        /// <summary>
        /// Gets the exception that occured during execution.
        /// </summary>
        public Exception Exception { get; internal set; }
    }
}
