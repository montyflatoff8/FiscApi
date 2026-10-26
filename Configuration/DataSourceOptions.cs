namespace FiscApi.Configuration;

/// <summary>
/// Binds the "DataSource" section of appsettings.json. Program.cs reads Provider once at
/// startup to decide which ITransactionRepository to register; no other code checks it.
/// </summary>
public class DataSourceOptions
{
    public const string SectionName = "DataSource";

    // "Json" or "SqlServer".
    public string Provider { get; set; } = "Json";

    // Development only: apply EF Core migrations at startup so the database and table exist.
    public bool MigrateOnStartup { get; set; } = true;
}
