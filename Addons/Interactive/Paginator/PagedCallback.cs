namespace PoE.Bot.Addons.Interactive.Paginator
{
    using System;
    using Discord;
    using System.Linq;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using Drawing = System.Drawing.Color;
    using PoE.Bot.Addons.Interactive.Criterias;

    public class PagedCallback : IReactionCallback
    {
        int Page = 1;
        readonly int Pages;
        public IContext Context { get; }
        readonly PagedMessage Paged;
        PageOptions Options => Paged.Options;
        public RunMode RunMode => RunMode.Sync;
        public TimeSpan? Timeout => Options.Timeout;
        public IUserMessage Message { get; private set; }
        public ICriteria<SocketReaction> Criteria { get; }
        public InteractiveService Interactive { get; private set; }

        public PagedCallback(InteractiveService service, IContext context, PagedMessage paged, ICriteria<SocketReaction> criteria = null)
        {
            Paged = paged;
            Context = context;
            Interactive = service;
            Pages = Paged.Pages.Count();
            Criteria = criteria ?? new EmptyCriteria<SocketReaction>();
        }

        public async Task DisplayAsync(PagedMessage Paged, bool Delete)
        {
            var message = await Context.Channel.SendMessageAsync(embed: Embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            _ = Task.Run(async () =>
            {
                if (Paged.Pages.Count() > 1)
                {
                    await message.AddReactionAsync(Options.Back);
                    await message.AddReactionAsync(Options.Next);
                }
                await message.AddReactionAsync(Options.Stop);
            });

            _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
            {
                Interactive.RemoveReactionCallback(message);
                _ = Delete ? Message.DeleteAsync() : Message.RemoveAllReactionsAsync();
            });
        }

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;
            if (emote.Equals(Options.Next))
            {
                if (Page >= Pages)
                    return false;
                ++Page;
            }
            else if (emote.Equals(Options.Back))
            {
                if (Page <= 1)
                    return false;
                --Page;
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

        Embed Embed => Extras.Embed(Drawing.Aqua)
            .WithAuthor(Paged.Author)
            .WithDescription($"{Paged.Pages.ElementAt(Page - 1)}")
            .WithFooter(x => x.Text = string.Format(Options.FooterFormat, Page, Pages)).Build();

        private async Task RenderAsync()
            => await Message.ModifyAsync(x => x.Embed = Embed).ConfigureAwait(false);
    }
}
