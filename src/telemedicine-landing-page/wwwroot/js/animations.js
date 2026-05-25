// Shared page and auth motion.
(function () {
    'use strict';

    const revealSelectors = '.reveal, .reveal-left, .reveal-right, .reveal-scale, .gold-line';
    const gsapCdnUrl = 'https://cdn.jsdelivr.net/npm/gsap@3.15.0/dist/gsap.min.js';
    const initializedAuthPages = new WeakSet();
    let activeAuthMatchMedia = null;
    let gsapLoadPromise = null;

    function prefersReducedMotion() {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    function initScrollReveal() {
        const elements = document.querySelectorAll(revealSelectors);
        if (!elements.length) return;

        if (prefersReducedMotion()) {
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

    function initStickyHeader() {
        var header = document.getElementById('site-header');
        if (!header || header.dataset.scrollBound === 'true') return;

        header.dataset.scrollBound = 'true';
        var scrollThreshold = 60;
        var ticking = false;

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(function () {
                    header.classList.toggle('scrolled', window.scrollY > scrollThreshold);
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });
    }

    function initSmoothScroll() {
        if (document.documentElement.dataset.smoothScrollBound === 'true') return;
        document.documentElement.dataset.smoothScrollBound = 'true';

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

    function initAuthPage() {
        const page = document.querySelector('[data-auth-page]');
        if (!page || initializedAuthPages.has(page)) return;

        page.classList.add('auth-ready');
        if (prefersReducedMotion()) {
            initializedAuthPages.add(page);
            return;
        }

        if (!window.gsap) {
            loadGsap().then(initAuthPage).catch(function () {
                initializedAuthPages.add(page);
            });
            return;
        }

        initializedAuthPages.add(page);
        const gsap = window.gsap;
        if (activeAuthMatchMedia) {
            activeAuthMatchMedia.revert();
            activeAuthMatchMedia = null;
        }

        const mm = gsap.matchMedia();
        activeAuthMatchMedia = mm;

        mm.add('(prefers-reduced-motion: no-preference)', function () {
            const brandContent = page.querySelector('.login-brand-content');
            const logo = page.querySelector('.login-logo');
            const features = page.querySelectorAll('.login-feature');
            const formWrapper = page.querySelector('.login-form-wrapper');
            const formItems = page.querySelectorAll('.login-form .form-group, .login-error, .login-success, .login-submit');
            const stripItems = page.querySelectorAll('.auth-command-strip span');

            gsap.set([brandContent, formWrapper], { transformPerspective: 900 });

            gsap.timeline({ defaults: { ease: 'power3.out' } })
                .from(brandContent, { autoAlpha: 0, y: 34, rotateX: -5, duration: 0.75 })
                .from(logo, { scale: 0.82, rotate: -8, duration: 0.65, ease: 'back.out(1.8)' }, '-=0.48')
                .from(stripItems, { autoAlpha: 0, y: 12, stagger: 0.055, duration: 0.36 }, '-=0.32')
                .from(features, { autoAlpha: 0, x: -18, stagger: 0.075, duration: 0.42 }, '-=0.16')
                .from(formWrapper, { autoAlpha: 0, y: 26, scale: 0.985, duration: 0.62 }, '-=0.55')
                .from(formItems, { autoAlpha: 0, y: 12, stagger: 0.045, duration: 0.34 }, '-=0.34');

            if (logo) {
                gsap.to(logo, {
                    y: -8,
                    rotate: 1.5,
                    duration: 2.8,
                    repeat: -1,
                    yoyo: true,
                    ease: 'sine.inOut'
                });
            }

            initAuthPointerParallax(page, gsap, brandContent, formWrapper);
            initAuthFieldMotion(page, gsap);
        });
    }

    function loadGsap() {
        if (window.gsap) return Promise.resolve(window.gsap);
        if (gsapLoadPromise) return gsapLoadPromise;

        const existingScript = document.querySelector('script[data-auth-gsap]');
        if (existingScript) {
            gsapLoadPromise = new Promise(function (resolve, reject) {
                existingScript.addEventListener('load', function () { resolve(window.gsap); }, { once: true });
                existingScript.addEventListener('error', reject, { once: true });
            });
            return gsapLoadPromise;
        }

        gsapLoadPromise = new Promise(function (resolve, reject) {
            const script = document.createElement('script');
            script.src = gsapCdnUrl;
            script.async = true;
            script.dataset.authGsap = '3.15.0';
            script.onload = function () { resolve(window.gsap); };
            script.onerror = reject;
            document.head.appendChild(script);
        });

        return gsapLoadPromise;
    }

    function initAuthPointerParallax(page, gsap, brandContent, formWrapper) {
        if (!brandContent || !formWrapper || page.dataset.pointerMotionBound === 'true') return;
        page.dataset.pointerMotionBound = 'true';

        const brandX = gsap.quickTo(brandContent, 'x', { duration: 0.75, ease: 'power3.out' });
        const brandY = gsap.quickTo(brandContent, 'y', { duration: 0.75, ease: 'power3.out' });
        const formX = gsap.quickTo(formWrapper, 'x', { duration: 0.85, ease: 'power3.out' });
        const formY = gsap.quickTo(formWrapper, 'y', { duration: 0.85, ease: 'power3.out' });

        page.addEventListener('pointermove', function (event) {
            if (event.pointerType === 'touch') return;
            const rect = page.getBoundingClientRect();
            const x = (event.clientX - rect.left) / rect.width - 0.5;
            const y = (event.clientY - rect.top) / rect.height - 0.5;
            brandX(x * 14);
            brandY(y * 10);
            formX(x * -8);
            formY(y * -6);
        }, { passive: true });

        page.addEventListener('pointerleave', function () {
            brandX(0);
            brandY(0);
            formX(0);
            formY(0);
        }, { passive: true });
    }

    function initAuthFieldMotion(page, gsap) {
        if (page.dataset.fieldMotionBound === 'true') return;
        page.dataset.fieldMotionBound = 'true';

        page.addEventListener('focusin', function (event) {
            const group = event.target.closest('.form-group');
            if (group) gsap.to(group, { y: -2, duration: 0.18, ease: 'power2.out' });
        });

        page.addEventListener('focusout', function (event) {
            const group = event.target.closest('.form-group');
            if (group) gsap.to(group, { y: 0, duration: 0.22, ease: 'power2.out' });
        });
    }

    function init() {
        initScrollReveal();
        initStickyHeader();
        initSmoothScroll();
        initAuthPage();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    document.addEventListener('enhancedload', init);
})();
