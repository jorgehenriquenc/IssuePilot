using IssuePilot.Application;
using Microsoft.Data.Sqlite;

namespace IssuePilot.Infrastructure;

public sealed class SqliteAnalysisHistory : IAnalysisHistory
{
    private readonly string _connectionString;

    public SqliteAnalysisHistory(string? path = null)
    {
        var azureHome = Environment.GetEnvironmentVariable("HOME");
        var onAzure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
        var dataDirectory = onAzure && !string.IsNullOrWhiteSpace(azureHome)
            ? Path.Combine(azureHome, "data", "IssuePilot")
            : AppContext.BaseDirectory;
        var databasePath = path ?? Path.Combine(dataDirectory, "issuepilot.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
    }

    public async Task SaveAsync(string mode, int qualityScore, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE IF NOT EXISTS analyses (id INTEGER PRIMARY KEY, created_utc TEXT NOT NULL, mode TEXT NOT NULL, quality_score INTEGER NOT NULL)";
        await create.ExecuteNonQueryAsync(ct);
        await using var prune = connection.CreateCommand();
        prune.CommandText = "DELETE FROM analyses WHERE created_utc < $cutoff";
        prune.Parameters.AddWithValue("$cutoff", DateTimeOffset.UtcNow.AddDays(-30).ToString("O"));
        await prune.ExecuteNonQueryAsync(ct);
        await using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO analyses (created_utc, mode, quality_score) VALUES ($at, $mode, $score)";
        insert.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        insert.Parameters.AddWithValue("$mode", mode);
        insert.Parameters.AddWithValue("$score", qualityScore);
        await insert.ExecuteNonQueryAsync(ct);
    }
}
