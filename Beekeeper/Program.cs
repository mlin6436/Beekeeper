using System;
using System.Collections.Generic;
using System.Configuration;
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
        public static string sourcePath = ConfigurationManager.AppSettings["Beehive"];

        private const string SystemVolumeInformationFolderName = "System Volume Information";
        private const string SystemRecycleBinFolderName = "$RECYCLE.BIN";
        private const string FileNamePattern = @"[_|.]";
        private const string FilePostfixPattern = "*.sqb";

        public static void Main(string[] args)
        {
            try
            {
                var command = Args.Configuration.Configure<CommandObject>().CreateAndBind(args);

                if (command.Option.Equals(CommandOption.CheckStatus))
                {
                    CheckBeehiveStatus(sourcePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("----> EXCEPTION: {0} <----", ex.Message);
                throw;
            }
        }

        private static void CheckBeehiveStatus(string sourcePath)
        {
            Console.WriteLine("Searching '{0}' for all directories...", sourcePath);

            var sourceDirectory = new DirectoryInfo(sourcePath);
            var targetDirectories = sourceDirectory.GetDirectories().Where(d => !d.Name.Equals(SystemVolumeInformationFolderName) && !d.Name.Equals(SystemRecycleBinFolderName)).ToList();
            Console.WriteLine("{0} directories found.", targetDirectories.Count());
            Console.WriteLine();

            var issue = 0;
            foreach (var targetDirectory in targetDirectories)
            {
                Console.WriteLine("Searching target directory '{0}' for 'TODO' items...", targetDirectory.FullName);

                var todoItems = targetDirectory.GetFiles(FilePostfixPattern).OrderByDescending(i => i.FullName);
                Console.WriteLine("{0} 'TODO' items found.", todoItems.Count());

                if (todoItems.Any())
                {
                    // "LOG_SQL2008_<DATABASE>_<DATE>_<TIME>.sqb"
                    var latestTodoItem = todoItems.First();
                    var latestTodoItemNames = Regex.Split(latestTodoItem.FullName, FileNamePattern);
                    var latestTodoItemNamesCount = latestTodoItemNames.Count();
                    var latestTodoItemDateTime = DateTime.ParseExact(String.Format("{0} {1}", latestTodoItemNames[latestTodoItemNamesCount - 3], latestTodoItemNames[latestTodoItemNamesCount - 2]), "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
                    var earliestTodoItem = todoItems.Last();
                    var earliestTodoItemNames = Regex.Split(earliestTodoItem.FullName, FileNamePattern);
                    var earliestTodoItemDateTime = DateTime.ParseExact(String.Format("{0} {1}", earliestTodoItemNames[latestTodoItemNamesCount - 3], earliestTodoItemNames[latestTodoItemNamesCount - 2]), "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
                    Console.WriteLine("Ranging from '{0}' to '{1}'.", earliestTodoItemDateTime.ToString(), latestTodoItemDateTime.ToString());

                    // give warming if log ships go back to more than one day.
                    if (earliestTodoItemDateTime.Date <= DateTime.Now.AddDays(-1))
                    {
                        issue++;
                        Console.WriteLine("----> Please investigate this issue immediately. <----");
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine("'{0}' issue(s) found!", issue.ToString());
            Console.WriteLine();
        }
    }
}
