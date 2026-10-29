using Npgsql;

try
{
    CheckModel();

    if (args.Contains("--model-only"))
    {
        Console.WriteLine("Проверки модели завершены. PostgreSQL не проверялся.");
        return 0;
    }

    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Telemetry")
        ?? "Host=127.0.0.1;Port=5432;Database=telemetry;Username=telemetry;Password=telemetry_local";

    await CheckStorageAsync(connectionString);
    Console.WriteLine("Все проверки пройдены.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: {error.GetType().Name}: {error.Message}");
    return 1;
}

static void CheckModel()
{
    foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
    {
        var rejected = false;
        try
        {
            _ = new TelemetryRecord(7, value, 1, DateTimeOffset.UtcNow);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }

        Require(rejected, "Модель должна отклонять NaN и бесконечность.");
    }

    _ = new TelemetryRecord(7, -10.5, 0, DateTimeOffset.UtcNow);
    _ = new TelemetryRecord(7, double.MaxValue, 1, DateTimeOffset.UtcNow);
    Console.WriteLine("OK: неконечные значения отклонены; конечные допустимы.");
}

static async Task CheckStorageAsync(string connectionString)
{
    // Имя генерируется самим тестом, не поступает от пользователя.
    var schema = "telemetry_check_" + Guid.NewGuid().ToString("N");
    await using var admin = NpgsqlDataSource.Create(connectionString);
    await ExecuteAsync(admin, $"CREATE SCHEMA {schema};");
    Console.WriteLine($"Тестовая схема: {schema}");

    try
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = schema
        };

        // using расположен внутри try: пул освобождается до удаления схемы.
        await using var dataSource = NpgsqlDataSource.Create(settings.ConnectionString);
        var initSql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "init.sql"));
        await ExecuteAsync(dataSource, initSql);

        var store = new PostgresTelemetryStore(dataSource);
        var time = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await store.AddBatchAsync(new List<TelemetryRecord>
        {
            new(7, 10, 1, time),
            new(7, 20, 0, time),
            new(8, 30, 1, time.AddSeconds(1))
        });

        Require((await store.GetLatestAsync(7))?.Value == 20,
            "При одинаковом времени должна выбираться запись с большим id.");
        var history = await store.GetHistoryAsync(7, time, time);
        Require(history.Count == 2 && history[0].Value == 20 && history[1].Value == 10,
            "Границы истории включаются; записи отсортированы от новых к старым.");
        Require((await store.GetHistoryAsync(7, time, time, 1)).Count == 1,
            "Лимит истории должен применяться.");
        Require(await store.GetLatestAsync(99) == null, "Нет измерений: latest должен вернуть null.");
        Require((await store.GetHistoryAsync(99, time, time)).Count == 0,
            "Нет измерений: history должна вернуть пустой список.");
        Console.WriteLine("OK: latest, одинаковое время, границы периода, порядок и лимит.");

        // Дополнительное ограничение существует только в тестовой схеме.
        // Значение допустимо для модели, поэтому ошибка возникнет именно в PostgreSQL.
        await ExecuteAsync(dataSource,
            "ALTER TABLE telemetry_records ADD CONSTRAINT integration_reject_value CHECK (value <> -999999);");

        var rejectedByDatabase = false;
        try
        {
            await store.AddBatchAsync(new List<TelemetryRecord>
            {
                new(7, 40, 1, time.AddSeconds(2)),
                new(7, -999999, 1, time.AddSeconds(3)),
                new(7, 60, 1, time.AddSeconds(4))
            });
        }
        catch (PostgresException error)
            when (error.SqlState == "23514" && error.ConstraintName == "integration_reject_value")
        {
            rejectedByDatabase = true;
        }

        Require(rejectedByDatabase, "Ожидалась ошибка CHECK внутри PostgreSQL.");
        Require(await CountAsync(dataSource) == 3,
            "Откат должен убрать всю новую пачку, сохранив три прежние записи.");
        Require((await store.GetLatestAsync(7))?.Value == 20,
            "Первая запись ошибочной пачки не должна сохраниться.");
        Console.WriteLine("OK: ошибка второй команды откатила всю пачку; прежние записи сохранены.");

        await store.AddBatchAsync(new List<TelemetryRecord> { new(7, 50, 1, time.AddSeconds(5)) });
        Require(await CountAsync(dataSource) == 4, "После отката хранилище должно продолжать работать.");
        Console.WriteLine("OK: запись после отката работает.");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await ExpectCancellationAsync(() => store.GetLatestAsync(7, cancellation.Token));
        await ExpectCancellationAsync(() => store.GetHistoryAsync(7, time, time, 100, cancellation.Token));
        await ExpectCancellationAsync(() => store.AddBatchAsync(
            new List<TelemetryRecord> { new(7, 70, 1, time) }, cancellation.Token));
        Require(await CountAsync(dataSource) == 4, "Отменённая запись не должна изменить таблицу.");
        Console.WriteLine("OK: предварительная отмена передана во все три операции.");
    }
    finally
    {
        await ExecuteAsync(admin, $"DROP SCHEMA {schema} CASCADE;");
        Console.WriteLine("Тестовая схема удалена.");
    }
}

static async Task ExecuteAsync(NpgsqlDataSource dataSource, string sql)
{
    await using var command = dataSource.CreateCommand(sql);
    await command.ExecuteNonQueryAsync();
}

static async Task<long> CountAsync(NpgsqlDataSource dataSource)
{
    await using var command = dataSource.CreateCommand("SELECT COUNT(*) FROM telemetry_records;");
    return (long)(await command.ExecuteScalarAsync())!;
}

static async Task ExpectCancellationAsync(Func<Task> operation)
{
    try
    {
        await operation();
    }
    catch (OperationCanceledException)
    {
        return;
    }

    throw new InvalidOperationException("Операция проигнорировала предварительно отменённый токен.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
