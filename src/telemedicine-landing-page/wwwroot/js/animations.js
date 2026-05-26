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
            const brandContent = selectVisibleOne(page, '.login-brand-content');
            const logo = selectVisibleOne(page, '.login-logo');
            const productRow = selectVisibleOne(page, '.auth-product-row');
            const features = selectVisible(page, '.login-feature');
            const formWrapper = selectVisibleOne(page, '.login-form-wrapper');
            const routePill = selectVisibleOne(page, '.auth-route-pill');
            const formAura = selectVisibleOne(page, '.auth-form-aura');
            const statusCard = selectVisibleOne(page, '.auth-status-card');
            const depthItems = selectVisible(page, '.auth-plane, .auth-ring');
            const planes = selectVisible(page, '.auth-plane');
            const rings = selectVisible(page, '.auth-ring');
            const signals = selectVisible(page, '.auth-signal');
            const meterBars = selectVisible(page, '.auth-status-meter span');
            const formItems = selectVisible(page, '.login-form .form-group, .login-error, .login-success, .login-submit');
            const stripItems = selectVisible(page, '.auth-command-strip span');
            const assuranceItems = selectVisible(page, '.auth-assurance span');

            gsap.set([brandContent, formWrapper, statusCard].filter(Boolean), {
                transformPerspective: 1100,
                transformStyle: 'preserve-3d'
            });
            gsap.set([].concat(Array.from(depthItems), Array.from(signals), [formAura].filter(Boolean)), {
                force3D: true,
                willChange: 'transform, opacity'
            });

            const intro = gsap.timeline({ defaults: { ease: 'power3.out' } });
            if (depthItems.length) {
                intro.from(depthItems, { autoAlpha: 0, y: 18, scale: 0.94, rotateX: -8, stagger: 0.06, duration: 0.72 });
            }
            if (signals.length) {
                intro.from(signals, { autoAlpha: 0, x: -30, scaleX: 0.82, stagger: 0.08, duration: 0.7 }, '-=0.58');
            }
            if (brandContent) {
                intro.from(brandContent, { autoAlpha: 0, y: 34, rotateX: -5, duration: 0.75 }, '-=0.5');
            }
            if (productRow) {
                intro.from(productRow, { autoAlpha: 0, y: 16, rotateX: -8, duration: 0.45 }, '-=0.54');
            }
            if (logo) {
                intro.from(logo, { scale: 0.82, rotate: -8, duration: 0.65, ease: 'back.out(1.8)' }, '-=0.4');
            }
            if (stripItems.length) {
                intro.from(stripItems, { autoAlpha: 0, y: 12, stagger: 0.055, duration: 0.36 }, '-=0.32');
            }
            if (features.length) {
                intro.from(features, { autoAlpha: 0, x: -18, stagger: 0.075, duration: 0.42 }, '-=0.16');
            }
            if (statusCard) {
                intro.from(statusCard, { autoAlpha: 0, y: 16, rotateX: -5, duration: 0.46 }, '-=0.18');
            }
            if (meterBars.length) {
                intro.from(meterBars, { scaleY: 0.2, transformOrigin: '50% 100%', stagger: 0.06, duration: 0.34 }, '-=0.28');
            }
            if (formAura) {
                intro.from(formAura, { autoAlpha: 0, scaleX: 0.82, x: 24, duration: 0.72 }, 0.14);
            }
            if (formWrapper) {
                intro.from(formWrapper, { autoAlpha: 0, y: 28, rotateX: 4, scale: 0.985, duration: 0.64 }, 0.24);
            }
            if (routePill) {
                intro.from(routePill, { autoAlpha: 0, y: 10, duration: 0.28 }, 0.5);
            }
            if (formItems.length) {
                intro.from(formItems, { autoAlpha: 0, y: 12, stagger: 0.045, duration: 0.34 }, 0.58);
            }
            if (assuranceItems.length) {
                intro.from(assuranceItems, { autoAlpha: 0, y: 8, stagger: 0.04, duration: 0.24 }, 0.76);
            }
            if (routePill || formItems.length || assuranceItems.length) {
                gsap.delayedCall(1.8, function () {
                    const formRevealTargets = [routePill].concat(formItems, assuranceItems).filter(Boolean);
                    gsap.set(formRevealTargets, { autoAlpha: 1, y: 0, clearProps: 'opacity,visibility,transform' });
                });
            }

            startAuthFloatingMotion(page, gsap, logo, planes, rings, signals, statusCard, formAura);
            initAuthPointerParallax(page, gsap, brandContent, formWrapper, statusCard);
            initAuthFieldMotion(page, gsap);
        });
    }

    function selectVisible(root, selector) {
        return Array.from(root.querySelectorAll(selector)).filter(isVisibleElement);
    }

    function selectVisibleOne(root, selector) {
        const element = root.querySelector(selector);
        return isVisibleElement(element) ? element : null;
    }

    function isVisibleElement(element) {
        return !!(element && element.getClientRects().length);
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

    function startAuthFloatingMotion(page, gsap, logo, planes, rings, signals, statusCard, formAura) {
        if (page.dataset.floatMotionBound === 'true') return;
        page.dataset.floatMotionBound = 'true';

        if (logo) {
            gsap.to(logo, {
                y: -8,
                rotation: 1.5,
                duration: 2.8,
                repeat: -1,
                yoyo: true,
                ease: 'sine.inOut'
            });
        }

        Array.from(planes).forEach(function (plane, index) {
            gsap.to(plane, {
                x: index % 2 === 0 ? 8 : -10,
                y: index % 2 === 0 ? -12 : 10,
                rotation: index % 2 === 0 ? '+=2.5' : '-=2.5',
                duration: 7.2 + index,
                repeat: -1,
                yoyo: true,
                ease: 'sine.inOut'
            });
        });

        Array.from(rings).forEach(function (ring, index) {
            gsap.to(ring, {
                rotation: index % 2 === 0 ? 10 : -8,
                scale: index % 2 === 0 ? 1.035 : 0.97,
                duration: 9 + index,
                repeat: -1,
                yoyo: true,
                ease: 'sine.inOut'
            });
        });

        Array.from(signals).forEach(function (signal, index) {
            gsap.to(signal, {
                x: index % 2 === 0 ? 26 : -24,
                autoAlpha: index % 2 === 0 ? 0.74 : 0.58,
                duration: 8.5 + index,
                repeat: -1,
                yoyo: true,
                ease: 'sine.inOut'
            });
        });

        if (statusCard) {
            gsap.to(statusCard, {
                y: -7,
                rotationX: 1.2,
                duration: 3.6,
                repeat: -1,
                yoyo: true,
                ease: 'sine.inOut'
            });
        }

        if (formAura) {
            gsap.to(formAura, {
                x: 18,
                y: -6,
                rotation: '+=1.8',
                duration: 7.5,
                repeat: -1,
                yoyo: true,
                ease: 'sine.inOut'
            });
        }
    }

    function initAuthPointerParallax(page, gsap, brandContent, formWrapper, statusCard) {
        if (!brandContent || !formWrapper || page.dataset.pointerMotionBound === 'true') return;
        page.dataset.pointerMotionBound = 'true';

        const brandX = gsap.quickTo(brandContent, 'x', { duration: 0.75, ease: 'power3.out' });
        const brandY = gsap.quickTo(brandContent, 'y', { duration: 0.75, ease: 'power3.out' });
        const brandRotateX = gsap.quickTo(brandContent, 'rotationX', { duration: 0.9, ease: 'power3.out' });
        const brandRotateY = gsap.quickTo(brandContent, 'rotationY', { duration: 0.9, ease: 'power3.out' });
        const formX = gsap.quickTo(formWrapper, 'x', { duration: 0.85, ease: 'power3.out' });
        const formY = gsap.quickTo(formWrapper, 'y', { duration: 0.85, ease: 'power3.out' });
        const formRotateX = gsap.quickTo(formWrapper, 'rotationX', { duration: 0.95, ease: 'power3.out' });
        const formRotateY = gsap.quickTo(formWrapper, 'rotationY', { duration: 0.95, ease: 'power3.out' });
        const statusX = createQuickTo(gsap, statusCard, 'x', 0.85);
        const statusRotateY = createQuickTo(gsap, statusCard, 'rotationY', 0.85);

        page.addEventListener('pointermove', function (event) {
            if (event.pointerType === 'touch') return;
            const rect = page.getBoundingClientRect();
            const x = (event.clientX - rect.left) / rect.width - 0.5;
            const y = (event.clientY - rect.top) / rect.height - 0.5;
            brandX(x * 14);
            brandY(y * 10);
            brandRotateX(y * -4);
            brandRotateY(x * 5);
            formX(x * -8);
            formY(y * -6);
            formRotateX(y * 3);
            formRotateY(x * -4);
            callQuickTo(statusX, x * 10);
            callQuickTo(statusRotateY, x * 4);
        }, { passive: true });

        page.addEventListener('pointerleave', function () {
            brandX(0);
            brandY(0);
            brandRotateX(0);
            brandRotateY(0);
            formX(0);
            formY(0);
            formRotateX(0);
            formRotateY(0);
            callQuickTo(statusX, 0);
            callQuickTo(statusRotateY, 0);
        }, { passive: true });
    }

    function createQuickTo(gsap, target, property, duration) {
        return target ? gsap.quickTo(target, property, { duration: duration, ease: 'power3.out' }) : null;
    }

    function callQuickTo(quickTo, value) {
        if (quickTo) quickTo(value);
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
