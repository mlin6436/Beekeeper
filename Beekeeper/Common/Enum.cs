using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beekeeper
{
    public enum CommandOption
    {
        None,
        CheckStatus,
        DropDatabase,
        RestoreDatabase,
        RestoreDatabaseUsingSqlBackup,
        GenerateRestoreQuery,
    }

    public enum WarningLevel
    {
        Red = 1,
        Amber = 2,
        Green = 3
    }
}
