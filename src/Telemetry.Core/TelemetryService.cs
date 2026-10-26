public class TelemetryService
{
    private readonly ITelemetryStore _store;

    public TelemetryService(ITelemetryStore store)
    {
        _store = store;
    }

    public async Task AddBatchAsync(
    List<TelemetryRecord> records,
    CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            throw new ArgumentException("Пачка не должна быть пустой.");
        }

        if (records.Count > 1000)
        {
            throw new ArgumentException(
                "В пачке должно быть не больше 1000 измерений.");
        }

        await _store.AddBatchAsync(records, cancellationToken);
    }

    public async Task<TelemetryRecord?> GetLatestAsync(int tagId)
    {
        if (tagId <= 0)
        {
            throw new ArgumentException("ID должен быть больше 0.");
        }

        return await _store.GetLatestAsync(tagId);
    }

    public async Task<List<TelemetryRecord>> GetHistoryAsync(
        int tagId,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit = 100)
    {
        if (tagId <= 0)
        {
            throw new ArgumentException("ID должен быть больше 0.");
        }

        if (from > to)
        {
            throw new ArgumentException(
                "Начало периода не может быть позже конца.");
        }

        if (limit <= 0)
        {
            throw new ArgumentException("Лимит должен быть больше 0.");
        }

        return await _store.GetHistoryAsync(tagId, from, to, limit);
    }
}