namespace PoE.Bot.Addons.Interactive
{
    using Criterias;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public interface IReactionCallback
    {
        Context Context { get; }
        ICriteria<SocketReaction> Criteria { get; }
        RunMode RunMode { get; }

        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}