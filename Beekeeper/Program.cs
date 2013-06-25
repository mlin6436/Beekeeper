using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Beekeeper
{
    public class Program
    {
        public static string SystemVolumeInformationFolder = ConfigurationManager.AppSettings["SystemVolumeInformationFolder"];
        public static string RecycleBinFolder = ConfigurationManager.AppSettings["RecycleBinFolder"];
        public static string FileNamePattern = ConfigurationManager.AppSettings["FileNamePattern"];
        public static string FilePostfixPattern = ConfigurationManager.AppSettings["FilePostfixPattern"];
        public static string FileDateTimePattern = ConfigurationManager.AppSettings["FileDateTimePattern"];
        public static int WarningLevelRed = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelRed"]);
        public static int WarningLevelAmber = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelAmber"]);

        public static void Main(string[] args)
        {
            try
            {
                var command = Args.Configuration.Configure<Command>().CreateAndBind(args);

                if (command.Action.Equals(CommandOption.CheckStatus))
                {
                    if (String.IsNullOrEmpty(command.Directory))
                    {
                        Console.WriteLine("Directory does not exist!");
                        return;
                    }

                    CheckStatus(command.Directory);
                }
                else if (command.Action.Equals(CommandOption.DropDatabase))
                {
                    if (String.IsNullOrEmpty(command.Server))
                    {
                        Console.WriteLine("Server does not exist!");
                        return;
                    }

                    if (String.IsNullOrEmpty(command.Database))
                    {
                        Console.WriteLine("Database does not exist!");
                        return;
                    }

                    DropDatabase(command.Server, command.Database);
                }
                else if (command.Action.Equals(CommandOption.RestoreDatabase))
                {
                    if (String.IsNullOrEmpty(command.Server))
                    {
                        Console.WriteLine("Server does not exist!");
                        return;
                    }

                    if (String.IsNullOrEmpty(command.Database))
                    {
                        Console.WriteLine("Database does not exist!");
                        return;
                    }

                    if (String.IsNullOrEmpty(command.DatabaseBackupFile))
                    {
                        Console.WriteLine("Database backup file does not exist!");
                        return;
                    }

                    RestoreDatabase(command.Server, command.Database, command.DatabaseBackupFile);
                }
                else
                {
                    Console.WriteLine("Command cannot be found!");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("----> EXCEPTION: {0} <----", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Check logship folder for status of logship items.
        /// </summary>
        /// <param name="path">Path to all logship directories</param>
        private static void CheckStatus(string path)
        {
            Console.WriteLine("--> Searching '{0}' ...", path);

            var directory = new DirectoryInfo(path);
            var logshipDirectories = directory.GetDirectories().Where(d => !d.Name.Equals(SystemVolumeInformationFolder) && !d.Name.Equals(RecycleBinFolder)).ToList();

            Console.WriteLine("Directories found: {0}", logshipDirectories.Count());
            Console.WriteLine();

            var logships = GetLogshipInfo(logshipDirectories);

            foreach (var logship in logships)
            {
                Console.WriteLine("----> Searching '{0}' ...", logship.Name);
                Console.WriteLine("To process items found: {0}", logship.ItemCount);
                Console.WriteLine("Date between '{0}' & '{1}'.", logship.StartTime, logship.EndTime);
                Console.WriteLine();
            }
            Console.WriteLine("--> Summary");
            Console.WriteLine("Folders searched: {0}", logshipDirectories.Count);
            Console.WriteLine("[RED] warning issued: {0}", logships.Count(l => l.Warning == WarningLevel.Red));
            foreach (var logship in logships.Where(l => l.Warning == WarningLevel.Red))
            {
                Console.WriteLine("'{0}' - total: {1}", logship.Name, logship.ItemCount);
            }
            Console.WriteLine("[AMBER] warning issued: {0}", logships.Count(l => l.Warning == WarningLevel.Amber));
            foreach (var logship in logships.Where(l => l.Warning == WarningLevel.Amber))
            {
                Console.WriteLine("'{0}' - total: {1}", logship.Name, logship.ItemCount);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Get a list of logship information.
        /// </summary>
        /// <param name="logshipDirectories">List of logship directories</param>
        /// <returns>A list of logship information</returns>
        private static List<Logship> GetLogshipInfo(List<DirectoryInfo> logshipDirectories)
        {
            var logships = new List<Logship>();

            foreach (var logshipFolder in logshipDirectories)
            {
                var logship = new Logship
                    {
                        Path = logshipFolder.FullName, 
                        Name = logshipFolder.Name
                    };

                var logshipDirectory = new DirectoryInfo(logshipFolder.FullName);
                var logshipFiles = logshipDirectory.GetFiles(FilePostfixPattern).OrderByDescending(i => i.FullName);
                logship.ItemCount = logshipFiles.Count();
                if (logship.ItemCount > WarningLevelRed)
                {
                    logship.Warning = WarningLevel.Red;
                }
                else if (logship.ItemCount < WarningLevelRed && logship.ItemCount > WarningLevelAmber)
                {
                    logship.Warning = WarningLevel.Amber;
                }

                if (logshipFiles.Any())
                {
                    var latestToProcess = logshipFiles.First();
                    var latestToProcessNames = Regex.Split(latestToProcess.FullName, FileNamePattern);
                    var latestToProcessNamesCount = latestToProcessNames.Count();
                    var latestToProcessDateTime = DateTime.ParseExact(String.Format("{0} {1}",
                        latestToProcessNames[latestToProcessNamesCount - 3],
                        latestToProcessNames[latestToProcessNamesCount - 2]),
                        FileDateTimePattern,
                        CultureInfo.InvariantCulture);
                    logship.EndTime = latestToProcessDateTime;

                    var earliestToProcess = logshipFiles.Last();
                    var earliestToProcessNames = Regex.Split(earliestToProcess.FullName, FileNamePattern);
                    var earliestToProcessNamesCount = earliestToProcessNames.Count();
                    var earliestToProcessDateTime = DateTime.ParseExact(String.Format("{0} {1}",
                        earliestToProcessNames[earliestToProcessNamesCount - 3],
                        earliestToProcessNames[earliestToProcessNamesCount - 2]),
                        FileDateTimePattern,
                        CultureInfo.InvariantCulture);
                    logship.StartTime = earliestToProcessDateTime;

                }

                logships.Add(logship);
            }

            return logships;
        }

        /// <summary>
        /// Initialise server information.
        /// </summary>
        /// <param name="serverName">Server name</param>
        /// <returns>Server instance</returns>
        private static Server InitialiseServer(string serverName, string login = null, string password = null)
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
        private static void DropDatabase(string serverName, string databaseName)
        {
            var server = InitialiseServer(serverName);
            var database = server.Databases[databaseName];

            Console.WriteLine("About to drop database '{0}' on server '{1}'", databaseName, serverName);
            if (database != null)
            {
                server.KillAllProcesses(databaseName);
                server.KillDatabase(databaseName);
                database.Drop();
                Console.WriteLine("Successfully dropped databse.");
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
        private static void RestoreDatabase(string serverName, string databaseName, string databaseBackupFileLocation, string mdfFileLocation = null, string ldfFileLocation = null)
        {
            Console.WriteLine("About to restore database '{0}' on server '{1}'", databaseName, serverName);
            var server = InitialiseServer(serverName);
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
            Console.WriteLine("Successfully restored databse.");
            Console.WriteLine();
        }



        private static string GetConnectionString(string serverName, string databaseName)
        {
            var connBuilder = new SqlConnectionStringBuilder
            {
                DataSource = serverName,
                InitialCatalog = databaseName,
                IntegratedSecurity = true
            };

            return connBuilder.ConnectionString;
        }

        private static object ExecuteCommandWithResult(string command, string connectionString)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(command, conn))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        private static bool TableExists(string serverName, string databaseName)
        {
            var exists = false;
            var command = String.Format(@"SELECT name FROM master.dbo.sysdatabases WHERE name = N'{0}'", databaseName);
            var connectionString = GetConnectionString(serverName, databaseName);

            var result = ExecuteCommandWithResult(command, connectionString);
            if (result != null && !String.IsNullOrEmpty(result.ToString()))
            {
                exists = true;
            }

            return exists;
        }

        private static List<string> GenerateRestoreQuery(string databaseName, string logshipFolderPath, string standbyFilePath)
        {
            var query = new List<string>();

            var dir = new DirectoryInfo(logshipFolderPath);
            var files = dir.GetFiles(FilePostfixPattern).OrderBy(f => f.FullName);

            if (files.Any())
            {
                foreach (var file in files)
                {
                    if (file == files.Last())
                    {
                        query.Add(String.Format("EXECUTE master..sqlbackup '-SQL \"RESTORE LOG [{0}] FROM DISK = ''{1}'' WITH STANDBY = ''{2}''\"'",
                            databaseName, file.FullName, standbyFilePath));
                    }
                    else
                    {
                        query.Add(String.Format("EXECUTE master..sqlbackup '-SQL \"RESTORE LOG [{0}] FROM DISK = ''{1}'' WITH NORECOVERY\"'",
                            databaseName, file.FullName));
                    }
                }
            }
            
            return query;
        }
    }
}
