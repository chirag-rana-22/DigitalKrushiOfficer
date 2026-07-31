// site.js - Digital Krushi Officer

// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-banner').forEach(function (alert) {
        setTimeout(function () {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(function () { alert.style.display = 'none'; }, 500);
        }, 5000);
    });

    // Active nav link highlighting
    var path = window.location.pathname;
    document.querySelectorAll('.sidebar-nav a').forEach(function (link) {
        if (link.getAttribute('href') === path) {
            link.classList.add('active');
        }
    });

    // Smooth table row hover
    document.querySelectorAll('.data-table tbody tr').forEach(function (row) {
        row.style.transition = 'background 0.2s ease';
    });

    // Tooltip for truncated text
    document.querySelectorAll('[title]').forEach(function (el) {
        el.style.cursor = 'help';
    });
});

// Close modal on outside click
window.addEventListener('click', function (e) {
    var modals = document.querySelectorAll('[id$="Modal"]');
    modals.forEach(function (modal) {
        if (e.target === modal) {
            modal.style.display = 'none';
        }
    });
});

// CSRF token helper
function getAntiForgeryToken() {
    var el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

// Number animation
function animateNumber(el, target, duration) {
    var start = 0;
    var step = target / (duration / 16);
    var timer = setInterval(function () {
        start += step;
        if (start >= target) { el.textContent = target; clearInterval(timer); }
        else { el.textContent = Math.floor(start); }
    }, 16);
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.stat-value').forEach(function (el) {
        var val = parseInt(el.textContent);
        if (!isNaN(val) && val > 0) {
            el.textContent = '0';
            animateNumber(el, val, 800);
        }
    });
});
