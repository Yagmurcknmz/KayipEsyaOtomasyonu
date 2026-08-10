using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<KayipEsya> KayipEsyalar { get; set; }

        public DbSet<KayipBildirimi> KayipBildirimleri { get; set; }

        public DbSet<Eslesme> Eslesmeler { get; set; }

        public DbSet<Bildirim> Bildirimler { get; set; }

        public DbSet<TeslimIslemi> TeslimIslemleri { get; set; }

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
                .WithMany()
                .HasForeignKey(t => t.EslesmeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeslimIslemi>()
                .HasOne(t => t.TeslimEden)
                .WithMany()
                .HasForeignKey(t => t.TeslimEdenUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeslimIslemi>()
                .HasIndex(t => t.EslesmeId)
                .IsUnique()
                .HasDatabaseName("IX_TeslimIslemleri_EslesmeId");
        }
    }
}