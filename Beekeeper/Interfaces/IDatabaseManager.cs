using Beekeeper.Entities;

namespace Beekeeper.Interfaces
{
    public interface IDatabaseManager
    {
        void CreateDatabase(string databaseName);

        void DropDatabase(string databaseName);

        void RestoreDatabase(DatabaseRestoreSettings settings);

        //void BackupDatabase();

        //void BackupTransaction();

        //string GetConnectionString(string serverName, string databaseName, string login = null, string password = null);

        //void RestoreDatabaseUsingSqlBackup(string serverName, string databaseName, string sourceFile,
        //                                   string standByFileLocation, string mdfFileLocation = null,
        //                                   string ldfFileLocation = null);
    }
}
