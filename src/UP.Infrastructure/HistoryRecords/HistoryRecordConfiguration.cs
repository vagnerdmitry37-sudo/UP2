using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.HistoryRecords;

namespace UP.Infrastructure.HistoryRecords;

internal sealed class HistoryRecordConfiguration : IEntityTypeConfiguration<HistoryRecord>
{
    private const int EntityTypeMaxLength = 100;
    private const int OperationMaxLength = 50;

    public void Configure(EntityTypeBuilder<HistoryRecord> builder)
    {
        builder.ToTable("HistoryRecords");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.EntityType)
            .HasMaxLength(EntityTypeMaxLength)
            .IsRequired();

        builder.Property(record => record.Operation)
            .HasConversion<string>()
            .HasMaxLength(OperationMaxLength)
            .IsRequired();

        builder.Property(record => record.Changes)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(record => new { record.EntityType, record.EntityId, record.OccurredAt });
    }
}
