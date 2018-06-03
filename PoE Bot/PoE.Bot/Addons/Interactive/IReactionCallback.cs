namespace PoE.Bot.Addons.Interactive
{
    using System;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Addons.Interactive.Criterias;

    public interface IReactionCallback
    {
        IContext Context { get; }
        TimeSpan? Timeout { get; }
        RunMode RunMode { get; }
        ICriteria<SocketReaction> Criteria { get; }
        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}
