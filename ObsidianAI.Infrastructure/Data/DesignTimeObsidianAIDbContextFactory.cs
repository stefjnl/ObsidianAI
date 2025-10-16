using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ObsidianAI.Infrastructure.Data;

/// <summary>
/// Provides design-time construction for <see cref="ObsidianAIDbContext"/> to support EF Core tooling.
/// </summary>
public sealed class DesignTimeObsidianAIDbContextFactory : IDesignTimeDbContextFactory<ObsidianAIDbContext>
{
    public ObsidianAIDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ObsidianAIDbContext>();
        optionsBuilder.UseSqlite("Data Source=obsidianai.db");
        return new ObsidianAIDbContext(optionsBuilder.Options);
    }
}
