# Konfigurasi Sinkronisasi Data

Dokumen ini menjelaskan cara kerja sistem sinkronisasi data dari External API dan cara mengkonfigurasinya.

## Cara Kerja Sinkronisasi

### Mekanisme
1. **Background Job** berjalan setiap **3 menit** (dapat dikonfigurasi)
2. Sistem menyimpan **timestamp terakhir** proses sinkronisasi
3. Setiap job berjalan, sistem mengambil data **dari waktu terakhir sampai 3 menit berikutnya**
4. Timestamp diupdate setelah proses berhasil
5. Proses agregasi dilakukan setelah data mentah disimpan

### Flow Sinkronisasi

```
┌─────────────────────────────────────────────────────────┐
│  Background Job (Setiap 3 Menit)                        │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Baca SyncMetadata → LastSyncTime                       │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Fetch External API                                      │
│  StartTime: LastSyncTime                                │
│  EndTime: LastSyncTime + 3 menit                        │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Simpan ke RawEvents Table                              │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Agregasi ke AI_*_T Tables                              │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Update SyncMetadata                                     │
│  LastSyncTime = EndTime                                 │
│  NextSyncTime = EndTime + 3 menit                       │
│  Status = "Success"                                     │
└─────────────────────────────────────────────────────────┘
```

## Konfigurasi

### 1. Setting di appsettings.json

```json
{
  "BackgroundJob": {
    "IntervalMinutes": 3,
    "InitialStartTime": "2026-02-01 00:00:00"
  }
}
```

**Parameter:**
- `IntervalMinutes`: Interval waktu background job (default: 3 menit)
- `InitialStartTime`: Waktu mulai awal sinkronisasi (format: yyyy-MM-dd HH:mm:ss)

### 2. Waktu Mulai Awal (Initial Start Time)

Waktu ini digunakan **hanya saat pertama kali** sistem berjalan. Setelah itu, sistem akan menggunakan timestamp terakhir dari database.

**Contoh Skenario:**

**Skenario 1: Mulai dari hari ini**
```json
"InitialStartTime": "2026-02-01 00:00:00"
```

**Skenario 2: Ambil data historis 1 minggu**
```json
"InitialStartTime": "2026-01-25 00:00:00"
```

**Skenario 3: Ambil data historis 1 bulan**
```json
"InitialStartTime": "2026-01-01 00:00:00"
```

⚠️ **Catatan:** Semakin lama periode historis, semakin lama waktu yang dibutuhkan untuk initial sync.

## API Endpoints untuk Manajemen Sync

### 1. Cek Status Sinkronisasi

**Endpoint:** `GET /api/sync/status`

**Response:**
```json
{
  "syncKey": "EventsSync",
  "lastSyncTime": "2026-02-01T03:15:00Z",
  "nextSyncTime": "2026-02-01T03:18:00Z",
  "status": "Success",
  "errorMessage": null,
  "updatedAt": "2026-02-01T03:15:10Z",
  "currentTime": "2026-02-01T03:16:30Z",
  "minutesUntilNextSync": 1.5
}
```

**Status yang mungkin:**
- `Initialized`: Baru dibuat, belum pernah sync
- `Running`: Sedang proses sinkronisasi
- `Success`: Sinkronisasi berhasil
- `Failed`: Sinkronisasi gagal
- `Manually Updated`: Diupdate manual via API
- `Reset`: Direset ke waktu awal

### 2. Update Waktu Sinkronisasi Manual

**Endpoint:** `POST /api/sync/update-sync-time`

**Request Body:**
```json
{
  "newSyncTime": "2026-02-01 06:00:00"
}
```

**Response:**
```json
{
  "message": "Sync time updated successfully",
  "lastSyncTime": "2026-02-01T06:00:00Z",
  "nextSyncTime": "2026-02-01T06:03:00Z",
  "updatedAt": "2026-02-01T06:00:05Z"
}
```

⚠️ **Hati-hati:** Mengubah waktu sync dapat menyebabkan:
- Data duplikat (jika mundur ke waktu yang sudah diproses)
- Data terlewat (jika maju melewati data yang belum diproses)

### 3. Reset Sinkronisasi

**Endpoint:** `POST /api/sync/reset`

**Request Body:**
```json
{
  "startTime": "2026-01-01 00:00:00"
}
```

**Response:**
```json
{
  "message": "Sync reset successfully. The background job will start from the specified time.",
  "lastSyncTime": "2026-01-01T00:00:00Z",
  "nextSyncTime": "2026-01-01T00:03:00Z",
  "warning": "This will cause the system to re-fetch and re-aggregate historical data."
}
```

⚠️ **Warning:** Reset akan:
- Menghapus metadata sync yang ada
- Membuat sistem mengambil ulang data dari waktu awal yang ditentukan
- Dapat menyebabkan data duplikat di RawEvents table

**Best Practice:**
1. Backup database sebelum reset
2. Atau truncate table RawEvents sebelum reset untuk data bersih

### 4. Statistik Sinkronisasi

**Endpoint:** `GET /api/sync/statistics`

**Response:**
```json
{
  "totalEvents": 15420,
  "eventsToday": 324,
  "oldestEvent": "2026-01-25T00:05:00Z",
  "newestEvent": "2026-02-01T03:15:00Z",
  "currentSyncStatus": "Success",
  "lastSyncTime": "2026-02-01T03:15:00Z",
  "nextSyncTime": "2026-02-01T03:18:00Z"
}
```

## Tabel SyncMetadata

Tabel ini menyimpan metadata sinkronisasi:

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary Key |
| SyncKey | varchar(100) | Unique key ("EventsSync") |
| LastSyncTime | datetime | Waktu terakhir sync |
| NextSyncTime | datetime | Waktu sync berikutnya |
| Status | varchar | Status sync (Success/Failed/Running) |
| ErrorMessage | text | Pesan error jika gagal |
| UpdatedAt | datetime | Waktu terakhir update |

## Troubleshooting

### Problem: Data tidak terupdate

**Solusi:**
1. Cek status sync:
   ```bash
   curl https://your-api.com/api/sync/status
   ```

2. Lihat log aplikasi untuk error:
   ```bash
   az webapp log tail --name your-app --resource-group your-rg
   ```

3. Cek apakah background job berjalan (Always On harus enabled di Azure)

### Problem: Data duplikat

**Penyebab:**
- Sync time diupdate mundur
- Aplikasi restart di tengah proses

**Solusi:**
1. Identifikasi duplikat:
   ```sql
   SELECT Identity, COUNT(*) 
   FROM RawEvents 
   GROUP BY Identity 
   HAVING COUNT(*) > 1
   ```

2. Hapus duplikat (keep yang terbaru):
   ```sql
   DELETE FROM RawEvents 
   WHERE Id NOT IN (
     SELECT MAX(Id) 
     FROM RawEvents 
     GROUP BY Identity
   )
   ```

### Problem: Data terlewat (gap)

**Identifikasi gap:**
```bash
curl https://your-api.com/api/sync/statistics
```

**Solusi:**
1. Jika gap kecil (<1 jam), update sync time ke waktu sebelum gap:
   ```bash
   curl -X POST https://your-api.com/api/sync/update-sync-time \
     -H "Content-Type: application/json" \
     -d '{"newSyncTime": "2026-02-01 10:00:00"}'
   ```

2. Jika gap besar, consider reset dan re-sync:
   ```bash
   curl -X POST https://your-api.com/api/sync/reset \
     -H "Content-Type: application/json" \
     -d '{"startTime": "2026-01-01 00:00:00"}'
   ```

### Problem: Background job berhenti

**Cek:**
1. Always On enabled?
2. App Service tidak crash?
3. Rate limit External API?

**Restart background job:**
- Restart App Service di Azure Portal

## Best Practices

### 1. Monitoring
- Monitor sync status secara berkala
- Set up alert jika status Failed
- Track gap antara `lastSyncTime` dan `currentTime`

### 2. Initial Setup
- Mulai dengan periode pendek (contoh: 1 hari)
- Pastikan data berhasil masuk sebelum extend ke periode lebih lama
- Test dengan small batch dulu

### 3. Production
- Enable Application Insights untuk monitoring
- Set up auto-scaling jika data volume besar
- Backup database secara berkala

### 4. Rate Limiting
- External API punya limit 1 login per menit
- Sistem sudah handle ini secara otomatis
- Jangan trigger manual sync terlalu sering

## Contoh Penggunaan

### Setup Awal - Development

1. Edit `appsettings.Development.json`:
   ```json
   {
     "BackgroundJob": {
       "IntervalMinutes": 3,
       "InitialStartTime": "2026-02-01 00:00:00"
     }
   }
   ```

2. Jalankan aplikasi:
   ```bash
   dotnet run
   ```

3. Cek status:
   ```bash
   curl http://localhost:5000/api/sync/status
   ```

### Setup Production - Azure

1. Set configuration via Azure Portal atau CLI:
   ```bash
   az webapp config appsettings set \
     --name your-app \
     --resource-group your-rg \
     --settings \
       BackgroundJob__IntervalMinutes=3 \
       BackgroundJob__InitialStartTime="2026-02-01 00:00:00"
   ```

2. Enable Always On:
   ```bash
   az webapp config set \
     --name your-app \
     --resource-group your-rg \
     --always-on true
   ```

3. Monitor via Application Insights

---

**Dibuat:** 1 Februari 2026  
**Terakhir Diupdate:** 1 Februari 2026
