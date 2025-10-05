using JasperFx;
using JasperFx.Events.Projections;
using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.UserManagement;
using Microsoft.Extensions.DependencyInjection;

namespace MartenAkkaTests.Api.Tests.Infrastructure;

[Obsolete("Use ServiceTestBase instead - this uses full integration setup")]
public abstract class IntegrationTestBase
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected IDocumentStore DocumentStore { get; private set; } = null!;

    [SetUp]
    public void BaseSetup()
    {
        var services = new ServiceCollection();

        // Register test doubles
        services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider());
        services.AddSingleton<IGuidProvider>(new FakeGuidProvider());

        // In-Memory Marten Store
        services.AddMarten(options =>
            {
                options.Connection("Host=localhost;Port=5435;Database=test_db;Username=postgres;Password=postgres");
                options.AutoCreateSchemaObjects = AutoCreate.All;
                options.CreateDatabasesForTenants(c =>
                {
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });

                options.DatabaseSchemaName = $"test_{Guid.NewGuid():N}"; // Isolierte Schema pro Test

                options.Schema.For<Session>().Identity(x => x.SessionId);
                options.Schema.For<User>().Identity(x => x.UserId);

                // Register Projections
                options.Projections.Add<SessionProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
            })
            .UseLightweightSessions();

        // Register command services
        services.AddScoped<SessionCommandService>();
        services.AddScoped<UserCommandService>();

        ServiceProvider = services.BuildServiceProvider();
        DocumentStore = ServiceProvider.GetRequiredService<IDocumentStore>();
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        // Cleanup: Schema droppen
        await DocumentStore.Advanced.Clean.CompletelyRemoveAllAsync();
        ((IDisposable)ServiceProvider).Dispose();
        DocumentStore.Dispose();
    }
}
