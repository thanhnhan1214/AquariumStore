import { $, api, esc, qs, renderLayout, productCard, bindAddButtons, loading, errorBox } from "../core.js";

await renderLayout("products");
const app = $("#app");
app.innerHTML = loading();
const norm = (s) => String(s).toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/đ/g, "d");

try {
  const { categories, products } = await api("/api/catalog");
  const state = { q: qs("q") || "", category: Number(qs("category")) || 0, sort: qs("sort") || "new", stock: false };
  app.innerHTML = `
  <div class="d-flex flex-wrap justify-content-between align-items-end gap-2 mb-3">
    <div><h1 class="h3 mb-1">Sản phẩm</h1><div class="text-secondary small" id="count"></div></div>
    <div class="d-flex gap-2 flex-wrap">
      <input type="search" class="form-control" id="q" placeholder="Tìm theo tên..." value="${esc(state.q)}" style="max-width:220px">
      <select class="form-select" id="sort" style="max-width:190px">
        <option value="new">Mới nhất</option><option value="best">Bán chạy</option>
        <option value="asc">Giá thấp → cao</option><option value="desc">Giá cao → thấp</option></select>
      <div class="form-check form-switch align-self-center ms-1"><input class="form-check-input" type="checkbox" id="stock"><label class="form-check-label small" for="stock">Còn hàng</label></div>
    </div>
  </div>
  <div class="d-flex gap-2 overflow-auto pb-2 mb-3" id="cats">
    <button class="btn btn-outline-primary btn-sm cat-pill" data-cat="0">Tất cả</button>
    ${categories.map((c) => `<button class="btn btn-outline-primary btn-sm cat-pill" data-cat="${c.id}">${esc(c.name)}</button>`).join("")}
  </div>
  <div class="row g-3" id="grid"></div>`;
  $("#sort").value = state.sort;

  const draw = () => {
    let list = products.filter((p) =>
      (!state.category || p.categoryId === state.category) &&
      (!state.q || norm(p.name + " " + (p.description || "")).includes(norm(state.q))) &&
      (!state.stock || p.stock > 0));
    const s = { new: () => 0, best: (a, b) => b.sold - a.sold, asc: (a, b) => a.price - b.price, desc: (a, b) => b.price - a.price }[state.sort];
    list = [...list].sort(s);
    document.querySelectorAll(".cat-pill").forEach((b) => b.classList.toggle("active", Number(b.dataset.cat) === state.category));
    $("#count").textContent = `${list.length} sản phẩm`;
    $("#grid").innerHTML = list.length ? list.map(productCard).join("")
      : `<div class="empty-state"><i class="bi bi-search"></i><p class="mt-2">Không tìm thấy sản phẩm phù hợp.</p></div>`;
    const u = new URLSearchParams();
    if (state.q) u.set("q", state.q); if (state.category) u.set("category", state.category); if (state.sort !== "new") u.set("sort", state.sort);
    history.replaceState(null, "", "/products" + (u.toString() ? "?" + u : ""));
  };
  $("#cats").onclick = (e) => { const b = e.target.closest("[data-cat]"); if (b) { state.category = Number(b.dataset.cat); draw(); } };
  $("#q").oninput = (e) => { state.q = e.target.value.trim(); draw(); };
  $("#sort").onchange = (e) => { state.sort = e.target.value; draw(); };
  $("#stock").onchange = (e) => { state.stock = e.target.checked; draw(); };
  bindAddButtons(app, products);
  draw();
} catch (e) {
  app.innerHTML = errorBox(e.message);
}
