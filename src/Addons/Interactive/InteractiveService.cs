namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using Discord.WebSocket;
    using PoE.Bot.Attributes;
    using PoE.Bot.Contexts;
    using Qmmands;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [Service]
    public class InteractiveService : IDisposable
    {
        private readonly Dictionary<ulong, IReactionCallback> _callbacks;
        private readonly TimeSpan _defaultTimeout;

        public InteractiveService(DiscordSocketClient discord, TimeSpan? defaultTimeout = null)
        {
            Discord = discord;
            Discord.ReactionAdded += HandleReactionAsync;

            _callbacks = new Dictionary<ulong, IReactionCallback>();
            _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(15);
        }

        public DiscordSocketClient Discord { get; }

        public void AddReactionCallback(IMessage message, IReactionCallback callback) => _callbacks[message.Id] = callback;

        public void ClearReactionCallbacks() => _callbacks.Clear();

        public void Dispose()
        {
            Discord.ReactionAdded -= HandleReactionAsync;
        }

        public Task<SocketMessage> NextMessageAsync(GuildContext context, bool fromSourceUser = true, bool inSourceChannel = true, TimeSpan? timeout = null)
        {
            var criterion = new Criteria<SocketMessage>();
            if (fromSourceUser)
                criterion.AddCriterion(new EnsureSourceUserCriterion());
            if (inSourceChannel)
                criterion.AddCriterion(new EnsureSourceChannelCriterion());
            return NextMessageAsync(context, criterion, timeout);
        }

        public async Task<SocketMessage> NextMessageAsync(GuildContext context, ICriterion<SocketMessage> criterion, TimeSpan? timeout = null)
        {
            timeout = timeout ?? _defaultTimeout;

            var eventTrigger = new TaskCompletionSource<SocketMessage>();

            async Task Handler(SocketMessage message)
            {
                var result = await criterion.JudgeAsync(context, message).ConfigureAwait(false);
                if (result)
                    eventTrigger.SetResult(message);
            }

            (context.Client).MessageReceived += Handler;

            var trigger = eventTrigger.Task;
            var delay = Task.Delay(timeout.Value);
            var task = await Task.WhenAny(trigger, delay).ConfigureAwait(false);

            (context.Client).MessageReceived -= Handler;

            if (task == trigger)
                return await trigger;
            else
                return null;
        }

        public void RemoveReactionCallback(IMessage message) => RemoveReactionCallback(message.Id);

        public void RemoveReactionCallback(ulong id) => _callbacks.Remove(id);

        public async Task<IUserMessage> ReplyAndDeleteAsync(GuildContext context, string content, bool isTTS = false, Embed embed = null, TimeSpan? timeout = null, RequestOptions options = null)
        {
            timeout = timeout ?? _defaultTimeout;
            var message = await context.Channel.SendMessageAsync(content, isTTS, embed, options);
            _ = Task.Delay(timeout.Value).ContinueWith(_ => message.DeleteAsync());
            return message;
        }

        public async Task<IUserMessage> SendPaginatedMessageAsync(GuildContext context, PaginatedMessage pager, ICriterion<SocketReaction> criterion = null)
        {
            var callback = new PaginatedMessageCallback(this, context, pager, criterion);
            await callback.DisplayAsync();
            return callback.Message;
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Discord.CurrentUser.Id)
                return;
            if (!_callbacks.TryGetValue(message.Id, out var callback))
                return;
            if (!(await callback.Criterion.JudgeAsync(callback.Context, reaction).ConfigureAwait(false)))
                return;
            switch (callback.RunMode)
            {
                case RunMode.Parallel:
                    _ = Task.Run(async () =>
                    {
                        if (await callback.HandleCallbackAsync(reaction))
                            RemoveReactionCallback(message.Id);
                    });
                    break;

                default:
                    if (await callback.HandleCallbackAsync(reaction))
                        RemoveReactionCallback(message.Id);
                    break;
            }
        }
    }
}