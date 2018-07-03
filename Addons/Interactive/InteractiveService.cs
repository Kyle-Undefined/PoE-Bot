namespace PoE.Bot.Addons.Interactive
{
    using Criterias;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Paginator;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class InteractiveService : IDisposable
    {
        public InteractiveService(DiscordSocketClient client)
        {
            Client = client;
            Client.ReactionAdded += ReactionAddedAsync;
            Callbacks = new Dictionary<ulong, IReactionCallback>();
        }

        private Dictionary<ulong, IReactionCallback> Callbacks { get; }
        private DiscordSocketClient Client { get; }

        public void AddReactionCallback(IMessage message, IReactionCallback callback)
            => Callbacks[message.Id] = callback;

        public void Dispose()
            => Client.ReactionAdded -= ReactionAddedAsync;

        public async Task<IUserMessage> PagedMessageAsync(Context context, PagedMessage paged, bool delete, ICriteria<SocketReaction> criteria = null)
        {
            PagedCallback callback = new PagedCallback(this, context, paged, criteria);
            await callback.DisplayAsync(paged, delete).ConfigureAwait(false);
            return callback.Message;
        }

        public void RemoveReactionCallback(IMessage message)
            => RemoveReactionCallback(message.Id);

        public async Task<SocketMessage> WaitAsync(Context context, ICriteria<SocketMessage> criteria, TimeSpan? timeout = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(15);
            TimeSpan cancelTimeout = timeout.Value + TimeSpan.FromMinutes(1);
            CancellationTokenSource tokenSource = new CancellationTokenSource(cancelTimeout);
            TaskCompletionSource<bool> cancelTask = new TaskCompletionSource<bool>();
            TaskCompletionSource<SocketMessage> trigger = new TaskCompletionSource<SocketMessage>();
            tokenSource.Token.Register(() => cancelTask.SetResult(true));

            async Task InteractiveHandlerAsync(SocketMessage message)
            {
                if (message.Author.IsBot)
                    return;

                bool result = await criteria.JudgeAsync(context, message).ConfigureAwait(false);
                if (result)
                    trigger.SetResult(message);
            }

            Client.MessageReceived += InteractiveHandlerAsync;
            Task personalTask = await Task.WhenAny(trigger.Task, Task.Delay(timeout.Value, tokenSource.Token), cancelTask.Task).ConfigureAwait(false);
            Client.MessageReceived -= InteractiveHandlerAsync;
            return personalTask == trigger.Task
                ? await trigger.Task.ConfigureAwait(false)
                : null;
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Emoji[] emoteArray = new[] { Extras.Next, Extras.Back, Extras.Cross };
            if (reaction.UserId == Client.CurrentUser.Id || !(Callbacks.TryGetValue(cache.Id, out IReactionCallback callback)) ||
                !await callback.Criteria.JudgeAsync(callback.Context, reaction).ConfigureAwait(false) || emoteArray.All(x => x.Name != reaction.Emote.Name))
                return;

            switch (callback.RunMode)
            {
                case RunMode.Async:
                    _ = Task.Run(async () =>
                    {
                        if (await callback.HandleCallbackAsync(reaction).ConfigureAwait(false))
                            RemoveReactionCallback(cache.Id);
                    });
                    break;

                default:
                    if (await callback.HandleCallbackAsync(reaction).ConfigureAwait(false))
                        RemoveReactionCallback(cache.Id);
                    break;
            }
        }

        private void RemoveReactionCallback(ulong id)
             => Callbacks.Remove(id);
    }
}