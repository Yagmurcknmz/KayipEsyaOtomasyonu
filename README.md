# Kayıp Eşya Yönetim Sistemi
Kayıp eşyaların kayıt altına alınması, vatandaş başvurularının yönetilmesi, bulunan eşyalarla başvuruların eşleştirilmesi ve teslim süreçlerinin takip edilmesi amacıyla geliştirilmiş web tabanlı bir otomasyon sistemidir.
# Kullanılan Teknolojiler
* ASP.NET Core MVC
* C#
* Entity Framework Core
* ASP.NET Core Identity
* Microsoft SQL Server
* Razor View Engine
* HTML5
* CSS3
* Bootstrap
* Bootstrap Icons
* JavaScript
# Kullanıcı Rolleri
Sistemde üç farklı kullanıcı rolü bulunmaktadır:
* Admin
* Personel
* Vatandaş
# Temel Özellikler

* Rol tabanlı giriş ve yetkilendirme
* Vatandaş kayıt ve giriş sistemi
* Kayıp eşya başvurusu oluşturma
* Belediyeye teslim edilen eşyaları kaydetme
* Kategori ekleme, düzenleme ve aktif/pasif yönetimi
* Başvuru, kategori ve durum bazlı arama ve filtreleme
* Otomatik eşleştirme sistemi
* Manuel eşleştirme oluşturma
* Yüzdelik eşleşme skoru hesaplama
* Eşleşme onaylama ve reddetme
* Vatandaşa otomatik bildirim gönderme
* Bildirimleri okunmuş veya okunmamış olarak takip etme
* Teslim işlemi oluşturma
* Yazdırılabilir teslim tutanağı hazırlama
* Admin ve personel yönetim paneli
* Kayıp eşya ve başvuru istatistikleri
* Mobil uyumlu kullanıcı arayüzü
# Projeyi Çalıştırma
1. Projeyi bilgisayarınıza klonlayın:

```bash
git clone PROJENIN_GITHUB_ADRESI
```

2. Projeyi Visual Studio ile açın.

3. `appsettings.json` dosyasındaki SQL Server bağlantı bilgisini kendi sisteminize göre düzenleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SUNUCU_ADI;Database=KayipEsyaOtomasyonuDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
4. Package Manager Console üzerinden veritabanını oluşturun:

```powershell
Update-Database
```
5. Projeyi çalıştırmak için Visual Studio üzerinden `F5` tuşuna basın.

## Proje Yapısı

```text
Controllers/    Uygulamanın işlem ve yönlendirme katmanı
Data/           Veritabanı bağlantısı ve başlangıç verileri
Models/         Veritabanı modelleri
Services/       Uygulamanın servis ve iş mantığı
ViewModels/     Sayfalara özel veri modelleri
Views/          Razor kullanıcı arayüzleri
wwwroot/        CSS, JavaScript ve statik dosyalar
Migrations/     Entity Framework Core veritabanı geçişleri
Program.cs      Uygulama servisleri ve başlangıç ayarları
``

# Güvenlik
Projede ASP.NET Core Identity kullanılmıştır. Kullanıcı parolaları güvenli biçimde saklanır ve rol tabanlı yetkilendirme ile vatandaş, personel ve admin sayfalarına erişimler birbirinden ayrılmıştır.

Ayrıca sistemde:

* Başarısız girişlerde hesap kilitleme
* Güvenli oturum yönetimi
* Tekrarlanan e-posta kontrolü
* Form doğrulama işlemleri
* Yetkisiz erişim yönlendirmesi
* POST işlemlerinde güvenlik kontrolleri
uygulanmıştır.

Bu proje eğitim ve staj çalışması amacıyla geliştirilmiştir.

Projenin amacı; vatandaş başvurularının, bulunan eşyaların, eşleşmelerin ve teslim süreçlerinin tek bir sistem üzerinden daha düzenli ve takip edilebilir şekilde yönetilmesini sağlamaktır.

> Bu proje eğitim ve staj çalışması amacıyla geliştirilmiştir.
