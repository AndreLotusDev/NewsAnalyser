const state = {
    knownPostIds: new Set(),
    filters: { accounts: [], tags: [], categories: [] },
    lastPostId: null,
    lastUpdated: null
};

let newPostsBuffer = 0;

$(document).ready(function () {
    loadInitialPosts();
    setInterval(fetchNewPosts, 30000);
    pollHealth();
    setInterval(pollHealth, 60000);
    bindFilterHandlers();
});

// ─── API calls ────────────────────────────────────────────────────────────────

function apiFetchPosts(afterPostId, callback) {
    const parts = ['pageSize=50'];
    state.filters.accounts.forEach(a => parts.push('accounts=' + encodeURIComponent(a)));
    state.filters.tags.forEach(t => parts.push('tags=' + encodeURIComponent(t)));
    state.filters.categories.forEach(c => parts.push('categories=' + encodeURIComponent(c)));
    if (afterPostId) parts.push('after=' + encodeURIComponent(afterPostId));
    return $.getJSON('/api/social/posts?' + parts.join('&'), callback);
}

function loadInitialPosts() {
    $('#feed-loading').show();
    apiFetchPosts(null, function (data) {
        $('#post-feed').empty();
        state.knownPostIds.clear();
        state.lastPostId = null;
        renderPosts(data.items, false);
        $('#feed-loading').hide();
    }).fail(function () {
        $('#feed-loading').html('<span class="text-danger">Failed to load posts. Will retry shortly.</span>');
    });
}

function fetchNewPosts() {
    if (!state.lastPostId) return;
    apiFetchPosts(state.lastPostId, function (data) {
        const fresh = (data.items || []).filter(p => !state.knownPostIds.has(p.postId));
        if (!fresh.length) return;
        const isScrolledDown = $(window).scrollTop() > 200;
        renderPosts(fresh, true);
        if (isScrolledDown) {
            newPostsBuffer += fresh.length;
            showNewPostsBanner(newPostsBuffer);
        }
    });
}

// ─── Rendering ────────────────────────────────────────────────────────────────

function renderPosts(posts, prepend) {
    posts.forEach(function (post) {
        if (state.knownPostIds.has(post.postId)) return;
        state.knownPostIds.add(post.postId);
        if (!state.lastPostId || post.postId > state.lastPostId) {
            state.lastPostId = post.postId;
        }
        const $card = buildPostCard(post);
        if (prepend) {
            $card.addClass('post-card-new');
            $('#post-feed').prepend($card);
        } else {
            $('#post-feed').append($card);
        }
        populateAccountOption(post.account);
        populateCategoryOptions(post.categories || []);
    });
}

function buildPostCard(post) {
    const sentimentClass = sentimentBadgeClass(post.sentiment);
    const avatarUrl = 'https://unavatar.io/twitter/' + encodeURIComponent(post.account);
    const postLink = post.link || ('https://x.com/' + encodeURIComponent(post.account));
    const timeAgo = formatTimeAgo(post.timestamp);

    const tagChips = (post.tags || []).map(function (tag) {
        const isActive = state.filters.tags.indexOf(tag) !== -1;
        const cls = isActive ? 'bg-primary text-white' : 'bg-secondary-subtle text-secondary-emphasis';
        return '<span class="badge rounded-pill ' + cls + ' tag-chip" data-tag="' + esc(tag) + '">' + esc(tag) + '</span>';
    }).join('');

    const sentimentBadge = post.sentiment
        ? '<span class="badge ' + sentimentClass + ' ms-auto">' + esc(post.sentiment) + '</span>'
        : '';

    const summaryHtml = post.summary
        ? '<p class="fw-semibold fs-6 mb-1"><i class="ri-sparkling-line me-1 text-primary"></i>' + esc(post.summary) + '</p>'
        : '';

    const tagsHtml = tagChips
        ? '<div class="d-flex flex-wrap gap-1 mb-2">' + tagChips + '</div>'
        : '';

    const isNegative = (post.sentiment || '').toLowerCase() === 'negative';
    const cardExtra = isNegative ? ' border border-danger bg-danger-subtle' : '';

    const html = [
        '<div class="card mb-3 post-card' + cardExtra + '" data-post-id="' + esc(post.postId) + '">',
        '  <div class="card-body">',
        '    <div class="d-flex align-items-center gap-2 mb-2">',
        '      <img class="avatar avatar-sm rounded-circle"',
        '           src="' + avatarUrl + '"',
        '           alt="' + esc(post.account) + '"',
        '           onerror="this.src=\'/fila-assets/images/user1.jpg\'">',
        '      <a class="fw-semibold text-dark text-decoration-none"',
        '         href="' + esc(postLink) + '" target="_blank" rel="noopener">',
        '        @' + esc(post.account),
        '      </a>',
        '      ' + sentimentBadge,
        '    </div>',
        '    ' + summaryHtml,
        '    <p class="mb-1 text-muted small">' + esc(post.text) + '</p>',
        '    ' + tagsHtml,
        '    <span class="text-muted small">' + timeAgo + '</span>',
        '  </div>',
        '</div>'
    ].join('\n');

    return $(html);
}

function sentimentBadgeClass(sentiment) {
    switch ((sentiment || '').toLowerCase()) {
        case 'positive': return 'bg-success-subtle text-success';
        case 'negative': return 'bg-danger-subtle text-danger';
        default:         return 'bg-secondary-subtle text-secondary';
    }
}

function formatTimeAgo(timestamp) {
    const diffMs = Date.now() - new Date(timestamp).getTime();
    const secs = Math.floor(diffMs / 1000);
    const mins = Math.floor(secs / 60);
    const hours = Math.floor(mins / 60);
    const days = Math.floor(hours / 24);
    if (secs < 60)  return 'just now';
    if (mins < 60)  return mins + 'm ago';
    if (hours < 24) return hours + 'h ago';
    return days + 'd ago';
}

function esc(str) {
    return $('<div>').text(str || '').html();
}

// ─── Health polling ───────────────────────────────────────────────────────────

function pollHealth() {
    $.getJSON('/api/social/health')
        .done(function (data) {
            const online = data.status === 'ok';
            $('#health-dot')
                .removeClass('status-online status-offline')
                .addClass(online ? 'status-online' : 'status-offline');
            const label = data.lastRunAt
                ? 'Last updated: ' + formatTimeAgo(data.lastRunAt)
                : 'Feed not yet run';
            $('#health-text').text(label);
        })
        .fail(function () {
            $('#health-dot').removeClass('status-online').addClass('status-offline');
            $('#health-text').text('Health check failed');
        });
}

// ─── Filters ──────────────────────────────────────────────────────────────────

function bindFilterHandlers() {
    // Accounts dropdown toggle
    $('#accounts-toggle').on('click', function (e) {
        e.stopPropagation();
        const $dd = $('#accounts-dropdown');
        const open = !$dd.prop('hidden');
        $dd.prop('hidden', open);
        $(this).attr('aria-expanded', String(!open));
        if (!open) { $('#accounts-search').val('').trigger('input').focus(); }
    });

    // Close dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('#accounts-multiselect').length) {
            $('#accounts-dropdown').prop('hidden', true);
            $('#accounts-toggle').attr('aria-expanded', 'false');
        }
    });

    // Search within accounts
    $('#accounts-search').on('input', function () {
        const q = $(this).val().toLowerCase();
        $('#accounts-options .filter-option').each(function () {
            const name = $(this).find('label').text().toLowerCase();
            $(this).toggle(name.includes(q));
        });
        const visible = $('#accounts-options .filter-option:visible').length;
        $('#accounts-options .filter-empty').remove();
        if (!visible) {
            $('#accounts-options').append('<div class="filter-empty">No accounts found</div>');
        }
    });

    // Account checkbox change
    $(document).on('change', '.account-checkbox', function () {
        const account = $(this).val();
        if (this.checked) {
            if (!state.filters.accounts.includes(account)) state.filters.accounts.push(account);
        } else {
            state.filters.accounts = state.filters.accounts.filter(a => a !== account);
        }
        updateAccountsBadge();
        loadInitialPosts();
    });

    // Category pills
    $(document).on('click', '.cat-pill', function () {
        const cat = $(this).data('cat');
        state.filters.categories = cat ? [cat] : [];
        $('.cat-pill').removeClass('cat-pill-active');
        $(this).addClass('cat-pill-active');
        loadInitialPosts();
    });

    // Clear / reset
    $('#clear-filters').on('click', function () {
        state.filters = { accounts: [], tags: [], categories: [] };
        $('.account-checkbox').prop('checked', false);
        $('.filter-option').removeClass('checked');
        updateAccountsBadge();
        $('.cat-pill').removeClass('cat-pill-active');
        $('.cat-pill[data-cat=""]').addClass('cat-pill-active');
        $('#active-tag-filters').empty();
        loadInitialPosts();
    });

    $(document).on('click', '.tag-chip', function () {
        toggleTagFilter($(this).data('tag'));
    });

    $('#scroll-to-top-link').on('click', function (e) {
        e.preventDefault();
        $('html, body').animate({ scrollTop: 0 }, 300);
        $('#new-posts-alert').addClass('d-none');
        newPostsBuffer = 0;
    });

    $(document).on('closed.bs.alert', '#new-posts-alert', function () {
        newPostsBuffer = 0;
    });
}

function toggleTagFilter(tag) {
    const idx = state.filters.tags.indexOf(tag);
    if (idx === -1) {
        state.filters.tags.push(tag);
    } else {
        state.filters.tags.splice(idx, 1);
    }
    renderActiveTagFilters();
    loadInitialPosts();
}

function renderActiveTagFilters() {
    const $container = $('#active-tag-filters');
    $container.empty();
    state.filters.tags.forEach(function (tag) {
        $container.append(
            $('<span class="badge rounded-pill bg-primary text-white tag-chip">')
                .attr('data-tag', tag)
                .html(esc(tag) + ' <span style="opacity:.7">×</span>')
        );
    });
}

function showNewPostsBanner(count) {
    const label = count + ' new post' + (count !== 1 ? 's' : '');
    $('#new-posts-count').text(label);
    $('#new-posts-alert').removeClass('d-none');
}

// ─── Dynamic select population ───────────────────────────────────────────────

function populateAccountOption(account) {
    const id = 'acc-' + account.replace(/[^a-zA-Z0-9]/g, '_');
    if ($('#' + id).length) return;
    const isChecked = state.filters.accounts.includes(account);
    const $opt = $('<div class="filter-option' + (isChecked ? ' checked' : '') + '">').append(
        $('<input type="checkbox" class="account-checkbox">').attr({ id: id, value: account }).prop('checked', isChecked),
        $('<label>').attr('for', id).text('@' + account)
    );
    $('#accounts-options').append($opt);
}

function populateCategoryOptions(categories) {
    categories.forEach(function (cat) {
        const exists = $('#category-pills .cat-pill').filter(function () {
            return $(this).data('cat') === cat;
        }).length;
        if (exists) return;
        const $pill = $('<button class="cat-pill">').attr('data-cat', cat).text(cat);
        $('#category-pills').append($pill);
    });
}

function updateAccountsBadge() {
    const count = state.filters.accounts.length;
    const $badge = $('#accounts-badge');
    if (count === 0) {
        $badge.html('All accounts');
    } else {
        $badge.html('Accounts <span class="filter-count-badge">' + count + '</span>');
    }
}

// ─── Utilities ────────────────────────────────────────────────────────────────

function debounce(fn, wait) {
    let timer;
    return function () {
        const ctx = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(function () { fn.apply(ctx, args); }, wait);
    };
}
