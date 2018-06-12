namespace PoE.Bot.Objects
{
    public class ShopObject
    {
        public ulong UserId { get; set; }
        public Leagues League { get; set; }
        public string Item { get; set; }
    }

    public enum Leagues
    {
        Standard,
        Hardcore,
        Challenge,
        ChallengeHC
    }
}
