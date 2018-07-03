namespace PoE.Bot.Addons.Interactive.Paginator
{
    using Criterias;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class PagedCallback : IReactionCallback
    {
        private readonly PagedMessage pagedMessage;
        private readonly int pages;
        private int page = 1;

        public PagedCallback(InteractiveService service, Context context, PagedMessage paged, ICriteria<SocketReaction> criteria = null)
        {
            pagedMessage = paged;
            Context = context;
            Interactive = service;
            pages = paged.Pages.Count();
            Criteria = criteria ?? new EmptyCriteria<SocketReaction>();
        }

        public Context Context { get; }
        public ICriteria<SocketReaction> Criteria { get; }

        private Embed Embed
            => Extras.Embed(Extras.Info)
            .WithAuthor(pagedMessage.Author)
            .WithDescription($"{pagedMessage.Pages.ElementAt(page - 1)}")
            .WithFooter(x => x.Text = string.Format(PageOptions.FooterFormat, page, pages)).Build();

        public InteractiveService Interactive { get; private set; }
        public IUserMessage Message { get; private set; }

        private PageOptions Options => pagedMessage.Options;
        public RunMode RunMode => RunMode.Sync;
        private TimeSpan? Timeout => Options.Timeout;

        public async Task DisplayAsync(PagedMessage paged, bool delete)
        {
            IUserMessage message = await Context.Channel.SendMessageAsync(embed: Embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);

            _ = Task.Run(async () =>
            {
                if (paged.Pages.Count() > 1)
                {
                    await message.AddReactionAsync(Options.Back);
                    await message.AddReactionAsync(Options.Next);
                }
                await message.AddReactionAsync(Options.Stop);
            });

            _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
            {
                Interactive.RemoveReactionCallback(message);
                if (delete)
                    Message.DeleteAsync();
                else
                    Message.RemoveAllReactionsAsync();
            });
        }

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            IEmote emote = reaction.Emote;
            if (emote.Equals(Options.Next))
            {
                if (page >= pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(Options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(Options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        private async Task RenderAsync()
            => await Message.ModifyAsync(x => x.Embed = Embed).ConfigureAwait(false);
    }
}