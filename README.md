# MediCare — Hastane Randevu ve Hasta Takip Sistemi

Modern, kurumsal bir hastane otomasyonu. **ASP.NET Core MVC (.NET 8)** ile katmanlı mimari, SOLID prensipleri, Repository Pattern ve Service Layer kullanılarak geliştirilmiştir. **Veritabanı kullanılmaz** — tüm veriler `Data/Json` altındaki JSON dosyalarında tutulur ve ilişkiler GUID id'ler üzerinden kurulur.

## Teknolojiler

- ASP.NET Core MVC (.NET 8), C#, Async/Await
- Katmanlı mimari: Controllers · Services · Repositories · Models (Entities/DTOs/ViewModels) · Helpers · Middleware
- Cookie tabanlı kimlik doğrulama + rol bazlı yetkilendirme (Hasta / Doktor / Admin)
- JSON veri depolama (generic `JsonRepository<T>`), Logger, global Exception Handling, Validation
- Razor View + Partial View'lar, Bootstrap-bağımsız özel CSS tasarım sistemi, Chart.js, Font Awesome

## Tasarım

Fluent / Windows 11 esintili özel tasarım: glassmorphism, soft shadow, modern kartlar, hover/transition efektleri, gradient arka planlar, **Dark/Light mode**, collapse edilebilir sidebar, toast bildirimleri ve modal yapıları. Klasik Bootstrap görünümü kullanılmaz.

## Çalıştırma

```bash
dotnet run
```

Uygulama ilk açılışta demo verilerini otomatik olarak oluşturur (`Data/Json`).

### Demo Hesaplar

| Rol    | TC Kimlik No  | Şifre       |
|--------|---------------|-------------|
| Admin  | `11111111110` | `Admin123!` |
| Doktor | `12345678950` | `Doktor123!`|
| Hasta  | `10000000014` | `Hasta123!` |

## Başlıca Özellikler

- **Hasta:** randevu alma (poliklinik → doktor → tarih → uygun saat), randevu/iptal, reçeteler, tahlil sonuçları, favori doktorlar & puanlama, bildirimler, profil
- **Doktor:** günlük randevular, hasta listesi & geçmişi, reçete yazma + **akıllı ilaç öneri sistemi** (tanıya göre öneri), tanı veritabanı, çalışma takvimi, bildirimler
- **Admin:** dashboard (Chart.js grafikleri), kullanıcı/doktor/hasta/poliklinik yönetimi, randevu yönetimi, duyurular, raporlar, log yönetimi, JSON yedekleme (ZIP)

## Randevu Kuralları

- Çakışan randevu oluşturulamaz, geçmiş tarih/saat seçilemez
- Dolu saatler listelenmez, doktor çalışma takvimi ve günlük kapasite dikkate alınır

## Klasör Yapısı

```
HospitalSystem
├── Controllers
├── Models (Entities, DTOs, ViewModels, Enums)
├── Services (Interfaces, Implementations)
├── Repositories
├── Data/Json
├── Helpers
├── Middleware
├── Views (Account, Patient, Doctor, Admin, Shared)
├── wwwroot (css, js, images, uploads)
└── Program.cs
```
