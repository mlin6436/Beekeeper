using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beekeeper.Entities
{
    public class DatabaseRestoreSettings
    {
        public string DatabaseName { get; set; }
        public string BackupFilePath { get; set; }
        public string MdfFilePath { get; set; }
        public string LdfFilePath { get; set; }
    }
}
