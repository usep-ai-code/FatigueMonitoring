using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog bootstrap logger
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateBootstrapLogger();

try
{
    Log.Information("========================================");
    Log.Information("Starting Fatigue Monitoring Web API");
    Log.Information("========================================");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Configure CORS for React frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:3000",
                    "https://*.azurewebsites.net"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Configure Database
    builder.Services.AddDbContext<FatigueMonitoringDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            )
        )
    );

    // Configure External API Settings
    builder.Services.Configure<ExternalApiSettings>(
        builder.Configuration.GetSection("ExternalApi")
    );

    // Configure Background Job Settings
    builder.Services.Configure<BackgroundJobSettings>(
        builder.Configuration.GetSection("BackgroundJob")
    );

    // Register HttpClient for External API
    builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // Register Services
    builder.Services.AddScoped<IDataAggregationService, DataAggregationService>();
    builder.Services.AddSingleton<ISseConnectionManager, SseConnectionManager>();

    // Register Background Services
    builder.Services.AddHostedService<BackgroundJobService>();
    builder.Services.AddHostedService<SseBroadcastService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        };
    });

    // Apply CORS before other middleware
    app.UseCors("AllowReactApp");

    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.MapControllers();

    // Apply database migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();
        
        try
        {
            Log.Information("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error applying database migrations. Attempting to ensure database is created...");
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                Log.Information("Database created/ensured successfully");
            }
            catch (Exception innerEx)
            {
                Log.Error(innerEx, "Failed to ensure database creation");
            }
        }
    }

    Log.Information("Application started. Listening for requests...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Application shutting down...");
    await Log.CloseAndFlushAsync();
}
