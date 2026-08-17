# Kayıp Eşya Otomasyonu Sistemi

Kayıp Eşya Otomasyonu; kamu kurumları, üniversite kampüsleri, havalimanları, belediyeler ve toplu taşıma merkezlerinde kaybolan veya bulunan eşyaların tek bir merkezi dijital havuzda toplanmasını, güvenli biçimde takip edilmesini ve akıllı metin benzerlik algoritmalarıyla otomatik eşleştirilmesini sağlayan kurumsal bir web otomasyonudur

---

# Öne Çıkan Özellikler

* **Rol Tabanlı Güvenlik (RBAC):** `Admin`, `Personel` ve `Vatandaş/Kullanıcı` rolleri ile ayrıştırılmış yetkilendirme ve ekran yapısı.
* **Kayıp Bildirimi ve Başvuru:** Vatandaşların kaybettiği eşyayı kategori, konum, tarih, detaylı açıklama ve fotoğraflarla sisteme bildirebilmesi.
* **Bulunan Eşya Envanteri:** Personel tarafından teslim alınan buluntu eşyaların çoklu fotoğraf, teslim yeri ve durum bilgisi (*Depoda*, *Eşleşti*, *Teslim Edildi*) ile kayıt altına alınması.
* **Akıllı Eşleştirme Motoru (Fuzzy Matching):** `FuzzyHelper` kütüphanesi ile *Levenshtein Distance* ve anahtar kelime benzerlik analizi yapılarak başvurular ve bulunan eşyalar arasında otomatik yüzdelik benzerlik skorlaması (%0 - %100).
* **Teslimat ve Tutanak Süreci:** Doğrulanan eşyaların kimlik teyidi, teslim tutanağı ve yetkili personel onayıyla hak sahibine teslim edilmesi ve arşivlenmesi.
* **Audit Logging (Denetim İzi):** Sistem genelinde yapılan kritik ekleme, güncelleme ve silme işlemlerinin IP, kullanıcı ve zaman damgasıyla kayıt altına alınması.
* **E-Posta Bildirim Servisi:** SMTP tabanlı e-posta aktivasyonu ve şifre sıfırlama mekanizması.
* **Medya Yönetimi:** Yüklenen görsellerin dinamik dizinlenmesi (*Yıl/Ay/Gün*) ve otomatik küçük resim (*thumbnail*) üretimi.

---

# Kullanılan Teknolojiler

* **Backend:** .NET 8.0 / ASP.NET Core MVC (Model-View-Controller)[cite: 1, 4]
* **Veritabanı & ORM:** Microsoft SQL Server & Entity Framework Core 8 (Code-First)[cite: 1, 4]
* **Kimlik Yönetimi:** ASP.NET Core Identity[cite: 1, 4]
* **Ön Yüz (UI):** Bootstrap 5.3, HTML5, CSS3, jQuery, jQuery Validation & Unobtrusive Scripts[cite: 1, 4]
* **Algoritmik Altyapı:** Levenshtein Distance & Token Tabanlı Metin Analitiği[cite: 1, 4]

---

# Kurulum ve Çalıştırma Yönergesi

### 1. Ön Gereksinimler
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) veya üzeri
* [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (MSSQL LocalDB, Express veya Developer Edition)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) veya [VS Code](https://code.visualstudio.com/)

### 2. Proje Dizinine Geçiş
```bash
cd KayipEsyaOtomasyonu
 3. Veritabanı Bağlantısını (Connection String) Yapılandırınappsettings.json dosyasını açarak DefaultConnection dizesini kendi yerel SQL Server ayarlarınıza göre düzenleyin[cite: 4]:JSON{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KayipEsyaOtomasyonuDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
4. Bağımlılıkları YükleyinBashdotnet restore
5. Veritabanını Oluşturun ve Migration'ları UygulayınTerminal veya Visual Studio Package Manager Console üzerinden aşağıdaki komutu çalıştırın[cite: 4]:Bashdotnet ef database update
Not: DbInitializer sınıfı, uygulama ilk kez çalıştığında varsayılan rolleri (Admin, Personel, Vatandas) ve başlangıç kategorilerini otomatik olarak veritabanına ekler.  6. Uygulamayı BaşlatınBashdotnet run
Uygulama derlendikten sonra tarayıcınızdan https://localhost:5001 veya http://localhost:5000 adresine giderek sistemi kullanmaya başlayabilirsiniz.
