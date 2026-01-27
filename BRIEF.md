# Yolpilot Proje Brifi

**Proje amaci**
Yolpilot, teslimat ve rota planlama operasyonlari icin web + mobil + backend katmanlarindan olusan cok kiracili (multi-tenant) bir SaaS urunudur. Ana hedef, tek aracli cok durakli rotalarda (70 durak dahil) zaman pencereleri, ETA hesaplama, depo-donus (circular route) ve durak kisitlari ile birlikte optimize rota sunmaktir.

**Temel gereksinimler**
- Zaman penceresi (time window) destegi: musteri varsayilani + durak override.
- ETA hesaplama: her durak icin tahmini varis/ayrilis zamani.
- Circular route: depo -> duraklar -> depo donus ETA dahil.
- Position constraints: first/last durak zorlamasi.
- Excluded stops: cozum yoksa disarida kalan duraklar ve gerekce.
- 70 durak tek rotada optimize edilebilmeli.

**Urun yuzeyi ve modul dagilimi**
- `yolpilot-backend`: .NET API, optimizasyon, kural ve veri dogrulama, loglama.
- `rotaapp-frontend`: Web arayuzu (rota olusturma, optimize, izleme).
- `RotaAppMobile`: Mobil uygulama (surucu/dispatcher is akislari).
- `yolpilot-marketing`: Pazarlama sitesi.

**Mevcut optimizasyon akisi (backend)**
- Time window var ise OR-Tools calisir; yoksa Google Directions optimize akisi calisir.
- OR-Tools modu: tek arac, time window, position constraints (first/last), circular route.
- >25 durakta Google Directions matrisi atlanir, Haversine fallback kullanilir.
- ETA hesaplama: OR-Tools sonucundan sonra Google Directions legs uzerinden ya da time window yoksa OptimizeAndCalculateETA ile.
- Baslangic saati 00:00 ise 00:01 e cekilir.
- EndDetails (depo donus) ETA hesaplanir ve responsea eklenir.

**Urun ozellikleri (ozet)**
- Rota planlama, optimize, durak listesi, ETA, toplam mesafe/sure.
- Time window ve service time destegi.
- Proof of delivery, signature, photo gibi teslimat zorunluluklari.
- Depo, surucu, arac yonetimi.
- Journey/route takip ve optimizasyon durumlari.

**Mobil uygulama durumu (son gelismeler)**
- Depo yonetimi eklendi (liste, detay, olustur, guncelle, sil, varsayilan depo).
- Konum guncelleme talepleri eklendi (surucuden talep + dispatcher onay akisi).
- Harita secici (MapPicker) eklendi; Google Places + static map + current location.
- Arama normalizasyonu eklenerek Turkce karakter hassasiyeti iyilestirildi.

**Web arayuzu durumu (son gelismeler)**
- Time window icin durak limiti 70 olarak guncellendi.
- Optimize sonucunda `success=false` gelirse hata mesaji gosterilip optimizasyon durumu resetlenir.
- Optimize istegi timeout 5 dkya cikti (axios).

**Backend durumu (son gelismeler)**
- Time window >25 durak limiti kaldirildi.
- >25 durakta Directions atlanip Haversine fallback kullanimi aktif.
- Route stop validation limitleri genisletildi (address, name, notes vb).
- Optimize cozum bulunamazsa daha detayli mesaj uretilir.
- Optimize response `Success` alaninin gercek sonucu yansitmasi saglandi.
- Excluded stop listesi, solver tarafindan uretilmisse responsea aktarilir.

**Aktuel sorunlar**
- 70 durakli time window rotalarda optimizasyon cok uzun suruyor.
- Azure App Service proxy ~230 sn civarinda istegi kesiyor ve 502 donuyor.
- Haversine fallback ile time window feasibility hatali olabiliyor.
- Slack (bekleme) olmadigi icin time window probleminde cozum bulunmasi zorlasiyor.
- K=1 stop cikarma dongusu zaman maliyetini patlatiyor.

**Operasyonel gercekler**
- Birden fazla sirket kiracisi var; kullanim profilleri farkli.
- Time window kullanimi ~%30-40.
- Eszamanli optimizasyon cok nadir.

**Deployment**
- Backend: Azure App Service (push -> auto deploy).
- Frontend: Vercel (push -> auto deploy).
- Mobil: React Native projesi, store/release akisi ayri.

**Karar gerektiren teknik konular**
- Async optimizasyon (job + polling) ile 502 probleminin kalici cozumlenmesi.
- OR-Tools iyilestirme kararlari: slack, disjunction penalty, arama stratejileri, time limit.
- Routing engine secimi: Google Routes Matrix, self-host OSRM/Valhalla, veya GraphHopper Cloud.
- Infra karari: Azure App Service ile devam mi, IaaS (DigitalOcean/Azure VM) mi.

**Kritik hedef**
70 durakli, time window iceren bir rotanin 30-120 sn araliginda cozulmesi ve kullaniciya net bir sonuc/mesaj verilmesi.

**Yarin icin gundem**
- Optimizasyon mimarisi ve maliyet analizi dogrulama.
- Slack ve disjunction temelli iyilestirme planinin onaylanmasi veya alternatife karar.
- Async job ve infra secimi netlestirme.
