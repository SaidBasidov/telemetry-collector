using Npgsql;

public class TelemetryCollector : BackgroundService
{
    private readonly TelemetryService _service;
    private readonly ILogger<TelemetryCollector> _logger;

    public TelemetryCollector(
        TelemetryService service,
        ILogger<TelemetryCollector> logger)
    {
        _service = service;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        _logger.LogInformation("Сборщик запущен.");

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var timestamp = DateTimeOffset.UtcNow;
                var records = new List<TelemetryRecord>();

                for (int tagId = 1; tagId <= 100; tagId++)
                {
                    double value = Math.Round(
                        Random.Shared.NextDouble() * 100, 2);

                    int quality = Random.Shared.Next(10) == 0 ? 0 : 1;

                    records.Add(new TelemetryRecord(
                        tagId,
                        value,
                        quality,
                        timestamp
                    ));
                }

                try
                {
                    await _service.AddBatchAsync(records, stoppingToken);

                    _logger.LogInformation(
                        "Сохранено {Count} измерений.", records.Count);
                }
                catch (NpgsqlException error)
                    when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(
                        error,
                        "Ошибка сохранения пачки. Следующая попытка — на следующем тике.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Ожидаемая отмена при остановке приложения.
        }
        finally
        {
            _logger.LogInformation("Сборщик остановлен.");
        }
    }
}