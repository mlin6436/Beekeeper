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
                else if (command.Action.Equals(CommandOption.CheckStatus))
                {
                    if (String.IsNullOrEmpty(command.Directory))
                    {
                        Console.WriteLine("Directory does not exist!");
                        return;
                    }

                    var fileManager = new FileManager();
                    fileManager.CheckStatus(command.Directory);
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
