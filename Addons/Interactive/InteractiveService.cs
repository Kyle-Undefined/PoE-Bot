namespace PoE.Bot.Addons.Interactive
{
    using System;
    using Discord;
    using System.Linq;
    using System.Threading;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using PoE.Bot.Addons.Interactive.Criterias;
    using PoE.Bot.Addons.Interactive.Paginator;

    public class InteractiveService : IDisposable
    {
        TimeSpan Timeout { get; }
        DiscordSocketClient Client { get; }
        Dictionary<ulong, IReactionCallback> Callbacks { get; }
        public InteractiveService(DiscordSocketClient client, TimeSpan? timeout = null)
        {
            Client = client;
            Client.ReactionAdded += ReactionAddedAsync;
            Timeout = timeout ?? TimeSpan.FromSeconds(15);
            Callbacks = new Dictionary<ulong, IReactionCallback>();
        }

        public async Task<SocketMessage> WaitAsync(IContext Context, ICriteria<SocketMessage> Criteria, TimeSpan? Timeout = null)
        {
            Timeout = Timeout ?? TimeSpan.FromSeconds(15);
            var CancelTimeout = Timeout.Value + TimeSpan.FromMinutes(1);
            var TokenSource = new CancellationTokenSource(CancelTimeout);
            var Client = Context.Client as DiscordSocketClient;
            var CancelTask = new TaskCompletionSource<bool>();
            var Trigger = new TaskCompletionSource<SocketMessage>();
            TokenSource.Token.Register(() => CancelTask.SetResult(true));
            async Task InteractiveHandlerAsync(SocketMessage Message)
            {
                if (Message.Author.IsBot)
                    return;
                var Result = await Criteria.JudgeAsync(Context, Message).ConfigureAwait(false);
                if (Result)
                    Trigger.SetResult(Message);
            }
            Client.MessageReceived += InteractiveHandlerAsync;
            var PersonalTask = await Task.WhenAny(Trigger.Task, Task.Delay(Timeout.Value), CancelTask.Task).ConfigureAwait(false);
            Client.MessageReceived -= InteractiveHandlerAsync;
            return PersonalTask == Trigger.Task ? await Trigger.Task.ConfigureAwait(false) : null;
        }

        public async Task<IUserMessage> PagedMessageAsync(IContext Context, PagedMessage Paged, bool Delete, ICriteria<SocketReaction> Criteria = null)
        {
            var callback = new PagedCallback(this, Context, Paged, Criteria);
            await callback.DisplayAsync(Paged, Delete).ConfigureAwait(false);
            return callback.Message;
        }

        async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> Cache, ISocketMessageChannel Channel, SocketReaction Reaction)
        {
            var EmoteArray = new[] { Extras.Next, Extras.Back, Extras.Cross };
            if (Reaction.UserId == Client.CurrentUser.Id || !(Callbacks.TryGetValue(Cache.Id, out var callback)) ||
                !(await callback.Criteria.JudgeAsync(callback.Context, Reaction).ConfigureAwait(false)) ||
                !EmoteArray.Any(x => x.Name == Reaction.Emote.Name))
                return;
            switch (callback.RunMode)
            {
                case RunMode.Async:
                    _ = Task.Run(async () =>
                    {
                        if (await callback.HandleCallbackAsync(Reaction).ConfigureAwait(false))
                            RemoveReactionCallback(Cache.Id);
                    });
                    break;
                default:
                    if (await callback.HandleCallbackAsync(Reaction).ConfigureAwait(false))
                        RemoveReactionCallback(Cache.Id);
                    break;
            }
        }

        public void RemoveReactionCallback(ulong id) => Callbacks.Remove(id);

        public void RemoveReactionCallback(IMessage message) => RemoveReactionCallback(message.Id);

        public void AddReactionCallback(IMessage message, IReactionCallback callback) => Callbacks[message.Id] = callback;

        public void Dispose()
        {
            Client.ReactionAdded -= ReactionAddedAsync;
        }
    }
}
