# FatigueMonitoring

Dashboard Monitoring Fatigue (kelelahan operator) dengan update real-time via SSE.

## Ringkasan Arsitektur

```
External API
   ↓ (Background Job)
RawEvents
   ↓ (Background Job Aggregation)
AI_DashboardSnapshot
   ↓
SSE Endpoint
   ↓
React Dashboard
```

## Backend (ASP.NET Core / .NET 10)

### 1) Konfigurasi

Update `FatigueMonitoring.Web.Api/appsettings.json` atau gunakan environment variables:

```
ConnectionStrings__Default="Server=...;Database=FatigueMonitoring;User Id=...;Password=...;TrustServerCertificate=True"
ExternalApi__BaseUrl="https://api-platform-integrator.transtrack.co/"
ExternalApi__Username="sis@mdvr"
ExternalApi__Password="Sis@mdvr12345"
```

Opsional:
- `AreaMapping` untuk menentukan Mining/Hauling berdasarkan `group_name`.
- `BackgroundJob` interval fetch & aggregation.
- `Sse` interval data & heartbeat.

### 2) Menjalankan Backend

```bash
cd FatigueMonitoring.Web.Api
dotnet run
```

> Catatan: Startup akan membuat tabel jika belum ada (EnsureCreated).

### 3) Endpoint

- `GET /api/dashboard/stream` — SSE stream (real-time)
- `GET /api/dashboard/snapshot` — snapshot JSON terbaru

## Frontend (Vite React)

### 1) Konfigurasi

```bash
cd fatigue-monitoring.mockup
cp .env.example .env
```

Edit `.env`:

```
VITE_API_BASE_URL=http://localhost:5000
```

### 2) Menjalankan Frontend

```bash
npm install
npm run dev
```

## Azure App Service (Ringkas)

1. Publish backend ke App Service.
2. Set **Connection Strings** dan **App Settings** (`ExternalApi__...`).
3. Pastikan SSE aktif (App Service mendukung streaming).
4. Deploy frontend (Static Web App / Storage static website) dan arahkan `VITE_API_BASE_URL` ke backend.

## Catatan

- Agregasi dilakukan di background job dan disimpan di tabel `AI_DashboardSnapshot`.
- SSE hanya membaca data agregasi (tanpa perhitungan ulang).
- UI menampilkan status koneksi SSE (Connecting / Connected / Disconnected).