using Azure.Data.Tables;

namespace EventTicketing.DemoLauncher;

/// <summary>Keeps the expiry as one row in the launcher's own storage account.</summary>
public class TableExpiryStore(TableClient table) : IExpiryStore
{
    public const string TableName = "demo";
    private const string PartitionKey = "demo";
    private const string RowKey = "expiry";
    private const string ExpiresAt = nameof(ExpiresAt);

    public async Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken)
    {
        var row = await table.GetEntityIfExistsAsync<TableEntity>(PartitionKey, RowKey, [ExpiresAt], cancellationToken);
        return row.HasValue ? row.Value!.GetDateTimeOffset(ExpiresAt) : null;
    }

    // Concurrent requests each write "now + lifetime", so last-writer-wins is fine.
    public Task SetAsync(DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        table.UpsertEntityAsync(new TableEntity(PartitionKey, RowKey) { [ExpiresAt] = expiresAt },
            TableUpdateMode.Replace, cancellationToken);
}
