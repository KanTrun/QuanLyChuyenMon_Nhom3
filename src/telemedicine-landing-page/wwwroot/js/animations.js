// ═══════════════════════════════════════════════════
// ANIMATION SYSTEM — Premium Healthcare
// IntersectionObserver reveal, parallax, counter,
// magnetic hover, stagger detection, sticky header
// ═══════════════════════════════════════════════════

(function () {
    'use strict';

    var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ── Scroll Reveal Observer ──
    var revealSelectors = '.reveal, .reveal-left, .reveal-right, .reveal-scale, .gold-line, .stagger-children';

    function initScrollReveal() {
        var elements = document.querySelectorAll(revealSelectors);
        if (!elements.length) return;

        if (reducedMotion) {
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
            threshold: 0.1,
            rootMargin: '0px 0px -60px 0px'
        });

        elements.forEach(function (el) { observer.observe(el); });
    }

    // ── Parallax Scroll Tracking ──
    function initParallax() {
        if (reducedMotion) return;

        var parallaxElements = document.querySelectorAll('[data-parallax-speed]');
        if (!parallaxElements.length) return;

        var ticking = false;

        function updateParallax() {
            var scrollY = window.scrollY;
            parallaxElements.forEach(function (el) {
                var speed = parseFloat(el.getAttribute('data-parallax-speed')) || 0.5;
                var rect = el.getBoundingClientRect();
                var elementTop = rect.top + scrollY;
                var offset = (scrollY - elementTop) * speed;
                var baseTransform = el.getAttribute('data-base-transform') || '';
                if (baseTransform) {
                    el.style.transform = baseTransform + ' translateY(' + offset + 'px)';
                } else {
                    el.style.transform = 'translateY(' + offset + 'px)';
                }
            });
            ticking = false;
        }

        // Cache existing CSS transforms as base transforms
        parallaxElements.forEach(function (el) {
            var computed = window.getComputedStyle(el).transform;
            if (computed && computed !== 'none') {
                el.setAttribute('data-base-transform', computed);
            }
        });

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(updateParallax);
                ticking = true;
            }
        }, { passive: true });
    }

    // ── Counter Animation ──
    function initCounterAnimation() {
        if (reducedMotion) return;

        var counters = document.querySelectorAll('.counter-animate');
        if (!counters.length) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.3
        });

        counters.forEach(function (el) { observer.observe(el); });
    }

    function animateCounter(el) {
        var target = parseInt(el.innerText, 10);
        if (isNaN(target)) return;

        var duration = 1800;
        var startTime = null;
        var startVal = 0;

        function step(timestamp) {
            if (!startTime) startTime = timestamp;
            var progress = Math.min((timestamp - startTime) / duration, 1);
            // Ease out cubic
            var easedProgress = 1 - Math.pow(1 - progress, 3);
            var current = Math.round(startVal + (target - startVal) * easedProgress);
            el.innerText = current;
            if (progress < 1) {
                window.requestAnimationFrame(step);
            } else {
                el.innerText = target;
            }
        }

        el.innerText = '0';
        window.requestAnimationFrame(step);
    }

    // ── Magnetic Hover Effect ──
    function initMagneticHover() {
        if (reducedMotion) return;

        var magnetics = document.querySelectorAll('.magnetic-hover');
        if (!magnetics.length) return;

        magnetics.forEach(function (el) {
            el.addEventListener('mousemove', function (e) {
                var rect = el.getBoundingClientRect();
                var x = e.clientX - rect.left - rect.width / 2;
                var y = e.clientY - rect.top - rect.height / 2;
                var strength = 0.15;
                el.style.transform = 'translate(' + (x * strength) + 'px, ' + (y * strength) + 'px)';
            });

            el.addEventListener('mouseleave', function () {
                el.style.transform = 'translate(0px, 0px)';
                el.style.transition = 'transform 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
                setTimeout(function () {
                    el.style.transition = '';
                }, 400);
            });
        });
    }

    // ── Stagger Children Auto Detection ──
    function initStaggerChildren() {
        if (reducedMotion) return;

        var parents = document.querySelectorAll('.stagger-children');
        if (!parents.length) return;

        parents.forEach(function (parent) {
            var children = parent.children;
            for (var i = 0; i < children.length; i++) {
                children[i].style.transitionDelay = (i * 80) + 'ms';
            }
        });
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
        initParallax();
        initCounterAnimation();
        initMagneticHover();
        initStaggerChildren();
        initStickyHeader();
        initSmoothScroll();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
