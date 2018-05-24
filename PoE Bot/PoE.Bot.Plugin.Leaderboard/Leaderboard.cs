namespace PoE.Bot.Plugin.Leaderboard
{
    internal class Leaderboard
    {
        public string Variant { get; private set; }
        public ulong ChannelId { get; private set; }
        public bool Enabled { get; private set; }

        public Leaderboard(string Variant, ulong ChannelId, bool Enabled)
        {
            this.Variant = Variant;
            this.ChannelId = ChannelId;
            this.Enabled = Enabled;
        }
    }
}
