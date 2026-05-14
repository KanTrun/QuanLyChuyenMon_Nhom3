// ═══════════════════════════════════════════════════════════════════
// ANIMATION SYSTEM — World-Class Premium Healthcare
// Modular IIFE with: scroll reveal, page transitions, magnetic cursor,
// 3D tilt, typewriter, counters, scroll progress, dark mode, toasts,
// skeleton auto-hide, parallax layers, ripple effects, keyboard shortcuts
// ═══════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ── Core State ──
    var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var root = document.documentElement;

    // ========================================================================
    // SCROLL REVEAL - Enhanced IntersectionObserver with configurable options
    // ========================================================================
    function initScrollReveal() {
        var revealSelectors = '.reveal, .reveal-left, .reveal-right, .reveal-scale, .gold-line, .stagger-children, .card-stagger-enter';
        var elements = document.querySelectorAll(revealSelectors);
        if (!elements.length) return;

        if (reducedMotion) {
            elements.forEach(function (el) { el.classList.add('is-visible'); });
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    // Stagger delay for children within stagger-children containers
                    var parent = entry.target.closest('.stagger-children');
                    if (parent && parent !== entry.target) {
                        var siblings = Array.prototype.slice.call(parent.children);
                        var index = siblings.indexOf(entry.target);
                        entry.target.style.transitionDelay = (index * 80) + 'ms';
                    }
                    entry.target.classList.add('is-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.12,
            rootMargin: '0px 0px -80px 0px'
        });

        elements.forEach(function (el) { observer.observe(el); });
    }

    // ========================================================================
    // PAGE TRANSITION SYSTEM
    // Intercept navigation for cinematic page transitions
    // ========================================================================
    function initPageTransitions() {
        if (reducedMotion) return;

        var mainContent = document.querySelector('main') || document.querySelector('.page-body') || document.body;

        // Add entrance animation to current page on initial load
        if (mainContent && !mainContent.classList.contains('page-enter')) {
            mainContent.classList.add('page-enter');
        }

        // NOTE: Page-exit animation removed intentionally.
        // Blazor enhanced navigation (SSR) replaces DOM in-place via fetch+diff,
        // which conflicts with exit animations. Only page-enter on initial load is safe.
    }

    // ========================================================================
    // MAGNETIC CURSOR EFFECT
    // Elements with .magnetic-hover translate toward mouse with spring
    // ========================================================================
    function initMagneticHover() {
        if (reducedMotion) return;

        var magnetics = document.querySelectorAll('.magnetic-hover');
        if (!magnetics.length) return;

        magnetics.forEach(function (el) {
            var strength = parseFloat(el.getAttribute('data-magnetic-strength')) || 0.15;

            el.addEventListener('mousemove', function (e) {
                var rect = el.getBoundingClientRect();
                var centerX = rect.left + rect.width / 2;
                var centerY = rect.top + rect.height / 2;
                var deltaX = (e.clientX - centerX) * strength;
                var deltaY = (e.clientY - centerY) * strength;
                el.style.transform = 'translate(' + deltaX + 'px, ' + deltaY + 'px)';
                el.style.transition = 'transform 0.15s ease-out';
            });

            el.addEventListener('mouseleave', function () {
                el.style.transform = 'translate(0px, 0px)';
                el.style.transition = 'transform 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
            });
        });
    }

    // ========================================================================
    // 3D TILT EFFECT
    // Perspective transform on mousemove, reset on mouseleave
    // ========================================================================
    function initTiltCards() {
        if (reducedMotion) return;

        var cards = document.querySelectorAll('.tilt-card');
        if (!cards.length) return;

        cards.forEach(function (card) {
            var maxTilt = parseFloat(card.getAttribute('data-tilt-max')) || 8;

            card.addEventListener('mousemove', function (e) {
                var rect = card.getBoundingClientRect();
                var x = (e.clientX - rect.left) / rect.width;
                var y = (e.clientY - rect.top) / rect.height;
                var tiltX = (0.5 - y) * maxTilt;
                var tiltY = (x - 0.5) * maxTilt;
                card.style.transform = 'perspective(800px) rotateX(' + tiltX + 'deg) rotateY(' + tiltY + 'deg) scale3d(1.02, 1.02, 1.02)';
            });

            card.addEventListener('mouseleave', function () {
                card.style.transform = 'perspective(800px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)';
                card.style.transition = 'transform 0.5s cubic-bezier(0.16, 1, 0.3, 1)';
                setTimeout(function () {
                    card.style.transition = '';
                }, 500);
            });

            card.addEventListener('mouseenter', function () {
                card.style.transition = 'transform 0.15s ease-out';
            });
        });
    }

    // ========================================================================
    // TYPEWRITER EFFECT
    // Character-by-character reveal with cursor blink
    // ========================================================================
    function initTypewriter() {
        if (reducedMotion) {
            document.querySelectorAll('.typewriter').forEach(function (el) {
                el.style.width = 'auto';
                el.style.borderRight = 'none';
                el.classList.add('typewriter--done');
            });
            return;
        }

        var typewriters = document.querySelectorAll('.typewriter');
        if (!typewriters.length) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    startTypewriter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.5 });

        typewriters.forEach(function (el) {
            // Store original text and clear it
            var text = el.getAttribute('data-typewriter-text') || el.textContent;
            el.setAttribute('data-typewriter-text', text);
            el.textContent = '';
            el.style.width = 'auto';
            el.classList.add('typewriter--active');
            observer.observe(el);
        });
    }

    function startTypewriter(el) {
        var text = el.getAttribute('data-typewriter-text') || '';
        var speed = parseInt(el.getAttribute('data-typewriter-speed'), 10) || 50;
        var index = 0;

        function type() {
            if (index < text.length) {
                el.textContent += text.charAt(index);
                index++;
                setTimeout(type, speed);
            } else {
                el.classList.remove('typewriter--active');
                el.classList.add('typewriter--done');
            }
        }

        type();
    }

    // ========================================================================
    // COUNTER ANIMATION
    // Animated counting with easeOutExpo for .counter-animate elements
    // ========================================================================
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
        }, { threshold: 0.3 });

        counters.forEach(function (el) { observer.observe(el); });
    }

    function animateCounter(el) {
        var target = parseFloat(el.getAttribute('data-counter-target') || el.textContent);
        if (isNaN(target)) return;

        var duration = parseInt(el.getAttribute('data-counter-duration'), 10) || 2000;
        var suffix = el.getAttribute('data-counter-suffix') || '';
        var prefix = el.getAttribute('data-counter-prefix') || '';
        var decimals = parseInt(el.getAttribute('data-counter-decimals'), 10) || 0;
        var startTime = null;
        var startVal = 0;

        function easeOutExpo(t) {
            return t === 1 ? 1 : 1 - Math.pow(2, -10 * t);
        }

        function step(timestamp) {
            if (!startTime) startTime = timestamp;
            var progress = Math.min((timestamp - startTime) / duration, 1);
            var easedProgress = easeOutExpo(progress);
            var current = startVal + (target - startVal) * easedProgress;

            if (decimals > 0) {
                el.textContent = prefix + current.toFixed(decimals) + suffix;
            } else {
                el.textContent = prefix + Math.round(current) + suffix;
            }

            if (progress < 1) {
                window.requestAnimationFrame(step);
            } else {
                el.textContent = prefix + (decimals > 0 ? target.toFixed(decimals) : target) + suffix;
            }
        }

        el.textContent = prefix + '0' + suffix;
        window.requestAnimationFrame(step);
    }

    // ========================================================================
    // SCROLL PROGRESS INDICATOR
    // Updates CSS variable --scroll-progress on :root (0 to 1)
    // ========================================================================
    function initScrollProgress() {
        var scrollProgressBar = document.querySelector('.scroll-progress');
        var ticking = false;

        function updateScrollProgress() {
            var scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            var docHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
            var progress = docHeight > 0 ? scrollTop / docHeight : 0;
            root.style.setProperty('--scroll-progress', progress.toFixed(4));

            if (scrollProgressBar) {
                scrollProgressBar.style.transform = 'scaleX(' + progress.toFixed(4) + ')';
            }
            ticking = false;
        }

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(updateScrollProgress);
                ticking = true;
            }
        }, { passive: true });

        // Initial update
        updateScrollProgress();
    }

    // ========================================================================
    // DARK MODE TOGGLE
    // Reads/writes data-theme attribute on <html>, persists to localStorage
    // ========================================================================
    function initDarkMode() {
        // Load saved preference
        var savedTheme = localStorage.getItem('qlcm-theme');
        var systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

        if (savedTheme) {
            root.setAttribute('data-theme', savedTheme);
        } else if (systemPrefersDark) {
            root.setAttribute('data-theme', 'dark');
        }

        // Expose global toggle function
        window.toggleDarkMode = function () {
            var current = root.getAttribute('data-theme');
            var next = current === 'dark' ? 'light' : 'dark';
            root.setAttribute('data-theme', next);
            localStorage.setItem('qlcm-theme', next);
            return next;
        };

        // Listen for system preference changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
            if (!localStorage.getItem('qlcm-theme')) {
                root.setAttribute('data-theme', e.matches ? 'dark' : 'light');
            }
        });
    }

    // ========================================================================
    // TOAST NOTIFICATION SYSTEM
    // Creates, animates, and auto-removes toast DOM elements
    // ========================================================================
    function initToastSystem() {
        // Create toast container if it doesn't exist
        var container = document.querySelector('.toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container';
            container.setAttribute('aria-live', 'polite');
            container.setAttribute('aria-atomic', 'false');
            document.body.appendChild(container);
        }

        /**
         * Show a toast notification
         * @param {string} message - Toast message text
         * @param {string} type - Toast type: 'success', 'error', 'warning', 'info'
         * @param {number} duration - Auto-dismiss duration in ms (default: 4000)
         */
        window.showToast = function (message, type, duration) {
            type = type || 'info';
            duration = duration || 4000;

            var toast = document.createElement('div');
            toast.className = 'toast toast--' + type;
            toast.setAttribute('role', 'alert');
            toast.textContent = message;

            container.appendChild(toast);

            // Auto dismiss
            var dismissTimeout = setTimeout(function () {
                dismissToast(toast);
            }, duration);

            // Click to dismiss
            toast.addEventListener('click', function () {
                clearTimeout(dismissTimeout);
                dismissToast(toast);
            });

            return toast;
        };

        function dismissToast(toast) {
            toast.classList.add('toast--exiting');
            setTimeout(function () {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }
    }

    // ========================================================================
    // SKELETON SCREEN AUTO-HIDE
    // Removes .skeleton class when content loads (MutationObserver)
    // ========================================================================
    function initSkeletonAutoHide() {
        var skeletons = document.querySelectorAll('.skeleton');
        if (!skeletons.length) return;

        // Use MutationObserver to detect when content is loaded into skeleton containers
        var observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                    var target = mutation.target;
                    if (target.classList && target.classList.contains('skeleton')) {
                        // Content was added, remove skeleton state
                        target.classList.remove('skeleton');
                        target.classList.add('skeleton--loaded');
                    }
                }
            });
        });

        skeletons.forEach(function (el) {
            observer.observe(el, { childList: true, subtree: true });
        });

        // Also remove skeletons after a maximum wait time (fallback)
        setTimeout(function () {
            document.querySelectorAll('.skeleton').forEach(function (el) {
                el.classList.remove('skeleton');
                el.classList.add('skeleton--loaded');
            });
        }, 5000);
    }

    // ========================================================================
    // PARALLAX DEPTH LAYERS
    // Multi-speed parallax based on data-parallax-speed attribute
    // ========================================================================
    function initParallax() {
        if (reducedMotion) return;

        var parallaxElements = document.querySelectorAll('[data-parallax-speed]');
        if (!parallaxElements.length) return;

        var ticking = false;

        function updateParallax() {
            var scrollY = window.pageYOffset;
            var windowHeight = window.innerHeight;

            parallaxElements.forEach(function (el) {
                var speed = parseFloat(el.getAttribute('data-parallax-speed')) || 0.5;
                var rect = el.getBoundingClientRect();
                var elementCenter = rect.top + rect.height / 2;
                var viewportCenter = windowHeight / 2;
                var distance = elementCenter - viewportCenter;
                var offset = distance * speed * -0.3;

                el.style.transform = 'translate3d(0, ' + offset + 'px, 0)';
            });
            ticking = false;
        }

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(updateParallax);
                ticking = true;
            }
        }, { passive: true });

        // Initial position
        updateParallax();
    }

    // ========================================================================
    // CLICK RIPPLE EFFECT
    // Material-design-like ripple on .btn-primary, .btn-secondary, .action-btn
    // ========================================================================
    function initRippleEffect() {
        if (reducedMotion) return;

        var rippleSelectors = '.btn-primary, .btn-secondary, .action-btn, .ripple-effect';

        document.addEventListener('click', function (e) {
            var button = e.target.closest(rippleSelectors);
            if (!button) return;

            // Ensure position relative for ripple containment
            var position = window.getComputedStyle(button).position;
            if (position === 'static') {
                button.style.position = 'relative';
            }
            button.style.overflow = 'hidden';

            var rect = button.getBoundingClientRect();
            var size = Math.max(rect.width, rect.height) * 2;
            var x = e.clientX - rect.left - size / 2;
            var y = e.clientY - rect.top - size / 2;

            var ripple = document.createElement('span');
            ripple.className = 'ripple';
            ripple.style.width = size + 'px';
            ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';

            button.appendChild(ripple);

            // Remove ripple after animation completes
            setTimeout(function () {
                if (ripple.parentNode) {
                    ripple.parentNode.removeChild(ripple);
                }
            }, 600);
        });
    }

    // ========================================================================
    // KEYBOARD SHORTCUTS
    // Ctrl+K toggles command palette, Escape closes modals/dropdowns
    // ========================================================================
    function initKeyboardShortcuts() {
        document.addEventListener('keydown', function (e) {
            // Ctrl+K or Cmd+K - Toggle command palette
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                var palette = document.querySelector('.command-palette');
                if (palette) {
                    palette.classList.toggle('is-open');
                    var input = palette.querySelector('input');
                    if (input && palette.classList.contains('is-open')) {
                        input.focus();
                    }
                }
            }

            // Escape - Close modals, dropdowns, command palette
            if (e.key === 'Escape') {
                // Close command palette
                var palette = document.querySelector('.command-palette.is-open');
                if (palette) {
                    palette.classList.remove('is-open');
                    return;
                }

                // Close modal backdrops
                var modal = document.querySelector('.modal-backdrop');
                if (modal) {
                    modal.classList.add('modal--closing');
                    setTimeout(function () {
                        if (modal.parentNode) {
                            modal.parentNode.removeChild(modal);
                        }
                    }, 300);
                    return;
                }

                // Close open dropdowns
                var openDropdowns = document.querySelectorAll('.dropdown.is-open, [data-dropdown].is-open');
                openDropdowns.forEach(function (dd) {
                    dd.classList.remove('is-open');
                });
            }
        });
    }

    // ========================================================================
    // SMOOTH SCROLL WITH PASSIVE LISTENERS
    // ========================================================================
    function initSmoothScroll() {
        document.addEventListener('click', function (e) {
            var link = e.target.closest('a[href^="#"]');
            if (!link) return;

            var targetId = link.getAttribute('href');
            if (!targetId || targetId === '#') return;

            try {
                var target = document.querySelector(targetId);
                if (target) {
                    e.preventDefault();
                    var headerOffset = 80;
                    var elementPosition = target.getBoundingClientRect().top;
                    var offsetPosition = elementPosition + window.pageYOffset - headerOffset;

                    window.scrollTo({
                        top: offsetPosition,
                        behavior: reducedMotion ? 'auto' : 'smooth'
                    });
                }
            } catch (err) {
                // Invalid selector, ignore
            }
        });
    }

    // ========================================================================
    // STAGGER CHILDREN AUTO DETECTION
    // ========================================================================
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

    // ========================================================================
    // STICKY HEADER SCROLL EFFECT
    // ========================================================================
    function initStickyHeader() {
        var header = document.getElementById('site-header') || document.querySelector('.app-header');
        if (!header) return;

        var scrollThreshold = 60;
        var ticking = false;

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(function () {
                    if (window.pageYOffset > scrollThreshold) {
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

    // ========================================================================
    // PASSWORD STRENGTH METER
    // Calculates strength from [data-password-strength] inputs and updates
    // adjacent .password-strength__segment elements with active/level classes
    // ========================================================================
    function initPasswordStrength() {
        var inputs = document.querySelectorAll('[data-password-strength]');
        if (!inputs.length) return;

        inputs.forEach(function (input) {
            var formGroup = input.closest('.form-group');
            if (!formGroup) return;
            var meter = formGroup.querySelector('.password-strength');
            if (!meter) return;
            var segments = meter.querySelectorAll('.password-strength__segment');
            var textEl = meter.querySelector('.password-strength__text');

            input.addEventListener('input', function () {
                var val = input.value;
                var score = 0;
                if (val.length >= 6) score++;
                if (val.length >= 10) score++;
                if (/[A-Z]/.test(val) && /[a-z]/.test(val)) score++;
                if (/[0-9]/.test(val)) score++;
                if (/[^A-Za-z0-9]/.test(val)) score++;
                score = Math.min(score, 4);

                segments.forEach(function (seg, i) {
                    seg.classList.toggle('password-strength__segment--active', i < score);
                    seg.classList.toggle('password-strength__segment--weak', score <= 1 && i < score);
                    seg.classList.toggle('password-strength__segment--fair', score === 2 && i < score);
                    seg.classList.toggle('password-strength__segment--good', score === 3 && i < score);
                    seg.classList.toggle('password-strength__segment--strong', score >= 4 && i < score);
                });

                if (textEl) {
                    var labels = ['Nhap mat khau de kiem tra do manh', 'Yeu', 'Trung binh', 'Manh', 'Rat manh'];
                    textEl.textContent = labels[score] || labels[0];
                }
            });
        });
    }

    // ========================================================================
    // INITIALIZATION
    // ========================================================================
    function init() {
        // Core interactions
        initScrollReveal();
        initPageTransitions();
        initMagneticHover();
        initTiltCards();
        initTypewriter();
        initCounterAnimation();
        initScrollProgress();
        initStaggerChildren();
        initStickyHeader();
        initSmoothScroll();

        // System features
        initDarkMode();
        initToastSystem();
        initSkeletonAutoHide();

        // Visual effects
        initParallax();
        initRippleEffect();

        // Keyboard
        initKeyboardShortcuts();

        // Forms
        initPasswordStrength();
    }

    // Wait for DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Re-initialize on Blazor enhanced navigation
    if (typeof Blazor !== 'undefined') {
        document.addEventListener('enhanced:load', function () {
            init();
        });
    }
})();
