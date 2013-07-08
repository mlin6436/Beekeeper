using System.Configuration;
using System.IO;
using System.Linq;
using Args.Help.Formatters;
using Beekeeper.Common;
using Beekeeper.Entities;
using Beekeeper.Managers;
using System;
using Ninject;
using Ninject.Modules;
using Ninject.Extensions.Logging.Log4net;
using Ninject.Extensions.Logging;

namespace Beekeeper
{
    public class Program
    {
        public static string FilePostfixPattern = ConfigurationManager.AppSettings["FilePostfixPattern"];
        public static string SystemVolumeInformationFolder = ConfigurationManager.AppSettings["SystemVolumeInformationFolder"];
        public static string RecycleBinFolder = ConfigurationManager.AppSettings["RecycleBinFolder"];

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            var kernel = CreateKernel();
            var loggerFactory = kernel.Get<ILoggerFactory>();
            var logger = loggerFactory.GetCurrentClassLogger();

            try
            {
                var command = Args.Configuration.Configure<Command>().CreateAndBind(args);

                if (command.Action.Equals(CommandOption.None))
                {
                    var definition = Args.Configuration.Configure<Command>();
                    var help = new Args.Help.HelpProvider().GenerateModelHelp(definition);
                    var formatter = new ConsoleHelpFormatter(80, 1, 5);
                    Console.WriteLine(formatter.GetHelp(help));
                }
                else if (command.Action.Equals(CommandOption.Test))
                {
                    Console.WriteLine("For testing purposes!!!");
                }
                else if (command.Action.Equals(CommandOption.CheckStatus))
                {
                    if (String.IsNullOrEmpty(command.Directory))
                    {
                        Console.WriteLine("Directory does not exist!");
                        return;
                    }

                    Console.WriteLine("Searching '{0}' ...", command.Directory);
                    var subDirectoryList = Directory.GetDirectories(command.Directory)
                        .Select(d => new DirectoryInfo(d))
                        .Where(d => !d.Name.Equals(SystemVolumeInformationFolder))
                        .Where(d => !d.Name.Equals(RecycleBinFolder))
                        .OrderBy(d => d.Name)
                        .ToList();

                    if (!subDirectoryList.Any())
                    {
                        Console.WriteLine("The directory is empty!");
                        return;
                    }

                    Console.WriteLine("Directories found: {0}", subDirectoryList.Count);
                    Console.WriteLine();
                    foreach (var subDirectory in subDirectoryList)
                    {
                        Console.WriteLine("Searching '{0}' ...", subDirectory.Name);
                        var files = Directory.GetFiles(subDirectory.FullName, FilePostfixPattern)
                            .Select(f => new FileInfo(f))
                            .OrderBy(f => f.LastWriteTime)
                            .ToList();

                        if (!files.Any())
                        {
                            Console.WriteLine("To process files found: {0}.", files.Count);
                        }
                        else
                        {
                            Console.WriteLine("To process files found: {0}, between '{1}' & '{2}'.", files.Count, files.FirstOrDefault().LastWriteTime.ToString("yyyymmdd HH:MM:ss"), files.LastOrDefault().LastWriteTime.ToString("yyyymmdd HH:MM:ss"));
                        }
                    }
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

                    var databaseManager = new DatabaseManager(command.Server);
                    databaseManager.DropDatabase(command.Database);
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

                    var databaseRestoreSettings = new DatabaseRestoreSettings
                        {
                            DatabaseName = command.Database,
                            BackupFilePath = command.DatabaseBackupFile,
                        };
                    var databaseManager = new DatabaseManager(command.Server);
                    databaseManager.RestoreDatabase(databaseRestoreSettings);
                }
                else
                {
                    Console.WriteLine("Command cannot be found!");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine("----> EXCEPTION: {0} <----", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create kernel.
        /// </summary>
        /// <returns>Kernel</returns>
        protected static IKernel CreateKernel()
        {
            var settings = new NinjectSettings { LoadExtensions = false };
            return new StandardKernel(settings, new INinjectModule[] { new Log4NetModule() });
        }
    }
}
