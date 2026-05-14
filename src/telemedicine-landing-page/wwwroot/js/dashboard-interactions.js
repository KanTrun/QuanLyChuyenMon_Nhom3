/**
 * Dashboard Interactions - Premium dashboard-specific behaviors
 * Counter animations, heatmap tooltips, calendar navigation,
 * donut hover, sparkline reveal, progress rings, auto-refresh
 */
(function () {
    'use strict';

    // Counter animation on viewport entry
    function initCounterAnimations() {
        var counters = document.querySelectorAll('.counter-animate');
        if (!counters.length) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting && !entry.target.dataset.animated) {
                    entry.target.dataset.animated = 'true';
                    animateCounter(entry.target);
                }
            });
        }, { threshold: 0.3 });

        counters.forEach(function (el) { observer.observe(el); });
    }

    function animateCounter(el) {
        var target = parseFloat(el.dataset.target || el.textContent);
        var suffix = el.dataset.suffix || '';
        var prefix = el.dataset.prefix || '';
        var duration = 1200;
        var start = 0;
        var startTime = null;
        var isFloat = String(target).indexOf('.') !== -1;

        function step(timestamp) {
            if (!startTime) startTime = timestamp;
            var progress = Math.min((timestamp - startTime) / duration, 1);
            // easeOutExpo
            var ease = progress === 1 ? 1 : 1 - Math.pow(2, -10 * progress);
            var current = start + (target - start) * ease;
            el.textContent = prefix + (isFloat ? current.toFixed(1) : Math.floor(current)) + suffix;
            if (progress < 1) {
                requestAnimationFrame(step);
            }
        }
        requestAnimationFrame(step);
    }

    // Heatmap cell tooltip positioning
    function initHeatmapTooltips() {
        var cells = document.querySelectorAll('.heatmap__cell[data-tooltip]');
        cells.forEach(function (cell) {
            cell.addEventListener('mouseenter', function () {
                cell.classList.add('heatmap__cell--active');
            });
            cell.addEventListener('mouseleave', function () {
                cell.classList.remove('heatmap__cell--active');
            });
        });
    }

    // Calendar month navigation
    function initCalendarNavigation() {
        var prevBtn = document.querySelector('.calendar__nav--prev');
        var nextBtn = document.querySelector('.calendar__nav--next');
        var monthLabel = document.querySelector('.calendar__month-label');
        if (!prevBtn || !nextBtn || !monthLabel) return;

        var months = [
            'Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4',
            'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8',
            'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'
        ];
        var now = new Date();
        var currentMonth = now.getMonth();
        var currentYear = now.getFullYear();

        function updateLabel() {
            monthLabel.textContent = months[currentMonth] + ' ' + currentYear;
        }

        prevBtn.addEventListener('click', function () {
            currentMonth--;
            if (currentMonth < 0) { currentMonth = 11; currentYear--; }
            updateLabel();
        });

        nextBtn.addEventListener('click', function () {
            currentMonth++;
            if (currentMonth > 11) { currentMonth = 0; currentYear++; }
            updateLabel();
        });
    }

    // Donut chart segment hover
    function initDonutHover() {
        var segments = document.querySelectorAll('.donut-chart__legend-item');
        var donut = document.querySelector('.donut-chart__ring');
        if (!donut || !segments.length) return;

        segments.forEach(function (seg) {
            seg.addEventListener('mouseenter', function () {
                seg.classList.add('donut-chart__legend-item--active');
                donut.classList.add('donut-chart__ring--hover');
            });
            seg.addEventListener('mouseleave', function () {
                seg.classList.remove('donut-chart__legend-item--active');
                donut.classList.remove('donut-chart__ring--hover');
            });
        });
    }

    // Sparkline SVG path animation (stroke-dashoffset reveal)
    function initSparklineAnimations() {
        var sparklines = document.querySelectorAll('.sparkline__path');
        if (!sparklines.length) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    var path = entry.target;
                    var length = path.getTotalLength ? path.getTotalLength() : 100;
                    path.style.strokeDasharray = length;
                    path.style.strokeDashoffset = length;
                    path.style.transition = 'stroke-dashoffset 1.2s cubic-bezier(0.4, 0, 0.2, 1)';
                    requestAnimationFrame(function () {
                        path.style.strokeDashoffset = '0';
                    });
                    observer.unobserve(path);
                }
            });
        }, { threshold: 0.5 });

        sparklines.forEach(function (el) { observer.observe(el); });
    }

    // Approval item stagger animation on scroll
    function initApprovalStagger() {
        var items = document.querySelectorAll('.approval-queue__item');
        if (!items.length) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    var children = entry.target.parentElement.querySelectorAll('.approval-queue__item');
                    children.forEach(function (child, i) {
                        child.style.transitionDelay = (i * 80) + 'ms';
                        child.classList.add('approval-queue__item--visible');
                    });
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.2 });

        if (items[0]) observer.observe(items[0]);
    }

    // Progress ring animation (SVG stroke-dasharray)
    function initProgressRings() {
        var rings = document.querySelectorAll('.health-ring__progress');
        if (!rings.length) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting && !entry.target.dataset.animated) {
                    entry.target.dataset.animated = 'true';
                    var pct = parseFloat(entry.target.dataset.percent || 0);
                    var circumference = 2 * Math.PI * 40;
                    var offset = circumference - (pct / 100) * circumference;
                    entry.target.style.strokeDasharray = circumference;
                    entry.target.style.strokeDashoffset = circumference;
                    entry.target.style.transition = 'stroke-dashoffset 1.5s cubic-bezier(0.4, 0, 0.2, 1)';
                    requestAnimationFrame(function () {
                        entry.target.style.strokeDashoffset = offset;
                    });
                }
            });
        }, { threshold: 0.3 });

        rings.forEach(function (el) { observer.observe(el); });
    }

    // Area chart point hover tooltip
    function initAreaChartHover() {
        var points = document.querySelectorAll('.area-chart__point');
        points.forEach(function (point) {
            point.addEventListener('mouseenter', function () {
                point.classList.add('area-chart__point--active');
            });
            point.addEventListener('mouseleave', function () {
                point.classList.remove('area-chart__point--active');
            });
        });
    }

    // Auto-refresh indicator pulse every 30s
    function initAutoRefreshPulse() {
        var indicator = document.querySelector('.dashboard__refresh-indicator');
        if (!indicator) return;

        setInterval(function () {
            indicator.classList.add('dashboard__refresh-indicator--pulse');
            setTimeout(function () {
                indicator.classList.remove('dashboard__refresh-indicator--pulse');
            }, 1000);
        }, 30000);
    }

    // Initialize all on DOM ready
    function init() {
        initCounterAnimations();
        initHeatmapTooltips();
        initCalendarNavigation();
        initDonutHover();
        initSparklineAnimations();
        initApprovalStagger();
        initProgressRings();
        initAreaChartHover();
        initAutoRefreshPulse();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
