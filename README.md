# Fatigue Monitoring Dashboard

A real-time dashboard for monitoring fatigue alerts in mining and hauling operations.

## Overview

This application is a **Fatigue Command Center - Operational Monitoring Dashboard** that monitors fatigue alerts from operators in real-time. It provides:

- Real-time KPI monitoring (Total Alarms, Followed Up, Waiting Follow Up)
- Area distribution visualization (Mining vs Hauling)
- Active alerts list with detailed information
- Delayed follow-up tracking (>30 minutes)
- Recurrent units identification
- High-risk area analysis

## Architecture

```
External API (Transtrack)
        ↓ (Background Job - every 60s)
    Raw Data Table
        ↓ (Background Job - aggregation)
AI_ Aggregation Tables
        ↓
    SSE Endpoint
        ↓
  React Dashboard
```

## Projects

### Backend - FatigueMonitoring.Web.Api
- **Framework**: ASP.NET Core (.NET 10)
- **Database**: SQL Server
- **Features**:
  - Server-Sent Events (SSE) for real-time updates
  - Background job for external API data fetching
  - Automatic data aggregation
  - Heartbeat mechanism for connection health

### Frontend - fatigue-monitoring.react
- **Framework**: React 19 with Vite
- **Styling**: Tailwind CSS
- **Features**:
  - Real-time data updates via SSE
  - SSE connection status indicator (Connecting/Connected/Disconnected)
  - Dark/Light mode toggle
  - Responsive design for command center displays
  - Notification pop-ups for new alerts

### Mockup - fatigue-monitoring.mockup
- Original mockup with dummy data for design reference

## Database Tables (AI_ prefix)

All aggregation tables use the `AI_` prefix and `_T` postfix:

- `AI_FatigueEvent_T` - Raw event data from external API
- `AI_DashboardSummary_T` - KPI aggregation data
- `AI_AreaDistribution_T` - Location-based alert distribution
- `AI_ActiveAlert_T` - Currently active alerts
- `AI_DelayedFollowUp_T` - Alerts with delayed follow-up (>30 min)
- `AI_RecurrentUnit_T` - Units with recurring fatigue events
- `AI_HighRiskArea_T` - Areas with high fatigue event frequency
- `AI_TokenCache_T` - External API token storage
- `AI_SyncState_T` - Sync state tracking (last sync time, status)

## Configuration

### Backend (appsettings.json)

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
    "DataFetchIntervalSeconds": 180,
    "HeartbeatIntervalSeconds": 30,
    "InitialStartTime": "2026-02-01 00:00:00",
    "FetchWindowMinutes": 3
  }
}
```

#### BackgroundJob Settings Explained:

| Setting | Description | Default |
|---------|-------------|---------|
| `DataFetchIntervalSeconds` | Interval between data fetches (180s = 3 minutes) | 180 |
| `HeartbeatIntervalSeconds` | SSE heartbeat interval | 30 |
| `InitialStartTime` | Start time for first data fetch (WIB timezone) | 2026-02-01 00:00:00 |
| `FetchWindowMinutes` | Window size for each fetch (minutes) | 3 |

**Note**: The system stores the last sync time in `AI_SyncState_T` table. Each fetch retrieves data from `lastSyncTime` to `lastSyncTime + FetchWindowMinutes`. This ensures no data is missed even if the service restarts.
```

### Frontend (.env)

```
VITE_API_URL=https://localhost:7001
```

## Running the Application

### Backend

```bash
cd FatigueMonitoring.Web.Api
dotnet restore
dotnet run
```

### Frontend

```bash
cd fatigue-monitoring.react
npm install
npm run dev
```

## API Endpoints

### Dashboard API

- `GET /api/dashboard` - Get dashboard data with optional area filter
- `GET /api/dashboard/stream` - SSE endpoint for real-time updates
- `GET /api/dashboard/connections` - Get current SSE connection count

## External API Integration

The application integrates with Transtrack API:

- **Auth**: `POST /api/v1/vss/auth` - Authentication
- **Events**: `POST /api/v1/events/` - Get fatigue events
- **Follow-ups**: `GET /api/v1/evidence/{alarmGuid}/follow-ups` - Get follow-up data

## Deployment

This application is designed for deployment on Azure App Service.

### Backend
- Deploy as Azure App Service (Windows or Linux)
- Configure connection strings and app settings in Azure
- Enable Application Insights for monitoring

### Frontend
- Build with `npm run build`
- Deploy to Azure Static Web Apps or App Service
- Configure environment variables for API URL

## License

Proprietary - Alamtri Resources Ltd.
