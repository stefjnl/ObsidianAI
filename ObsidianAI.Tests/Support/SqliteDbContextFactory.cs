using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ObsidianAI.Infrastructure.Data;

namespace ObsidianAI.Tests.Support;

/// <summary>
/// Utilities for creating SQLite in-memory DbContext instances for tests.
/// </summary>
internal static class SqliteDbContextFactory
{
    public static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);
        return connection;
    }

    public static DbContextOptions<ObsidianAIDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<ObsidianAIDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    public static async Task EnsureCreatedAsync(DbContextOptions<ObsidianAIDbContext> options)
    {
        await using var context = new ObsidianAIDbContext(options);
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}
