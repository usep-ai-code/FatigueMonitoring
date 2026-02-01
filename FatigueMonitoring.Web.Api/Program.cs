using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<DashboardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TimeZoneProvider>();
builder.Services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));

builder.Services.Configure<ExternalApiOptions>(builder.Configuration.GetSection(ExternalApiOptions.SectionName));
builder.Services.Configure<DashboardOptions>(builder.Configuration.GetSection(DashboardOptions.SectionName));
builder.Services.Configure<BackgroundJobOptions>(builder.Configuration.GetSection(BackgroundJobOptions.SectionName));
builder.Services.Configure<SseOptions>(builder.Configuration.GetSection(SseOptions.SectionName));
builder.Services.Configure<AreaMappingOptions>(builder.Configuration.GetSection(AreaMappingOptions.SectionName));

builder.Services.AddScoped<AreaClassifier>();
builder.Services.AddScoped<DashboardAggregationService>();

builder.Services.AddHttpClient<ExternalApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<ExternalEventSyncJob>();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
