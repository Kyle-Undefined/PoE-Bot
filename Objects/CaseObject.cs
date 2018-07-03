namespace PoE.Bot.Objects
{
    using System;

    public enum CaseType
    {
        AutoModMute,
        AutoModPermMute,
        Ban,
        Bans,
        Kick,
        Kicks,
        Mute,
        Purge,
        Softban,
        Warning
    }

    public class CaseObject
    {
        public DateTime CaseDate { get; set; }
        public CaseType CaseType { get; set; }
        public ulong MessageId { get; set; }
        public string Moderator { get; set; }
        public ulong ModeratorId { get; set; }
        public int Number { get; set; }
        public string Reason { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; }
    }
}