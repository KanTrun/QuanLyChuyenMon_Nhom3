// Shared page and auth motion.
(function () {
    'use strict';

    const revealSelectors = '.reveal, .reveal-left, .reveal-right, .reveal-scale, .gold-line';
    const initializedAuthPages = new WeakSet();

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

        initializedAuthPages.add(page);
        page.classList.add('auth-ready');

        if (prefersReducedMotion()) return;

        const brandContent = page.querySelector('.login-brand-content');
        const logo = page.querySelector('.login-logo');
        const formWrapper = page.querySelector('.login-form-wrapper');

        animateAuthIntro(page, brandContent, logo, formWrapper);
        initAuthPointerParallax(page, brandContent, formWrapper);
        initAuthFieldMotion(page);
    }

    function animateAuthIntro(page, brandContent, logo, formWrapper) {
        animateElement(brandContent, [
            { opacity: 0, transform: 'translate3d(0, 34px, 0) rotateX(-5deg)' },
            { opacity: 1, transform: 'translate3d(0, 0, 0) rotateX(0deg)' }
        ], 760, 0, 'cubic-bezier(.16, 1, .3, 1)');

        animateElement(logo, [
            { opacity: 0, transform: 'scale(.82) rotate(-8deg)' },
            { opacity: 1, transform: 'scale(1) rotate(0deg)' }
        ], 620, 180, 'cubic-bezier(.34, 1.56, .64, 1)');

        staggerElements(page.querySelectorAll('.auth-command-strip span'), function () {
            return [
                { opacity: 0, transform: 'translate3d(0, 12px, 0)' },
                { opacity: 1, transform: 'translate3d(0, 0, 0)' }
            ];
        }, 360, 260, 55);

        staggerElements(page.querySelectorAll('.login-feature'), function () {
            return [
                { opacity: 0, transform: 'translate3d(-18px, 0, 0)' },
                { opacity: 1, transform: 'translate3d(0, 0, 0)' }
            ];
        }, 420, 360, 75);

        animateElement(formWrapper, [
            { opacity: 0, transform: 'translate3d(0, 26px, 0) scale(.985)' },
            { opacity: 1, transform: 'translate3d(0, 0, 0) scale(1)' }
        ], 620, 220, 'cubic-bezier(.16, 1, .3, 1)');

        staggerElements(page.querySelectorAll('.login-form .form-group, .login-error, .login-success, .login-submit'), function () {
            return [
                { opacity: 0, transform: 'translate3d(0, 12px, 0)' },
                { opacity: 1, transform: 'translate3d(0, 0, 0)' }
            ];
        }, 340, 460, 45);

        if (logo) {
            logo.animate([
                { transform: 'translate3d(0, 0, 0) rotate(0deg)' },
                { transform: 'translate3d(0, -8px, 0) rotate(1.5deg)' }
            ], {
                duration: 2800,
                direction: 'alternate',
                easing: 'ease-in-out',
                iterations: Infinity
            });
        }
    }

    function animateElement(element, keyframes, duration, delay, easing) {
        if (!element || typeof element.animate !== 'function') return;

        element.animate(keyframes, {
            duration: duration,
            delay: delay,
            easing: easing,
            fill: 'both'
        });
    }

    function staggerElements(elements, keyframesFactory, duration, baseDelay, stepDelay) {
        elements.forEach(function (element, index) {
            animateElement(element, keyframesFactory(element), duration, baseDelay + index * stepDelay, 'cubic-bezier(.16, 1, .3, 1)');
        });
    }

    function initAuthPointerParallax(page, brandContent, formWrapper) {
        if (!brandContent || !formWrapper || page.dataset.pointerMotionBound === 'true') return;
        page.dataset.pointerMotionBound = 'true';

        let frameId = 0;
        const offsets = {
            brandX: 0,
            brandY: 0,
            formX: 0,
            formY: 0
        };

        function scheduleTransform() {
            if (frameId) return;
            frameId = window.requestAnimationFrame(function () {
                brandContent.style.transform = `translate3d(${offsets.brandX}px, ${offsets.brandY}px, 0)`;
                formWrapper.style.transform = `translate3d(${offsets.formX}px, ${offsets.formY}px, 0)`;
                frameId = 0;
            });
        }

        page.addEventListener('pointermove', function (event) {
            if (event.pointerType === 'touch') return;
            const rect = page.getBoundingClientRect();
            const x = (event.clientX - rect.left) / rect.width - 0.5;
            const y = (event.clientY - rect.top) / rect.height - 0.5;
            offsets.brandX = Math.round(x * 14);
            offsets.brandY = Math.round(y * 10);
            offsets.formX = Math.round(x * -8);
            offsets.formY = Math.round(y * -6);
            scheduleTransform();
        }, { passive: true });

        page.addEventListener('pointerleave', function () {
            offsets.brandX = 0;
            offsets.brandY = 0;
            offsets.formX = 0;
            offsets.formY = 0;
            scheduleTransform();
        }, { passive: true });
    }

    function initAuthFieldMotion(page) {
        if (page.dataset.fieldMotionBound === 'true') return;
        page.dataset.fieldMotionBound = 'true';

        page.addEventListener('focusin', function (event) {
            const group = event.target.closest('.form-group');
            if (group) group.style.transform = 'translate3d(0, -2px, 0)';
        });

        page.addEventListener('focusout', function (event) {
            const group = event.target.closest('.form-group');
            if (group) group.style.transform = '';
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
