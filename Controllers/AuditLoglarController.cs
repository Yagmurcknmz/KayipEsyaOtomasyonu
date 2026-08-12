using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin,Personel")]
    public class AuditLoglarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditLoglarController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            int Sayfa = 1,
            int SayfaBoyutu = 25,
            string? Ara = null,
            string? TabloAdi = null,
            AuditTip? Tip = null,
            string? UserId = null,
            DateTime? Baslangic = null,
            DateTime? Bitis = null)
        {
            if (Sayfa < 1) Sayfa = 1;
            if (SayfaBoyutu < 5) SayfaBoyutu = 25;
            if (SayfaBoyutu > 200) SayfaBoyutu = 200;

            var vm = new AuditLogIndexViewModel
            {
                Sayfa = Sayfa,
                SayfaBoyutu = SayfaBoyutu,
                Ara = Ara?.Trim(),
                TabloAdi = string.IsNullOrWhiteSpace(TabloAdi) ? null : TabloAdi.Trim(),
                Tip = Tip,
                UserId = string.IsNullOrWhiteSpace(UserId) ? null : UserId.Trim(),
                Baslangic = Baslangic,
                Bitis = Bitis
            };

            IQueryable<AuditLog> q = _context.AuditLoglar.AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(vm.Ara))
            {
                var a = vm.Ara.ToLowerInvariant();
                q = q.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.UserName) && x.UserName.ToLower().Contains(a)) ||
                    (!string.IsNullOrWhiteSpace(x.TabloAdi) && x.TabloAdi.ToLower().Contains(a)) ||
                    (!string.IsNullOrWhiteSpace(x.Aciklama) && x.Aciklama.ToLower().Contains(a)) ||
                    (!string.IsNullOrWhiteSpace(x.EskiDegerlerJson) && x.EskiDegerlerJson.ToLower().Contains(a)) ||
                    (!string.IsNullOrWhiteSpace(x.YeniDegerlerJson) && x.YeniDegerlerJson.ToLower().Contains(a)) ||
                    (!string.IsNullOrWhiteSpace(x.IpAdresi) && x.IpAdresi.ToLower().Contains(a)) ||
                    (x.KayitId.HasValue && x.KayitId.Value.ToString() == a) ||
                    (!string.IsNullOrWhiteSpace(x.KayitAnahtari) && x.KayitAnahtari.ToLower().Contains(a)));
            }

            if (!string.IsNullOrWhiteSpace(vm.TabloAdi))
            {
                q = q.Where(x => x.TabloAdi == vm.TabloAdi);
            }

            if (vm.Tip.HasValue)
            {
                q = q.Where(x => x.Tip == vm.Tip.Value);
            }

            if (!string.IsNullOrWhiteSpace(vm.UserId))
            {
                q = q.Where(x => x.UserId == vm.UserId);
            }

            if (vm.Baslangic.HasValue)
            {
                q = q.Where(x => x.Tarih.Date >= vm.Baslangic.Value.Date);
            }

            if (vm.Bitis.HasValue)
            {
                q = q.Where(x => x.Tarih.Date <= vm.Bitis.Value.Date);
            }

            vm.ToplamKayit = await q.CountAsync();

            vm.Kayitlar = await q
                .OrderByDescending(x => x.Tarih)
                .Skip((vm.Sayfa - 1) * vm.SayfaBoyutu)
                .Take(vm.SayfaBoyutu)
                .ToListAsync();

            vm.TabloAdlari = await _context.AuditLoglar.AsNoTracking()
                .Select(x => x.TabloAdi)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .Select(x => x!)
                .ToListAsync();

            return View(vm);
        }
    }
}
