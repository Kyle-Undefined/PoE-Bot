namespace PoE.Bot.Addons.Interactive
{
    using Discord;
    using PoE.Bot.Helpers;
    using System;

    public enum JumpDisplayOptions
    {
        Never,
        WithManageMessages,
        Always
    }

    public class PaginatedAppearanceOptions
    {
        public static PaginatedAppearanceOptions Default = new PaginatedAppearanceOptions();

        public bool DisplayInformationIcon = true;
        public string FooterFormat = "Page {0}/{1}";
        public string InformationText = "This is a paginator. React with the respective icons to change page.";
        public TimeSpan InfoTimeout = TimeSpan.FromSeconds(30);
        public JumpDisplayOptions JumpDisplayOptions = JumpDisplayOptions.WithManageMessages;
        public TimeSpan? Timeout;
        public IEmote Back => EmoteHelper.Back;
        public IEmote First => EmoteHelper.First;
        public IEmote Info => EmoteHelper.Info;
        public IEmote Jump => EmoteHelper.Jump;
        public IEmote Last => EmoteHelper.Last;
        public IEmote Next => EmoteHelper.Next;
        public IEmote Stop => EmoteHelper.Cross;
    }
}