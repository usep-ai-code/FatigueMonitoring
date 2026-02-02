# Fatigue Monitoring Dashboard

A real-time fatigue monitoring dashboard for mining and hauling operations. This application monitors driver fatigue alerts, tracks follow-up status, and provides insights into high-risk units and areas.

## 🏗️ Architecture

### Stack

**Backend:**
- ASP.NET Core (.NET 10)
- Entity Framework Core 10
- SQL Server
- Serilog (Structured Logging)
- Server-Sent Events (SSE)
- Background Jobs for data aggregation

**Frontend:**
- React 19
- Vite
- Tailwind CSS
- Real-time SSE updates

### Data Flow

```
External API (TransTrack)
    ↓ (Background Job - every 5 minutes)
Raw Data Tables
    ↓ (Background Job - aggregation)
AI_ Aggregation Tables (_T suffix)
    ↓ (SSE Endpoint)
React Dashboard (Real-time updates)
```

## 📊 Features

### Real-Time Monitoring
- **SSE Connection**: Live updates every 3 seconds with automatic reconnection
- **Connection Status Indicator**: Visual feedback (Connected/Connecting/Disconnected)
- **Heartbeat Mechanism**: Keeps connection alive with 15-second intervals

### Dashboard Components

1. **KPI Cards**
   - Total Alarms (All/Mining/Hauling)
   - Followed Up count
   - Waiting Follow Up count

2. **Active Alerts**
   - Real-time list of open fatigue alerts
   - Unit details, operator name, location
   - Open duration tracking
   - Click to view detailed modal

3. **Area Distribution**
   - Alert distribution by location
   - Separate views for Mining and Hauling
   - Click to filter alerts by location

4. **Delayed Follow-Up**
   - Alerts open for more than 30 minutes
   - Priority visual indicators

5. **Strategic Insights**
   - Recurrent Units: Operators with multiple events
   - High Risk Areas: Locations with frequent alerts

### User Interface Features
- Dark/Light mode toggle
- Dynamic pagination based on available screen space
- Responsive layout
- Real-time notifications for new alerts
- Global area filter (All/Mining/Hauling)

## 🗄️ Database Schema

### Raw Data Tables
- `RawEvents`: Raw event data from external API
- `RawFollowUps`: Follow-up information for events

### Aggregation Tables (AI_*_T prefix/suffix)
- `AI_DashboardStats_T`: KPI statistics by area
- `AI_ActiveAlert_T`: Currently open alerts
- `AI_AreaDistribution_T`: Alert counts by location
- `AI_RecurrentUnit_T`: Units with multiple events
- `AI_HighRiskArea_T`: High-frequency alert locations

## 🚀 Setup Instructions

### Prerequisites
- .NET 9 SDK
- Node.js 18+ and npm
- SQL Server database
- Access to TransTrack External API

### Backend Setup

1. **Navigate to API project:**
   ```bash
   cd FatigueMonitoring.Web.Api
   ```

2. **Update connection string in `appsettings.json`:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server;Database=your-db;User Id=your-user;Password=your-password"
     },
     "ExternalApi": {
       "Username": "sis@mdvr",
       "Password": "Sis@mdvr12345"
     }
   }
   ```

3. **Configure initial sync time in `appsettings.json`:**
   ```json
   {
     "BackgroundJob": {
       "IntervalMinutes": 3,
       "InitialStartTime": "2026-02-01 00:00:00"
     }
   }
   ```
   
   Adjust `InitialStartTime` to when you want to start syncing data from.

4. **Run migrations:**
   ```bash
   dotnet ef database update
   ```

5. **Run the API:**
   ```bash
   dotnet run
   ```

   The API will start on `http://localhost:5000`

6. **Verify sync is working:**
   ```bash
   curl http://localhost:5000/api/sync/status
   ```

   You should see the sync metadata with status and timestamps.

### Frontend Setup

1. **Navigate to React project:**
   ```bash
   cd fatigue-monitoring.react
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Update environment variables in `.env`:**
   ```
   VITE_API_URL=http://localhost:5000
   ```

4. **Run the development server:**
   ```bash
   npm run dev
   ```

   The frontend will start on `http://localhost:5173`

## 🔧 Configuration

### External API Rate Limiting
The application respects the TransTrack API rate limit of 1 login request per minute. The `ExternalApiService` automatically manages token refresh and rate limiting.

### Background Job Configuration
The background aggregation job runs every **3 minutes** and fetches data incrementally.

**Configuration in `appsettings.json`:**
```json
{
  "BackgroundJob": {
    "IntervalMinutes": 3,
    "InitialStartTime": "2026-02-01 00:00:00"
  }
}
```

**How it works:**
1. Job runs every 3 minutes
2. Fetches data from `LastSyncTime` to `LastSyncTime + 3 minutes`
3. Saves timestamp after successful sync
4. Next job uses the saved timestamp

**Important:** The `InitialStartTime` is only used on first run. After that, the system uses the saved timestamp from the database.

For detailed sync configuration, see [SYNC-CONFIGURATION.md](./SYNC-CONFIGURATION.md)

### SSE Update Frequency
SSE checks for updates every 3 seconds. To adjust:
```csharp
// In SseController.cs
await Task.Delay(3000, HttpContext.RequestAborted);
```

## 📡 API Endpoints

### Dashboard API
- `GET /api/dashboard/stats?area={area}` - Get KPI statistics
- `GET /api/dashboard/active-alerts?area={area}&location={location}` - Get active alerts
- `GET /api/dashboard/area-distribution?area={area}` - Get area distribution
- `GET /api/dashboard/recurrent-units?area={area}` - Get recurrent units
- `GET /api/dashboard/high-risk-areas?area={area}` - Get high-risk areas
- `GET /api/dashboard/delayed-alerts?area={area}` - Get delayed alerts (>30 min)

### Sync Management API
- `GET /api/sync/status` - Get current sync status and metadata
- `POST /api/sync/update-sync-time` - Update sync time manually
- `POST /api/sync/reset` - Reset sync to start from specific date
- `GET /api/sync/statistics` - Get sync statistics and event counts

### SSE Endpoint
- `GET /api/sse/stream` - Server-Sent Events stream

## 🌐 Deployment to Azure App Service

### Backend Deployment

1. **Publish the API:**
   ```bash
   cd FatigueMonitoring.Web.Api
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to Azure App Service:**
   - Create an Azure App Service (ASP.NET Core)
   - Deploy the `publish` folder
   - Update connection string in Azure Portal > Configuration

### Frontend Deployment

1. **Build the React app:**
   ```bash
   cd fatigue-monitoring.react
   npm run build
   ```

2. **Update `.env.production`:**
   ```
   VITE_API_URL=https://your-api-name.azurewebsites.net
   ```

3. **Deploy to Azure Static Web Apps or App Service:**
   - Deploy the `dist` folder
   - Configure SPA routing if needed

## 🔐 Security Considerations

1. **Connection Strings**: Store sensitive data in Azure Key Vault or App Service Configuration
2. **CORS**: Configure CORS properly for production
3. **API Authentication**: Consider adding authentication for the dashboard API
4. **External API Credentials**: Secure the TransTrack API credentials

## 📝 Development Notes

### Primary Constructors
The backend uses C# primary constructors for dependency injection:
```csharp
public class ExternalApiService(IConfiguration configuration, ILogger<ExternalApiService> logger)
{
    // Dependencies are automatically available as fields
}
```

### Area Determination
The application determines whether a unit belongs to Mining or Hauling based on:
- **Mining**: IPD, Kerinci, Pit, Front, D3-, EX- prefixes
- **Hauling**: CSA, KM, HD-, H prefixes

You can adjust this logic in `DataAggregationService.cs` > `DetermineArea()` method.

## 📋 Logging

The application uses **Serilog** for structured logging with the following features:
- Console output with colored levels
- File logging with daily rotation (7 days retention)
- Request logging for all HTTP requests
- Structured logging for easy searching and filtering

**Log locations:**
- Production: `logs/fatigue-monitoring-YYYYMMDD.log`
- Development: `logs/dev-YYYYMMDD.log`

For detailed logging configuration, see [SERILOG-CONFIGURATION.md](./SERILOG-CONFIGURATION.md)

## 🐛 Troubleshooting

### SSE Connection Issues
- Check CORS configuration
- Verify API is running and accessible
- Check browser console for errors
- Check logs in `logs/` folder
- Ensure firewall allows SSE connections

### Background Job Not Running
- Check logs in `logs/` folder for errors
- Verify External API credentials
- Check rate limiting (1 login per minute)
- Ensure Always On is enabled in Azure

### No Data Showing
- Verify database migrations are applied
- Check if background job has run at least once
- Check sync status: `GET /api/sync/status`
- Review logs for errors
- Verify External API is returning data

## 📄 License

This project is proprietary and confidential.

## 👥 Contributors

- Development Team

## 📞 Support

For issues and questions, contact the development team.

---

**Last Updated:** February 1, 2026
