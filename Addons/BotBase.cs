namespace PoE.Bot.Addons
{
    using System;
    using Discord;
    using PoE.Bot.Handlers;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using PoE.Bot.Objects;
    using PoE.Bot.Addons.Interactive;
    using System.Collections.Generic;
    using PoE.Bot.Addons.Interactive.Criterias;
    using PoE.Bot.Addons.Interactive.Paginator;

    public class BotBase : ModuleBase<IContext>
    {
        public InteractiveService Interactive { get; set; }
        public async Task<IUserMessage> ReplyAsync(string Message = null, Embed Embed = null, char Save = 'n')
        {
            await Context.Channel.TriggerTypingAsync();
            SaveDocument(Save);
            return await base.ReplyAsync(Message, embed: Embed);
        }

        public async Task<IUserMessage> ReplyAndDeleteAsync(string Message, TimeSpan? Timeout = null)
        {
            Timeout = Timeout ?? TimeSpan.FromSeconds(5);
            var Msg = await ReplyAsync(Message).ConfigureAwait(false);
            _ = Task.Delay(Timeout.Value).ContinueWith(_ => Msg.DeleteAsync()).ConfigureAwait(false);
            return Msg;
        }

        public Task<SocketMessage> WaitAsync(string Message, bool User = true, bool Channel = true, TimeSpan? Timeout = null)
        {
            _ = ReplyAsync($"{Message}\n**To cancel**, type `c`.");
            var Criteria = new Criteria<SocketMessage>();
            if (User)
                Criteria.AddCriteria(new SourceUser());
            if (Channel)
                Criteria.AddCriteria(new SourceChannel());
            return Interactive.WaitAsync(Context, Criteria, Timeout);
        }

        public Task<IUserMessage> PagedReplyAsync(IEnumerable<object> Pages, string Title, bool Delete = false)
            => PagedReplyAsync(new PagedMessage
            {
                Pages = Pages,
                Author = new EmbedAuthorBuilder
                {
                    Name = Title,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                }
            }, Delete, true);

        Task<IUserMessage> PagedReplyAsync(PagedMessage Paged, bool Delete, bool SourceUser = true)
        {
            var criteria = new Criteria<SocketReaction>();
            if (SourceUser)
                criteria.AddCriteria(new ReactionCriteria());
            return Interactive.PagedMessageAsync(Context, Paged, Delete, criteria);
        }

        Task<IUserMessage> PagedReplyAsync(PagedMessage Paged, ICriteria<SocketReaction> Criteria, bool Delete)
           => Interactive.PagedMessageAsync(Context, Paged, Delete, Criteria);

        public void SaveDocument(char Document)
        {
            switch (Document)
            {
                case 'c': Context.DBHandler.Save<ConfigObject>(Context.Config, "Config"); break;
                case 's': Context.DBHandler.Save<GuildObject>(Context.Server, $"{Context.Guild.Id}"); break;
                case 'n': break;
            }
        }
    }
}
