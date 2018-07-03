namespace PoE.Bot.Objects
{
    using System.Collections.Generic;

    public class RuleObject
    {
        public string Description { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
        public int TotalFields { get; set; }
    }
}