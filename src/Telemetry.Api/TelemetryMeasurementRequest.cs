public class TelemetryMeasurementRequest
{
    public int? TagId { get; set; }
    public double? Value { get; set; }
    public int? Quality { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}