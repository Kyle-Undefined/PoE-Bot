namespace PoE.Bot.Addons.Interactive
{
    using Discord.Commands;

    public class OkResult : RuntimeResult
    {
        public OkResult(string reason = null) : base(null, reason) { }
    }
}
