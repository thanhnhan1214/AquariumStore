import { $, api, esc, qs, img, money, cart, renderLayout, productCard, bindAddButtons, loading, errorBox } from "../core.js";

await renderLayout("products");
const app = $("#app");
app.innerHTML = loading();
try {
  const id = qs("id");
  if (!id || !/^\d+$/.test(id)) throw new Error("Không tìm thấy sản phẩm.");
  const { product: p, related } = await api(`/api/catalog/${id}`);
  document.title = `${p.name} | Aquarium Store`;
  const out = p.stock <= 0;
  app.innerHTML = `
  <nav aria-label="breadcrumb"><ol class="breadcrumb small"><li class="breadcrumb-item"><a href="/">Trang chủ</a></li>
    <li class="breadcrumb-item"><a href="/products?category=${p.categoryId}">${esc(p.categoryName)}</a></li><li class="breadcrumb-item active">${esc(p.name)}</li></ol></nav>
  <div class="row g-4 mb-5">
    <div class="col-md-6"><div class="card-soft bg-white overflow-hidden"><img src="${img(p.imageUrl)}" alt="${esc(p.name)}" class="w-100 img-cover" style="aspect-ratio:1/1"></div></div>
    <div class="col-md-6">
      <span class="badge bg-foam text-primary mb-2">${esc(p.categoryName)}</span>
      <h1 class="h2">${esc(p.name)}</h1>
      <div class="fs-3 price mb-3">${money(p.price)}</div>
      <p class="text-secondary" style="white-space:pre-line">${esc(p.description || "Chưa có mô tả cho sản phẩm này.")}</p>
      <p class="mb-4">${out ? `<span class="badge bg-dark">Tạm hết hàng</span>` : `<i class="bi bi-check-circle-fill text-success me-1"></i>Còn <strong>${p.stock}</strong> sản phẩm`}</p>
      <div class="d-flex gap-2 align-items-center mb-4">
        <div class="input-group" style="width:140px"><button class="btn btn-outline-secondary" id="minus" aria-label="Giảm">−</button>
          <input type="number" class="form-control text-center" id="qty" value="1" min="1" max="${p.stock}" ${out ? "disabled" : ""} aria-label="Số lượng">
          <button class="btn btn-outline-secondary" id="plus" aria-label="Tăng">+</button></div>
        <button class="btn btn-primary rounded-pill px-4" id="add" ${out ? "disabled" : ""}><i class="bi bi-bag-plus me-1"></i>Thêm vào giỏ</button>
        <button class="btn btn-coral rounded-pill px-4" id="buy" ${out ? "disabled" : ""}>Mua ngay</button>
      </div>
      <div class="card-soft bg-foam p-3 small"><div><i class="bi bi-truck me-2"></i>Đóng gói túi oxy, thùng xốp chống sốc nhiệt</div>
        <div><i class="bi bi-shield-check me-2"></i>Bảo hành sống 24h đối với sinh vật cảnh</div><div><i class="bi bi-chat-dots me-2"></i>Cần tư vấn? Hỏi AquaBot ở góc phải màn hình</div></div>
    </div>
  </div>
  ${related.length ? `<h2 class="h4 mb-3">Sản phẩm cùng danh mục</h2><div class="row g-3">${related.map(productCard).join("")}</div>` : ""}`;
  const qty = $("#qty");
  const val = () => Math.max(1, Math.min(p.stock, parseInt(qty.value) || 1));
  $("#minus").onclick = () => (qty.value = Math.max(1, val() - 1));
  $("#plus").onclick = () => (qty.value = Math.min(p.stock, val() + 1));
  qty.onchange = () => (qty.value = val());
  $("#add").onclick = () => cart.add(p, val());
  $("#buy").onclick = () => { const ex = cart.items().find((i) => i.id === p.id); if (ex || cart.add(p, val())) location.href = "/cart"; };
  bindAddButtons(app, related);
} catch (e) {
  app.innerHTML = errorBox(e.message) + `<a href="/products" class="btn btn-primary">Quay lại danh sách sản phẩm</a>`;
}
