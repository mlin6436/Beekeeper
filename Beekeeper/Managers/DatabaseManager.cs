using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Beekeeper
{
    public class DatabaseManager
    {
        /// <summary>
        /// Initialise server information.
        /// </summary>
        /// <param name="serverName">Server name</param>
        /// <returns>Server instance</returns>
        public Server InitialiseServer(string serverName, string login = null, string password = null)
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

            var sqlServer = new Server(connection);

            return sqlServer;
        }

        /// <summary>
        /// Drop database.
        /// </summary>
        /// <param name="serverName">Server name</param>
        /// <param name="databaseName">Database name</param>
        public void DropDatabase(string serverName, string databaseName)
        {
            Console.WriteLine("About to drop database '{0}' on server '{1}' ...", databaseName, serverName);

            var server = InitialiseServer(serverName);
            var database = server.Databases[databaseName];

            if (database != null)
            {
                Console.WriteLine("Dropping database ...");
                server.KillAllProcesses(databaseName);
                server.KillDatabase(databaseName);
                database.Drop();
                Console.WriteLine("Successfully dropped databse!");
            }
            else
            {
                Console.WriteLine("Database does not exist!");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Restore database from .bak file.
        /// NOTE: Make sure the server is compatible with the back up file.
        /// NOTE: In case that back up was done via RedGate SQLBackup, the file needs to be converted from .sqb file into .bak.
        ///     Go to "C:\Program Files\Red Gate\SQL Backup 6".
        ///     Use "SQBConverter.exe inputfile outputfile [password] [/sqb]" to convert.
        /// </summary>
        /// <param name="serverName">Server name</param>
        /// <param name="databaseName">Database name</param>
        /// <param name="databaseBackupFileLocation">Database backup file location</param>
        /// <param name="mdfFileLocation">MDF file location</param>
        /// <param name="ldfFileLocation">LDF file location</param>
        public void RestoreDatabase(string serverName, string databaseName, string databaseBackupFileLocation, string mdfFileLocation = null, string ldfFileLocation = null)
        {
            Console.WriteLine("About to restore database '{0}' on server '{1}' ...", databaseName, serverName);
            var server = InitialiseServer(serverName);
            var database = server.Databases[databaseName];

            if (database != null)
            {
                Console.WriteLine("Database already exists, removing the current one ...");
                DropDatabase(serverName, databaseName);
                Console.WriteLine("Database has been successfully removed!");
            }

            Console.WriteLine("Restoring database ...");
            var restore = new Restore
            {
                Database = databaseName,
                Action = RestoreActionType.Database,
                ReplaceDatabase = true,
            };
            restore.Devices.AddDevice(databaseBackupFileLocation, DeviceType.File);
            if (!String.IsNullOrEmpty(mdfFileLocation) && !String.IsNullOrEmpty(ldfFileLocation))
            {
                restore.RelocateFiles.Add(new RelocateFile(databaseName, String.Format(@"{0}\{1}.mdf", mdfFileLocation, databaseName)));
                restore.RelocateFiles.Add(new RelocateFile(String.Format("{0}_Log", databaseName), String.Format(@"{0}\{1}.ldf", ldfFileLocation, databaseName)));
            }
            restore.SqlRestore(server);

            Console.WriteLine("Successfully restored databse!");
            Console.WriteLine();
        }

        public string GetConnectionString(string serverName, string databaseName, string login = null, string password = null)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = serverName,
                InitialCatalog = databaseName,
                IntegratedSecurity = true
            };

            if (!String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
            {
                connectionStringBuilder.UserID = login;
                connectionStringBuilder.Password = password;
            }

            return connectionStringBuilder.ConnectionString;
        }

        // TODO: test
        public void RestoreDatabaseUsingSqlBackup(string serverName, string databaseName, string sourceFile, string standByFileLocation, string mdfFileLocation = null, string ldfFileLocation = null)
        {
            Console.WriteLine("About to restore database '{0}' on server '{1}' ...", databaseName, serverName);
            var server = InitialiseServer(serverName);
            var database = server.Databases[databaseName];

            if (database != null)
            {
                Console.WriteLine("Database already exists, removing the current one ...");
                DropDatabase(serverName, databaseName);
                Console.WriteLine("Database has been successfully removed!");
            }

            Console.WriteLine("Restoring database ...");
            var connectionString = GetConnectionString(serverName, databaseName);
            using (var connection = new SqlConnection(connectionString))
            {
                var standBy = String.Format(@"{0}\{1}_StandBy.dat", standByFileLocation, databaseName);
                var mdf = String.Format(@"{0}\{1}.mdf", mdfFileLocation, databaseName);
                var ldf = String.Format(@"{0}\{1}.ldf", ldfFileLocation, databaseName);
                var statement = String.Format(@"EXECUTE master..sqlbackup 
                    '-SQL ""RESTORE DATABASE [{0}] 
                    FROM DISK = ''{1}'' 
                    WITH STANDBY = ''{2}'', 
                    MOVE ''{3}'' TO ''{4}'', 
                    MOVE ''{5}_Log'' TO ''{6}''""' "
                        , databaseName, sourceFile, standBy, databaseName, mdf, databaseName, ldf);

                using (var command = new SqlCommand(statement))
                {
                    connection.Open();
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Successfully restored databse!");
            Console.WriteLine();
        }
    }
}
