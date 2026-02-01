using System.Text.Json;
using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Options;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fatigue Monitoring API",
        Version = "v1"
    });
});

builder.Services.Configure<ExternalApiOptions>(
    builder.Configuration.GetSection(ExternalApiOptions.SectionName));
builder.Services.Configure<AggregationOptions>(
    builder.Configuration.GetSection(AggregationOptions.SectionName));
builder.Services.Configure<SseOptions>(
    builder.Configuration.GetSection(SseOptions.SectionName));

builder.Services.AddDbContext<DashboardDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddHttpClient<ExternalApiClient>((serviceProvider, client) =>
{
    var apiOptions = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalApiOptions>>()
        .Value;
    if (!string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
    {
        client.BaseAddress = new Uri(apiOptions.BaseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
});

builder.Services.AddHostedService<AggregationWorker>();
builder.Services.AddScoped<DashboardSnapshotService>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fatigue Monitoring API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();

app.Run();
