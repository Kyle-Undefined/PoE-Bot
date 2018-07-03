namespace PoE.Bot.Objects
{
    using System.IO;

    public class DatabaseObject
    {
#if DEBUG
        public const string Name = "PoE_Bot_Test";
#else
        public const string Name = "PoE_Bot";
#endif
        public string FullBackup = "0 03 * * * ";
        public string URL = "http://127.0.0.1:4009";
        public bool IsConfigCreated = false;
        public string BackupFolder => Directory.CreateDirectory("Backup").FullName;
    }
}