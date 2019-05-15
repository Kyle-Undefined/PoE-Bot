namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using Discord.WebSocket;
    using PoE.Bot.Contexts;
    using Qmmands;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class PaginatedMessageCallback : IReactionCallback
    {
        private readonly PaginatedMessage _pager;
        private readonly int _pages;
        private int page = 1;

        public PaginatedMessageCallback(InteractiveService interactive,
            GuildContext context,
            PaginatedMessage pager,
            ICriterion<SocketReaction> criterion = null)
        {
            Interactive = interactive;
            Context = context;
            Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
            _pager = pager;
            _pages = _pager.Pages.Count();
        }

        public GuildContext Context { get; }
        public ICriterion<SocketReaction> Criterion { get; }
        public InteractiveService Interactive { get; }
        public IUserMessage Message { get; private set; }

        private PaginatedAppearanceOptions Options => _pager.Options;
        public RunMode RunMode => RunMode.Parallel;
        public TimeSpan? Timeout => Options.Timeout;

        public async Task DisplayAsync()
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(_pager.Content, embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(Options.First);
                await message.AddReactionAsync(Options.Back);
                await message.AddReactionAsync(Options.Next);
                await message.AddReactionAsync(Options.Last);

                var manageMessages = Context.Channel is IGuildChannel guildChannel && (Context.User as IGuildUser)?.GetPermissions(guildChannel).ManageMessages is true;

                if (Options.JumpDisplayOptions == JumpDisplayOptions.Always || (Options.JumpDisplayOptions == JumpDisplayOptions.WithManageMessages && manageMessages))
                    await message.AddReactionAsync(Options.Jump);

                await message.AddReactionAsync(Options.Stop);

                if (Options.DisplayInformationIcon)
                    await message.AddReactionAsync(Options.Info);
            });
            // TODO: (Next major version) timeouts need to be handled at the service-level!
            if (Timeout.HasValue && Timeout.Value != default)
            {
                _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
                {
                    Interactive.RemoveReactionCallback(message);
                    _ = Message.DeleteAsync();
                });
            }
        }

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(Options.First))
            {
                page = 1;
            }
            else if (emote.Equals(Options.Next))
            {
                if (page >= _pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(Options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(Options.Last))
            {
                page = _pages;
            }
            else if (emote.Equals(Options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            else if (emote.Equals(Options.Jump))
            {
                _ = Task.Run(async () =>
                {
                    var criteria = new Criteria<SocketMessage>()
                        .AddCriterion(new EnsureSourceChannelCriterion())
                        .AddCriterion(new EnsureFromUserCriterion(reaction.UserId))
                        .AddCriterion(new EnsureIsIntegerCriterion());
                    var response = await Interactive.NextMessageAsync(Context, criteria, TimeSpan.FromSeconds(15));
                    var request = int.Parse(response.Content);
                    if (request < 1 || request > _pages)
                    {
                        _ = response.DeleteAsync();
                        await Interactive.ReplyAndDeleteAsync(Context, Options.Stop.Name);
                        return;
                    }
                    page = request;
                    _ = response.DeleteAsync();
                    await RenderAsync();
                });
            }
            else if (emote.Equals(Options.Info))
            {
                await Interactive.ReplyAndDeleteAsync(Context, Options.InformationText, timeout: Options.InfoTimeout);
                return false;
            }
            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync();
            return false;
        }

        protected Embed BuildEmbed()
        {
            return new EmbedBuilder()
                .WithAuthor(_pager.Author)
                .WithColor(_pager.Color)
                .WithDescription(_pager.Pages.ElementAt(page - 1).ToString())
                .WithFooter(f => f.Text = string.Format(Options.FooterFormat, page, _pages))
                .WithTitle(_pager.Title)
                .Build();
        }

        private async Task RenderAsync()
        {
            var embed = BuildEmbed();
            await Message.ModifyAsync(m => m.Embed = embed);
        }
    }
}