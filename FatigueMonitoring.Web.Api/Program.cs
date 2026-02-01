using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply CORS before other middleware
app.UseCors("AllowReactApp");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations. Attempting to ensure database is created...");
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database created/ensured successfully");
        }
        catch (Exception innerEx)
        {
            logger.LogError(innerEx, "Failed to ensure database creation");
        }
    }
}

app.Run();
