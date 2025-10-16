using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext responsible for persisting conversation history.
/// </summary>
public class ObsidianAIDbContext : DbContext
{
    public ObsidianAIDbContext(DbContextOptions<ObsidianAIDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; } = null!;

    public DbSet<Message> Messages { get; set; } = null!;

    public DbSet<ActionCardRecord> ActionCards { get; set; } = null!;

    public DbSet<PlannedActionRecord> PlannedActions { get; set; } = null!;

    public DbSet<FileOperationRecord> FileOperations { get; set; } = null!;

    /// <inheritdoc />
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

    private static void ConfigureMessage(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Content).HasColumnType("TEXT");
        builder.Property(m => m.Timestamp).HasConversion(new UtcDateTimeConverter());
        builder.Property(m => m.Role)
            .HasConversion(new EnumToStringConverter<MessageRole>())
            .HasMaxLength(32);
        builder.Property(m => m.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(m => m.ActionCard)
            .WithOne(ac => ac.Message)
            .HasForeignKey<ActionCardRecord>(ac => ac.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.FileOperation)
            .WithOne(fo => fo.Message)
            .HasForeignKey<FileOperationRecord>(fo => fo.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => new { m.ConversationId, m.Timestamp });
    }

    private static void ConfigureActionCard(EntityTypeBuilder<ActionCardRecord> builder)
    {
        builder.ToTable("ActionCards");

        builder.HasKey(ac => ac.Id);
        builder.Property(ac => ac.Id).ValueGeneratedNever();
        builder.Property(ac => ac.Title).HasMaxLength(256);
        builder.Property(ac => ac.Operation).HasMaxLength(64);
        builder.Property(ac => ac.Status)
            .HasConversion(new EnumToStringConverter<ActionCardStatus>())
            .HasMaxLength(32);
        builder.Property(ac => ac.StatusMessage).HasMaxLength(512);
        builder.Property(ac => ac.CreatedAt).HasConversion(new UtcDateTimeConverter());
        builder.Property(ac => ac.CompletedAt).HasConversion(new NullableUtcDateTimeConverter());

        builder.HasMany(ac => ac.PlannedActions)
            .WithOne(pa => pa.ActionCard)
            .HasForeignKey(pa => pa.ActionCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePlannedAction(EntityTypeBuilder<PlannedActionRecord> builder)
    {
        builder.ToTable("PlannedActions");

        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Id).ValueGeneratedNever();
        builder.Property(pa => pa.Source).HasMaxLength(512);
        builder.Property(pa => pa.Destination).HasMaxLength(512);
        builder.Property(pa => pa.Description).HasMaxLength(512);
        builder.Property(pa => pa.Operation).HasMaxLength(64);
        builder.Property(pa => pa.Content).HasColumnType("TEXT");
        builder.Property(pa => pa.Type)
            .HasConversion(new EnumToStringConverter<PlannedActionType>())
            .HasMaxLength(32);
    }

    private static void ConfigureFileOperation(EntityTypeBuilder<FileOperationRecord> builder)
    {
        builder.ToTable("FileOperations");

        builder.HasKey(fo => fo.Id);
        builder.Property(fo => fo.Id).ValueGeneratedNever();
        builder.Property(fo => fo.Action).HasMaxLength(64);
        builder.Property(fo => fo.FilePath).HasMaxLength(512);
        builder.Property(fo => fo.Timestamp).HasConversion(new UtcDateTimeConverter());
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    private sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter()
            : base(
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
