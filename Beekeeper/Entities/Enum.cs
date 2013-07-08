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
    }

    public enum WarningLevel
    {
        Red = 1,
        Amber = 2,
        Green = 3
    }
}
