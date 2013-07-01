using System.Data;
using Args.Help.Formatters;
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
using log4net;
using Ninject;
using Ninject.Modules;
using Ninject.Extensions.Logging.Log4net;
using Ninject.Extensions.Logging;

namespace Beekeeper
{
    public class Program
    {
        public static int WarningLevelRed = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelRed"]);
        public static int WarningLevelAmber = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelAmber"]);

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

                    var databaseManager = new DatabaseManager();
                    databaseManager.DropDatabase(command.Server, command.Database);
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

                    var databaseManager = new DatabaseManager();
                    databaseManager.RestoreDatabase(command.Server, command.Database, command.DatabaseBackupFile);
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
