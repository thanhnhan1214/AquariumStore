import { $, api, esc, renderLayout, productCard, bindAddButtons, loading, errorBox } from "../core.js";

await renderLayout("home");
const app = $("#app");
app.innerHTML = `
<section class="hero p-4 p-md-5 mb-5">
  ${[[8, 18, 0], [22, 70, 2], [14, 85, 4], [10, 40, 6]].map(([s, l, d]) => `<span class="bubble" style="width:${s}px;height:${s}px;left:${l}%;bottom:0;animation-delay:${d}s"></span>`).join("")}
  <div class="row align-items-center position-relative" style="z-index:1">
    <div class="col-lg-7">
      <span class="badge rounded-pill bg-light text-primary mb-3"><i class="bi bi-stars me-1"></i>Tư vấn miễn phí cùng AquaBot AI</span>
      <h1 class="display-5 fw-bold mb-3">Mang cả đại dương thu nhỏ về ngôi nhà của bạn</h1>
      <p class="lead opacity-75 mb-4">Cá cảnh, tép màu, cây thủy sinh và thiết bị bể cá chọn lọc — đóng gói oxy an toàn, giao tận nơi.</p>
      <div class="d-flex gap-2 flex-wrap">
        <a href="/products" class="btn btn-coral btn-lg rounded-pill px-4">Mua sắm ngay</a>
        <button class="btn btn-outline-light btn-lg rounded-pill px-4" id="askBot"><i class="bi bi-chat-dots me-1"></i>Hỏi AquaBot</button>
      </div>
    </div>
    <div class="col-lg-5 d-none d-lg-block text-center"><i class="bi bi-water" style="font-size:11rem;line-height:1;opacity:.85"></i></div>
  </div>
</section>
<section class="mb-5"><h2 class="h4 mb-3">Danh mục nổi bật</h2><div class="row g-3" id="cats">${loading()}</div></section>
<section class="mb-5"><div class="d-flex justify-content-between align-items-end mb-3"><h2 class="h4 mb-0"><i class="bi bi-fire text-danger"></i> Bán chạy nhất</h2><a href="/products?sort=best">Xem tất cả</a></div><div class="row g-3" id="best"></div></section>
<section class="mb-5"><div class="d-flex justify-content-between align-items-end mb-3"><h2 class="h4 mb-0">Hàng mới về</h2><a href="/products">Xem tất cả</a></div><div class="row g-3" id="latest"></div></section>
<section class="row g-3 text-center">
  ${[["bi-truck", "Giao hàng an toàn", "Đóng túi oxy, thùng xốp giữ nhiệt"], ["bi-shield-check", "Bảo hành sống 24h", "Đổi mới nếu cá chết khi nhận"], ["bi-qr-code-scan", "Thanh toán VietQR", "Quét mã chuyển khoản tự động"], ["bi-robot", "AquaBot AI", "Tư vấn setup bể 24/7"]]
    .map(([i, t, d]) => `<div class="col-6 col-md-3"><div class="card-soft bg-white p-3 h-100"><i class="bi ${i} fs-2 text-ocean"></i><div class="fw-semibold mt-2">${t}</div><div class="small text-secondary">${d}</div></div></div>`).join("")}
</section>`;
$("#askBot").onclick = () => document.querySelector(".chat-fab")?.click();

const icons = ["droplet-half", "tsunami", "flower1", "lightbulb", "basket", "tree", "bug", "tools"];
try {
  const { categories, products } = await api("/api/catalog");
  $("#cats").innerHTML = categories.map((c, i) => `<div class="col-6 col-md-3"><a href="/products?category=${c.id}" class="card-soft bg-white p-3 d-flex align-items-center gap-3 text-decoration-none text-body h-100">
    <span class="fs-3 text-ocean bg-foam rounded-3 d-grid" style="width:52px;height:52px;place-items:center"><i class="bi bi-${icons[i % icons.length]}"></i></span><div><div class="fw-semibold">${esc(c.name)}</div><div class="small text-secondary">${products.filter((p) => p.categoryId === c.id).length} sản phẩm</div></div></a></div>`).join("");
  const best = [...products].sort((a, b) => b.sold - a.sold).slice(0, 4);
  $("#best").innerHTML = best.map(productCard).join("");
  $("#latest").innerHTML = products.slice(0, 8).map(productCard).join("");
  bindAddButtons(app, products);
} catch (e) {
  $("#cats").innerHTML = errorBox(e.message);
}
