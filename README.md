# 🔍 Kayıp Eşya Otomasyonu

Kayıp ve bulunan eşyaların tek bir merkezden kaydedilmesini, **Fuzzy Matching (Bulanık Benzerlik)** algoritmasıyla otomatik eşleştirilmesini ve güvenli teslimat tutanağı ile arşivlenmesini sağlayan **ASP.NET Core MVC** tabanlı kurumsal web otomasyonudur.

---

## ⚡ Temel Özellikler

- 👥 **Rol Bazlı Yetki:** `Admin`, `Personel` ve `Vatandaş` rolleri (ASP.NET Core Identity).
- 📢 **Kayıp Bildirimi & Envanter:** Fotoğraflı, konum ve kategori etiketli kayıp/buluntu eşya kaydı.
- 🧠 **Akıllı Eşleştirme:** `FuzzyHelper` ile Levenshtein mesafesi ve anahtar kelime analizi üzerinden otomatik benzerlik skorlaması (%0 - %100).
- 📝 **Teslimat & Tutanak:** Eşleşen eşyaların kimlik doğrulaması ve teslim tutanağı ile teslimi.
- 🛡️ **Denetim İzi (Audit Log):** Sistemdeki ekleme, güncelleme ve silme hareketlerinin loglanması.
- 📧 **E-Posta Servisi:** SMTP tabanlı e-posta onayı ve şifre sıfırlama.

---

## 🛠️ Teknoloji Yığını

- **Backend:** C# / .NET 8.0 / ASP.NET Core MVC
- **Veritabanı & ORM:** Microsoft SQL Server & Entity Framework Core 8 (Code-First)
- **Güvenlik:** ASP.NET Core Identity, Anti-CSRF, PBKDF2 Hashing
- **Frontend:** Bootstrap 5, HTML5/CSS3, jQuery & Unobtrusive Validation

---

## 🚀 Kurulum ve Çalıştırma Adımları

### 1. Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (MSSQL LocalDB, Express vb.)
- Visual Studio 2022 veya VS Code

### 2. Projeyi İndirin ve Dizinine Geçin
```bash
git clone [https://github.com/yagmurcknmz/kayipesyaotomasyonu.git](https://github.com/yagmurcknmz/kayipesyaotomasyonu.git)
cd kayipesyaotomasyonu/KayipEsyaOtomasyonu
3. Veritabanı Bağlantısını (Connection String) Ayarlayın
appsettings.json dosyasını açıp yerel SQL Server ayarınıza göre düzenleyin:

JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KayipEsyaDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
4. Bağımlılıkları Yükleyin & Veritabanını Oluşturun
Bash
dotnet restore
dotnet ef database update
💡 Not: DbInitializer ilk çalıştırmada varsayılan rolleri ve başlangıç kategorilerini otomatik oluşturur.

5. Projeyi Başlatın
Bash
dotnet run
Tarayıcınızdan https://localhost:5001 veya http://localhost:5000 adresine giderek sistemi kullanabilirsiniz.

👥 Kullanıcı Rolleri
Rol	Yetki Kapsamı
 Admin	Tam yetki, Personel ekleme/yönetme, Denetim izlerini (Audit Logs) inceleme
 Personel	Buluntu eşya kaydı, Eşleştirme onaylama, Teslimat tutanağı oluşturma
 Vatandaş	Kayıp bildiriminde bulunma, Kendi başvuru durumlarını takip etme


Bu proje eğitim ve staj amacıyla geliştirilmiştir.
