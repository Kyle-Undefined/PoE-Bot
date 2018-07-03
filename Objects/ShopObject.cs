namespace PoE.Bot.Objects
{
    public enum Leagues
    {
        Challenge,
        ChallengeHC,
        Hardcore,
        Standard
    }

    public class ShopObject
    {
        public string Item { get; set; }
        public Leagues League { get; set; }
        public ulong UserId { get; set; }
    }
}