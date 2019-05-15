namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using System.Collections.Generic;

    public class PaginatedMessage
    {
        public EmbedAuthorBuilder Author { get; set; } = null;
        public Color Color { get; set; } = Color.Default;
        public string Content { get; set; } = "";
        public PaginatedAppearanceOptions Options { get; set; } = PaginatedAppearanceOptions.Default;
        public IEnumerable<object> Pages { get; set; }
        public string Title { get; set; } = "";
    }
}