using System.Collections.Generic;

namespace PoE.Bot.Config
{
    public class GuildConfig
    {
        public ulong? AllLogChannel { get; internal set; }
        public ulong? ModLogChannel { get; internal set; }
        public ulong? ReportUserChannel { get; internal set; }
        public bool? DeleteCommands { get; internal set; }
        public string CommandPrefix { get; internal set; }
        public ulong? MuteRole { get; internal set; }
        public ulong? PriceCheckerRole { get; internal set; }
        public ulong? RulesChannel { get; internal set; }
        public string Rules { get; internal set; }
        public string Game { get; internal set; }
        internal List<ModAction> ModActions { get; set; }

        public GuildConfig()
        {
            this.ModActions = new List<ModAction>();
        }
    }
}
