namespace PoE.Bot.Handlers.Objects
{
    using System.IO;

    public class DBObject
    {
#if DEBUG
        public string Name = "PoE_Bot_Test";
#else
        public string Name = "PoE_Bot";
#endif
        public bool IsConfigCreated = false;
        public string FullBackup = "*/10 * * * *";
        public string URL = "http://127.0.0.1:4009";
        public string IncrementalBackup = "0 2 * * *";
        public string BackupFolder { get => Directory.CreateDirectory("Backup").FullName; }
    }
}
