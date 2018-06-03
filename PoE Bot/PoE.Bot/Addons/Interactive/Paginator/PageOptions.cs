namespace PoE.Bot.Addons.Interactive.Paginator
{
    using System;
    using Discord;

    public class PageOptions
    {
        public IEmote Back => Extras.Back;
        public IEmote Next => Extras.Next;
        public IEmote Stop => Extras.Cross;
        public string FooterFormat = "Page {0}/{1}";
        public TimeSpan? Timeout = TimeSpan.FromMinutes(3);
        public static PageOptions Default { get => new PageOptions(); }
    }
}
