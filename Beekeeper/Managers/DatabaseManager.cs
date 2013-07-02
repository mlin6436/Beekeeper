using System;
using System.Data;
using System.Data.SqlClient;
using Beekeeper.Entities;
using Beekeeper.Interfaces;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Beekeeper.Managers
{
    public class DatabaseManager : IDatabaseManager
    {
        public Server Server { get; set; }

        public DatabaseManager(string serverName, string login = null, string password = null)
        {
            var connection = new ServerConnection(serverName)
            {
                LoginSecure = true
            };

            if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
            {
                connection.Login = login;
                connection.Password = password;
            }

            Server = new Server(connection);
        }

        public DatabaseManager(string serverName, string databaseName, string login = null, string password = null)
        {
            var connection = new ServerConnection(serverName)
            {
                DatabaseName = databaseName,
                LoginSecure = true
            };

            if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
            {
                connection.Login = login;
                connection.Password = password;
            }

            Server = new Server(connection);
        }

        public void CreateDatabase(string databaseName)
        {
            var exsitingDatabase = Server.Databases[databaseName];
            
            if (exsitingDatabase != null)
            {
                DropDatabase(databaseName);
            }

            var database = new Database(Server, databaseName);
            database.Create();
        }

        public void DropDatabase(string databaseName)
        {
            var database = Server.Databases[databaseName];
            Server.KillAllProcesses(databaseName);
            Server.KillDatabase(databaseName);
            database.Drop();
        }

        public void RestoreDatabase(DatabaseRestoreSettings settings)
        {
            var restore = new Restore
            {
                Database = settings.DatabaseName,
                Action = RestoreActionType.Database,
                ReplaceDatabase = true,
            };

            restore.Devices.AddDevice(settings.BackupFilePath, DeviceType.File);

            if (!String.IsNullOrEmpty(settings.MdfFilePath))
            {
                restore.RelocateFiles.Add(new RelocateFile(settings.DatabaseName, settings.MdfFilePath));
                
            }

            if (!String.IsNullOrEmpty(settings.LdfFilePath))
            {
                restore.RelocateFiles.Add(new RelocateFile(String.Format("{0}_Log", settings.DatabaseName), settings.LdfFilePath));
            }

            restore.SqlRestore(Server);
        }

        //public string GetConnectionString(string serverName, string databaseName, string login = null, string password = null)
        //{
        //    var connectionStringBuilder = new SqlConnectionStringBuilder
        //    {
        //        DataSource = serverName,
        //        InitialCatalog = databaseName,
        //        IntegratedSecurity = true
        //    };

        //    if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
        //    {
        //        connectionStringBuilder.UserID = login;
        //        connectionStringBuilder.Password = password;
        //    }

        //    return connectionStringBuilder.ConnectionString;
        //}

//        // TODO: test
//        public void RestoreDatabaseUsingSqlBackup(string serverName, string databaseName, string sourceFile, string standByFileLocation, string mdfFileLocation = null, string ldfFileLocation = null)
//        {
//            Console.WriteLine("About to restore databaseName '{0}' on server '{1}' ...", databaseName, serverName);
//            var server = InitialiseServer(serverName);
//            var database = server.Databases[databaseName];

//            if (database != null)
//            {
//                Console.WriteLine("Database already exists, removing the current one ...");
//                DropDatabaseSmo(serverName, databaseName);
//                Console.WriteLine("Database has been successfully removed!");
//            }

//            Console.WriteLine("Restoring databaseName ...");
//            var connectionString = GetConnectionString(serverName, databaseName);
//            using (var connection = new SqlConnection(connectionString))
//            {
//                var standBy = String.Format(@"{0}\{1}_StandBy.dat", standByFileLocation, databaseName);
//                var mdf = String.Format(@"{0}\{1}.mdf", mdfFileLocation, databaseName);
//                var ldf = String.Format(@"{0}\{1}.ldf", ldfFileLocation, databaseName);
//                var statement = String.Format(@"EXECUTE master..sqlbackup 
//                    '-SQL ""RESTORE DATABASE [{0}] 
//                    FROM DISK = ''{1}'' 
//                    WITH STANDBY = ''{2}'', 
//                    MOVE ''{3}'' TO ''{4}'', 
//                    MOVE ''{5}_Log'' TO ''{6}''""' "
//                        , databaseName, sourceFile, standBy, databaseName, mdf, databaseName, ldf);

//                using (var command = new SqlCommand(statement))
//                {
//                    connection.Open();
//                    command.CommandType = CommandType.Text;
//                    command.ExecuteNonQuery();
//                }
//            }

//            Console.WriteLine("Successfully restored databse!");
//            Console.WriteLine();
//        }
    }
}
