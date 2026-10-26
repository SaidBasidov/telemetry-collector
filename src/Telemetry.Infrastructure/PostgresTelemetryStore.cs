using Npgsql;

public class PostgresTelemetryStore : ITelemetryStore
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresTelemetryStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<TelemetryRecord?> GetLatestAsync(int tagId)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT tag_id, value, quality, timestamp
            FROM telemetry_records
            WHERE tag_id = $1
            ORDER BY timestamp DESC, id DESC
            LIMIT 1;
            """
        );

        command.Parameters.Add(new NpgsqlParameter { Value = tagId });

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new TelemetryRecord(
                reader.GetInt32(0),
                reader.GetDouble(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateTimeOffset>(3)
            );
        }

        return null;
    }

    public async Task<List<TelemetryRecord>> GetHistoryAsync(
    int tagId,
    DateTimeOffset from,
    DateTimeOffset to,
    int limit = 100)
    {
        await using var command = _dataSource.CreateCommand(
            """
        SELECT tag_id, value, quality, timestamp
        FROM telemetry_records
        WHERE tag_id = $1
          AND timestamp >= $2
          AND timestamp <= $3
        ORDER BY timestamp DESC, id DESC
        LIMIT $4;
        """
        );

        command.Parameters.Add(new NpgsqlParameter { Value = tagId });
        command.Parameters.Add(new NpgsqlParameter { Value = from.ToUniversalTime() });
        command.Parameters.Add(new NpgsqlParameter { Value = to.ToUniversalTime() });
        command.Parameters.Add(new NpgsqlParameter { Value = limit });

        await using var reader = await command.ExecuteReaderAsync();

        var records = new List<TelemetryRecord>();

        while (await reader.ReadAsync())
        {
            var record = new TelemetryRecord(
                reader.GetInt32(0),
                reader.GetDouble(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateTimeOffset>(3)
            );

            records.Add(record);
        }

        return records;
    }

    public async Task AddBatchAsync(
    List<TelemetryRecord> records,
    CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            return;
        }

        await using var connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(connection);

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var command = new NpgsqlBatchCommand(
                """
            INSERT INTO telemetry_records (tag_id, timestamp, value, quality)
            VALUES ($1, $2, $3, $4);
            """
            );

            command.Parameters.Add(new NpgsqlParameter { Value = record.TagId });
            command.Parameters.Add(new NpgsqlParameter
            {
                Value = record.Timestamp.ToUniversalTime()
            });
            command.Parameters.Add(new NpgsqlParameter { Value = record.Value });
            command.Parameters.Add(new NpgsqlParameter { Value = record.Quality });

            batch.BatchCommands.Add(command);
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
    }
}