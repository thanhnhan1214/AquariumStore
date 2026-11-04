import { $, api, esc, img, money, cart, renderLayout, loading, toast } from "../core.js";

await renderLayout();
const app = $("#app");
app.innerHTML = loading("Đang kiểm tra giỏ hàng...");

// Đồng bộ giá & tồn kho mới nhất từ máy chủ
try {
  const { products } = await api("/api/catalog");
  let changed = false;
  const items = cart.items().flatMap((i) => {
    const p = products.find((x) => x.id === i.id);
    if (!p || p.stock <= 0) { changed = true; return []; }
    const qty = Math.min(i.qty, p.stock);
    if (qty !== i.qty || p.price !== i.price) changed = true;
    return [{ ...i, name: p.name, price: p.price, stock: p.stock, imageUrl: p.imageUrl, qty }];
  });
  cart.save(items);
  if (changed) toast("Giỏ hàng đã được cập nhật theo giá và tồn kho mới nhất.", "info");
} catch {}

function draw() {
  const items = cart.items();
  if (!items.length) {
    app.innerHTML = `<div class="empty-state"><i class="bi bi-bag"></i><h1 class="h4 mt-3">Giỏ hàng đang trống</h1><p>Hãy chọn vài chú cá xinh xắn nhé!</p><a href="/products" class="btn btn-primary rounded-pill px-4">Mua sắm ngay</a></div>`;
    return;
  }
  app.innerHTML = `<h1 class="h3 mb-4">Giỏ hàng (${cart.count()})</h1>
  <div class="row g-4"><div class="col-lg-8"><div class="card-soft bg-white">
    ${items.map((i) => `<div class="d-flex gap-3 align-items-center p-3 border-bottom">
      <a href="/product?id=${i.id}"><img src="${img(i.imageUrl)}" alt="" class="rounded img-cover" width="76" height="76"></a>
      <div class="flex-grow-1"><a href="/product?id=${i.id}" class="fw-semibold text-body text-decoration-none">${esc(i.name)}</a>
        <div class="small text-secondary">${money(i.price)} · còn ${i.stock}</div></div>
      <input type="number" class="form-control qty-input" min="1" max="${i.stock}" value="${i.qty}" data-qty="${i.id}" aria-label="Số lượng">
      <div class="fw-semibold text-end" style="width:110px">${money(i.price * i.qty)}</div>
      <button class="btn btn-sm btn-outline-danger" data-del="${i.id}" aria-label="Xóa"><i class="bi bi-trash"></i></button></div>`).join("")}
    <div class="p-3 d-flex justify-content-between"><a href="/products"><i class="bi bi-arrow-left me-1"></i>Tiếp tục mua sắm</a><button class="btn btn-sm btn-link text-danger" id="clear">Xóa tất cả</button></div>
  </div></div>
  <div class="col-lg-4"><div class="card-soft bg-white p-4 position-sticky" style="top:90px">
    <h2 class="h5">Tóm tắt đơn hàng</h2>
    <div class="d-flex justify-content-between my-2"><span>Tạm tính</span><span>${money(cart.total())}</span></div>
    <div class="d-flex justify-content-between mb-2"><span>Phí vận chuyển</span><span class="text-success">Miễn phí</span></div>
    <hr><div class="d-flex justify-content-between fs-5 fw-bold mb-3"><span>Tổng cộng</span><span class="price">${money(cart.total())}</span></div>
    <a href="/checkout" class="btn btn-coral w-100 rounded-pill py-2">Tiến hành đặt hàng</a></div></div></div>`;
}
app.addEventListener("change", (e) => {
  const inp = e.target.closest("[data-qty]");
  if (!inp) return;
  const it = cart.items().find((i) => i.id === Number(inp.dataset.qty));
  let q = parseInt(inp.value) || 1;
  if (q > it.stock) { q = it.stock; toast(`Chỉ còn ${it.stock} sản phẩm trong kho.`, "warning"); }
  cart.setQty(it.id, Math.max(1, q));
  draw();
});
app.addEventListener("click", (e) => {
  const d = e.target.closest("[data-del]");
  if (d) { cart.remove(Number(d.dataset.del)); draw(); }
  if (e.target.id === "clear" && confirm("Xóa toàn bộ giỏ hàng?")) { cart.clear(); draw(); }
});
draw();
