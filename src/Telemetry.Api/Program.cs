using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Telemetry");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не задана строка подключения Telemetry.");
}

builder.Services.AddSingleton<NpgsqlDataSource>(
    _ => NpgsqlDataSource.Create(connectionString));

builder.Services.AddSingleton<ITelemetryStore, PostgresTelemetryStore>();

builder.Services.AddSingleton<TelemetryService>();

if (builder.Configuration.GetValue("Collector:Enabled", true))
{
    builder.Services.AddHostedService<TelemetryCollector>();
}

var app = builder.Build();

app.MapGet("/", () => "Telemetry API работает");

app.MapGet("/api/tags/{tagId}/latest", async (
    int tagId,
    TelemetryService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var record = await service.GetLatestAsync(tagId, cancellationToken);

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
    TelemetryService service,
    CancellationToken cancellationToken,
    int limit = 100) =>
{
    try
    {
        var records = await service.GetHistoryAsync(
            tagId, from, to, limit, cancellationToken);

        return Results.Ok(records);
    }
    catch (ArgumentException error)
    {
        return Results.BadRequest(error.Message);
    }
});

app.MapPost("/api/telemetry/batch", async (
    List<TelemetryMeasurementRequest?>? requests,
    TelemetryService service,
    CancellationToken cancellationToken) =>
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

        await service.AddBatchAsync(records, cancellationToken);

        return Results.Ok(new { saved = records.Count });
    }
    catch (ArgumentException error)
    {
        return Results.BadRequest(error.Message);
    }
});

app.Run();
