# YolPilot / RotaApp – Satış Öncesi Checklist (Uygulanabilir)

Bu dosya, repodaki kritik güvenlik/konfigürasyon değişikliklerinden sonra **adım adım doğrulama** yapmak için hazırlanmıştır.

## 0) Hızlı Özet (Bugün)

- Repodan hassas dosyalar kaldırıldı: `debug-log.txt`, `driver_credentials_*.txt`, frontend `.env*`, mobil crash/log dosyaları.
- Backend config artık “repo içinde secret tutma” yerine **ENV / Key Vault** yaklaşımını bekliyor.
- Backend’te:
  - CORS policy yeniden düzenlendi (`DefaultCors`)
  - Rate limiting IP tespiti iyileştirildi (ARR/X-Forwarded-For)
  - Login/Register endpoint’lerine rate limit eklendi
  - Lockout policy eklendi
  - Demo seed artık sadece `Development` + `--seed-demo` ile
  - Driver migration artık sadece `--migrate-drivers` ile
  - Google Places proxy endpoint’leri eklendi (`/api/google/...`)
- Mobile: Google Places araması artık doğrudan Google’a gitmiyor; backend proxy kullanıyor (API key mobilde hardcoded değil).

---

## 1) Secret’ları Rotate Et (Zorunlu)

Rotate edilmesi gerekenler:
- Google Maps/Places API Key
- Brevo API Key
- Cloudinary API Secret
- Twilio Auth Token
- JWT signing key
- Encryption key
- Tracking secret

**Kural:** rotate ettikten sonra eski anahtarları iptal etmeden önce staging’de doğrula.

---

## 2) Production’da Set Edilmesi Gereken Değerler (ENV / Key Vault)

Backend (`yolpilot-backend/src/Monolith.WebAPI`) artık production’da boş geçilince **fail-fast** yapar.

Azure App Service için örnek ENV isimleri:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Encryption__Key`
- `Tracking__Secret`
- `GoogleMaps__ApiKey`
- `Email__Provider` (ör: `Brevo`)
- `Email__Brevo__ApiKey`
- `Twilio__Enabled` (`true/false`)
- `Twilio__SharedAccountSid`
- `Twilio__SharedAuthToken`
- `Twilio__SharedWhatsAppNumber`
- `Storage__Mode` (ör: `Cloudinary` veya `Local`)
- `Cloudinary__CloudName`
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`
- (opsiyonel) `KeyVault__Url`

Not: Azure App Service “Connection Strings” kısmı da kullanılabilir; yine `DefaultConnection` map’lenir.

---

## 3) Backend Smoke Test (adım adım)

### 3.1) Build doğrulaması

```powershell
dotnet restore yolpilot-backend/src/Monolith.WebAPI/Monolith.WebAPI.csproj
dotnet build yolpilot-backend/src/Monolith.WebAPI/Monolith.WebAPI.csproj -c Release --no-restore
```

### 3.2) Local run (Development)

1) Local DB yoksa bile compile doğrulaması yeterli; DB’li test için SQL Server ayağa kaldırın.
2) (Opsiyonel) Demo seed:
```powershell
dotnet run --project yolpilot-backend/src/Monolith.WebAPI/Monolith.WebAPI.csproj -- --seed-demo
```

### 3.3) Health check

Uygulama çalışıyorken:
- `GET /health` → `Healthy`
- `GET /api/health` → `Healthy`
- `GET /ping` → `pong`

### 3.4) Login rate limit

6 kez hızlı login denemesi yapın; 429 dönmesi gerekir:
```powershell
$body = @{ email="test@example.com"; password="wrong"; rememberMe=$false } | ConvertTo-Json
1..6 | % { Invoke-RestMethod -Method Post -Uri "https://api.yolpilot.com/api/auth/login" -ContentType "application/json" -Body $body -ErrorAction SilentlyContinue }
```

### 3.5) Google Places proxy (mobil için)

1) Bir JWT token alın (login sonrası).
2) Autocomplete:
```powershell
$token = "<BEARER_TOKEN>"
Invoke-RestMethod -Headers @{ Authorization="Bearer $token" } -Uri "https://api.yolpilot.com/api/google/places/autocomplete?input=migros&language=tr&components=country:tr&types=establishment"
```
Beklenen: `status: "OK"` ve `predictions` dolu.

---

## 4) Frontend Smoke Test (RotaApp Web)

### 4.1) Local env

1) `rotaapp-frontend/.env.example` → `.env` kopyala.
2) Aşağıdakileri set et:
   - `VITE_API_URL` (local backend ise `http://localhost:5055/api`)
   - `VITE_GOOGLE_MAPS_API_KEY` (HTTP referrer restriction ile)

### 4.2) Build ve run

```powershell
cd rotaapp-frontend
npm.cmd ci
npm.cmd run build
npm.cmd run dev
```

### 4.3) Maps key doğrulama

- Harita içeren sayfayı aç (örn. Route/Depot ekranları)
- Browser console’da `Google Maps JavaScript API error` olmamalı
- Network tab’da `maps.googleapis.com` çağrıları 200 dönmeli

---

## 5) Mobile Smoke Test (RN)

### 5.1) Google Places araması

Değişiklikten sonra mobil uygulama artık backend endpoint’ine gider:
- Autocomplete: `/api/google/places/autocomplete`
- Details: `/api/google/places/details`

Test:
1) Uygulamaya login ol
2) İşletme arama input’una “Migros” yaz
3) Öneriler gelmeli; seçince detay/adres/lat-lng dolmalı

Eğer öneriler gelmiyorsa:
- Backend `GoogleMaps:ApiKey` set mi?
- Token gidiyor mu? (401 görüyorsan auth/storage tarafı)
- Backend loglarında Google API error var mı?

---

## 6) Deploy sonrası kontrol (Production)

- Azure App Service → Configuration: ENV/KeyVault değerleri set
- Restart sonrası log: “Application started successfully”
- `/health` 200
- Login + token alma
- 429 rate limit
- Mobil autocomplete

---

## 7) Bilinen Teknik Borçlar (şu an blocker değil)

- `rotaapp-frontend` `npm audit` high/moderate uyarıları var → planlayıp düzeltin.
- `rotaapp-frontend` eslint script’i mevcut config ile uyumsuz görünüyor → lint pipeline’ı için güncellemek gerekir.
- `RotaAppMobile` jest testleri `AsyncStorage` mock olmadığı için fail ediyor → Jest setup’ta AsyncStorage mock eklemek gerekir.

