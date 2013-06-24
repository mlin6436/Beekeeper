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

        public static void Main(string[] args)
        {
            try
            {
                var command = Args.Configuration.Configure<CommandObject>().CreateAndBind(args);

                if (command.Action.Equals(CommandOption.CheckStatus))
                {
                    if (String.IsNullOrEmpty(command.Directory))
                    {
                        Console.WriteLine("The directory does not exist!");
                        return;
                    }

                    CheckFolderStatus(command.Directory);
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

        #region Nectar

        private static void CheckFolderStatus(string path)
        {
            Console.WriteLine("Searching '{0}'...", path);

            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories()
                .Where(d => !d.Name.Equals(SystemVolumeInformationFolder) && !d.Name.Equals(RecycleBinFolder))
                .ToList();
            Console.WriteLine("--> {0} directories found in '{1}'.", subDirs.Count(), path);
            Console.WriteLine();

            var red = 0;
            var amber = 0;
            var redList = new List<string>();
            var amberList = new List<string>();
            foreach (var subDir in subDirs)
            {
                Console.WriteLine("Searching '{0}' for TODO files...", subDir.FullName);

                var fileCount = GetFileCount(subDir.FullName);
                var warningLevel = GetWarningLevel(fileCount);

                if (warningLevel == WarningLevel.Red)
                {
                    red++;
                    redList.Add(subDir.FullName);
                    Console.WriteLine("[WARNNING] Please investigate this item immediately!");
                }
                else if (warningLevel == WarningLevel.Amber)
                {
                    amber++;
                    amberList.Add(subDir.FullName);
                    Console.WriteLine("[WARNNING] There are slight Delays on this item!");
                }

                Console.WriteLine();
            }

            Console.WriteLine("Summary");
            Console.WriteLine("Folders searched: {0}", subDirs.Count);
            Console.WriteLine("[WARNING] RED issued: {0}", red);
            if (redList.Any())
            {
                foreach (var redItem in redList)
                {
                    Console.WriteLine("{0} - '{1}'", redList.IndexOf(redItem), redItem);
                }
            }
            Console.WriteLine("[WARNING] AMBER issued: {0}", amber);
            if (amberList.Any())
            {
                foreach (var amberItem in amberList)
                {
                    Console.WriteLine("{0} - '{1}'", amberList.IndexOf(amberItem), amberItem);
                }
            }
            Console.WriteLine();
        }

        private static WarningLevel GetWarningLevel(int i)
        {
            if (i > WarningLevelRed)
            {
                return WarningLevel.Red;
            }

            if (i > WarningLevelAmber)
            {
                return WarningLevel.Amber;
            }

            return WarningLevel.Green;
        }

        private static int GetFileCount(string path)
        {
            var dir = new DirectoryInfo(path);
            var files = dir.GetFiles(FilePostfixPattern).OrderByDescending(i => i.FullName);
            Console.WriteLine("--> TODO items found: {0}", files.Count());

            if (files.Any())
            {
                var latestTodo = files.First();
                var latestTodoNames = Regex.Split(latestTodo.FullName, FileNamePattern);
                var latestTodoNamesCount = latestTodoNames.Count();
                var latestTodoDateTime = DateTime.ParseExact(String.Format("{0} {1}",
                    latestTodoNames[latestTodoNamesCount - 3],
                    latestTodoNames[latestTodoNamesCount - 2]),
                    "yyyyMMdd HHmmss",
                    CultureInfo.InvariantCulture);

                var earliestTodo = files.Last();
                var earliestTodoNames = Regex.Split(earliestTodo.FullName, FileNamePattern);
                var earliestTodoNamesCount = earliestTodoNames.Count();
                var earliestTodoDateTime = DateTime.ParseExact(String.Format("{0} {1}",
                    earliestTodoNames[earliestTodoNamesCount - 3],
                    earliestTodoNames[earliestTodoNamesCount - 2]),
                    "yyyyMMdd HHmmss",
                    CultureInfo.InvariantCulture);

                Console.WriteLine("--> Date between '{0}' & '{1}'.",
                    earliestTodoDateTime.ToString(),
                    latestTodoDateTime.ToString());
            }

            return files.Count();
        }

        #endregion

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
