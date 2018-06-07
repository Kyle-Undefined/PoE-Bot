namespace PoE.Bot.Objects
{
    using System.Collections.Generic;

    public class ConfigObject
    {
        public string Id { get => "Config"; }
        public string Prefix { get; set; }
        public ulong ReportChannel { get; set; }
        public List<ulong> Blacklist { get; set; } = new List<ulong>();
        public IList<string> Namespaces { get; set; } = new List<string>();
        public Dictionary<string, string> APIKeys { get; set; } = new Dictionary<string, string>();
    }
}
