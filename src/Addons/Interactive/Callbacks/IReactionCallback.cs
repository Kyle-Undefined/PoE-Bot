namespace PoE.Bot.Addons.Interactive
{
    using Discord.WebSocket;
    using PoE.Bot.Contexts;
    using Qmmands;
    using System;
    using System.Threading.Tasks;

    public interface IReactionCallback
    {
        GuildContext Context { get; }
        ICriterion<SocketReaction> Criterion { get; }
        RunMode RunMode { get; }
        TimeSpan? Timeout { get; }

        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}