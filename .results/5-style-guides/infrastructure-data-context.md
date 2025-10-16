# infrastructure-data-context Style Guide

- Keep `ObsidianAIDbContext` focused on EF Core configuration; avoid business logic inside the context.
- Use explicit `DbSet` names that align with domain entities and configure table names via `OnModelCreating` to guard against migrations drift.
- Maintain the design-time factory for CLI tooling; update connection string logic whenever deployment environments change.
