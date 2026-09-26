using CassandraDemo;

using var cluster = CassandraConnection.BuildCluster(
    Environment.GetEnvironmentVariable("CASSANDRA_HOST") ?? "127.0.0.1");
using var session = await cluster.ConnectAsync();

await CassandraConnection.CreateSchemaAsync(session, Path.Combine(AppContext.BaseDirectory, "schema.cql"));
Console.WriteLine("✅ schema ready");

// ---------- Example 1: Chat ----------
var chat = await ChatRepository.CreateAsync(session);
var conversation = Guid.NewGuid();
var (sara, omar) = (Guid.NewGuid(), Guid.NewGuid());

for (var i = 1; i <= 7; i++)
    await chat.SendAsync(conversation, i % 2 == 0 ? sara : omar, i % 2 == 0 ? omar : sara, $"message #{i}");

var bucket = ChatRepository.BucketOf(DateTimeOffset.UtcNow);
byte[]? pagingState = null;
var pageNo = 0;
do
{
    var page = await chat.GetMessagesAsync(conversation, bucket, pageSize: 3, pagingState);
    Console.WriteLine($"page {++pageNo}: {string.Join(", ", page.Messages.Select(m => m.Body))}");
    pagingState = page.PagingState;
} while (pagingState is not null);

var email = $"sara-{conversation:N}@example.com";
Console.WriteLine($"claim email (sara): {await chat.TryClaimEmailAsync(email, sara)}");
Console.WriteLine($"claim email (omar): {await chat.TryClaimEmailAsync(email, omar)}");

// ---------- Example 2: IoT ----------
var iot = await IotRepository.CreateAsync(session);
var device = $"device-{Random.Shared.Next(1000, 9999)}";
var start = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(1), TimeSpan.Zero); // 01:00 today, whole seconds

for (var m = 0; m < 10; m++)
    await iot.RecordAsync(device, start.AddMinutes(m), temperature: 70 + m * 2, humidity: m % 3 == 0 ? null : 40);

var readings = await iot.GetReadingsAsync(device, IotRepository.DayOf(start), start, start.AddMinutes(10));
Console.WriteLine($"readings: {readings.Count} (newest {readings[0].Temperature})");

var alerts = await iot.GetAlertsAsync(IotRepository.DayOf(start));
Console.WriteLine($"alerts today for {device}: {alerts.Count(a => a.DeviceId == device)}");
