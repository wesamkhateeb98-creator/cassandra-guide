using Shortly.Infrastructure;
using Shortly.Repositories;
using ISession = Cassandra.ISession;

namespace Shortly.Extensions;

public static class Registration
{
    // Cassandra: connect, create schema, prepare statements — all once at startup.
    public static async Task<IServiceCollection> AddCassandraAsync(this IServiceCollection services, IConfiguration configuration)
    {
        var session = await CassandraSetup.ConnectAsync(configuration);
        await CassandraSetup.CreateSchemaAsync(session);

        return services
            .AddSingleton(session)
            .AddSingleton<ILinkRepository>(await LinkRepository.CreateAsync(session))
            .AddSingleton<IClickRepository>(await ClickRepository.CreateAsync(session));
    }

    // Instances passed to AddSingleton(obj) are not disposed by the container — close the pools ourselves.
    public static WebApplication CloseCassandraOnStop(this WebApplication app)
    {
        var session = app.Services.GetRequiredService<ISession>();
        app.Lifetime.ApplicationStopped.Register(session.Cluster.Dispose);
        return app;
    }
}
