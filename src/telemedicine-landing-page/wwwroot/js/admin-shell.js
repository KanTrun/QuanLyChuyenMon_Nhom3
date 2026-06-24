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
        signaturePads: new Map(),
        scrollTrackers: new WeakMap(),
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

    function isChatbotToggle(event) {
        if (event.altKey || event.shiftKey) return false;
        if (!event.ctrlKey && !event.metaKey) return false;
        if (event.key === '/' || event.code === 'Slash') return true;
        return false;
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
                if (isChatbotToggle(event)) {
                    event.preventDefault();
                    dotnetRef.invokeMethodAsync('OnToggleChatbot');
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

    function setMotionPreference(enabled) {
        var next = enabled === false ? 'off' : 'on';
        document.documentElement.setAttribute('data-motion', next);
        try {
            window.localStorage.setItem('qlcm.motion', next);
        } catch (_) { /* ignore */ }
        return next;
    }

    function getMotionPreference() {
        try {
            var stored = window.localStorage.getItem('qlcm.motion');
            if (stored === 'off') return false;
            if (stored === 'on') return true;
        } catch (_) { /* ignore */ }
        return !(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
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

    function downloadFile(filename, content, contentType) {
        try {
            var blob = new Blob([content], { type: contentType || 'application/octet-stream' });
            var url = window.URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = filename || 'qlcm-export';
            a.style.display = 'none';
            document.body.appendChild(a);
            a.click();
            window.setTimeout(function () {
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
            }, 0);
            return true;
        } catch (err) {
            console && console.warn && console.warn('qlcmShell downloadFile error:', err);
            return false;
        }
    }

    function downloadCsv(filename, content) {
        return downloadFile(filename || 'bao-cao.csv', content, 'text/csv;charset=utf-8');
    }

    function openPrintableHtml(filename, content) {
        try {
            var blob = new Blob([content], { type: 'text/html;charset=utf-8' });
            var url = window.URL.createObjectURL(blob);
            var preview = window.open(url, '_blank');
            if (!preview) {
                window.URL.revokeObjectURL(url);
                return downloadFile(filename || 'quy-trinh.html', content, 'text/html;charset=utf-8');
            }
            try { preview.opener = null; } catch (_) { /* ignore */ }
            window.setTimeout(function () { window.URL.revokeObjectURL(url); }, 120000);
            return true;
        } catch (err) {
            console && console.warn && console.warn('qlcmShell openPrintableHtml error:', err);
            return false;
        }
    }

    async function downloadProcedureAttachment(attachmentId, filename) {
        try {
            var token = window.sessionStorage.getItem('qlcm_session');
            if (!token || !attachmentId) return false;
            var response = await fetch('/api/procedure-attachments/' + encodeURIComponent(attachmentId), {
                headers: { 'X-QLCM-Session': token }
            });
            if (!response.ok) return false;
            var blob = await response.blob();
            var url = window.URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = filename || 'procedure-attachment';
            a.style.display = 'none';
            document.body.appendChild(a);
            a.click();
            window.setTimeout(function () {
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
            }, 0);
            return true;
        } catch (err) {
            console && console.warn && console.warn('qlcmShell downloadProcedureAttachment error:', err);
            return false;
        }
    }

    function lockBodyScroll(lock) {
        var html = document.documentElement;
        if (lock) {
            html.style.overflow = 'hidden';
        } else {
            html.style.overflow = '';
        }
    }

    function normalizeScrollThreshold(threshold) {
        return (typeof threshold === 'number' && threshold >= 0) ? threshold : 24;
    }

    function isElementAtBottom(el, threshold) {
        return (el.scrollHeight - el.scrollTop - el.clientHeight) <= normalizeScrollThreshold(threshold);
    }

    function getScrollTracker(el, threshold) {
        var tracker = state.scrollTrackers.get(el);
        if (tracker) {
            tracker.threshold = normalizeScrollThreshold(threshold);
            return tracker;
        }

        tracker = {
            pinned: isElementAtBottom(el, threshold),
            threshold: normalizeScrollThreshold(threshold),
        };
        el.addEventListener('scroll', function () {
            tracker.pinned = isElementAtBottom(el, tracker.threshold);
        }, { passive: true });
        state.scrollTrackers.set(el, tracker);
        return tracker;
    }

    function scrollElementToBottom(el, smooth) {
        try {
            if (smooth && typeof el.scrollTo === 'function') {
                el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
            } else {
                var previousBehavior = el.style.scrollBehavior;
                try {
                    el.style.scrollBehavior = 'auto';
                    el.scrollTop = el.scrollHeight;
                } finally {
                    el.style.scrollBehavior = previousBehavior;
                }
            }
            return true;
        } catch (_) {
            try { el.scrollTop = el.scrollHeight; return true; } catch (__) { return false; }
        }
    }

    function scrollToBottom(selector, smooth) {
        if (!selector) return false;
        var el = document.querySelector(selector);
        if (!el) return false;
        var tracker = getScrollTracker(el);
        tracker.pinned = true;
        return scrollElementToBottom(el, smooth);
    }

    function scrollToBottomIfPinned(selector, threshold, smooth) {
        if (!selector) return false;
        var el = document.querySelector(selector);
        if (!el) return false;
        // Native scroll events update this before the Blazor scroll callback
        // returns, so a manual upward scroll wins against streaming renders.
        var tracker = getScrollTracker(el, threshold);
        if (!tracker.pinned) return false;
        return scrollElementToBottom(el, smooth);
    }

    function isAtBottom(selector, threshold) {
        if (!selector) return true;
        var el = document.querySelector(selector);
        if (!el) return true;
        var tracker = getScrollTracker(el, threshold);
        tracker.pinned = isElementAtBottom(el, threshold);
        return tracker.pinned;
    }

    function autoGrowTextarea(selector, maxRows) {
        if (!selector) return;
        var el = document.querySelector(selector);
        if (!el) return;
        var rows = (typeof maxRows === 'number' && maxRows > 0) ? maxRows : 5;
        var styles = window.getComputedStyle(el);
        var lineHeight = parseFloat(styles.lineHeight);
        if (!lineHeight || isNaN(lineHeight)) {
            var fontSize = parseFloat(styles.fontSize) || 16;
            lineHeight = fontSize * 1.4;
        }
        var paddingTop = parseFloat(styles.paddingTop) || 0;
        var paddingBottom = parseFloat(styles.paddingBottom) || 0;
        var maxHeight = lineHeight * rows + paddingTop + paddingBottom;
        el.style.height = 'auto';
        var next = Math.min(el.scrollHeight, maxHeight);
        el.style.height = next + 'px';
        el.style.overflowY = el.scrollHeight > maxHeight ? 'auto' : 'hidden';
    }

    function getSignaturePenColor() {
        try {
            var styles = window.getComputedStyle(document.documentElement);
            var ink = styles.getPropertyValue('--signature-pad-ink').trim();
            if (ink) {
                return ink;
            }
            ink = styles.getPropertyValue('--color-ink').trim();
            if (ink) {
                return ink;
            }
        } catch (_) { /* ignore */ }
        return '#1f2937';
    }

    function resizeSignatureCanvas(canvas, pad) {
        var ratio = Math.max(window.devicePixelRatio || 1, 1);
        var rect = canvas.getBoundingClientRect();
        var data = pad && typeof pad.toData === 'function' ? pad.toData() : null;
        canvas.width = Math.max(1, Math.floor(rect.width * ratio));
        canvas.height = Math.max(1, Math.floor(rect.height * ratio));
        var ctx = canvas.getContext('2d');
        if (ctx) {
            ctx.scale(ratio, ratio);
        }
        if (pad && data && typeof pad.fromData === 'function') {
            pad.clear();
            pad.fromData(data);
        }
    }

    function createFallbackSignaturePad(canvas) {
        var drawing = false;
        var hasInk = false;
        var ctx = canvas.getContext('2d');
        if (ctx) {
            ctx.lineWidth = 2;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
            ctx.strokeStyle = getSignaturePenColor();
        }

        function point(event) {
            var rect = canvas.getBoundingClientRect();
            var source = event.touches && event.touches.length ? event.touches[0] : event;
            return { x: source.clientX - rect.left, y: source.clientY - rect.top };
        }

        function start(event) {
            if (!ctx) return;
            event.preventDefault();
            drawing = true;
            var p = point(event);
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
        }

        function move(event) {
            if (!drawing || !ctx) return;
            event.preventDefault();
            var p = point(event);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            hasInk = true;
        }

        function end(event) {
            if (!drawing) return;
            event.preventDefault();
            drawing = false;
        }

        canvas.addEventListener('mousedown', start);
        canvas.addEventListener('mousemove', move);
        canvas.addEventListener('mouseup', end);
        canvas.addEventListener('mouseleave', end);
        canvas.addEventListener('touchstart', start, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        canvas.addEventListener('touchend', end, { passive: false });

        return {
            clear: function () {
                if (ctx) ctx.clearRect(0, 0, canvas.width, canvas.height);
                hasInk = false;
            },
            isEmpty: function () { return !hasInk; },
            toDataURL: function () { return canvas.toDataURL('image/png'); },
            dispose: function () {
                canvas.removeEventListener('mousedown', start);
                canvas.removeEventListener('mousemove', move);
                canvas.removeEventListener('mouseup', end);
                canvas.removeEventListener('mouseleave', end);
                canvas.removeEventListener('touchstart', start);
                canvas.removeEventListener('touchmove', move);
                canvas.removeEventListener('touchend', end);
            }
        };
    }

    function initSignaturePad(canvasId) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return false;
        disposeSignaturePad(canvasId);
        var pad = window.SignaturePad
            ? new window.SignaturePad(canvas, {
                backgroundColor: 'rgba(255, 255, 255, 0)',
                penColor: getSignaturePenColor(),
                minWidth: 0.8,
                maxWidth: 2.4,
                throttle: 12
            })
            : createFallbackSignaturePad(canvas);
        resizeSignatureCanvas(canvas, pad);
        var resize = function () { resizeSignatureCanvas(canvas, pad); };
        window.addEventListener('resize', resize);
        state.signaturePads.set(canvasId, { canvas: canvas, pad: pad, resize: resize });
        return true;
    }

    function getSignaturePadDataUrl(canvasId) {
        var entry = state.signaturePads.get(canvasId);
        if (!entry || !entry.pad) return null;
        if (entry.pad.isEmpty() && !canvasHasInk(entry.canvas)) return null;
        return normalizeSignatureDataUrl(entry.canvas);
    }

    function canvasHasInk(canvas) {
        try {
            var ctx = canvas.getContext('2d', { willReadFrequently: true });
            if (!ctx) return false;
            var image = ctx.getImageData(0, 0, canvas.width, canvas.height);
            for (var i = 0; i < image.data.length; i += 4) {
                if (image.data[i + 3] > 12) return true;
            }
        } catch (_) { /* ignore */ }
        return false;
    }

    function normalizeSignatureDataUrl(canvas) {
        try {
            var normalized = document.createElement('canvas');
            normalized.width = canvas.width;
            normalized.height = canvas.height;
            var ctx = normalized.getContext('2d');
            if (!ctx) return canvas.toDataURL('image/png');

            ctx.drawImage(canvas, 0, 0);
            var image = ctx.getImageData(0, 0, normalized.width, normalized.height);
            for (var i = 0; i < image.data.length; i += 4) {
                if (image.data[i + 3] === 0) continue;
                image.data[i] = 17;
                image.data[i + 1] = 24;
                image.data[i + 2] = 39;
                image.data[i + 3] = Math.max(image.data[i + 3], 190);
            }
            ctx.putImageData(image, 0, 0);
            return normalized.toDataURL('image/png');
        } catch (_) {
            return canvas.toDataURL('image/png');
        }
    }

    function hasSignaturePadInk(canvasId) {
        var entry = state.signaturePads.get(canvasId);
        if (!entry || !entry.pad) return false;
        return !entry.pad.isEmpty();
    }

    function clearSignaturePad(canvasId) {
        var entry = state.signaturePads.get(canvasId);
        if (!entry || !entry.pad) return false;
        entry.pad.clear();
        return true;
    }

    function disposeSignaturePad(canvasId) {
        var entry = state.signaturePads.get(canvasId);
        if (!entry) return;
        window.removeEventListener('resize', entry.resize);
        if (entry.pad && typeof entry.pad.dispose === 'function') {
            entry.pad.dispose();
        }
        state.signaturePads.delete(canvasId);
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

    function setSessionJson(key, json) {
        if (!key || typeof json !== 'string') return false;
        try {
            window.sessionStorage.setItem(key, json);
            return true;
        } catch (_) {
            return false;
        }
    }

    function consumeSessionJson(key) {
        if (!key) return null;
        try {
            var value = window.sessionStorage.getItem(key);
            if (value !== null) {
                window.sessionStorage.removeItem(key);
            }
            return value;
        } catch (_) {
            return null;
        }
    }

    // Apply the saved/preferred theme as early as possible so the first paint matches.
    try {
        var initial = getThemePreference();
        document.documentElement.setAttribute('data-theme', initial);
        setMotionPreference(getMotionPreference());
    } catch (_) { /* ignore */ }

    root.qlcmShell = {
        registerHotkeys: registerHotkeys,
        unregisterHotkeys: unregisterHotkeys,
        getThemePreference: getThemePreference,
        setTheme: setTheme,
        toggleTheme: toggleTheme,
        setMotionPreference: setMotionPreference,
        getMotionPreference: getMotionPreference,
        enterFullscreen: enterFullscreen,
        exitFullscreen: exitFullscreen,
        isFullscreen: isFullscreen,
        focusElement: focusElement,
        lockBodyScroll: lockBodyScroll,
        downloadFile: downloadFile,
        openPrintableHtml: openPrintableHtml,
        downloadCsv: downloadCsv,
        downloadProcedureAttachment: downloadProcedureAttachment,
        registerOutsideClick: registerOutsideClick,
        unregisterOutsideClick: unregisterOutsideClick,
        setSessionJson: setSessionJson,
        consumeSessionJson: consumeSessionJson,
        scrollToBottom: scrollToBottom,
        scrollToBottomIfPinned: scrollToBottomIfPinned,
        isAtBottom: isAtBottom,
        autoGrowTextarea: autoGrowTextarea,
        initSignaturePad: initSignaturePad,
        getSignaturePadDataUrl: getSignaturePadDataUrl,
        hasSignaturePadInk: hasSignaturePadInk,
        clearSignaturePad: clearSignaturePad,
        disposeSignaturePad: disposeSignaturePad,
    };
})(window);
