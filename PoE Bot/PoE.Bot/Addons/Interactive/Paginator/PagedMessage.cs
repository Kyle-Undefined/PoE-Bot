﻿namespace PoE.Bot.Addons.Interactive.Paginator
{
    using Discord;
    using System.Collections.Generic;

    public class PagedMessage
    {
        public IEnumerable<object> Pages { get; set; }
        public EmbedAuthorBuilder Author { get; set; }
        public PageOptions Options { get; set; } = PageOptions.Default;
    }
}
