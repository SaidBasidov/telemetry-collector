using Npgsql;

var connectionString =
    "Host=127.0.0.1;Port=5432;Database=telemetry;" +
    "Username=telemetry;Password=telemetry_local";

await using var dataSource = NpgsqlDataSource.Create(connectionString);

var store = new PostgresTelemetryStore(dataSource);

var record = await store.GetLatestAsync(5);

if (record == null)
{
    Console.WriteLine("Измерения не найдены.");
}
else
{
    Console.WriteLine($"Датчик: {record.TagId}");
    Console.WriteLine($"Значение: {record.Value}");
    Console.WriteLine($"Качество: {record.Quality}");
    Console.WriteLine($"Время: {record.Timestamp:O}");
}

var from = new DateTimeOffset(2026, 10, 21, 4, 0, 0, TimeSpan.Zero);
var to = new DateTimeOffset(2026, 10, 21, 5, 0, 0, TimeSpan.Zero);

var history = await store.GetHistoryAsync(5, from, to, 10);

Console.WriteLine($"История: {history.Count}");

foreach (var item in history)
{
    Console.WriteLine($"{item.Timestamp:O} — {item.Value}");
}
