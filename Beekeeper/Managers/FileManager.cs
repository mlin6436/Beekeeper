using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Beekeeper.Common;
using Beekeeper.Entities;

namespace Beekeeper.Managers
{
    public class FileManager
    {
        public static string SystemVolumeInformationFolder = ConfigurationManager.AppSettings["SystemVolumeInformationFolder"];
        public static string RecycleBinFolder = ConfigurationManager.AppSettings["RecycleBinFolder"];

        public static string FileNamePattern = ConfigurationManager.AppSettings["FileNamePattern"];
        public static string FilePostfixPattern = ConfigurationManager.AppSettings["FilePostfixPattern"];
        public static string FileDateTimePattern = ConfigurationManager.AppSettings["FileDateTimePattern"];

        public static int WarningLevelRed = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelRed"]);
        public static int WarningLevelAmber = Int32.Parse(ConfigurationManager.AppSettings["WarningLevelAmber"]);

        /// <summary>
        /// Check logship folder for status of logship items.
        /// </summary>
        /// <param name="path">Path to all logship directories</param>
        public void CheckStatus(string path)
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
        public List<Logship> GetLogshipInfo(List<DirectoryInfo> logshipDirectories)
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
    }
}
