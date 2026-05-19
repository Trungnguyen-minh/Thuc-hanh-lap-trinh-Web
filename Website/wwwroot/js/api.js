// ── CONFIG ─────────────────────────────────────────────────────
const API_BASE = '';   // same origin — served by ASP.NET Core

// ── AUTH STORAGE ───────────────────────────────────────────────
const Auth = {
    get token() { return localStorage.getItem('shopvn_token'); },
    get user() { try { return JSON.parse(localStorage.getItem('shopvn_user') || 'null'); } catch { return null; } },
    set(token, user) {
        localStorage.setItem('shopvn_token', token);
        localStorage.setItem('shopvn_user', JSON.stringify(user));
    },
    clear() {
        localStorage.removeItem('shopvn_token');
        localStorage.removeItem('shopvn_user');
    },
    isLoggedIn() { return !!this.token; }
};

// ── BASE FETCH ─────────────────────────────────────────────────
async function apiFetch(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    if (Auth.token) headers['Authorization'] = `Bearer ${Auth.token}`;
    const res = await fetch(`${API_BASE}${path}`, { ...options, headers });
    const data = await res.json().catch(() => null);
    if (!res.ok) {
        const msg = data?.message || `HTTP ${res.status}`;
        throw new ApiError(msg, res.status, data);
    }
    return data;
}

class ApiError extends Error {
    constructor(message, status, data) {
        super(message); this.status = status; this.data = data;
    }
}

// ── AUTH ────────────────────────────────────────────────────────
const AuthAPI = {
    async login(email, password) {
        const res = await apiFetch('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
        Auth.set(res.data.token, { email: res.data.email, fullName: res.data.fullName, expiry: res.data.expiry });
        return res.data;
    },
    async register(dto) {
        const res = await apiFetch('/api/auth/register', {
            method: 'POST',
            body: JSON.stringify(dto)
        });
        Auth.set(res.data.token, { email: res.data.email, fullName: res.data.fullName, expiry: res.data.expiry });
        return res.data;
    },
    logout() { Auth.clear(); window.location.href = '/login.html'; }
};

// ── PRODUCTS ────────────────────────────────────────────────────
const ProductsAPI = {
    getAll(page = 1, pageSize = 12, search = null, categoryId = null) {
        const q = new URLSearchParams({ page, pageSize });
        if (search) q.set('search', search);
        if (categoryId) q.set('categoryId', categoryId);
        return apiFetch(`/api/products?${q}`);
    },
    getById(id) { return apiFetch(`/api/products/${id}`); }
};

// ── CATEGORIES ──────────────────────────────────────────────────
const CategoriesAPI = {
    getAll(page = 1, pageSize = 100) {
        return apiFetch(`/api/categories?page=${page}&pageSize=${pageSize}`);
    }
};

// ── CART ────────────────────────────────────────────────────────
const CartAPI = {
    get() { return apiFetch('/api/cart'); },
    addItem(productId, quantity) {
        return apiFetch('/api/cart/items', { method: 'POST', body: JSON.stringify({ productId, quantity }) });
    },
    updateItem(cartItemId, quantity) {
        return apiFetch(`/api/cart/items/${cartItemId}`, { method: 'PUT', body: JSON.stringify({ quantity }) });
    },
    removeItem(cartItemId) {
        return apiFetch(`/api/cart/items/${cartItemId}`, { method: 'DELETE' });
    },
    clear() { return apiFetch('/api/cart', { method: 'DELETE' }); }
};

// ── ORDERS ──────────────────────────────────────────────────────
const OrderAPI = {
    getAll(page = 1, pageSize = 10) {
        return apiFetch(`/api/order?page=${page}&pageSize=${pageSize}`);
    },
    getById(orderId) {
        return apiFetch(`/api/order/${orderId}`);
    },
    reserve(dto) { return apiFetch('/api/order/reserve', { method: 'POST', body: JSON.stringify(dto) }); },
    createPayment(orderId) { return apiFetch(`/api/order/${orderId}/payment`, { method: 'POST' }); },
    getConfirmation(orderId) { return apiFetch(`/api/order/${orderId}/confirmation`); }
};

// ── HELPERS ─────────────────────────────────────────────────────
function formatPrice(n) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);
}

function showToast(msg, type = 'success') {
    const existing = document.getElementById('toast');
    if (existing) existing.remove();
    const t = document.createElement('div');
    t.id = 'toast';
    t.style.cssText = `
    position:fixed; bottom:24px; right:24px; z-index:9999;
    padding:14px 22px; border-radius:12px; font-size:.9rem; font-weight:600;
    max-width:340px; animation:fadeIn .3s ease;
    background:${type === 'success' ? 'rgba(34,197,94,.15)' : type === 'error' ? 'rgba(239,68,68,.15)' : 'rgba(108,99,255,.15)'};
    border:1px solid ${type === 'success' ? 'rgba(34,197,94,.4)' : type === 'error' ? 'rgba(239,68,68,.4)' : 'rgba(108,99,255,.4)'};
    color:${type === 'success' ? '#86efac' : type === 'error' ? '#fca5a5' : '#a89fff'};
    box-shadow:0 8px 32px rgba(0,0,0,.4);
  `;
    t.textContent = msg;
    document.body.appendChild(t);
    setTimeout(() => t.remove(), 3500);
}

function getCartCount() {
    return parseInt(localStorage.getItem('shopvn_cart_count') || '0');
}
function setCartCount(n) {
    localStorage.setItem('shopvn_cart_count', n);
    document.querySelectorAll('.cart-count-badge').forEach(el => {
        el.textContent = n;
        el.style.display = n > 0 ? 'flex' : 'none';
    });
}
async function refreshCartCount() {
    if (!Auth.isLoggedIn()) { setCartCount(0); return; }
    try {
        const res = await CartAPI.get();
        setCartCount(res.data?.totalItems || 0);
    } catch { setCartCount(0); }
}
