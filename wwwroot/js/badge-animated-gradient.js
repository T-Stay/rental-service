// badge-animated-gradient.js
// Animated gradient badge: seamless loop chuẩn, màu chữ phù hợp từng gói
(function() {
    function animateBadge(badge, gradient, textColor) {
        let pos = 0;
        let lastTimestamp = null;
        const speed = 0.03; // càng nhỏ càng chậm, càng mượt
        const bgSize = 200; // background-size: 200% 100%
        function step(ts) {
            if (!lastTimestamp) lastTimestamp = ts;
            const delta = ts - lastTimestamp;
            lastTimestamp = ts;
            pos += delta * speed;
            if (pos > 100) pos -= 100;
            badge.style.setProperty('background', gradient, 'important');
            badge.style.setProperty('background-size', bgSize + '% 100%', 'important');
            badge.style.setProperty('background-position', pos + '% 50%', 'important');
            badge.style.setProperty('background-repeat', 'repeat', 'important');
            badge.style.setProperty('color', textColor, 'important');
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }
    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('.badge.animated-gradient').forEach(function(badge) {
            badge.classList.remove('bg-info', 'bg-opacity-10', 'bg-primary', 'bg-success', 'bg-warning', 'bg-danger', 'bg-secondary', 'bg-light', 'bg-dark');
            if (badge.classList.contains('bg-gradient-diamond')) {
                animateBadge(badge, 'linear-gradient(90deg, #00bcd4, #e0f7fa, #00bcd4, #e3f0ff, #00bcd4)', '#006b8f');
            } else if (badge.classList.contains('bg-gradient-gold')) {
                animateBadge(badge, 'linear-gradient(90deg, #ffd700, #fffbe6, #ffd700, #fffde7, #ffd700)', '#7c5c00');
            } else if (badge.classList.contains('bg-gradient-silver')) {
                animateBadge(badge, 'linear-gradient(90deg, #b0c4de, #e3eafc, #b0c4de, #f8fafc, #b0c4de)', '#444a5a');
            } else if (badge.classList.contains('bg-gradient-bronze')) {
                animateBadge(badge, 'linear-gradient(90deg, #cd7f32, #f5e6d3, #cd7f32, #fff3e0, #cd7f32)', '#6b3e13');
            }
        });
    });
})();
