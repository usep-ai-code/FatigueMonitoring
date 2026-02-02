# Fatigue Monitoring Web API

Backend API for the Fatigue Monitoring Dashboard.

## Features

- **SSE (Server-Sent Events)**: Real-time data streaming to frontend
- **Background Job**: Automatic data fetching and aggregation
- **External API Integration**: Fetches data from Transtrack API
- **Heartbeat Mechanism**: Keeps SSE connections alive

## Architecture

### Services

- `ExternalApiService` - Handles authentication and data fetching from Transtrack API
- `DataAggregationService` - Calculates and stores aggregated data
- `BackgroundJobService` - Periodic data fetching and aggregation
- `SseBroadcastService` - Broadcasts updates to connected clients
- `SseConnectionManager` - Manages SSE client connections

### Database Tables

All tables use the `AI_` prefix:

| Table | Description |
|-------|-------------|
| `AI_FatigueEvent_T` | Raw event data |
| `AI_DashboardSummary_T` | KPI aggregations |
| `AI_AreaDistribution_T` | Location-based distribution |
| `AI_ActiveAlert_T` | Active alerts |
| `AI_DelayedFollowUp_T` | Delayed follow-ups (>30 min) |
| `AI_RecurrentUnit_T` | Recurring fatigue units |
| `AI_HighRiskArea_T` | High-risk areas |
| `AI_TokenCache_T` | API token storage |

## API Endpoints

### Dashboard

```
GET  /api/dashboard?area={All|Mining|Hauling}
GET  /api/dashboard/stream
GET  /api/dashboard/connections
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;User Id=...;Password=..."
  },
  "ExternalApi": {
    "BaseUrl": "https://api-platform-integrator.transtrack.co/api/v1",
    "Username": "...",
    "Password": "...",
    "TokenRefreshIntervalMinutes": 1
  },
  "BackgroundJob": {
    "DataFetchIntervalSeconds": 60,
    "HeartbeatIntervalSeconds": 30
  }
}
```

## Database Migration

### Option 1: EF Core Migration (Recommended)

```bash
dotnet ef database update
```

### Option 2: SQL Script

Run `Migrations/CreateTables.sql` directly on the database.

## Running Locally

```bash
dotnet restore
dotnet run
```

The API will be available at `https://localhost:7001`.

## External API Integration

### Authentication
- Endpoint: `POST /api/v1/vss/auth`
- Token refresh: Every 1 minute (API restriction)

### Events
- Endpoint: `POST /api/v1/events/`
- Filter: `manual_verification_is_true_alarm = true` AND `level = 3`

### Follow-ups
- Endpoint: `GET /api/v1/evidence/{alarmGuid}/follow-ups`

## Deployment

### Azure App Service

1. Configure connection strings in Application Settings
2. Enable Application Insights
3. Set environment variables for External API credentials
