using Npgsql;

Console.WriteLine("------------ Telemetry Console Application ---------");

var connectionString =
    "Host=127.0.0.1;Port=5432;Database=telemetry;" +
    "Username=telemetry;Password=telemetry_local";

await using var dataSource = NpgsqlDataSource.Create(connectionString);

ITelemetryStore store = new PostgresTelemetryStore(dataSource);

var service = new TelemetryService(store);

var record = await service.GetLatestAsync(5);

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

var history = await service.GetHistoryAsync(5, from, to, 10);

Console.WriteLine($"История: {history.Count}");

foreach (var item in history)
{
    Console.WriteLine($"{item.Timestamp:O} — {item.Value}");
}

try
{
    await service.AddBatchAsync(new List<TelemetryRecord>());
}
catch (ArgumentException error)
{
    Console.WriteLine($"Ошибка: {error.Message}");
}

Console.WriteLine("------------ ----------------------- ---------");