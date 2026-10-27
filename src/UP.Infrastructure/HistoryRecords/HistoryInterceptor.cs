using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using UP.Application.HistoryRecords;
using UP.Domain.HistoryRecords;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Infrastructure.HistoryRecords;

internal sealed class HistoryInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    private const string IdPropertyName = "Id";
    private const string JsonColumnType = "jsonb";

    private static readonly HashSet<Type> TypesWithHistory = [typeof(Product), typeof(Order)];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddHistoryRecords(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddHistoryRecords(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void AddHistoryRecords(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        List<EntityEntry> changedEntries = context.ChangeTracker.Entries()
            .Where(entry => TypesWithHistory.Contains(entry.Metadata.ClrType))
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        DateTimeOffset occurredAt = timeProvider.GetUtcNow();

        foreach (EntityEntry entry in changedEntries)
        {
            Dictionary<string, object> changes = entry.State switch
            {
                EntityState.Added => CreatedValues(entry),
                EntityState.Modified => ChangedValues(entry),
                _ => [],
            };

            if (entry.State == EntityState.Modified && changes.Count == 0)
            {
                continue;
            }

            context.Add(new HistoryRecord
            {
                Id = Guid.CreateVersion7(),
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = (Guid)entry.Property(IdPropertyName).CurrentValue!,
                Operation = ToOperation(entry.State),
                UserId = currentUser.UserId,
                OccurredAt = occurredAt,
                Changes = JsonSerializer.Serialize(changes, JsonOptions),
            });
        }
    }

    private static Dictionary<string, object> CreatedValues(EntityEntry entry) =>
        entry.Properties
            .Where(property => property.Metadata.Name != IdPropertyName)
            .ToDictionary(
                property => property.Metadata.Name,
                property => (object)ToJson(property, property.CurrentValue));

    private static Dictionary<string, object> ChangedValues(EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();

        foreach (PropertyEntry property in entry.Properties.Where(property => property.IsModified))
        {
            JsonElement oldValue = ToJson(property, property.OriginalValue);
            JsonElement newValue = ToJson(property, property.CurrentValue);

            if (!JsonElement.DeepEquals(oldValue, newValue))
            {
                changes[property.Metadata.Name] = new { old = oldValue, @new = newValue };
            }
        }

        return changes;
    }

    private static JsonElement ToJson(PropertyEntry property, object? value) =>
        property.Metadata.GetColumnType() == JsonColumnType && value is string json
            ? JsonSerializer.Deserialize<JsonElement>(json)
            : JsonSerializer.SerializeToElement(value, JsonOptions);

    private static HistoryOperation ToOperation(EntityState state) => state switch
    {
        EntityState.Added => HistoryOperation.Created,
        EntityState.Modified => HistoryOperation.Updated,
        _ => HistoryOperation.Deleted,
    };
}
