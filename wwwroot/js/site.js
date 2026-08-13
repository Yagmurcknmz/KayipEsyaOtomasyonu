// ============================================================
//  Kayıp Eşya Otomasyonu — Global Modern Frontend Scriptleri
// ============================================================

(function () {
    'use strict';

    const SCRIPT_NAME = 'site.js';

    // --------------------------------------------------------
    //  1. TOAST BILDIRIM SISTEMI
    //  Kullanim: window.showToast('İşlem başarılı', 'success' | 'error' | 'info' | 'warning', 4500);
    // --------------------------------------------------------
    function toastContainerOlustur() {
        let c = document.querySelector('.toast-container');
        if (c) return c;
        c = document.createElement('div');
        c.className = 'toast-container';
        c.setAttribute('aria-live', 'polite');
        c.setAttribute('aria-atomic', 'true');
        document.body.appendChild(c);
        return c;
    }

    function toastIkon(tip) {
        switch (tip) {
            case 'success': return '<i class="bi bi-check2-circle"></i>';
            case 'error':   return '<i class="bi bi-x-circle-fill"></i>';
            case 'warning': return '<i class="bi bi-exclamation-triangle-fill"></i>';
            case 'info':
            default:        return '<i class="bi bi-info-circle-fill"></i>';
        }
    }

    function toastBaslik(tip) {
        switch (tip) {
            case 'success': return 'İşlem Başarılı';
            case 'error':   return 'Hata Oluştu';
            case 'warning': return 'Dikkat';
            case 'info':
            default:        return 'Bilgi';
        }
    }

    window.showToast = function (mesaj, tip, sureMs) {
        if (typeof mesaj === 'undefined' || mesaj === null || mesaj.toString().trim() === '') return;

        tip = tip || 'info';
        sureMs = typeof sureMs === 'number' && sureMs > 0 ? sureMs : 4500;

        const container = toastContainerOlustur();

        const wrapper = document.createElement('div');
        wrapper.className = 'toast-notif toast-' + tip;
        wrapper.setAttribute('role', 'status');

        wrapper.innerHTML =
            '<div class="toast-icon">' + toastIkon(tip) + '</div>' +
            '<div class="toast-body">' +
                '<h6 class="toast-title">' + toastBaslik(tip) + '</h6>' +
                '<p class="toast-msg"></p>' +
            '</div>' +
            '<button type="button" class="toast-close" aria-label="Kapat">' +
                '<i class="bi bi-x-lg"></i>' +
            '</button>';

        wrapper.querySelector('.toast-msg').textContent = mesaj.toString();

        let killTimer = null;

        function kapat() {
            if (killTimer) {
                clearTimeout(killTimer);
                killTimer = null;
            }
            wrapper.classList.add('toast-out');
            wrapper.addEventListener('animationend', function () {
                if (wrapper.parentNode) wrapper.parentNode.removeChild(wrapper);
            }, { once: true });
        }

        wrapper.querySelector('.toast-close').addEventListener('click', kapat);

        // Mobilde dokunmatik ile kapatma (2sn dokununca)
        let dokunmaZaman = 0;
        wrapper.addEventListener('touchstart', function () { dokunmaZaman = Date.now(); }, { passive: true });
        wrapper.addEventListener('touchend', function () {
            if (Date.now() - dokunmaZaman > 250) kapat();
        });

        killTimer = setTimeout(kapat, sureMs);

        container.appendChild(wrapper);
    };

    // --------------------------------------------------------
    //  2. FORM SUBMIT'TE LOADING SPINNER (Butona koy + kilitle)
    //     Double-click engelle
    // --------------------------------------------------------
    function submitteSpinnerKur() {
        document.addEventListener('submit', function (ev) {
            const form = ev.target;
            if (!form || form.tagName !== 'FORM') return;

            // Data attribute ile devre disi birakma: data-disable-spinner="true"
            if (form.getAttribute('data-disable-spinner') === 'true') return;

            const gonderen = ev.submitter;
            if (!gonderen || gonderen.tagName !== 'BUTTON') return;

            // Zaten disabled ise tekrar isleme
            if (gonderen.disabled) {
                ev.preventDefault();
                return;
            }

            // Form invalid (HTML5 validasyon) ise dokunma
            if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;

            const eskiHTML = gonderen.innerHTML;
            const eskiWidth = gonderen.offsetWidth;

            gonderen.dataset.eskiHtml = eskiHTML;
            gonderen.disabled = true;
            gonderen.style.width = eskiWidth + 'px';
            gonderen.innerHTML =
                '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' +
                '<span class="visually-hidden">Yükleniyor</span>İşleniyor...';

            // 30 sn sonra force unlock (request sonsuza kadar takilirsa)
            setTimeout(function () {
                if (gonderen.disabled) {
                    gonderen.disabled = false;
                    gonderen.style.width = '';
                    if (gonderen.dataset.eskiHtml) gonderen.innerHTML = gonderen.dataset.eskiHtml;
                }
            }, 30000);
        }, true);
    }

    // --------------------------------------------------------
    //  3. DATA-SWAL-CONFIRM Handler
    //  Butonlarda: <a data-swal-confirm="Emin misiniz?" href="...">Sil</a>
    //  Otomatik SweetAlert2 ile sor.
    // --------------------------------------------------------
    function swalConfirmKur() {
        document.addEventListener('click', function (ev) {
            const el = ev.target.closest('[data-swal-confirm]') ||
                       ev.target.closest('[data-swal-title]') ||
                       ev.target.closest('[confirm]');

            if (!el) return;

            if (typeof window.Swal === 'undefined') {
                // SweetAlert2 yuklenmemis ise eski native confirm
                const mesaj = el.getAttribute('data-swal-confirm') ||
                              el.getAttribute('confirm') ||
                              'Bu işlemi yapmak istediğinize emin misiniz?';
                if (!window.confirm(mesaj)) {
                    ev.preventDefault();
                    ev.stopImmediatePropagation();
                }
                return;
            }

            ev.preventDefault();
            ev.stopImmediatePropagation();

            const baslik = el.getAttribute('data-swal-title') || 'Emin misiniz?';
            const mesaj = el.getAttribute('data-swal-confirm') ||
                          el.getAttribute('confirm') ||
                          'Bu işlem geri alınamaz.';
            const confirmText = el.getAttribute('data-swal-confirm-text') || 'Evet, Onayla';
            const cancelText = el.getAttribute('data-swal-cancel-text') || 'İptal';
            const icon = el.getAttribute('data-swal-icon') || 'warning';

            window.Swal.fire({
                title: baslik,
                html: '<div class="text-muted" style="font-size:14px;">' + (mesaj || '') + '</div>',
                icon: icon,
                iconColor: icon === 'warning' ? '#f59e0b' : undefined,
                showCancelButton: true,
                confirmButtonText: confirmText,
                cancelButtonText: cancelText,
                reverseButtons: true,
                confirmButtonColor: '#0b5cff',
                cancelButtonColor: '#64748b',
                customClass: {
                    popup: 'rounded-4 shadow-lg',
                    confirmButton: 'fw-semibold rounded-3 px-4 py-2',
                    cancelButton: 'fw-semibold rounded-3 px-4 py-2 me-1',
                    title: 'fw-bold'
                },
                buttonsStyling: true
            }).then(function (result) {
                if (!result.isConfirmed) return;

                // Buton ise (form submit ise)
                if (el.tagName === 'BUTTON') {
                    const frm = el.closest('form');
                    if (frm) {
                        el.setAttribute('data-sw-confirmed', 'true');
                        el.click();
                        frm.submit();
                        return;
                    }
                }

                // Link ise doğrudan yönlendir
                if (el.tagName === 'A' && el.getAttribute('href')) {
                    let href = el.getAttribute('href');
                    if (href && href !== '#' && href !== 'javascript:void(0)') {
                        window.location.href = href;
                        return;
                    }
                }

                // Post back gerekiyorsa form icindeki click
                el.setAttribute('data-sw-confirmed', 'true');
                el.click();
            });

            return false;
        }, true);
    }

    // --------------------------------------------------------
    //  4. DATA-TOAST Handler (data-toast-success, data-toast-error vb.)
    //  Örn: <a href="/sil" data-toast-success="Silindi">Tıkla</a>
    // --------------------------------------------------------
    function dataToastKur() {
        document.addEventListener('click', function (ev) {
            const el = ev.target.closest('[data-toast-success], [data-toast-info], [data-toast-warning], [data-toast-error]');
            if (!el) return;
            const s = el.getAttribute('data-toast-success');
            const i = el.getAttribute('data-toast-info');
            const w = el.getAttribute('data-toast-warning');
            const e = el.getAttribute('data-toast-error');
            if (s) setTimeout(function () { window.showToast(s, 'success'); }, 250);
            if (i) setTimeout(function () { window.showToast(i, 'info'); }, 250);
            if (w) setTimeout(function () { window.showToast(w, 'warning'); }, 250);
            if (e) setTimeout(function () { window.showToast(e, 'error'); }, 250);
        });
    }

    // --------------------------------------------------------
    //  5. External Link yeni sekmede (target="_blank" değilse) güvenli
    // --------------------------------------------------------
    function externalLinkGuvenligi() {
        document.addEventListener('click', function (ev) {
            const a = ev.target.closest('a[href^="http"]');
            if (!a) return;
            if (a.host && a.host !== window.location.host && !a.target) {
                a.target = '_blank';
                a.rel = (a.rel ? a.rel + ' ' : '') + 'noopener noreferrer';
            }
        });
    }

    // --------------------------------------------------------
    //  6. Otomatik TempData-Toast (inline basarili/hata verisini oku toast olarak goster)
    //  _Layout'un icinde hidden input ile gonderilecek degerler burada yakalanir.
    // --------------------------------------------------------
    function tempDataToastOku() {
        try {
            const sEl = document.querySelector('meta[name="x-toast-success"]');
            const eEl = document.querySelector('meta[name="x-toast-error"]');
            const wEl = document.querySelector('meta[name="x-toast-warning"]');
            const iEl = document.querySelector('meta[name="x-toast-info"]');
            const s = sEl && sEl.content ? sEl.content.trim() : '';
            const e = eEl && eEl.content ? eEl.content.trim() : '';
            const w = wEl && wEl.content ? wEl.content.trim() : '';
            const i = iEl && iEl.content ? iEl.content.trim() : '';
            if (s) window.showToast(s, 'success');
            if (e) window.showToast(e, 'error');
            if (w) window.showToast(w, 'warning');
            if (i) window.showToast(i, 'info');

            // Backward: Eski standart DOM TempData Alert bloklarini da toast'a donustur
            const eskiBasarili = document.querySelectorAll('div.alert.alert-success[data-convert-toast="true"]');
            eskiBasarili.forEach(function (el) {
                window.showToast(el.textContent.trim(), 'success');
                if (el.parentNode) el.parentNode.removeChild(el);
            });

            const eskiHata = document.querySelectorAll('div.alert.alert-danger[data-convert-toast="true"]');
            eskiHata.forEach(function (el) {
                window.showToast(el.textContent.trim(), 'error');
                if (el.parentNode) el.parentNode.removeChild(el);
            });
        } catch (err) {
            if (window.console && console.warn) console.warn(SCRIPT_NAME + ' TempData oku hatasi:', err);
        }
    }

    // --------------------------------------------------------
    //  7. Global hata yakala (beklenmedik JS hatasinda toast)
    // --------------------------------------------------------
    function globalHataYakala() {
        window.addEventListener('error', function (e) {
            try {
                if (!e || !e.message || e.message.indexOf('ResizeObserver loop') !== -1) return; // Onemsiz browser hatalarini sustur
                // Sadece gelistirme ortaminda goster:
                if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                    console.error('Hata:', e.message, e.filename, e.lineno);
                }
            } catch (_) { /* no-op */ }
        });
    }

    // --------------------------------------------------------
    //  DOMContentLoaded ile tum handler'lari bagla
    // --------------------------------------------------------
    document.addEventListener('DOMContentLoaded', function () {
        submitteSpinnerKur();
        swalConfirmKur();
        dataToastKur();
        externalLinkGuvenligi();
        tempDataToastOku();
        globalHataYakala();
    });
})();
