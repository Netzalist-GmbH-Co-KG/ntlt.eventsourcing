using System.Reflection;
using FluentValidation;
using Marten;
using ntlt.eventsourcing.core;
using ntlt.eventsourcing.autx;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings.json

const string appName = "ES Demo Host";

// ##### CORE
builder.Host.AddNtltSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.InitNtltDocGroupings(appName);
    options.InitNtltSecurity();
});

// FluentValidation - automatically discovers and registers all validators in assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.InitNtltEventSourcing();

// ##### AUTX
builder.Services.AddControllers(options =>
    {
        options.InjectModelBinder();
    })
    .AddApplicationPart(Assembly.Load("ntlt.eventsourcing.autx"));

// Register HttpContextAccessor for session retrieval optimization
builder.Services.AddHttpContextAccessor();
builder.Services.RegisterAutxServices();

// ##### MARTEN

// Add Marten and Register Schemas and Projections
var connectionString = builder.Configuration.GetConnectionString("EventSourcing")
    ?? throw new InvalidOperationException("Connection string 'EventSourcing' not found.");

await builder.EnsureDatabaseExists(connectionString);

builder.Services.AddMarten(options =>
    {
        options.InitNtltEventSourcing(connectionString);
        options.RegisterAutxMarten();
    })
    .UseLightweightSessions();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

// IMPORTANT: Middleware must be registered BEFORE MapControllers
app.AddSessionValidationMiddleware();
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RegisterNtltSwaggerUi(appName);
});

try
{
    Log.Information("Starting demo ntlt.web api");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}