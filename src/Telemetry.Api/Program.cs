using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Telemetry");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не задана строка подключения Telemetry.");
}

await using var dataSource = NpgsqlDataSource.Create(connectionString);

ITelemetryStore store = new PostgresTelemetryStore(dataSource);
var service = new TelemetryService(store);

var app = builder.Build();

app.MapGet("/", () => "Telemetry API работает");

app.MapGet("/api/tags/{tagId}/latest", async (int tagId) =>
{
    try
    {
        var record = await service.GetLatestAsync(tagId);

        if (record == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(record);
    }
    catch (ArgumentException error)
    {
        return Results.BadRequest(error.Message);
    }
});

app.MapGet("/api/tags/{tagId}/history", async (
    int tagId,
    DateTimeOffset from,
    DateTimeOffset to,
    int limit = 100) =>
{
    try
    {
        var records = await service.GetHistoryAsync(
            tagId,
            from,
            to,
            limit
        );

        return Results.Ok(records);
    }
    catch (ArgumentException error)
    {
        return Results.BadRequest(error.Message);
    }
});

app.MapPost("/api/telemetry/batch", async (
    List<TelemetryMeasurementRequest?>? requests) =>
{
    if (requests == null)
    {
        return Results.BadRequest("Нужно передать массив измерений.");
    }

    try
    {
        var records = new List<TelemetryRecord>();

        foreach (var request in requests)
        {
            if (request == null ||
                request.TagId == null ||
                request.Value == null ||
                request.Quality == null ||
                request.Timestamp == null)
            {
                return Results.BadRequest(
                    "У каждого измерения должны быть tagId, value, quality и timestamp.");
            }

            var record = new TelemetryRecord(
                request.TagId.Value,
                request.Value.Value,
                request.Quality.Value,
                request.Timestamp.Value
            );

            records.Add(record);
        }

        await service.AddBatchAsync(records);

        return Results.Ok(new { saved = records.Count });
    }
    catch (ArgumentException error)
    {
        return Results.BadRequest(error.Message);
    }
});

app.Run();