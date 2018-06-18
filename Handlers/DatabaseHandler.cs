namespace PoE.Bot.Handlers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Raven.Client.Documents;
    using PoE.Bot.Objects;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;
    using Raven.Client.Documents.Operations.Backups;
    using System.Collections.Generic;

    public class DatabaseHandler
    {
        DatabaseObject Settings { get; set; }
        public IDocumentStore Store { get; set; }

        public async Task InitializeAsync()
        {
            LogHandler.PrintApplicationInformation();
            if (Process.GetProcesses().FirstOrDefault(x => x.ProcessName is "Raven.Server") is null)
                await LogHandler.CriticalFail(Source.EXC, $"Please make sure RavenDB is running.");
            if (File.Exists("DBConfig.json")) Settings = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("DBConfig.json"));
            else
            {
                File.WriteAllText("DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject(), Formatting.Indented), Encoding.UTF8);
                Settings = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("DBConfig.json"));
            }
            Store = new Lazy<IDocumentStore>(() => new DocumentStore { Database = Settings.Name, Urls = new[] { Settings.URL } }.Initialize(), true).Value;
            if (Store is null)
                await LogHandler.CriticalFail(Source.EXC, $"Failed to build document store.");

            if (!Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).Any(x => x == Settings.Name))
                Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Settings.Name)));

            var Record = Store.Maintenance.Server.Send(new GetDatabaseRecordOperation(Settings.Name));
            if (!Record.PeriodicBackups.Any(x => x.Name is "Backup"))
                Store.Maintenance.Send(new UpdatePeriodicBackupOperation(new PeriodicBackupConfiguration
                {
                    Name = "Backup",
                    BackupType = BackupType.Backup,
                    FullBackupFrequency = Settings.FullBackup,
                    IncrementalBackupFrequency = Settings.IncrementalBackup,
                    LocalSettings = new LocalSettings { FolderPath = Settings.BackupFolder }
                }));

            if (Settings.IsConfigCreated is false)
            {
                LogHandler.Write(Source.DTB, $"Enter bot's token: ");
                var Token = Console.ReadLine();
                LogHandler.Write(Source.DTB, $"Enter bot's prefix: ");
                var Prefix = Console.ReadLine();
                Execute<ConfigObject>(Operation.CREATE, new ConfigObject { Prefix = Prefix, APIKeys = new Dictionary<string, string> { { "BT", Token }, { "TC", "" }, { "TA", "" } } }, "Config");
                File.WriteAllText("DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject { IsConfigCreated = true }, Formatting.Indented));
            }
            Settings = null;
            LogHandler.ForceGC();
        }

        public T Execute<T>(Operation Operation, object Data = null, object Id = null) where T : class
        {
            using (var Session = Store.OpenSession(Store.Database))
            {
                switch (Operation)
                {
                    case Operation.CREATE:
                        if (Session.Advanced.Exists($"{Id}"))
                            break;
                        Session.Store((T)Data, $"{Id}");
                        LogHandler.Write(Source.DTB, $"Created => {typeof(T).Name} | ID: {Id}");
                        break;

                    case Operation.DELETE:
                        LogHandler.Write(Source.DTB, $"Removed => {typeof(T).Name} | ID: {Id}");
                        Session.Delete<T>(Session.Load<T>($"{Id}")); break;
                    case Operation.LOAD: return Session.Load<T>($"{Id}");
                }
                Session.SaveChanges();
                Session.Dispose();
            }
            return default;
        }

        public void Save<T>(object Data, object Id) where T : class
        {
            using (var Session = Store.OpenSession())
            {
                var Load = Session.Load<T>($"{Id}");
                if (Load == Data)
                    return;
                Load = (T)Data;
                Session.SaveChanges();
            }
        }

        public GuildObject[] Servers()
        {
            using (var Session = Store.OpenSession(Store.Database))
                return Session.Query<GuildObject>().Customize(
                    x => x.NoCaching()).Customize(
                    x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5))).ToArray();
        }
    }

    public enum Operation
    {
        LOAD,
        DELETE,
        CREATE
    }
}
