namespace PoE.Bot.Objects
{
    using System.Collections.Generic;

    public class ConfigObject
    {
        public Dictionary<string, string> APIKeys { get; set; } = new Dictionary<string, string>();
        public List<ulong> Blacklist { get; } = new List<ulong>();
        public ulong FeedbackChannel { get; set; }
        public IList<string> Namespaces { get; } = new List<string>();
        public string Prefix { get; set; }
    }
}