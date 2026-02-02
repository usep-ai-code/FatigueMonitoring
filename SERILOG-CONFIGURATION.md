# Serilog Configuration Guide

Aplikasi ini menggunakan **Serilog** sebagai logging framework untuk memberikan logging yang lebih fleksibel dan powerful dibanding default ASP.NET Core logging.

## Fitur Serilog yang Digunakan

### 1. Multiple Sinks (Output Destinations)
- **Console**: Output ke console dengan format yang readable
- **File**: Rotating log files dengan retention policy
- **Seq** (Optional): Structured logging server untuk production monitoring

### 2. Structured Logging
Serilog mendukung structured logging yang memudahkan searching dan filtering:

```csharp
logger.LogInformation("User {UserId} fetched {EventCount} events from {StartTime} to {EndTime}", 
    userId, eventCount, startTime, endTime);
```

Output akan tersimpan sebagai structured data, bukan plain text.

### 3. Log Enrichment
Log otomatis diperkaya dengan informasi tambahan:
- Machine Name
- Thread ID
- Environment Name (Development/Production)
- Application Name
- Request Context (untuk HTTP requests)

### 4. Request Logging
Semua HTTP requests otomatis di-log dengan informasi:
- Method & Path
- Status Code
- Duration
- User Agent
- Remote IP
- Host

## Konfigurasi

### Production (appsettings.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/fatigue-monitoring-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentName" ],
    "Properties": {
      "Application": "FatigueMonitoring.Web.Api"
    }
  }
}
```

### Development (appsettings.Development.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/dev-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## Log Levels

### Level Hierarchy (dari rendah ke tinggi)
1. **Verbose** / **Debug**: Informasi detail untuk debugging
2. **Information**: Event normal aplikasi
3. **Warning**: Event yang perlu perhatian tapi tidak critical
4. **Error**: Error yang terjadi tapi aplikasi masih berjalan
5. **Fatal**: Error critical yang menyebabkan aplikasi crash

### Kapan Menggunakan Log Level

**Debug/Verbose:**
```csharp
logger.LogDebug("Fetching events from {StartDate} to {EndDate}", startDate, endDate);
logger.LogTrace("Processing item {ItemId} with value {Value}", itemId, value);
```

**Information:**
```csharp
logger.LogInformation("Background job started");
logger.LogInformation("Successfully fetched {Count} events", count);
logger.LogInformation("Data aggregation completed");
```

**Warning:**
```csharp
logger.LogWarning("External API rate limit hit, retrying in {Seconds}s", seconds);
logger.LogWarning("Invalid sync time configuration, using default");
```

**Error:**
```csharp
logger.LogError(ex, "Error fetching events from External API");
logger.LogError(ex, "Failed to aggregate data");
```

**Fatal:**
```csharp
logger.LogFatal(ex, "Application failed to start");
logger.LogFatal(ex, "Database connection failed");
```

## Output Template Format

### Console Output (Production)
```
[2026-02-01 10:30:45.123 +00:00] [INF] [FatigueMonitoring.Services.DataAggregationService] Successfully fetched 25 events
```

Format:
- `Timestamp`: Waktu dengan timezone
- `Level`: Log level (DBG, INF, WRN, ERR, FTL)
- `SourceContext`: Nama class yang menghasilkan log
- `Message`: Pesan log
- `Exception`: Stack trace jika ada error

### Console Output (Development - Simplified)
```
[10:30:45.123] [INF] Successfully fetched 25 events
```

## File Logging

### Location
Logs disimpan di folder `logs/` di root aplikasi:
```
logs/
  ├── fatigue-monitoring-20260201.log
  ├── fatigue-monitoring-20260202.log
  └── dev-20260201.log
```

### Rolling Policy
- **Production**: File baru setiap hari (`fatigue-monitoring-YYYYMMDD.log`)
- **Development**: File baru setiap hari (`dev-YYYYMMDD.log`)
- **Retention**: Hanya 7 hari terakhir yang disimpan (configurable)

### File Size
Jika ingin limit file size, tambahkan konfigurasi:

```json
{
  "Name": "File",
  "Args": {
    "path": "logs/fatigue-monitoring-.log",
    "rollingInterval": "Day",
    "retainedFileCountLimit": 7,
    "fileSizeLimitBytes": 10485760,
    "rollOnFileSizeLimit": true
  }
}
```

## Request Logging

Setiap HTTP request otomatis di-log:

```
[10:30:45.123] [INF] HTTP GET /api/dashboard/stats responded 200 in 45.3456 ms
```

Enriched dengan:
- Request Host
- Request Scheme (http/https)
- User Agent
- Remote IP Address

## Advanced Configuration

### 1. Add Seq Sink (Structured Logging Server)

Install di server (Docker):
```bash
docker run --name seq -d --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  datalust/seq:latest
```

Update `appsettings.json`:
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

### 2. Azure Application Insights

Install package:
```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```

Update configuration:
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "InstrumentationKey=your-key-here",
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
        }
      }
    ]
  }
}
```

### 3. Filter by Namespace

Untuk reduce noise dari specific namespaces:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "Microsoft.EntityFrameworkCore.Infrastructure": "Warning"
      }
    }
  }
}
```

### 4. Conditional Logging

Di code, gunakan `LogLevel` check untuk expensive operations:

```csharp
if (logger.IsEnabled(LogLevel.Debug))
{
    var detailData = ExpensiveOperation();
    logger.LogDebug("Detail data: {@Data}", detailData);
}
```

## Best Practices

### 1. Use Structured Logging
❌ **Bad:**
```csharp
logger.LogInformation($"User {userId} fetched {count} events");
```

✅ **Good:**
```csharp
logger.LogInformation("User {UserId} fetched {EventCount} events", userId, count);
```

### 2. Don't Log Sensitive Data
```csharp
// ❌ Don't log passwords, tokens, etc
logger.LogInformation("Login successful for {Username} with password {Password}", username, password);

// ✅ Log only safe information
logger.LogInformation("Login successful for {Username}", username);
```

### 3. Use Appropriate Log Levels
- Don't use `Information` for debugging details
- Don't use `Error` for expected exceptions (like validation failures)
- Use `Warning` for recoverable issues

### 4. Log Context Information
```csharp
using (logger.BeginScope(new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["TenantId"] = tenantId
}))
{
    // All logs in this scope will include UserId and TenantId
    logger.LogInformation("Processing request");
}
```

### 5. Exception Logging
Always pass exception as first parameter:

```csharp
// ❌ Bad
logger.LogError("Error occurred: " + ex.Message);

// ✅ Good
logger.LogError(ex, "Error fetching events from External API");
```

## Troubleshooting

### Problem: Logs tidak muncul di file

**Check:**
1. Folder `logs/` writable?
2. `appsettings.json` syntax correct?
3. Log level configuration blocking messages?

**Solution:**
```bash
# Check permissions
ls -la logs/

# Check configuration
cat appsettings.json | grep -A 20 "Serilog"
```

### Problem: Terlalu banyak logs

**Solution:**
Adjust minimum level:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"  // Only Warning and above
    }
  }
}
```

### Problem: Logs tidak readable

**Solution:**
Use custom output template untuk readability yang lebih baik.

## Monitoring in Production

### 1. File Size Monitoring
Setup alert jika log files terlalu besar:

```bash
# Check log file sizes
du -sh logs/
```

### 2. Error Rate Monitoring
Parse logs untuk count errors:

```bash
# Count errors today
grep "ERR" logs/fatigue-monitoring-$(date +%Y%m%d).log | wc -l
```

### 3. Log Aggregation
Untuk production, consider:
- **Seq**: Self-hosted structured log server
- **Azure Application Insights**: Cloud logging & monitoring
- **ELK Stack**: Elasticsearch + Logstash + Kibana
- **Splunk**: Enterprise log management

## Example Log Output

### Startup
```
[2026-02-01 10:00:00.000 +00:00] [INF] Starting Fatigue Monitoring Web API
[2026-02-01 10:00:00.123 +00:00] [INF] [Microsoft.Hosting.Lifetime] Now listening on: http://localhost:5000
[2026-02-01 10:00:00.124 +00:00] [INF] [Microsoft.Hosting.Lifetime] Application started. Press Ctrl+C to shut down.
```

### Background Job
```
[2026-02-01 10:00:10.000 +00:00] [INF] [FatigueMonitoring.Services.BackgroundAggregationJob] Background Aggregation Job started with 3 minute interval
[2026-02-01 10:00:20.000 +00:00] [INF] [FatigueMonitoring.Services.BackgroundAggregationJob] Running data aggregation cycle at 2026-02-01T10:00:20Z
[2026-02-01 10:00:20.500 +00:00] [INF] [FatigueMonitoring.Services.DataAggregationService] Starting data aggregation at 2026-02-01T10:00:20Z
[2026-02-01 10:00:21.234 +00:00] [INF] [FatigueMonitoring.Services.DataAggregationService] Fetching events from 2026-02-01T09:57:00Z to 2026-02-01T10:00:00Z
[2026-02-01 10:00:23.456 +00:00] [INF] [FatigueMonitoring.Services.ExternalApiService] Successfully obtained access token
[2026-02-01 10:00:25.789 +00:00] [INF] [FatigueMonitoring.Services.ExternalApiService] Successfully fetched 42 events
[2026-02-01 10:00:26.123 +00:00] [INF] [FatigueMonitoring.Services.DataAggregationService] Saved 42 raw events
[2026-02-01 10:00:27.456 +00:00] [INF] [FatigueMonitoring.Services.DataAggregationService] Aggregated 42 active alerts
[2026-02-01 10:00:27.789 +00:00] [INF] [FatigueMonitoring.Services.DataAggregationService] Data aggregation completed successfully. Next sync at: 2026-02-01T10:03:00Z
```

### HTTP Request
```
[2026-02-01 10:05:30.123 +00:00] [INF] HTTP GET /api/dashboard/stats responded 200 in 45.3456 ms
[2026-02-01 10:05:31.456 +00:00] [INF] HTTP GET /api/sse/stream responded 200 in 1234.5678 ms
```

### Error
```
[2026-02-01 10:10:00.000 +00:00] [ERR] [FatigueMonitoring.Services.ExternalApiService] Error fetching events from External API
System.Net.Http.HttpRequestException: Connection refused
   at System.Net.Http.HttpClient.SendAsync(HttpRequestMessage request)
   at FatigueMonitoring.Services.ExternalApiService.GetEventsAsync(DateTime startDate, DateTime endDate)
```

---

**Created:** February 1, 2026  
**Last Updated:** February 1, 2026
