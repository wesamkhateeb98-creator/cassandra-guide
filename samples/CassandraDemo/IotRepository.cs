using Cassandra;
using Cassandra.Mapping;

namespace CassandraDemo;

public sealed class Reading
{
    public string DeviceId { get; set; } = "";
    public LocalDate Day { get; set; } = null!;
    public DateTimeOffset Ts { get; set; }
    public double Temperature { get; set; }
    public double? Humidity { get; set; }
}

public sealed class IotRepository
{
    public const int AlertBuckets = 16;
    public const double AlertThreshold = 80;

    private readonly ISession _session;
    private readonly IMapper _mapper;
    private readonly PreparedStatement _insertReading;
    private readonly PreparedStatement _upsertLatest;
    private readonly PreparedStatement _insertAlert;
    private readonly PreparedStatement _selectAlerts;

    static IotRepository()
    {
        MappingConfiguration.Global.Define(
            new Map<Reading>()
                .KeyspaceName("iot")
                .TableName("readings_by_device_day")
                .PartitionKey(r => r.DeviceId, r => r.Day)
                .ClusteringKey(r => r.Ts, SortOrder.Descending)
                .Column(r => r.DeviceId, c => c.WithName("device_id"))
                .Column(r => r.Day, c => c.WithName("day"))
                .Column(r => r.Ts, c => c.WithName("ts"))
                .Column(r => r.Temperature, c => c.WithName("temperature"))
                .Column(r => r.Humidity, c => c.WithName("humidity")));
    }

    private IotRepository(ISession session, PreparedStatement[] ps)
    {
        _session = session;
        _mapper = new Mapper(session);
        (_insertReading, _upsertLatest, _insertAlert, _selectAlerts) = (ps[0], ps[1], ps[2], ps[3]);
    }

    public static async Task<IotRepository> CreateAsync(ISession session)
    {
        var ps = await Task.WhenAll(
            session.PrepareAsync(
                "INSERT INTO iot.readings_by_device_day (device_id, day, ts, temperature, humidity) VALUES (?, ?, ?, ?, ?)"),
            session.PrepareAsync(
                "INSERT INTO iot.latest_reading_by_device (device_id, ts, temperature, humidity) VALUES (?, ?, ?, ?)"),
            session.PrepareAsync(
                "INSERT INTO iot.alerts_by_day (day, bucket, ts, device_id, temperature) VALUES (?, ?, ?, ?, ?)"),
            session.PrepareAsync(
                "SELECT ts, device_id, temperature FROM iot.alerts_by_day WHERE day = ? AND bucket = ?"));
        return new IotRepository(session, ps);
    }

    public static LocalDate DayOf(DateTimeOffset t) => new(t.Year, t.Month, t.Day);

    // Stable across processes (string.GetHashCode is randomized per process).
    public static int BucketOf(string deviceId) =>
        (int)(deviceId.Aggregate(2166136261u, (h, c) => (h ^ c) * 16777619u) % AlertBuckets);

    public async Task RecordAsync(string deviceId, DateTimeOffset ts, double temperature, double? humidity)
    {
        var day = DayOf(ts);
        // Unset.Value instead of null → no tombstone for a missing column.
        object humidityValue = humidity.HasValue ? humidity.Value : Unset.Value;

        var writes = new List<Task>
        {
            _session.ExecuteAsync(_insertReading.Bind(deviceId, day, ts, temperature, humidityValue).SetIdempotence(true)),
            _session.ExecuteAsync(_upsertLatest.Bind(deviceId, ts, temperature, humidityValue).SetIdempotence(true)),
        };
        if (temperature > AlertThreshold)
            writes.Add(_session.ExecuteAsync(
                _insertAlert.Bind(day, BucketOf(deviceId), ts, deviceId, temperature).SetIdempotence(true)));

        await Task.WhenAll(writes);
    }

    /// <summary>Mapper (object ↔ row). Single partition, clustering range.</summary>
    public async Task<IReadOnlyList<Reading>> GetReadingsAsync(string deviceId, LocalDate day, DateTimeOffset from, DateTimeOffset to)
    {
        var rows = await _mapper.FetchAsync<Reading>(
            "WHERE device_id = ? AND day = ? AND ts >= ? AND ts < ?", deviceId, day, from, to);
        // The result wraps a forward-only RowSet: enumerate it exactly once.
        return rows.ToList();
    }

    /// <summary>Scatter-gather over all buckets in parallel, one partition each.</summary>
    public async Task<IReadOnlyList<(DateTimeOffset Ts, string DeviceId, double Temperature)>> GetAlertsAsync(LocalDate day)
    {
        var pages = await Task.WhenAll(Enumerable.Range(0, AlertBuckets).Select(bucket =>
            _session.ExecuteAsync(_selectAlerts.Bind(day, bucket).SetIdempotence(true))));

        return pages
            .SelectMany(rs => rs.Select(row => (
                row.GetValue<DateTimeOffset>("ts"),
                row.GetValue<string>("device_id"),
                row.GetValue<double>("temperature"))))
            .OrderByDescending(a => a.Item1)
            .ToList();
    }
}
