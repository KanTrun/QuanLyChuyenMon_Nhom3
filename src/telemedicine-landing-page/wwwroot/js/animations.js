// ═══════════════════════════════════════════════════
// SCROLL ANIMATIONS — Royal Hospital
// IntersectionObserver-based reveal system
// ═══════════════════════════════════════════════════

(function () {
    'use strict';

    // ── Scroll Reveal Observer ──
    const revealSelectors = '.reveal, .reveal-left, .reveal-right, .reveal-scale, .gold-line';

    function initScrollReveal() {
        const elements = document.querySelectorAll(revealSelectors);
        if (!elements.length) return;

        // Respect reduced motion preference
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            elements.forEach(function (el) { el.classList.add('is-visible'); });
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.12,
            rootMargin: '0px 0px -40px 0px'
        });

        elements.forEach(function (el) { observer.observe(el); });
    }

    // ── Sticky Header Scroll Effect ──
    function initStickyHeader() {
        var header = document.getElementById('site-header');
        if (!header) return;

        var scrollThreshold = 60;
        var ticking = false;

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(function () {
                    if (window.scrollY > scrollThreshold) {
                        header.classList.add('scrolled');
                    } else {
                        header.classList.remove('scrolled');
                    }
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });
    }

    // ── Smooth Scroll for Anchor Links ──
    function initSmoothScroll() {
        document.addEventListener('click', function (e) {
            var link = e.target.closest('a[href^="#"]');
            if (!link) return;

            var targetId = link.getAttribute('href');
            if (targetId === '#') return;

            var target = document.querySelector(targetId);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    }

    // ── Initialize All ──
    function init() {
        initScrollReveal();
        initStickyHeader();
        initSmoothScroll();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
