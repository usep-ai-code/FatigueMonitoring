# Quick Start Guide - Fatigue Monitoring Dashboard

Panduan cepat untuk menjalankan aplikasi Fatigue Monitoring Dashboard.

## Prasyarat

- .NET 9 SDK
- Node.js 18+
- SQL Server
- Akses ke TransTrack External API

## Langkah 1: Setup Backend

### 1.1 Konfigurasi appsettings.json

```bash
cd FatigueMonitoring.Web.Api
```

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true"
  },
  "ExternalApi": {
    "Username": "sis@mdvr",
    "Password": "Sis@mdvr12345"
  },
  "BackgroundJob": {
    "IntervalMinutes": 3,
    "InitialStartTime": "2026-02-01 00:00:00"
  }
}
```

**Penting:** Sesuaikan `InitialStartTime` dengan waktu mulai yang Anda inginkan untuk mengambil data.

### 1.2 Jalankan Migrasi Database

```bash
dotnet ef database update
```

### 1.3 Jalankan API

```bash
dotnet run
```

API akan berjalan di `http://localhost:5000`

### 1.4 Verifikasi Background Job

Buka browser atau gunakan curl:

```bash
curl http://localhost:5000/api/sync/status
```

Output:
```json
{
  "syncKey": "EventsSync",
  "lastSyncTime": "2026-02-01T00:00:00Z",
  "nextSyncTime": "2026-02-01T00:03:00Z",
  "status": "Success",
  "minutesUntilNextSync": 2.5
}
```

## Langkah 2: Setup Frontend

### 2.1 Install Dependencies

```bash
cd fatigue-monitoring.react
npm install
```

### 2.2 Konfigurasi API URL

File `.env` sudah dibuat, pastikan isinya:

```
VITE_API_URL=http://localhost:5000
```

### 2.3 Jalankan Development Server

```bash
npm run dev
```

Frontend akan berjalan di `http://localhost:5173`

## Langkah 3: Test Aplikasi

### 3.1 Buka Dashboard

Buka browser dan akses: `http://localhost:5173`

### 3.2 Verifikasi Koneksi SSE

Di kanan atas dashboard, Anda akan melihat:
- **SSE Status Indicator**: Harus menunjukkan "Connected" (hijau)
- **Sensor Health**: Menunjukkan status sensor
- **Jam Real-time**: Harus berjalan

### 3.3 Cek Data

Tunggu 3-5 menit untuk background job pertama berjalan:

1. Cek status sync:
   ```bash
   curl http://localhost:5000/api/sync/status
   ```

2. Cek statistik:
   ```bash
   curl http://localhost:5000/api/sync/statistics
   ```

3. Dashboard akan otomatis update dengan data real-time

## Troubleshooting Cepat

### Problem: "Connection Failed" di dashboard

**Solusi:**
1. Pastikan backend API berjalan
2. Cek CORS di backend (sudah dikonfigurasi)
3. Periksa console browser untuk error

### Problem: Data tidak muncul

**Solusi:**
1. Cek background job status:
   ```bash
   curl http://localhost:5000/api/sync/status
   ```

2. Lihat log backend untuk error
3. Verifikasi External API credentials benar
4. Pastikan `InitialStartTime` sudah lewat (tidak di masa depan)

### Problem: "Rate limit" error

**Solusi:**
- External API membatasi 1 login per menit
- Tunggu 1 menit, sistem akan retry otomatis
- Cek log untuk konfirmasi

## Tips Development

### 1. Adjust Initial Start Time

Untuk development, gunakan waktu yang dekat:

```json
"InitialStartTime": "2026-02-01 08:00:00"
```

Jangan terlalu jauh ke belakang karena akan memakan waktu lama untuk sync data historis.

### 2. Monitor Background Job

Lihat log real-time:

```bash
# Di terminal backend
dotnet run
```

Setiap 3 menit Anda akan lihat:
```
Running data aggregation cycle at 2026-02-01 03:15:00
Fetching events from 2026-02-01T03:12:00Z to 2026-02-01T03:15:00Z
Fetched 15 events from external API
Data aggregation completed successfully
```

### 3. Reset Sync Jika Perlu

Jika perlu reset dan mulai dari awal:

```bash
curl -X POST http://localhost:5000/api/sync/reset \
  -H "Content-Type: application/json" \
  -d '{"startTime": "2026-02-01 00:00:00"}'
```

## Area Filter

Dashboard memiliki 3 filter area:
- **All**: Menampilkan semua data (Mining + Hauling)
- **Mining**: Hanya data Mining (IPD, Pit, Front, dll)
- **Hauling**: Hanya data Hauling (CSA, KM, route)

## Fitur Dashboard

### Real-time Updates
- Data update otomatis setiap 3 detik via SSE
- Notifikasi popup untuk alert baru
- Connection status indicator

### KPI Cards
- Total Alarms
- Followed Up
- Waiting Follow Up

### Active Alerts
- List semua alert yang Open
- Click untuk detail modal
- Menampilkan GPS coordinates, speed, operator

### Area Distribution
- Alert per lokasi
- Click location untuk filter

### Delayed Follow-Up
- Alert yang sudah >30 menit Open
- Priority untuk follow-up

### Strategic Insights
- **Recurrent Units**: Operator/unit dengan banyak event
- **High Risk Areas**: Lokasi dengan banyak alert

## Next Steps

1. **Production Deployment**: Lihat [DEPLOYMENT.md](./DEPLOYMENT.md)
2. **Sync Configuration**: Lihat [SYNC-CONFIGURATION.md](./SYNC-CONFIGURATION.md)
3. **Full Documentation**: Lihat [README.md](./README.md)

## Support

Jika ada masalah:
1. Cek log backend dan frontend
2. Verifikasi konfigurasi di `appsettings.json`
3. Test koneksi ke External API
4. Cek status sync: `curl http://localhost:5000/api/sync/status`

---

**Happy Monitoring! 🚀**
