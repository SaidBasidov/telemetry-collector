public class TelemetryRecord
{
    public int TagId { get; }
    public double Value { get; }
    public int Quality { get; }
    public DateTimeOffset Timestamp { get; }

    public TelemetryRecord(
        int tagId,
        double value,
        int quality,
        DateTimeOffset timestamp)
    {
        if (tagId <= 0)
        {
            throw new ArgumentException("ID должен быть больше 0.");
        }

        if (quality != 0 && quality != 1)
        {
            throw new ArgumentException("Качество должно быть 0 или 1.");
        }

        if (!double.IsFinite(value))
        {
            throw new ArgumentException("Значение должно быть конечным числом.");
        }

        TagId = tagId;
        Value = value;
        Quality = quality;
        Timestamp = timestamp;
    }
}
