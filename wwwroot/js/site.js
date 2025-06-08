// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function setLang(lang) {
    // TODO: Implement real language switching (cookie, reload, etc.)
    alert('Switch language to: ' + lang + ' (not implemented)');
}

// Animated Search Bar Logic
$(document).ready(function () {
    const $input = $('#globalSearchInput');
    const $suggestions = $('#searchSuggestions');
    let timer = null;
    let lastQuery = '';

    // Typing animation for placeholder
    const typingSuggestions = [
        'Tìm phòng trọ gần Đại học...',
        'Tìm quảng cáo giá rẻ...',
        'Tìm phòng có điều hòa, máy giặt...',
        'Tìm phòng ở Hà Nội, Hồ Chí Minh...',
        'Tìm phòng cho nữ, phòng ở ghép...',
        'Tìm phòng có ban công, cửa sổ lớn...'
    ];
    let typingIndex = 0;
    let charIndex = 0;
    let typingTimeout;
    function typePlaceholder() {
        const text = typingSuggestions[typingIndex];
        if (charIndex <= text.length) {
            $input.attr('placeholder', text.substring(0, charIndex) + (charIndex % 2 === 0 ? '|' : ''));
            charIndex++;
            typingTimeout = setTimeout(typePlaceholder, 70);
        } else {
            // Wait, then animate erasing
            setTimeout(() => erasePlaceholder(), 1200);
        }
    }
    function erasePlaceholder() {
        const text = typingSuggestions[typingIndex];
        if (charIndex > 0) {
            charIndex--;
            $input.attr('placeholder', text.substring(0, charIndex) + (charIndex % 2 === 0 ? '|' : ''));
            typingTimeout = setTimeout(erasePlaceholder, 35);
        } else {
            typingIndex = (typingIndex + 1) % typingSuggestions.length;
            setTimeout(typePlaceholder, 300);
        }
    }
    $input.on('focus', function () {
        clearTimeout(typingTimeout);
        $input.attr('placeholder', 'Tìm kiếm quảng cáo, phòng trọ...');
    });
    $input.on('blur', function () {
        setTimeout(() => $suggestions.hide(), 150);
        charIndex = 0;
        typingTimeout = setTimeout(typePlaceholder, 500);
    });
    typePlaceholder();

    $input.on('input', function () {
        const query = $(this).val().trim();
        if (query.length < 2) {
            $suggestions.hide();
            return;
        }
        if (query === lastQuery) return;
        lastQuery = query;
        clearTimeout(timer);
        timer = setTimeout(function () {
            // Gọi API lấy gợi ý quảng cáo (AdPost) theo content
            $.get('/AdPosts/SearchSuggestions', { q: query }, function (data) {
                if (data && data.length > 0) {
                    $suggestions.empty();
                    // Highlight match in suggestion
                    const regex = new RegExp('(' + query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'ig');
                    data.forEach(function (item) {
                        let html = item.content.replace(regex, '<span class="search-suggest-highlight">$1</span>');
                        $suggestions.append('<div class="search-bar-suggestion-item" tabindex="0" data-id="' + item.id + '">' + html + '</div>');
                    });
                    $suggestions.show();
                } else {
                    $suggestions.hide();
                }
            });
        }, 200);
    });
    $input.on('focus', function () {
        if ($suggestions.children().length > 0) $suggestions.show();
    });
    $suggestions.on('mousedown', '.search-bar-suggestion-item', function () {
        $input.val($(this).text());
        $suggestions.hide();
        $('#globalSearchForm').submit();
    });
    // Enter key chọn suggestion đầu tiên
    $input.on('keydown', function (e) {
        if (e.key === 'Enter' && $suggestions.is(':visible')) {
            const $first = $suggestions.children().first();
            if ($first.length) {
                $input.val($first.text());
                $suggestions.hide();
            }
        }
    });
});
