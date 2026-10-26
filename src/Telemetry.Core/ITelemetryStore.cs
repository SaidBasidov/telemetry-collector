public interface ITelemetryStore
{
    Task AddBatchAsync(
    List<TelemetryRecord> records,
    CancellationToken cancellationToken = default);

    Task<TelemetryRecord?> GetLatestAsync(
    int tagId,
    CancellationToken cancellationToken = default);

    Task<List<TelemetryRecord>> GetHistoryAsync(
        int tagId,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit = 100,
        CancellationToken cancellationToken = default);
}