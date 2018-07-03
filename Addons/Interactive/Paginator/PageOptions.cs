namespace PoE.Bot.Addons.Interactive.Paginator
{
    using Discord;
    using System;

    public class PageOptions
    {
        public const string FooterFormat = "Page {0}/{1}";
        public TimeSpan? Timeout => TimeSpan.FromMinutes(3);
        public static PageOptions Default => new PageOptions();
        public IEmote Back => Extras.Back;
        public IEmote Next => Extras.Next;
        public IEmote Stop => Extras.Cross;
    }
}