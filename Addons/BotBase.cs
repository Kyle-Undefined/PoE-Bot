namespace PoE.Bot.Addons
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Interactive;
    using Interactive.Criterias;
    using Interactive.Paginator;
    using Objects;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class BotBase : ModuleBase<Context>
    {
        public enum CommandAction
        {
            Add,
            Delete,
            List,
            Update,
        }

        public enum DocumentType
        {
            Config,
            None,
            Server
        }

        public InteractiveService Interactive { get; set; }

        public Task<IUserMessage> PagedReplyAsync(IEnumerable<object> pages, string title, bool delete = false)
            => PagedReplyAsync(new PagedMessage
            {
                Pages = pages,
                Author = new EmbedAuthorBuilder { Name = title, IconUrl = Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl() }
            }, delete);

        public async Task<IUserMessage> ReplyAndDeleteAsync(string message, TimeSpan? timeout = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(5);
            IUserMessage msg = await ReplyAsync(message).ConfigureAwait(false);
            _ = Task.Delay(timeout.Value).ContinueWith(_ => msg.DeleteAsync()).ConfigureAwait(false);
            return msg;
        }

        public async Task<IUserMessage> ReplyAsync(string message = null, Embed embed = null, DocumentType save = DocumentType.None)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            SaveDocument(save);
            return await base.ReplyAsync(message, embed: embed).ConfigureAwait(false);
        }

        public Task<SocketMessage> WaitAsync(string message, bool user = true, bool channel = true, TimeSpan? timeout = null)
        {
            _ = ReplyAsync($"{message}\n**To cancel**, type `c`.");
            Criteria<SocketMessage> criteria = new Criteria<SocketMessage>();
            if (user)
                criteria.AddCriteria(new SourceUser());
            if (channel)
                criteria.AddCriteria(new SourceChannel());
            return Interactive.WaitAsync(Context, criteria, timeout);
        }

        protected void SaveDocument(DocumentType document)
        {
            if (document is DocumentType.Config)
                Context.DatabaseHandler.Save<ConfigObject>(Context.Config, "Config");
            if (document is DocumentType.Server)
                Context.DatabaseHandler.Save<GuildObject>(Context.Server, $"{Context.Guild.Id}");
        }

        private Task<IUserMessage> PagedReplyAsync(PagedMessage paged, bool delete, bool sourceUser = true)
        {
            Criteria<SocketReaction> criteria = new Criteria<SocketReaction>();
            if (sourceUser)
                criteria.AddCriteria(new ReactionCriteria());
            return Interactive.PagedMessageAsync(Context, paged, delete, criteria);
        }

        private Task<IUserMessage> PagedReplyAsync(PagedMessage paged, ICriteria<SocketReaction> criteria, bool delete)
           => Interactive.PagedMessageAsync(Context, paged, delete, criteria);
    }
}