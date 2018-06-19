namespace PoE.Bot.Objects
{
    using System.IO;

    public class DatabaseObject
    {
#if DEBUG
        public string Name = "PoE_Bot_Test";
#else
        public string Name = "PoE_Bot";
#endif
        public bool IsConfigCreated = false;
        public string FullBackup = "0 * * * *";
        public string URL = "http://127.0.0.1:4009";
        public string IncrementalBackup = "";
        public string BackupFolder { get => Directory.CreateDirectory("Backup").FullName; }
    }
}
