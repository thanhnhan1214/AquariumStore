// ===== Tiện ích dùng chung cho toàn bộ website Aquarium Store =====
export const $ = (s, root = document) => root.querySelector(s);
export const $$ = (s, root = document) => [...root.querySelectorAll(s)];

export const esc = (v) => String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
export const money = (n) => Number(n || 0).toLocaleString("vi-VN") + "đ";
export const fdate = (d, time = true) => {
  if (!d) return "";
  const x = new Date(d);
  return time ? x.toLocaleString("vi-VN", { hour: "2-digit", minute: "2-digit", day: "2-digit", month: "2-digit", year: "numeric" }) : x.toLocaleDateString("vi-VN");
};
export const img = (url) => esc(url || "/images/erd_aquariumstore.png");
export const qs = (k) => new URLSearchParams(location.search).get(k);

export const STATUS_STYLE = {
  "Chờ xử lý": "warning text-dark", "Đã xác nhận": "info text-dark", "Đang giao": "primary",
  "Hoàn thành": "success", "Đã hủy": "secondary",
};
export const statusBadge = (s) => `<span class="badge rounded-pill bg-${STATUS_STYLE[s] || "light text-dark"}">${esc(s)}</span>`;

export const BANK = { bank: "MB", account: "0817687979", name: "LE THANH NHAN" };
export const qrUrl = (amount, info) =>
  `https://img.vietqr.io/image/${BANK.bank}-${BANK.account}-compact2.png?amount=${Math.round(amount)}&addInfo=${encodeURIComponent(info)}&accountName=${encodeURIComponent(BANK.name)}`;

// ----- Gọi API -----
export async function api(path, opts = {}) {
  const init = { method: opts.method || "GET", headers: {}, credentials: "same-origin" };
  if (opts.body instanceof FormData) init.body = opts.body;
  else if (opts.body !== undefined) { init.body = JSON.stringify(opts.body); init.headers["content-type"] = "application/json"; }
  let res;
  try { res = await fetch(path, init); } catch { throw new Error("Không kết nối được máy chủ. Vui lòng kiểm tra mạng."); }
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const err = new Error(data.error || `Lỗi ${res.status}`);
    err.status = res.status;
    throw err;
  }
  return data;
}

// ----- Thông báo -----
export function toast(message, type = "success") {
  let box = $(".toast-container");
  if (!box) { box = document.createElement("div"); box.className = "toast-container position-fixed top-0 end-0 p-3"; document.body.append(box); }
  const el = document.createElement("div");
  el.className = `toast align-items-center text-bg-${type} border-0`;
  el.setAttribute("role", "status");
  el.innerHTML = `<div class="d-flex"><div class="toast-body">${esc(message)}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Đóng"></button></div>`;
  box.append(el);
  const t = new bootstrap.Toast(el, { delay: 3500 });
  el.addEventListener("hidden.bs.toast", () => el.remove());
  t.show();
}

export const loading = (text = "Đang tải...") => `<div class="text-center py-5 text-secondary"><div class="spinner-border text-primary mb-2" role="status"></div><div>${text}</div></div>`;
export const errorBox = (msg) => `<div class="alert alert-danger my-4"><i class="bi bi-exclamation-triangle-fill me-2"></i>${esc(msg)}</div>`;

/** Khóa nút trong lúc gửi form để tránh bấm nhiều lần. */
export async function busy(btn, fn) {
  const html = btn.innerHTML;
  btn.disabled = true;
  btn.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...`;
  try { return await fn(); } finally { btn.disabled = false; btn.innerHTML = html; }
}

// ----- Người dùng -----
let mePromise;
export const getMe = (refresh = false) => {
  if (!mePromise || refresh) mePromise = api("/api/auth/me").then((d) => d.user).catch(() => null);
  return mePromise;
};
export const isStaff = (u) => u && (u.role === "Admin" || u.role === "Staff");
export async function requireLogin() {
  const me = await getMe();
  if (!me) { location.href = `/login?next=${encodeURIComponent(location.pathname + location.search)}`; return new Promise(() => {}); }
  return me;
}
export async function logout() {
  await api("/api/auth/logout", { method: "POST" }).catch(() => {});
  location.href = "/";
}

// ----- Giỏ hàng (lưu trên trình duyệt) -----
const CART_KEY = "aq_cart_v1";
export const cart = {
  items() { try { const v = JSON.parse(localStorage.getItem(CART_KEY) || "[]"); return Array.isArray(v) ? v : []; } catch { return []; } },
  save(items) { localStorage.setItem(CART_KEY, JSON.stringify(items)); updateCartBadge(); },
  count() { return this.items().reduce((s, i) => s + i.qty, 0); },
  total() { return this.items().reduce((s, i) => s + i.qty * i.price, 0); },
  add(p, qty = 1) {
    const items = this.items();
    const ex = items.find((i) => i.id === p.id);
    const current = ex ? ex.qty : 0;
    if (p.stock <= 0) { toast("Sản phẩm đã hết hàng.", "danger"); return false; }
    if (current + qty > p.stock) { toast(`Chỉ còn ${p.stock} sản phẩm trong kho.`, "warning"); return false; }
    if (ex) Object.assign(ex, { qty: current + qty, price: p.price, stock: p.stock, name: p.name, imageUrl: p.imageUrl });
    else items.push({ id: p.id, name: p.name, price: p.price, imageUrl: p.imageUrl, stock: p.stock, qty });
    this.save(items);
    toast(`Đã thêm "${p.name}" vào giỏ hàng.`);
    return true;
  },
  setQty(id, qty) { this.save(this.items().map((i) => (i.id === id ? { ...i, qty } : i)).filter((i) => i.qty > 0)); },
  remove(id) { this.save(this.items().filter((i) => i.id !== id)); },
  clear() { this.save([]); },
};
function updateCartBadge() { $$(".cart-count").forEach((el) => { const n = cart.count(); el.textContent = n; el.classList.toggle("d-none", n === 0); }); }
window.addEventListener("storage", (e) => e.key === CART_KEY && updateCartBadge());

// ----- Header / Footer -----
export async function renderLayout(active = "") {
  const header = document.createElement("header");
  header.className = "no-print sticky-top";
  header.innerHTML = `
  <div class="topbar py-1"><div class="container d-flex justify-content-between">
    <span><i class="bi bi-truck me-1"></i>Giao cá sống tận nơi nội thành TP.HCM</span>
    <span class="d-none d-sm-inline"><i class="bi bi-telephone-fill me-1"></i>Hotline: 0901 234 567</span></div></div>
  <nav class="navbar navbar-expand-lg aq-nav"><div class="container">
    <a class="navbar-brand d-flex align-items-center gap-2" href="/"><span class="logo"><i class="bi bi-water"></i></span>Aquarium Store</a>
    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#mainNav" aria-label="Mở menu"><span class="navbar-toggler-icon"></span></button>
    <div class="collapse navbar-collapse" id="mainNav">
      <ul class="navbar-nav me-auto ms-lg-3">
        <li class="nav-item"><a class="nav-link ${active === "home" ? "active" : ""}" href="/">Trang chủ</a></li>
        <li class="nav-item"><a class="nav-link ${active === "products" ? "active" : ""}" href="/products">Sản phẩm</a></li>
        <li class="nav-item"><a class="nav-link ${active === "guide" ? "active" : ""}" href="/guide">Hướng dẫn mua hàng</a></li>
      </ul>
      <form class="d-flex my-2 my-lg-0 me-lg-3" action="/products" role="search">
        <input class="form-control form-control-sm rounded-pill" type="search" name="q" placeholder="Tìm cá, tép, cây..." aria-label="Tìm kiếm" value="${esc(qs("q") || "")}">
      </form>
      <div class="d-flex align-items-center gap-2" id="navUser">
        <a href="/cart" class="btn btn-outline-primary btn-sm position-relative rounded-pill px-3"><i class="bi bi-bag"></i> Giỏ hàng
          <span class="cart-count position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger d-none">0</span></a>
      </div>
    </div></div></nav>`;
  document.body.prepend(header);

  const footer = document.createElement("footer");
  footer.className = "aq-footer pt-5 pb-4 mt-5 no-print";
  footer.innerHTML = `<div class="container"><div class="row g-4">
    <div class="col-md-5"><h5 class="text-white display-font"><i class="bi bi-water me-1"></i>Aquarium Store</h5>
      <p class="small mb-2">Cá cảnh, tép cảnh, cây thủy sinh và thiết bị bể cá chính hãng. Tư vấn tận tâm cùng trợ lý AquaBot AI.</p>
      <p class="small mb-1"><i class="bi bi-geo-alt-fill me-2"></i>123 Nguyễn Huệ, Quận 1, TP.HCM</p>
      <p class="small mb-1"><i class="bi bi-envelope-fill me-2"></i>info@aquariumstore.vn</p></div>
    <div class="col-6 col-md-3"><h6 class="text-white text-uppercase small fw-bold">Hỗ trợ</h6><ul class="list-unstyled small">
      <li class="mb-1"><a href="/guide">Hướng dẫn mua hàng</a></li><li class="mb-1"><a href="/guide#policy">Chính sách đổi trả</a></li><li class="mb-1"><a href="/orders">Tra cứu đơn hàng</a></li></ul></div>
    <div class="col-6 col-md-4"><h6 class="text-white text-uppercase small fw-bold">Thanh toán</h6>
      <p class="small mb-1"><i class="bi bi-cash-coin me-2"></i>Thanh toán khi nhận hàng (COD)</p>
      <p class="small"><i class="bi bi-qr-code me-2"></i>Chuyển khoản VietQR – ${BANK.bank} Bank</p></div>
  </div><hr class="border-secondary"><div class="small text-center">&copy; ${new Date().getFullYear()} Aquarium Store — Đồ án 2: Website thương mại điện tử cá cảnh – thủy sinh.</div></div>`;
  document.body.append(footer);
  updateCartBadge();
  mountChat();

  const me = await getMe();
  const box = $("#navUser");
  if (me) {
    box.insertAdjacentHTML("beforeend", `<div class="dropdown">
      <button class="btn btn-sm btn-light rounded-pill dropdown-toggle" data-bs-toggle="dropdown"><i class="bi bi-person-circle me-1"></i>${esc(me.fullName)}</button>
      <ul class="dropdown-menu dropdown-menu-end">
        ${isStaff(me) ? `<li><a class="dropdown-item" href="/admin"><i class="bi bi-speedometer2 me-2"></i>Trang quản trị</a></li><li><hr class="dropdown-divider"></li>` : ""}
        <li><a class="dropdown-item" href="/orders"><i class="bi bi-receipt me-2"></i>Đơn hàng của tôi</a></li>
        <li><a class="dropdown-item" href="/profile"><i class="bi bi-person-gear me-2"></i>Hồ sơ cá nhân</a></li>
        <li><hr class="dropdown-divider"></li>
        <li><button class="dropdown-item text-danger" id="btnLogout"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</button></li>
      </ul></div>`);
    $("#btnLogout").onclick = logout;
  } else {
    box.insertAdjacentHTML("beforeend", `<a class="btn btn-sm btn-primary rounded-pill px-3" href="/login">Đăng nhập</a>`);
  }
  return me;
}

// ----- Thẻ sản phẩm -----
export function productCard(p) {
  const hot = (p.sold || 0) >= 10;
  const out = p.stock <= 0;
  return `<div class="col-6 col-md-4 col-lg-3"><div class="product-card d-flex flex-column">
    <a class="thumb" href="/product?id=${p.id}">
      <img src="${img(p.imageUrl)}" alt="${esc(p.name)}" loading="lazy">
      ${hot ? `<span class="badge badge-hot position-absolute top-0 start-0 m-2"><i class="bi bi-fire"></i> HOT</span>` : ""}
      ${out ? `<span class="badge bg-dark position-absolute top-0 end-0 m-2">Hết hàng</span>` : ""}
    </a>
    <div class="p-3 d-flex flex-column flex-grow-1">
      <div class="small text-secondary mb-1">${esc(p.categoryName)}</div>
      <a class="title mb-2" href="/product?id=${p.id}">${esc(p.name)}</a>
      <div class="mt-auto d-flex align-items-center justify-content-between">
        <span class="price">${money(p.price)}</span>
        <button class="btn btn-sm btn-primary rounded-circle" data-add="${p.id}" ${out ? "disabled" : ""} aria-label="Thêm vào giỏ"><i class="bi bi-bag-plus"></i></button>
      </div>
    </div></div></div>`;
}
export function bindAddButtons(root, list) {
  root.addEventListener("click", (e) => {
    const b = e.target.closest("[data-add]");
    if (!b) return;
    const p = list.find((x) => x.id === Number(b.dataset.add));
    if (p) cart.add(p);
  });
}

// ----- AquaBot AI -----
function mountChat() {
  if (location.pathname.startsWith("/admin")) return;
  const KEY = "aq_chat_v1";
  let history = [];
  try { history = JSON.parse(sessionStorage.getItem(KEY) || "[]"); } catch {}
  const fab = document.createElement("button");
  fab.className = "chat-fab no-print"; fab.setAttribute("aria-label", "Mở trợ lý AquaBot"); fab.innerHTML = `<i class="bi bi-chat-dots-fill"></i>`;
  const panel = document.createElement("div");
  panel.className = "chat-panel no-print";
  panel.innerHTML = `<div class="chat-head d-flex align-items-center gap-2">
      <span class="fs-4">🐠</span><div class="flex-grow-1"><div class="fw-bold">AquaBot AI</div><div class="small opacity-75">Trợ lý tư vấn thủy sinh</div></div>
      <button class="btn btn-sm text-white" id="chatClear" title="Xóa hội thoại"><i class="bi bi-arrow-counterclockwise"></i></button>
      <button class="btn-close btn-close-white" id="chatClose" aria-label="Đóng"></button></div>
    <div class="chat-body" id="chatBody"></div>
    <div class="chat-chips px-2 pt-2 d-flex gap-1 flex-wrap bg-white">
      <button class="btn btn-outline-secondary btn-sm rounded-pill">Bể 60cm nên nuôi cá gì?</button>
      <button class="btn btn-outline-secondary btn-sm rounded-pill">Cách nuôi tép Red Cherry</button>
      <button class="btn btn-outline-secondary btn-sm rounded-pill">Cây thủy sinh dễ trồng</button></div>
    <form class="p-2 d-flex gap-2 bg-white border-top" id="chatForm">
      <input class="form-control rounded-pill" id="chatInput" placeholder="Hỏi AquaBot..." autocomplete="off" maxlength="1000">
      <button class="btn btn-primary rounded-circle" aria-label="Gửi"><i class="bi bi-send-fill"></i></button></form>`;
  document.body.append(fab, panel);
  const body = $("#chatBody", panel);
  const fmt = (t) => esc(t).replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>").replace(/\[#(\d+)\]/g, (_, id) => ` <a href="/product?id=${id}">[xem]</a>`);
  const draw = () => {
    body.innerHTML = `<div class="msg bot">Xin chào! Mình là AquaBot 🐠. Mình có thể tư vấn chọn cá, tép, cây thủy sinh và thiết bị bể cho bạn.</div>` +
      history.map((m) => `<div class="msg ${m.role}">${m.role === "bot" ? fmt(m.text) : esc(m.text)}</div>`).join("");
    body.scrollTop = body.scrollHeight;
  };
  const send = async (text) => {
    text = text.trim();
    if (!text) return;
    const prior = history.slice(-8);
    history.push({ role: "user", text });
    draw();
    body.insertAdjacentHTML("beforeend", `<div class="msg bot typing" id="typing"><span></span><span></span><span></span></div>`);
    body.scrollTop = body.scrollHeight;
    try {
      const d = await api("/api/chat", { method: "POST", body: { message: text, history: prior } });
      history.push({ role: "bot", text: d.reply });
    } catch (e) {
      history.push({ role: "bot", text: "Xin lỗi, AquaBot đang bận. " + e.message });
    }
    sessionStorage.setItem(KEY, JSON.stringify(history.slice(-30)));
    draw();
  };
  fab.onclick = () => { panel.classList.toggle("open"); if (panel.classList.contains("open")) { draw(); $("#chatInput", panel).focus(); } };
  $("#chatClose", panel).onclick = () => panel.classList.remove("open");
  $("#chatClear", panel).onclick = () => { history = []; sessionStorage.removeItem(KEY); draw(); };
  $("#chatForm", panel).onsubmit = (e) => { e.preventDefault(); const i = $("#chatInput", panel); send(i.value); i.value = ""; };
  $$(".chat-chips button", panel).forEach((b) => (b.onclick = () => send(b.textContent)));
}
