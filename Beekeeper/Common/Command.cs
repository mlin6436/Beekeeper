using Args;
using System.ComponentModel;
using Beekeeper.Entities;

namespace Beekeeper.Common
{
    public class Command
    {
        [ArgsMemberSwitch("a", "action")]
        [Description("Choose an action to perform.")]
        public CommandOption Action { get; set; }

        [ArgsMemberSwitch("d", "directory")]
        [Description("Specify directory path to check folder status.")]
        public string Directory { get; set; }

        [ArgsMemberSwitch("server")]
        [Description("Specify server name.")]
        public string Server { get; set; }

        [ArgsMemberSwitch("database")]
        [Description("Specify database name.")]
        public string Database { get; set; }

        [ArgsMemberSwitch("bak", "databasebackupfile")]
        [Description("Specify path to full database backup file.")]
        public string DatabaseBackupFile { get; set; }
    }
}
