using System;

namespace PoE.Bot.Config
{
    public class ModAction
    {
        public ulong UserId { get; internal set; }
        public ulong Issuer { get; internal set; }
        public DateTime Until { get; internal set; }
        public DateTime Issued { get; internal set; }
        public ModActionType ActionType { get; internal set; }
        public string Reason { get; internal set; }

        public ModAction()
        {
            this.Issued = DateTime.UtcNow;
        }
    }
}
