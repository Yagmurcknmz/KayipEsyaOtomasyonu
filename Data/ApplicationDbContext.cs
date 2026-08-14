using System.Security.Claims;
using System.Text.Json;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace KayipEsyaOtomasyonu.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<KayipEsya> KayipEsyalar { get; set; }
        public DbSet<KayipBildirimi> KayipBildirimleri { get; set; }
        public DbSet<Eslesme> Eslesmeler { get; set; }
        public DbSet<Bildirim> Bildirimler { get; set; }
        public DbSet<TeslimIslemi> TeslimIslemleri { get; set; }

        public DbSet<AuditLog> AuditLoglar { get; set; }
        public DbSet<KayipEsyaResim> KayipEsyaResimler { get; set; }
        public DbSet<KayipBildirimiResim> KayipBildirimiResimler { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var degisiklikler = ChangeTracker.Entries()
                .Where(e =>
                    e.Entity is not AuditLog &&
                    (e.State == EntityState.Added ||
                     e.State == EntityState.Modified ||
                     e.State == EntityState.Deleted))
                .ToList();

            var kullaniciId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var kullaniciAdi = _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
            var ipAdresi = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString();

            foreach (var entry in degisiklikler)
            {
                try
                {
                    var tip = entry.State switch
                    {
                        EntityState.Added => AuditTip.Create,
                        EntityState.Modified => AuditTip.Update,
                        EntityState.Deleted => AuditTip.Delete,
                        _ => AuditTip.Custom
                    };

                    var tabloAdi = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                    long? kayitId = null;
                    string? kayitAnahtari = null;

                    var pk = entry.Metadata.FindPrimaryKey();
                    if (pk != null)
                    {
                        var keyler = pk.Properties.Select(p =>
                            entry.Property(p.Name).CurrentValue?.ToString()).ToList();
                        if (keyler.Count == 1)
                        {
                            if (long.TryParse(keyler[0], out var lid)) kayitId = lid;
                            else kayitAnahtari = keyler[0];
                        }
                        else
                        {
                            kayitAnahtari = string.Join("|", keyler);
                        }
                    }

                    var eskiDegerler = string.Empty;
                    var yeniDegerler = string.Empty;

                    if (tip == AuditTip.Update)
                    {
                        var eskiDict = new Dictionary<string, object?>();
                        var yeniDict = new Dictionary<string, object?>();
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.IsTemporary || prop.Metadata.IsConcurrencyToken) continue;
                            if (!prop.IsModified && tip != AuditTip.Delete) continue;

                            var pName = prop.Metadata.Name;
                            if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;

                            eskiDict[pName] = prop.OriginalValue;
                            yeniDict[pName] = prop.CurrentValue;
                        }
                        if (eskiDict.Count > 0) eskiDegerler = JsonSerializer.Serialize(eskiDict, JsonOptions);
                        if (yeniDict.Count > 0) yeniDegerler = JsonSerializer.Serialize(yeniDict, JsonOptions);
                    }
                    else if (tip == AuditTip.Create)
                    {
                        var dict = new Dictionary<string, object?>();
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.IsTemporary) continue;
                            if (prop.CurrentValue == null) continue;
                            dict[prop.Metadata.Name] = prop.CurrentValue;
                        }
                        if (dict.Count > 0) yeniDegerler = JsonSerializer.Serialize(dict, JsonOptions);
                    }
                    else if (tip == AuditTip.Delete)
                    {
                        var dict = new Dictionary<string, object?>();
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.OriginalValue == null) continue;
                            dict[prop.Metadata.Name] = prop.OriginalValue;
                        }
                        if (dict.Count > 0) eskiDegerler = JsonSerializer.Serialize(dict, JsonOptions);
                    }

                    AuditLoglar.Add(new AuditLog
                    {
                        UserId = string.IsNullOrWhiteSpace(kullaniciId) ? null : kullaniciId,
                        UserName = string.IsNullOrWhiteSpace(kullaniciAdi) ? null : kullaniciAdi,
                        Tip = tip,
                        TabloAdi = tabloAdi,
                        KayitId = kayitId,
                        KayitAnahtari = kayitAnahtari,
                        EskiDegerlerJson = eskiDegerler.Length == 0 ? null : eskiDegerler,
                        YeniDegerlerJson = yeniDegerler.Length == 0 ? null : yeniDegerler,
                        IpAdresi = string.IsNullOrWhiteSpace(ipAdresi) ? null : ipAdresi,
                        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : (userAgent.Length > 1000 ? userAgent.Substring(0, 1000) : userAgent),
                        Tarih = DateTime.Now
                    });
                }
                catch
                {
                    // Audit log kaydedilemezse ana işlemi BLOKLAma
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Eslesme>()
                .HasOne(e => e.KayipBildirimi)
                .WithMany()
                .HasForeignKey(e => e.KayipBildirimiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Eslesme>()
                .HasOne(e => e.KayipEsya)
                .WithMany()
                .HasForeignKey(e => e.KayipEsyaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Eslesme>()
                .HasIndex(e => new { e.KayipBildirimiId, e.KayipEsyaId })
                .IsUnique()
                .HasDatabaseName("IX_Eslesmeler_Basvuru_Esya");

            builder.Entity<Bildirim>()
                .HasOne(b => b.Alici)
                .WithMany()
                .HasForeignKey(b => b.AliciUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Bildirim>()
                .HasOne(b => b.KayipBildirimi)
                .WithMany()
                .HasForeignKey(b => b.KayipBildirimiId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Bildirim>()
                .HasOne(b => b.Eslesme)
                .WithMany()
                .HasForeignKey(b => b.EslesmeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeslimIslemi>()
                .HasOne(t => t.Eslesme)
                .WithOne(e => e.TeslimIslemi)
                .HasForeignKey<TeslimIslemi>(t => t.EslesmeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeslimIslemi>()
                .HasOne(t => t.TeslimEdenUser)
                .WithMany()
                .HasForeignKey(t => t.TeslimEdenUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeslimIslemi>()
                .HasIndex(t => t.EslesmeId)
                .IsUnique()
                .HasDatabaseName("IX_TeslimIslemleri_EslesmeId");

            builder.Entity<KayipEsyaResim>()
                .HasOne(r => r.KayipEsya)
                .WithMany(e => e.Resimler)
                .HasForeignKey(r => r.KayipEsyaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<KayipEsyaResim>()
                .HasIndex(r => new { r.KayipEsyaId, r.SiraNumarasi })
                .HasDatabaseName("IX_KayipEsyaResimler_Esya_Sira");

            builder.Entity<KayipBildirimiResim>()
                .HasOne(r => r.KayipBildirimi)
                .WithMany(b => b.Resimler)
                .HasForeignKey(r => r.KayipBildirimiId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<KayipBildirimiResim>()
                .HasIndex(r => new { r.KayipBildirimiId, r.SiraNumarasi })
                .HasDatabaseName("IX_KayipBildirimiResimler_Basvuru_Sira");

            builder.Entity<AuditLog>()
                .HasIndex(a => a.Tarih)
                .HasDatabaseName("IX_AuditLoglar_Tarih");

            builder.Entity<AuditLog>()
                .HasIndex(a => new { a.UserId, a.Tarih })
                .HasDatabaseName("IX_AuditLoglar_User_Tarih");

            builder.Entity<AuditLog>()
                .HasIndex(a => new { a.TabloAdi, a.KayitId, a.Tarih })
                .HasDatabaseName("IX_AuditLoglar_Tablo_Kayit_Tarih");
        }
    }
}
