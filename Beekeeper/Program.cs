using Microsoft.SqlServer.Management.Common;
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
        public static string SourcePath = ConfigurationManager.AppSettings["Beehive"];
        public static string DataSourceName = ConfigurationManager.AppSettings["DataSourceName"];
        public static int WarningLevelRed = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelRed"]);
        public static int WarningLevelAmber = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelAmber"]);

        private const string SystemVolumeInformationFolder = "System Volume Information";
        private const string RecycleBinFolder = "$RECYCLE.BIN";
        private const string FileNamePattern = @"[_|.]";
        private const string FilePostfixPattern = "*.sqb";
        private const string FileDateTimePattern = "yyyyMMdd HHmmss";

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

                    var server = InitialiseServer(command.Server);
                    var database = server.Databases[command.Database];

                    Console.WriteLine("About to drop database: {0} on server {1}", command.Database, command.Server);
                    if (database != null)
                    {
                        database.Drop();
                        Console.WriteLine("Successfully dropped databse.");
                    }
                    else
                    {
                        Console.WriteLine("Database does not exist!");
                    }
                }
                //if (command.Action.Equals(CommandOption.GenerateRestoreQuery))
                //{
                //    var databaseName = "NCRInterface";
                //    var query = GenerateRestoreQuery(databaseName,
                //        String.Format(@"\\eprhdcdbwh\f$\LogShipping-EPRHDCDBWH\{0}", databaseName),
                //        String.Format(@"\\eprhdcdbwh\f$\StandBy-EPRHDCDBWH\{0}_StandBy.dat", databaseName));

                //    foreach (var seg in query)
                //    {
                //        Console.WriteLine(seg);
                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("----> EXCEPTION: {0} <----", ex.Message);
                throw;
            }
        }
       
        private static void CheckStatus(string directoryPath)
        {
            Console.WriteLine("--> Searching '{0}' ...", directoryPath);

            var directory = new DirectoryInfo(directoryPath);
            var logshipFolders = directory.GetDirectories().Where(d => !d.Name.Equals(SystemVolumeInformationFolder) && !d.Name.Equals(RecycleBinFolder)).ToList();

            Console.WriteLine("Directories found: {0}", logshipFolders.Count(), directoryPath);
            Console.WriteLine();

            var logships = GetLogshipInfo(logshipFolders);

            foreach (var logship in logships)
            {
                Console.WriteLine("----> Searching '{0}' ...", logship.Name);
                Console.WriteLine("TODO items found: {0}", logship.ItemCount);
                Console.WriteLine("Date between '{0}' & '{1}'.", logship.StartTime, logship.EndTime);
                Console.WriteLine();
            }
            Console.WriteLine("--> Summary");
            Console.WriteLine("Folders searched: {0}", logshipFolders.Count);
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

        private static List<Logship> GetLogshipInfo(List<DirectoryInfo> logshipFolders)
        {
            var logships = new List<Logship>();

            foreach (var logshipFolder in logshipFolders)
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
                    var latestTodo = logshipFiles.First();
                    var latestTodoNames = Regex.Split(latestTodo.FullName, FileNamePattern);
                    var latestTodoNamesCount = latestTodoNames.Count();
                    var latestTodoDateTime = DateTime.ParseExact(String.Format("{0} {1}",
                        latestTodoNames[latestTodoNamesCount - 3],
                        latestTodoNames[latestTodoNamesCount - 2]),
                        FileDateTimePattern,
                        CultureInfo.InvariantCulture);
                    logship.EndTime = latestTodoDateTime;

                    var earliestTodo = logshipFiles.Last();
                    var earliestTodoNames = Regex.Split(earliestTodo.FullName, FileNamePattern);
                    var earliestTodoNamesCount = earliestTodoNames.Count();
                    var earliestTodoDateTime = DateTime.ParseExact(String.Format("{0} {1}",
                        earliestTodoNames[earliestTodoNamesCount - 3],
                        earliestTodoNames[earliestTodoNamesCount - 2]),
                        FileDateTimePattern,
                        CultureInfo.InvariantCulture);
                    logship.StartTime = earliestTodoDateTime;

                }

                logships.Add(logship);
            }

            return logships;
        }

        private static Server InitialiseServer(string serverInstance)
        {
            var connection = new ServerConnection(serverInstance)
                {
                    // TODO: log in credential
                    LoginSecure = true
                };

            var sqlServer = new Server(connection);

            return sqlServer;
        }

        #region Beehive

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

        #endregion

        #region Waggle Dance

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

        #endregion

        private static void RemoveTable(string serverName, string databaseName)
        {
            if (TableExists(serverName, databaseName))
            {
                var sqlConnectionBuilder = new SqlConnectionStringBuilder
                    {
                        DataSource = DataSourceName,
                        InitialCatalog = databaseName,
                        IntegratedSecurity = true
                    };

                using (var sqlConnection = new SqlConnection(sqlConnectionBuilder.ConnectionString))
                {
                    sqlConnection.Open();
                    var query = String.Format(@"ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{0}];", databaseName);
                    using (var sqlCommand = new SqlCommand(query, sqlConnection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
