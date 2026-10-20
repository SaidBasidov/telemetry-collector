var store = new TelemetryStore();
var now = DateTimeOffset.UtcNow;

store.Add(new TelemetryRecord(5, 83.7, 1, now.AddMinutes(-10)));
store.Add(new TelemetryRecord(5, 85.0, 1, now));
store.Add(new TelemetryRecord(8, 40.0, 1, now.AddMinutes(1)));
store.Add(new TelemetryRecord(5, 84.2, 1, now.AddMinutes(-5)));

var latest = store.GetLatest(5);

if (latest == null)
{
    Console.WriteLine("Измерений нет");
}
else
{
    Console.WriteLine($"Последнее значение: {latest.Value}");
}

var history = store.GetHistory(
    5,
    now.AddMinutes(-10),
    now,
    2
);

Console.WriteLine($"История: {history.Count}");

foreach (var record in history)
{
    Console.WriteLine(record.Value);
}