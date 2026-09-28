using Shortly.Extensions;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.AddCassandraAsync(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
app.CloseCassandraOnStop();
app.MapApiDocs();

app.MapControllers();
app.Run();
