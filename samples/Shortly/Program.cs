using Shortly.Extensions;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.AddCassandraAsync(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();
app.CloseCassandraOnStop();

app.MapControllers();
app.Run();
