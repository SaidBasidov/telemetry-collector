public class TelemetryStore
{
    private List<TelemetryRecord> _records = new List<TelemetryRecord>();

    public void Add(TelemetryRecord record)
    {
        _records.Add(record);
    }

    public List<TelemetryRecord> GetByTag(int tagId)
    {
        List<TelemetryRecord> result = new List<TelemetryRecord>();
        foreach (var record in _records)
        {
            if (record.TagId == tagId)
            {
                result.Add(record);
            }
        }
        return result;
    }

    public TelemetryRecord? GetLatest(int tagId)
    {
        TelemetryRecord? latest = null;
        foreach (var record in _records)
        {
            if (record.TagId == tagId)
            {
                if (latest == null || record.Timestamp > latest.Timestamp)
                {
                    latest = record;
                }
            }
        }
        return latest;
    }

    public List<TelemetryRecord> GetHistory(
    int tagId,
    DateTimeOffset from,
    DateTimeOffset to,
    int limit = 100)
    {
        if (from > to)
        {
            throw new ArgumentException("Начало периода не может быть позже конца.");
        }

        if (limit <= 0)
        {
            throw new ArgumentException("Лимит должен быть больше 0.");
        }

        List<TelemetryRecord> result = new List<TelemetryRecord>();
        foreach (var record in _records)
        {
            if (record.TagId == tagId &&
                record.Timestamp >= from &&
                record.Timestamp <= to)
            {
                result.Add(record);
            }
        }

        result.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

        if (result.Count > limit)
        {
            result = result.GetRange(0, limit);
        }

        return result;
    }
}