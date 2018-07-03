namespace PoE.Bot.Handlers
{
    using Newtonsoft.Json;
    using Objects;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Backups;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum Operation
    {
        Create,
        Delete,
        Load
    }

    public class DatabaseHandler
    {
        public IDocumentStore Store { get; set; }
        private DatabaseObject DatabaseObject { get; set; }

        public T Execute<T>(Operation Operation, object Data = null, object Id = null) where T : class
        {
            using (var Session = Store.OpenSession(Store.Database))
            {
                switch (Operation)
                {
                    case Operation.Create:
                        if (Session.Advanced.Exists($"{Id}"))
                            break;

                        Session.Store((T)Data, $"{Id}");
                        LogHandler.Write(Source.Database, $"Created => {typeof(T).Name} | ID: {Id}");
                        break;

                    case Operation.Delete:
                        LogHandler.Write(Source.Database, $"Removed => {typeof(T).Name} | ID: {Id}");
                        Session.Delete(Session.Load<T>($"{Id}"));
                        break;

                    case Operation.Load:
                        return Session.Load<T>($"{Id}");
                }
                Session.SaveChanges();
                Session.Dispose();
            }
            return default;
        }

        public async Task InitializeAsync()
        {
            LogHandler.PrintApplicationInformation();
            if (Process.GetProcesses().FirstOrDefault(x => x.ProcessName is "Raven.Server") is null)
                await LogHandler.CriticalFail(Source.Exception, "Please make sure RavenDB is running.");

            if (File.Exists("DBConfig.json"))
                DatabaseObject = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("DBConfig.json"));
            else
            {
                File.WriteAllText("DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject(), Formatting.Indented), Encoding.UTF8);
                DatabaseObject = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("DBConfig.json"));
            }
            Store = new Lazy<IDocumentStore>(() => new DocumentStore { Database = DatabaseObject.Name, Urls = new[] { DatabaseObject.URL } }.Initialize(), true).Value;

            if (Store is null)
                await LogHandler.CriticalFail(Source.Exception, "Failed to build document store.");

            if (!Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).Any(x => x is DatabaseObject.Name))
                Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(DatabaseObject.Name)));

            DatabaseRecordWithEtag Record = Store.Maintenance.Server.Send(new GetDatabaseRecordOperation(DatabaseObject.Name));
            if (!Record.PeriodicBackups.All(x => x.Name is "Backup"))
                Store.Maintenance.Send(new UpdatePeriodicBackupOperation(new PeriodicBackupConfiguration
                {
                    Name = "Backup",
                    BackupType = BackupType.Backup,
                    FullBackupFrequency = DatabaseObject.FullBackup,
                    LocalSettings = new LocalSettings { FolderPath = DatabaseObject.BackupFolder }
                }));

            Store.AggressivelyCacheFor(TimeSpan.FromMinutes(5), Record.DatabaseName);
            Store.AggressivelyCache(Record.DatabaseName);

            if (DatabaseObject.IsConfigCreated is false)
            {
                LogHandler.Write(Source.Database, "Enter bot's token: ");
                string Token = Console.ReadLine();
                LogHandler.Write(Source.Database, "Enter bot's prefix: ");
                string Prefix = Console.ReadLine();

                Execute<ConfigObject>(Operation.Create, new ConfigObject
                {
                    Prefix = Prefix,
                    APIKeys = new Dictionary<string, string> { { "BT", Token }, { "TC", "" }, { "TA", "" } }
                }, "Config");

                File.WriteAllText("DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject { IsConfigCreated = true }, Formatting.Indented));
            }

            DatabaseObject = null;
            LogHandler.ForceGC();
        }

        public void Save<T>(object Data, object Id) where T : class
        {
            using (var Session = Store.OpenSession())
            {
                Session.Store((T)Data, $"{Id}");
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
}