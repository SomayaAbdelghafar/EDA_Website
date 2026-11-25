// EDA website scripts
(function () {
    "use strict";
    document.documentElement.classList.add("js");

    // ---------- Reveal-on-scroll for cards and section content ----------
    var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var revealTargets = document.querySelectorAll(
        ".eda-service-card, .eda-project-card, .eda-value, .eda-client, .eda-process li, .eda-sectors div, .eda-section-head, .eda-contact-card, .eda-tl-copy, .eda-tl-visual");
    if ("IntersectionObserver" in window) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("is-visible");
                    observer.unobserve(entry.target);
                }
            });
        }, { rootMargin: "0px 0px -40px 0px" });
        revealTargets.forEach(function (el) {
            el.classList.add("eda-reveal");
            observer.observe(el);
        });
    }

    // ---------- Count-up numbers (TRANE license card) ----------
    var counters = document.querySelectorAll("[data-eda-count]");
    if (counters.length && "IntersectionObserver" in window && !reduceMotion) {
        var countObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                countObserver.unobserve(entry.target);
                var el = entry.target;
                var end = parseInt(el.dataset.edaCount, 10) || 0;
                var start = null;
                var step = function (ts) {
                    if (start === null) start = ts;
                    var p = Math.min((ts - start) / 1400, 1);
                    el.textContent = Math.round(end * (1 - Math.pow(1 - p, 3))); // ease-out
                    if (p < 1) requestAnimationFrame(step);
                };
                requestAnimationFrame(step);
            });
        }, { threshold: 0.6 });
        counters.forEach(function (el) {
            el.textContent = "0";
            countObserver.observe(el);
        });
    }

    // ---------- TRANE license card: gentle 3D tilt that follows the pointer ----------
    var tiltCard = document.querySelector("[data-eda-tilt]");
    if (tiltCard && !reduceMotion && window.matchMedia("(hover: hover) and (pointer: fine)").matches) {
        tiltCard.addEventListener("pointermove", function (e) {
            var r = tiltCard.getBoundingClientRect();
            var x = (e.clientX - r.left) / r.width;
            var y = (e.clientY - r.top) / r.height;
            tiltCard.classList.add("is-tilting");
            tiltCard.style.setProperty("--ry", ((x - 0.5) * 10).toFixed(2) + "deg");
            tiltCard.style.setProperty("--rx", ((0.5 - y) * 8).toFixed(2) + "deg");
            tiltCard.style.setProperty("--mx", (x * 100).toFixed(1) + "%");
            tiltCard.style.setProperty("--my", (y * 100).toFixed(1) + "%");
        });
        tiltCard.addEventListener("pointerleave", function () {
            tiltCard.classList.remove("is-tilting");
            tiltCard.style.setProperty("--rx", "0deg");
            tiltCard.style.setProperty("--ry", "0deg");
        });
    }

    // ---------- Lightbox (project galleries, certificates) ----------
    var modalEl = document.getElementById("edaLightbox");
    if (modalEl && window.bootstrap) {
        var modal = new bootstrap.Modal(modalEl);
        var img = modalEl.querySelector(".eda-lightbox-img");
        var caption = modalEl.querySelector(".eda-lightbox-caption");
        var group = [];
        var current = 0;

        var show = function (i) {
            current = (i + group.length) % group.length;
            var link = group[current];
            img.src = link.getAttribute("href");
            img.alt = link.querySelector("img") ? link.querySelector("img").alt : "";
            var text = link.dataset.caption || "";
            caption.textContent = group.length > 1 ? text + " (" + (current + 1) + " / " + group.length + ")" : text;
        };

        document.addEventListener("click", function (e) {
            var link = e.target.closest("[data-eda-lightbox]");
            if (!link) return;
            e.preventDefault();
            var name = link.dataset.edaLightbox;
            group = name
                ? Array.prototype.slice.call(document.querySelectorAll('[data-eda-lightbox="' + name + '"]'))
                : [link];
            modalEl.classList.toggle("is-single", group.length < 2);
            show(group.indexOf(link));
            modal.show();
        });

        // RTL: "previous" sits on the right, "next" on the left
        modalEl.querySelector(".eda-lightbox-prev").addEventListener("click", function () { show(current - 1); });
        modalEl.querySelector(".eda-lightbox-next").addEventListener("click", function () { show(current + 1); });
        modalEl.addEventListener("keydown", function (e) {
            if (e.key === "ArrowRight") show(current - 1);
            if (e.key === "ArrowLeft") show(current + 1);
        });
    }

    // ---------- Contact form: send the same details over WhatsApp ----------
    var waButton = document.getElementById("sendWhatsApp");
    if (waButton) {
        waButton.addEventListener("click", function () {
            var form = document.getElementById("contactForm");
            var val = function (name) {
                var field = form.querySelector('[name="' + name + '"]');
                return field ? field.value.trim() : "";
            };
            var lines = ["السلام عليكم، أرغب في طلب خدمة من شركة إيدا للتكييف والتبريد."];
            if (val("Name")) lines.push("الاسم: " + val("Name"));
            if (val("Phone")) lines.push("الجوال: " + val("Phone"));
            if (val("City")) lines.push("المدينة: " + val("City"));
            if (val("Service")) lines.push("الخدمة: " + val("Service"));
            if (val("Message")) lines.push("التفاصيل: " + val("Message"));
            window.open("https://wa.me/" + waButton.dataset.wa + "?text=" + encodeURIComponent(lines.join("\n")), "_blank", "noopener");
        });
    }

    // ---------- Static preview (GitHub Pages): no server to post the form to ----------
    // #previewNotice is only rendered when the site runs with StaticPreview=true
    var previewNotice = document.getElementById("previewNotice");
    var previewForm = document.getElementById("contactForm");
    if (previewNotice && previewForm) {
        previewForm.addEventListener("submit", function (e) {
            e.preventDefault();
            var $ = window.jQuery;
            if ($ && $.fn.valid && !$(previewForm).valid()) return;
            previewNotice.classList.remove("d-none");
            previewNotice.classList.add("d-flex");
            previewNotice.scrollIntoView({ behavior: "smooth", block: "center" });
        });
    }

    // ---------- Contact form: block double submits while the email is being sent ----------
    var contactForm = document.getElementById("contactForm");
    var submitButton = contactForm && contactForm.querySelector('button[type="submit"]');
    if (submitButton && !previewNotice) {
        var submitLabel = submitButton.innerHTML;
        contactForm.addEventListener("submit", function () {
            var $ = window.jQuery;
            if ($ && $.fn.valid && !$(contactForm).valid()) return;
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> جارٍ الإرسال...';
        });
        // Coming back with the browser's back button can restore the page with the button still disabled
        window.addEventListener("pageshow", function () {
            submitButton.disabled = false;
            submitButton.innerHTML = submitLabel;
        });
    }
})();
