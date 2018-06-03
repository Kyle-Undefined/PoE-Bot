namespace PoE.Bot.Handlers.Objects
{
    using System;

    public class CaseObject
    {
        public int Number { get; set; }
        public ulong UserId { get; set; }
        public string Reason { get; set; }
        public string Username { get; set; }
        public ulong MessageId { get; set; }
        public string Moderator { get; set; }
        public CaseType CaseType { get; set; }
        public DateTime CaseDate { get; set; }
        public ulong ModeratorId { get; set; }
    }

    public enum CaseType
    {
        BAN,
        KICK,
        BANS,
        KICKS,
        SOFTBAN,
        MUTE,
        WARNING,
        AUTOMODKICK,
        AUTOMODMUTE,
        PURGE
    }
}
