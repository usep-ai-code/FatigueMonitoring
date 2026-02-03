using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.AspNetCore;
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

    // Configure CORS for React frontend (read from configuration)
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:5173", "http://localhost:3000" };
    
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp", policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Configure Database with extended timeout for bulk operations
    builder.Services.AddDbContext<FatigueMonitoringDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
                sqlOptions.CommandTimeout(180); // 3 minutes timeout for bulk operations
            }
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
    // Enable OpenAPI and Scalar UI for API testing
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Fatigue Monitoring API");
        options.WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient);
    });

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
    
    // Serve static files from wwwroot (React build output)
    app.UseDefaultFiles();
    app.UseStaticFiles();
    
    app.UseAuthorization();

    app.MapControllers();
    
    // SPA Fallback: For any request that doesn't match an API route or static file,
    // serve the React app's index.html (client-side routing)
    app.MapFallbackToFile("index.html");

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
        Log.Information("Scalar API Docs: {ScalarUrl}", $"{addresses.FirstOrDefault()}/scalar/v1");
        Log.Information("OpenAPI JSON: {OpenApiUrl}", $"{addresses.FirstOrDefault()}/openapi/v1.json");
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
