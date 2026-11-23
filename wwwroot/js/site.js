// EDA website scripts
(function () {
    "use strict";
    document.documentElement.classList.add("js");

    // ---------- Reveal-on-scroll for cards and section content ----------
    var revealTargets = document.querySelectorAll(
        ".eda-service-card, .eda-project-card, .eda-value, .eda-client, .eda-process li, .eda-sectors div, .eda-section-head, .eda-contact-card");
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
})();
