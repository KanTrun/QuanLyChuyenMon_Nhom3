// QLCM Pro admin shell helpers. Loaded as a regular <script> from App.razor and
// exposes the global namespace `window.qlcmShell` used by the .NET components via
// IJSRuntime invocations. Keep everything side-effect-free at load time so the
// Blazor circuit can attach listeners on demand.
(function (root) {
    'use strict';

    var THEME_KEY = 'qlcm.theme';

    var state = {
        nextHandleId: 1,
        handles: new Map(),
    };

    function getDigit(event) {
        // Match Alt+0..6 across both top-row digits and the numpad.
        if (!event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
            return -1;
        }
        if (event.key && event.key.length === 1 && event.key >= '0' && event.key <= '6') {
            return parseInt(event.key, 10);
        }
        if (typeof event.code === 'string') {
            var match = event.code.match(/^(?:Digit|Numpad)([0-6])$/);
            if (match) {
                return parseInt(match[1], 10);
            }
        }
        return -1;
    }

    function isPaletteToggle(event) {
        var key = (event.key || '').toLowerCase();
        return key === 'k' && (event.ctrlKey || event.metaKey) && !event.altKey;
    }

    function isEscape(event) {
        return event.key === 'Escape' || event.key === 'Esc';
    }

    function registerHotkeys(dotnetRef) {
        if (!dotnetRef) {
            return -1;
        }
        var handleId = state.nextHandleId++;
        var listener = function (event) {
            try {
                if (isPaletteToggle(event)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync('OnTogglePalette');
                    return;
                }
                if (isEscape(event)) {
                    dotnetRef.invokeMethodAsync('OnEscape');
                    return;
                }
                var digit = getDigit(event);
                if (digit >= 0) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync('OnNavigateHotkey', digit);
                }
            } catch (err) {
                // Swallow interop errors so the keyboard handler never bubbles to the page.
                console && console.warn && console.warn('qlcmShell hotkey error:', err);
            }
        };
        document.addEventListener('keydown', listener, true);
        state.handles.set(handleId, { listener: listener });
        return handleId;
    }

    function unregisterHotkeys(handleId) {
        var entry = state.handles.get(handleId);
        if (!entry) {
            return;
        }
        document.removeEventListener('keydown', entry.listener, true);
        state.handles.delete(handleId);
    }

    function getThemePreference() {
        try {
            var stored = window.localStorage.getItem(THEME_KEY);
            if (stored === 'light' || stored === 'dark') {
                return stored;
            }
        } catch (_) { /* localStorage may be blocked */ }
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    function setTheme(theme) {
        var next = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', next);
        try {
            window.localStorage.setItem(THEME_KEY, next);
        } catch (_) { /* ignore */ }
        return next;
    }

    function toggleTheme() {
        var current = document.documentElement.getAttribute('data-theme') || getThemePreference();
        return setTheme(current === 'dark' ? 'light' : 'dark');
    }

    function enterFullscreen() {
        var el = document.documentElement;
        if (el.requestFullscreen) {
            return el.requestFullscreen();
        }
        return Promise.resolve();
    }

    function exitFullscreen() {
        if (document.exitFullscreen && document.fullscreenElement) {
            return document.exitFullscreen();
        }
        return Promise.resolve();
    }

    function isFullscreen() {
        return Boolean(document.fullscreenElement);
    }

    function focusElement(selector) {
        if (!selector) return false;
        var el = document.querySelector(selector);
        if (el && typeof el.focus === 'function') {
            el.focus();
            if (typeof el.select === 'function') {
                try { el.select(); } catch (_) { /* ignore */ }
            }
            return true;
        }
        return false;
    }

    function lockBodyScroll(lock) {
        var html = document.documentElement;
        if (lock) {
            html.style.overflow = 'hidden';
        } else {
            html.style.overflow = '';
        }
    }

    var outsideClickHandlers = new Map();

    function registerOutsideClick(elementId, dotnetRef, methodName) {
        if (!elementId || !dotnetRef) return -1;
        var handleId = state.nextHandleId++;
        var listener = function (event) {
            var el = document.getElementById(elementId);
            if (!el) return;
            if (el.contains(event.target)) return;
            try {
                dotnetRef.invokeMethodAsync(methodName || 'OnOutsideClick');
            } catch (_) { /* ignore */ }
        };
        // Defer one tick so the click that opened the panel doesn't immediately close it.
        window.setTimeout(function () {
            document.addEventListener('click', listener, true);
        }, 0);
        outsideClickHandlers.set(handleId, listener);
        return handleId;
    }

    function unregisterOutsideClick(handleId) {
        var listener = outsideClickHandlers.get(handleId);
        if (listener) {
            document.removeEventListener('click', listener, true);
            outsideClickHandlers.delete(handleId);
        }
    }

    // Apply the saved/preferred theme as early as possible so the first paint matches.
    try {
        var initial = getThemePreference();
        document.documentElement.setAttribute('data-theme', initial);
    } catch (_) { /* ignore */ }

    root.qlcmShell = {
        registerHotkeys: registerHotkeys,
        unregisterHotkeys: unregisterHotkeys,
        getThemePreference: getThemePreference,
        setTheme: setTheme,
        toggleTheme: toggleTheme,
        enterFullscreen: enterFullscreen,
        exitFullscreen: exitFullscreen,
        isFullscreen: isFullscreen,
        focusElement: focusElement,
        lockBodyScroll: lockBodyScroll,
        registerOutsideClick: registerOutsideClick,
        unregisterOutsideClick: unregisterOutsideClick,
    };
})(window);
