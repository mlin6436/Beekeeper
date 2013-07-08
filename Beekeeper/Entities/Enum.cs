namespace Beekeeper.Entities
{
    public enum CommandOption
    {
        None,
        CheckStatus,
        DropDatabase,
        RestoreDatabase,
        RestoreDatabaseUsingSqlBackup,
        GenerateRestoreQuery,
        Test,
    }
}
