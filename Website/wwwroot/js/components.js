// ── SHARED COMPONENTS ───────────────────────────────────────
function renderNavbar(activePage = '') {
    const user = Auth.user;
    const cartCount = getCartCount();
    const isAdmin = Auth.isLoggedIn() && Auth.isAdmin();
    return `
  <nav class="navbar">
    <div class="navbar-inner">
      <a href="/" class="navbar-brand">✦ ShopVN</a>
      <div class="navbar-links">
        <a href="/" class="${activePage === 'home' ? 'active' : ''}">Trang chủ</a>
        <a href="/products.html" class="${activePage === 'products' ? 'active' : ''}">Sản phẩm</a>
      </div>
      <div class="navbar-right">
        <a href="/cart.html" class="btn-icon" title="Giỏ hàng">
          🛒
          <span class="badge cart-count-badge" style="display:${cartCount > 0 ? 'flex' : 'none'}">${cartCount}</span>
        </a>
        ${user ? `
          <div class="nav-user">
            ${isAdmin ? `<a href="/admin.html" class="btn-outline" style="padding: 6px 12px; border-radius: 6px; text-decoration: none; font-size: 0.9rem; margin-right: 8px; border-color: var(--accent); color: var(--accent);">⚙️ Bảng Quản Trị</a>` : ''}
            <span class="username" title="${user.fullName}">👋 ${user.fullName}</span>
            <button onclick="AuthAPI.logout()" class="btn-logout">Đăng xuất</button>
          </div>
        ` : `
          <a href="/login.html" class="btn-nav-auth btn-nav-login">Đăng nhập</a>
          <a href="/register.html" class="btn-nav-auth btn-nav-register">Đăng ký</a>
        `}
      </div>
    </div>
  </nav>`;
}

function renderFooter() {
    return `
  <footer class="footer">
    <div class="footer-inner">
      <div>
        <div class="footer-brand">✦ ShopVN</div>
        <p class="footer-desc">Nền tảng mua sắm trực tuyến hàng đầu Việt Nam. Sản phẩm chất lượng, giá tốt nhất, giao hàng nhanh chóng.</p>
      </div>
      <div>
        <div class="footer-col-title">Liên kết</div>
        <div class="footer-links">
          <a href="/">Trang chủ</a>
          <a href="/products.html">Sản phẩm</a>
          <a href="/cart.html">Giỏ hàng</a>
        </div>
      </div>
      <div>
        <div class="footer-col-title">Hỗ trợ</div>
        <div class="footer-links">
          <a href="#">Hướng dẫn mua hàng</a>
          <a href="#">Chính sách đổi trả</a>
          <a href="#">Liên hệ</a>
        </div>
      </div>
      <div>
        <div class="footer-col-title">Liên hệ</div>
        <div class="footer-links">
          <a href="#">📧 support@shopvn.vn</a>
          <a href="#">📞 1900-xxxx</a>
          <a href="#">📍 TP. Hồ Chí Minh</a>
        </div>
      </div>
    </div>
    <div class="footer-bottom">
      <span>© 2026 ShopVN. All rights reserved.</span>
      <span>Made with 💜 in Vietnam</span>
    </div>
  </footer>`;
}

function renderProductCard(p) {
    const img = p.imageUrl
        ? `<img src="${p.imageUrl}" alt="${p.name}" class="product-img" loading="lazy">`
        : `<div class="product-img-placeholder">📦</div>`;
    return `
  <div class="product-card" onclick="window.location='/product-detail.html?id=${p.id}'" style="cursor:pointer">
    <div class="product-img-wrap">${img}</div>
    <div class="product-info">
      <span class="product-category">${p.categoryName || ''}</span>
      <h3 class="product-name">${p.name}</h3>
      <div class="product-price">${formatPrice(p.price)}</div>
    </div>
    <div class="product-actions">
      <button class="btn-add-cart" onclick="event.stopPropagation(); handleAddToCart(${p.id})">
        🛒 Thêm vào giỏ
      </button>
    </div>
  </div>`;
}

async function handleAddToCart(productId) {
    if (!Auth.isLoggedIn()) {
        showToast('Vui lòng đăng nhập để thêm vào giỏ hàng', 'error');
        setTimeout(() => window.location.href = '/login.html', 1200);
        return;
    }
    try {
        await CartAPI.addItem(productId, 1);
        await refreshCartCount();
        showToast('Đã thêm vào giỏ hàng!');
    } catch (e) {
        showToast(e.message || 'Lỗi khi thêm vào giỏ hàng', 'error');
    }
}

function renderPagination(page, totalPages, onPageChange) {
    if (totalPages <= 1) return '';
    let html = '<div class="pagination">';
    html += `<button class="page-btn" ${page <= 1 ? 'disabled' : ''} onclick="${onPageChange}(${page - 1})">‹</button>`;
    const start = Math.max(1, page - 2);
    const end = Math.min(totalPages, page + 2);
    for (let i = start; i <= end; i++) {
        html += `<button class="page-btn ${i === page ? 'active' : ''}" onclick="${onPageChange}(${i})">${i}</button>`;
    }
    html += `<button class="page-btn" ${page >= totalPages ? 'disabled' : ''} onclick="${onPageChange}(${page + 1})">›</button>`;
    html += '</div>';
    return html;
}

// Init navbar + footer on DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
    const navEl = document.getElementById('navbar-container');
    const footEl = document.getElementById('footer-container');
    if (navEl) navEl.innerHTML = renderNavbar(navEl.dataset.active || '');
    if (footEl) footEl.innerHTML = renderFooter();
    refreshCartCount();
});
