# Data Layer Deep Dive

Persistence lives in `ObsidianAI.Infrastructure` and is deliberately isolated behind domain ports. Entity Framework Core (SQLite) handles relational storage for conversations, messages, action cards, planned actions, and file operations.

## Key Conventions
- **DbContext-first mapping:** `ObsidianAIDbContext` centralizes entity configuration with explicit table names, max lengths, and UTC value converters.
- **Repository wrappers:** Infrastructure repositories implement domain interfaces (`IConversationRepository`, `IMessageRepository`) and never surface EF-specific APIs to callers.
- **Asynchronous IO:** All data access is asynchronous and respects cancellation tokens provided by higher layers.
- **Migrations in-source:** Schema changes are tracked under `Data/Migrations`, keeping the snapshot aligned with model changes.

## Representative Code
### Entity configuration
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    ConfigureConversation(modelBuilder.Entity<Conversation>());
    ConfigureMessage(modelBuilder.Entity<Message>());
    ConfigureActionCard(modelBuilder.Entity<ActionCardRecord>());
    ConfigurePlannedAction(modelBuilder.Entity<PlannedActionRecord>());
    ConfigureFileOperation(modelBuilder.Entity<FileOperationRecord>());
}

private static void ConfigureConversation(EntityTypeBuilder<Conversation> builder)
{
    builder.ToTable("Conversations");

    builder.HasKey(c => c.Id);
    builder.Property(c => c.Id).ValueGeneratedNever();
    builder.Property(c => c.UserId).HasMaxLength(128);
    builder.Property(c => c.Title).HasMaxLength(256);
    builder.Property(c => c.ModelName).HasMaxLength(128);
    builder.Property(c => c.ThreadId).HasMaxLength(128);
    builder.Property(c => c.CreatedAt).HasConversion(new UtcDateTimeConverter());
    builder.Property(c => c.UpdatedAt).HasConversion(new UtcDateTimeConverter());
    builder.Property(c => c.Provider)
        .HasConversion(new EnumToStringConverter<ConversationProvider>())
        .HasMaxLength(64);
    builder.Property(c => c.RowVersion)
        .IsRowVersion()
        .IsConcurrencyToken();

    builder.HasMany(c => c.Messages)
        .WithOne(m => m.Conversation)
        .HasForeignKey(m => m.ConversationId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(c => c.UpdatedAt);
    builder.HasIndex(c => new { c.UserId, c.IsArchived });
    builder.HasIndex(c => c.ThreadId);
}
```

### Repository implementation pattern
```csharp
public async Task<Conversation?> GetByIdAsync(Guid id, bool includeMessages = false, CancellationToken ct = default)
{
    IQueryable<Conversation> query = _dbContext.Conversations;

    if (includeMessages)
    {
        query = query
            .Include(c => c.Messages)
                .ThenInclude(m => m.ActionCard)
                    .ThenInclude(ac => ac!.PlannedActions)
            .Include(c => c.Messages)
                .ThenInclude(m => m.FileOperation)
            .AsSplitQuery();
    }

    return await query.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
}
```

## Implementation Notes
- **GUID identifiers:** All aggregates use GUID keys with `ValueGeneratedNever` to support deterministic creation in higher layers.
- **Split queries by default:** `AsSplitQuery()` keeps EF from materializing massive join graphs when including planned actions and file operations.
- **Cascade cleanup:** `OnDelete(DeleteBehavior.Cascade)` ensures related messages, action cards, and operations are removed when a conversation is deleted.
- **Concurrency tokens:** RowVersion fields on conversations and messages allow optimistic concurrency without leaking EF-specific logic outside the infrastructure layer.
- **Migration cadence:** Each schema change is timestamped (e.g., `20251016101818_AddThreadIdToConversation`). Developers should extend these migrations rather than altering tables directly.
