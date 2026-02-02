using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    
    // Add Swagger for API testing
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

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
        .ConfigureWarnings(warnings => warnings
            .Ignore(RelationalEventId.PendingModelChangesWarning))
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
    // Enable Swagger in all environments for testing
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fatigue Monitoring API v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
    });
    
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

    // Log the URLs/ports the application is listening on
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = app.Urls;
        Log.Information("========================================");
        Log.Information("Application started successfully!");
        Log.Information("========================================");
        Log.Information("Listening on the following URLs:");
        foreach (var address in addresses)
        {
            Log.Information("  → {Address}", address);
        }
        Log.Information("========================================");
        Log.Information("Swagger UI: {SwaggerUrl}", $"{addresses.FirstOrDefault()}/swagger");
        Log.Information("Dashboard API: {ApiUrl}", $"{addresses.FirstOrDefault()}/api/dashboard");
        Log.Information("SSE Stream: {SseUrl}", $"{addresses.FirstOrDefault()}/api/dashboard/stream");
        Log.Information("========================================");
    });
    
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
