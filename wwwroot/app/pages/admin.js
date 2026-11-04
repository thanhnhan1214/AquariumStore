import { $, api, esc, img, money, fdate, statusBadge, getMe, isStaff, logout, loading, errorBox, toast, busy } from "../core.js";

const me = await getMe();
if (!me) location.replace("/login?next=/admin");
else if (!isStaff(me)) location.replace("/");
if (!me || !isStaff(me)) await new Promise(() => {});

const TABS = [["dashboard", "speedometer2", "Tổng quan"], ["orders", "receipt", "Đơn hàng"], ["products", "box-seam", "Sản phẩm"],
  ["categories", "tags", "Danh mục"], ["inventory", "boxes", "Kho hàng"], ...(me.role === "Admin" ? [["users", "people", "Người dùng"]] : [])];
$("#app").innerHTML = `<div class="d-lg-flex">
  <aside class="admin-side p-3 no-print"><a href="/" class="d-flex align-items-center gap-2 text-white text-decoration-none mb-4 px-2"><i class="bi bi-water fs-4"></i><span class="display-font fs-5">Aquarium Admin</span></a>
    <nav class="nav flex-lg-column gap-1" id="tabs">${TABS.map(([k, i, t]) => `<a class="nav-link" href="#${k}" data-tab="${k}"><i class="bi bi-${i} me-2"></i>${t}</a>`).join("")}</nav>
    <hr class="border-secondary"><div class="small text-white-50 px-2 mb-2">${esc(me.fullName)} · ${esc(me.role)}</div>
    <a class="nav-link" href="/"><i class="bi bi-shop me-2"></i>Xem cửa hàng</a><button class="nav-link btn text-start w-100" id="out"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</button></aside>
  <main class="flex-grow-1 p-3 p-md-4" id="view" style="min-width:0"></main></div>
<div class="modal fade" id="modal" tabindex="-1"><div class="modal-dialog modal-lg modal-dialog-scrollable"><div class="modal-content" id="modalContent"></div></div></div>`;
$("#out").onclick = logout;
const view = $("#view");
const modal = new bootstrap.Modal($("#modal"));
const openModal = (html) => { $("#modalContent").innerHTML = html; modal.show(); };
let chart = [];

const VIEWS = {
  async dashboard() {
    const s = await api("/api/admin/stats");
    view.innerHTML = `<h1 class="h3 mb-4">Tổng quan</h1><div class="row g-3 mb-4">
      ${[["Doanh thu (hoàn thành)", money(s.revenue), "cash-stack", "success"], ["Tổng đơn hàng", s.orderCount, "receipt", "primary"], ["Đơn chờ xử lý", s.pending, "hourglass-split", "warning"], ["Khách hàng", s.customers, "people", "info"]]
        .map(([t, v, i, c]) => `<div class="col-6 col-xl-3"><div class="stat-card bg-white p-3 d-flex gap-3 align-items-center"><span class="icon bg-${c}-subtle text-${c}"><i class="bi bi-${i}"></i></span><div><div class="small text-secondary">${t}</div><div class="fs-5 fw-bold">${v}</div></div></div></div>`).join("")}</div>
      <div class="row g-3 mb-4"><div class="col-lg-8"><div class="card-soft bg-white p-3"><h2 class="h6">Doanh thu 6 tháng gần nhất</h2><canvas id="c1" height="120"></canvas></div></div>
      <div class="col-lg-4"><div class="card-soft bg-white p-3"><h2 class="h6">Trạng thái đơn hàng</h2><canvas id="c2"></canvas></div></div></div>
      <div class="row g-3"><div class="col-lg-6"><div class="card-soft bg-white p-3"><h2 class="h6">Top sản phẩm bán chạy</h2><table class="table table-sm mb-0"><tbody>
        ${s.top.map((p) => `<tr><td><img src="${img(p.imageUrl)}" class="thumb-sm me-2" alt="">${esc(p.name)}</td><td class="text-end">${p.sold} đã bán</td><td class="text-end">${money(p.revenue)}</td></tr>`).join("")}</tbody></table></div></div>
      <div class="col-lg-6"><div class="card-soft bg-white p-3 mb-3"><h2 class="h6">Đơn hàng mới</h2><table class="table table-sm mb-0"><tbody>
        ${s.recent.map((o) => `<tr><td><a href="#orders/${o.id}">#${o.id}</a></td><td>${esc(o.customer)}</td><td>${money(o.totalAmount)}</td><td>${statusBadge(o.status)}</td></tr>`).join("")}</tbody></table></div>
        <div class="card-soft bg-white p-3"><h2 class="h6 text-danger"><i class="bi bi-exclamation-triangle me-1"></i>Sắp hết hàng (≤ 10)</h2>
        ${s.lowStock.length ? s.lowStock.map((p) => `<div class="d-flex justify-content-between small border-bottom py-1"><span>${esc(p.name)}</span><span class="badge bg-${p.stock ? "warning text-dark" : "danger"}">${p.stock}</span></div>`).join("") : `<div class="small text-secondary">Tồn kho ổn định.</div>`}</div></div></div>`;
    chart.forEach((c) => c.destroy());
    if (window.Chart) chart = [
      new Chart($("#c1"), { type: "bar", data: { labels: s.monthly.map((m) => m.label), datasets: [{ label: "Doanh thu (đ)", data: s.monthly.map((m) => m.revenue), backgroundColor: "#11a3a3", borderRadius: 6 }] },
        options: { plugins: { legend: { display: false } }, scales: { y: { ticks: { callback: (v) => (v / 1e6).toLocaleString("vi-VN") + "tr" } } } } }),
      new Chart($("#c2"), { type: "doughnut", data: { labels: s.byStatus.map((x) => x.status), datasets: [{ data: s.byStatus.map((x) => x.count), backgroundColor: ["#0a5d7d", "#11a3a3", "#f4b740", "#ff6b4a", "#8fa6ad"] }] }, options: { plugins: { legend: { position: "bottom" } } } }),
    ];
  },

  async orders(id) {
    if (id) return orderDetail(id);
    const status = new URLSearchParams(location.hash.split("?")[1]).get("status") || "";
    const d = await api(`/api/admin/orders${status ? `?status=${encodeURIComponent(status)}` : ""}`);
    view.innerHTML = `<div class="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2"><h1 class="h3 mb-0">Đơn hàng</h1>
      <select class="form-select" style="max-width:220px" id="st"><option value="">Tất cả trạng thái</option>${d.statuses.map((s) => `<option ${s === status ? "selected" : ""}>${s}</option>`).join("")}</select></div>
      <div class="card-soft bg-white table-responsive"><table class="table table-hover mb-0"><thead class="table-light"><tr><th>Mã</th><th>Khách hàng</th><th>Ngày đặt</th><th>Thanh toán</th><th>Tổng tiền</th><th>Trạng thái</th><th></th></tr></thead><tbody>
      ${d.orders.map((o) => `<tr><td class="fw-semibold">#${o.id}</td><td>${esc(o.customer)}<div class="small text-secondary">${esc(o.recipientPhone)}</div></td><td>${fdate(o.orderDate)}</td><td>${esc(o.paymentMethod)}</td><td>${money(o.totalAmount)}</td><td>${statusBadge(o.status)}</td><td><a class="btn btn-sm btn-outline-primary" href="#orders/${o.id}">Xử lý</a></td></tr>`).join("") || `<tr><td colspan="7" class="text-center text-secondary py-4">Không có đơn hàng.</td></tr>`}
      </tbody></table></div>`;
    $("#st").onchange = (e) => (location.hash = e.target.value ? `orders?status=${encodeURIComponent(e.target.value)}` : "orders");
  },

  async products() {
    const [{ products }, { categories }] = await Promise.all([api("/api/admin/products"), api("/api/admin/categories")]);
    view.innerHTML = `<div class="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap"><h1 class="h3 mb-0">Sản phẩm</h1>
      <div class="d-flex gap-2"><input type="search" class="form-control" id="q" placeholder="Tìm sản phẩm..."><button class="btn btn-primary text-nowrap" id="add"><i class="bi bi-plus-lg me-1"></i>Thêm mới</button></div></div>
      <div class="card-soft bg-white table-responsive"><table class="table table-hover mb-0"><thead class="table-light"><tr><th>Sản phẩm</th><th>Danh mục</th><th class="text-end">Giá</th><th class="text-end">Tồn</th><th>Trạng thái</th><th></th></tr></thead><tbody id="rows"></tbody></table></div>`;
    const draw = (q = "") => ($("#rows").innerHTML = products.filter((p) => p.name.toLowerCase().includes(q.toLowerCase())).map((p) => `<tr>
      <td><div class="d-flex align-items-center gap-2"><img src="${img(p.imageUrl)}" class="thumb-sm" alt=""><span>${esc(p.name)}</span></div></td><td>${esc(p.categoryName)}</td>
      <td class="text-end">${money(p.price)}</td><td class="text-end"><span class="badge bg-${p.stock > 10 ? "success" : p.stock > 0 ? "warning text-dark" : "danger"}">${p.stock}</span></td>
      <td>${p.isActive ? `<span class="badge bg-success-subtle text-success">Đang bán</span>` : `<span class="badge bg-secondary">Ngừng bán</span>`}</td>
      <td class="text-nowrap"><button class="btn btn-sm btn-outline-primary" data-edit="${p.id}" aria-label="Sửa"><i class="bi bi-pencil"></i></button> <button class="btn btn-sm btn-outline-danger" data-del="${p.id}" aria-label="Xóa"><i class="bi bi-trash"></i></button></td></tr>`).join(""));
    draw();
    $("#q").oninput = (e) => draw(e.target.value);
    $("#add").onclick = () => productForm(null, categories);
    view.onclick = async (e) => {
      const ed = e.target.closest("[data-edit]"), del = e.target.closest("[data-del]");
      if (ed) productForm(products.find((p) => p.id === Number(ed.dataset.edit)), categories);
      if (del && confirm("Xóa sản phẩm này? Sản phẩm đã có đơn hàng sẽ được chuyển sang Ngừng bán.")) {
        try { const r = await api(`/api/admin/products/${del.dataset.del}`, { method: "DELETE" }); toast(r.softDeleted ? "Sản phẩm đã có lịch sử bán nên được chuyển sang Ngừng bán." : "Đã xóa sản phẩm."); route(); }
        catch (err) { toast(err.message, "danger"); }
      }
    };
  },

  async categories() {
    const { categories } = await api("/api/admin/categories");
    view.innerHTML = `<div class="d-flex justify-content-between align-items-center mb-3"><h1 class="h3 mb-0">Danh mục</h1><button class="btn btn-primary" id="add"><i class="bi bi-plus-lg me-1"></i>Thêm danh mục</button></div>
      <div class="card-soft bg-white table-responsive"><table class="table table-hover mb-0"><thead class="table-light"><tr><th>#</th><th>Tên danh mục</th><th>Mô tả</th><th class="text-end">Số SP</th><th>Trạng thái</th><th></th></tr></thead><tbody>
      ${categories.map((c) => `<tr><td>${c.id}</td><td class="fw-semibold">${esc(c.name)}</td><td class="small text-secondary">${esc(c.description || "")}</td><td class="text-end">${c.productCount}</td>
        <td>${c.isActive ? `<span class="badge bg-success-subtle text-success">Hiển thị</span>` : `<span class="badge bg-secondary">Đang ẩn</span>`}</td>
        <td class="text-nowrap"><button class="btn btn-sm btn-outline-primary" data-edit="${c.id}" aria-label="Sửa"><i class="bi bi-pencil"></i></button> <button class="btn btn-sm btn-outline-danger" data-del="${c.id}" aria-label="Xóa"><i class="bi bi-trash"></i></button></td></tr>`).join("")}</tbody></table></div>`;
    const form = (c) => {
      openModal(`<form id="cf"><div class="modal-header"><h5 class="modal-title">${c ? "Sửa" : "Thêm"} danh mục</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <div class="modal-body"><div class="mb-3"><label class="form-label">Tên danh mục *</label><input class="form-control" name="name" required maxlength="200" value="${esc(c?.name || "")}"></div>
        <div class="mb-3"><label class="form-label">Mô tả</label><textarea class="form-control" name="description" maxlength="500" rows="3">${esc(c?.description || "")}</textarea></div>
        <div class="form-check form-switch"><input class="form-check-input" type="checkbox" name="isActive" id="ca" ${c?.isActive === false ? "" : "checked"}><label class="form-check-label" for="ca">Hiển thị trên cửa hàng</label></div><div id="err" class="mt-3"></div></div>
        <div class="modal-footer"><button type="button" class="btn btn-light" data-bs-dismiss="modal">Hủy</button><button class="btn btn-primary" id="save">Lưu</button></div></form>`);
      $("#cf").onsubmit = (e) => { e.preventDefault(); const f = e.target;
        busy($("#save"), async () => { try {
          await api(`/api/admin/categories${c ? `/${c.id}` : ""}`, { method: c ? "PUT" : "POST", body: { name: f.name.value, description: f.description.value, isActive: f.isActive.checked } });
          modal.hide(); toast("Đã lưu danh mục."); route();
        } catch (err) { $("#err").innerHTML = `<div class="alert alert-danger small mb-0">${esc(err.message)}</div>`; } }); };
    };
    $("#add").onclick = () => form(null);
    view.onclick = async (e) => {
      const ed = e.target.closest("[data-edit]"), del = e.target.closest("[data-del]");
      if (ed) form(categories.find((c) => c.id === Number(ed.dataset.edit)));
      if (del && confirm("Xóa danh mục này?")) {
        try { await api(`/api/admin/categories/${del.dataset.del}`, { method: "DELETE" }); toast("Đã xóa danh mục."); route(); } catch (err) { toast(err.message, "danger"); }
      }
    };
  },

  async inventory() {
    const [{ logs }, { products }] = await Promise.all([api("/api/admin/inventory"), api("/api/admin/products")]);
    view.innerHTML = `<h1 class="h3 mb-4">Kho hàng</h1><div class="row g-4"><div class="col-lg-4"><form class="card-soft bg-white p-4" id="imp">
      <h2 class="h6 mb-3"><i class="bi bi-box-arrow-in-down me-1"></i>Nhập kho</h2>
      <div class="mb-3"><label class="form-label">Sản phẩm *</label><select class="form-select" name="productId" required><option value="">-- Chọn sản phẩm --</option>${products.map((p) => `<option value="${p.id}">${esc(p.name)} (tồn ${p.stock})</option>`).join("")}</select></div>
      <div class="mb-3"><label class="form-label">Số lượng nhập *</label><input type="number" class="form-control" name="quantity" min="1" max="100000" required></div>
      <div class="mb-3"><label class="form-label">Lý do / Nhà cung cấp</label><input class="form-control" name="reason" maxlength="400" placeholder="Nhập hàng từ nhà cung cấp"></div>
      <button class="btn btn-primary w-100" id="save">Nhập kho</button></form></div>
      <div class="col-lg-8"><div class="card-soft bg-white table-responsive"><table class="table table-sm table-hover mb-0"><thead class="table-light"><tr><th>Thời gian</th><th>Sản phẩm</th><th class="text-end">Biến động</th><th>Lý do</th><th>Người thực hiện</th></tr></thead><tbody>
      ${logs.map((l) => `<tr><td class="small text-nowrap">${fdate(l.createdAt)}</td><td>${esc(l.productName)}</td><td class="text-end fw-semibold text-${l.quantityChange > 0 ? "success" : "danger"}">${l.quantityChange > 0 ? "+" : ""}${l.quantityChange}</td><td class="small">${esc(l.reason)}</td><td class="small">${esc(l.userName)}</td></tr>`).join("")}</tbody></table></div></div></div>`;
    $("#imp").onsubmit = (e) => { e.preventDefault(); const f = e.target;
      busy($("#save"), async () => { try {
        const { product } = await api("/api/admin/inventory", { method: "POST", body: Object.fromEntries(new FormData(f)) });
        toast(`Đã nhập kho. "${product.name}" hiện tồn ${product.stock}.`); route();
      } catch (err) { toast(err.message, "danger"); } }); };
  },

  async users() {
    const { users, roles } = await api("/api/admin/users");
    view.innerHTML = `<h1 class="h3 mb-4">Người dùng</h1><div class="card-soft bg-white table-responsive"><table class="table table-hover mb-0"><thead class="table-light"><tr><th>#</th><th>Tài khoản</th><th>Liên hệ</th><th>Vai trò</th><th class="text-end">Đơn</th><th>Trạng thái</th></tr></thead><tbody>
      ${users.map((u) => { const self = u.id === me.id; return `<tr><td>${u.id}</td><td><div class="fw-semibold">${esc(u.fullName)}</div><div class="small text-secondary">@${esc(u.username)} · ${fdate(u.createdAt, false)}</div></td>
        <td class="small">${esc(u.email)}<br>${esc(u.phone || "")}</td>
        <td><select class="form-select form-select-sm" data-role="${u.id}" ${self ? "disabled" : ""}>${roles.map((r) => `<option ${r === u.role ? "selected" : ""}>${r}</option>`).join("")}</select></td>
        <td class="text-end">${u.orderCount}</td>
        <td><div class="form-check form-switch"><input class="form-check-input" type="checkbox" data-active="${u.id}" ${u.isActive ? "checked" : ""} ${self ? "disabled" : ""} aria-label="Hoạt động"><span class="small">${u.isActive ? "Hoạt động" : "Đã khóa"}</span></div></td></tr>`; }).join("")}</tbody></table></div>`;
    view.onchange = async (e) => {
      const r = e.target.closest("[data-role]"), a = e.target.closest("[data-active]");
      try {
        if (r) await api(`/api/admin/users/${r.dataset.role}`, { method: "PUT", body: { role: r.value } });
        if (a) await api(`/api/admin/users/${a.dataset.active}`, { method: "PUT", body: { isActive: a.checked } });
        toast("Đã cập nhật người dùng.");
      } catch (err) { toast(err.message, "danger"); }
      route();
    };
  },
};

async function orderDetail(id) {
  const { order: o, items, statuses } = await api(`/api/admin/orders/${id}`);
  const locked = o.status === "Đã hủy" || o.status === "Hoàn thành";
  view.innerHTML = `<a href="#orders" class="small"><i class="bi bi-arrow-left"></i> Danh sách đơn hàng</a>
    <div class="d-flex justify-content-between align-items-center my-3 flex-wrap gap-2"><h1 class="h3 mb-0">Đơn hàng #${o.id} ${statusBadge(o.status)}</h1>
      <div class="d-flex gap-2 no-print"><select class="form-select" id="st" ${locked ? "disabled" : ""}>${statuses.map((s) => `<option ${s === o.status ? "selected" : ""}>${s}</option>`).join("")}</select><button class="btn btn-primary text-nowrap" id="save" ${locked ? "disabled" : ""}>Cập nhật</button><button class="btn btn-outline-secondary" onclick="print()" aria-label="In"><i class="bi bi-printer"></i></button></div></div>
    ${locked ? `<div class="alert alert-light small">Đơn hàng đã ${o.status === "Đã hủy" ? "hủy (tồn kho đã được hoàn lại)" : "hoàn thành"} nên không thể thay đổi trạng thái.</div>` : ""}
    <div class="row g-3"><div class="col-lg-4"><div class="card-soft bg-white p-3 small">
      <div class="mb-2"><div class="text-secondary">Khách hàng</div><strong>${esc(o.customer)}</strong><br>${esc(o.email)}</div>
      <div class="mb-2"><div class="text-secondary">Người nhận</div>${esc(o.recipientName || "")} · ${esc(o.recipientPhone)}</div>
      <div class="mb-2"><div class="text-secondary">Địa chỉ</div>${esc(o.shippingAddress)}</div>
      <div class="mb-2"><div class="text-secondary">Ngày đặt</div>${fdate(o.orderDate)}</div>
      <div class="mb-2"><div class="text-secondary">Thanh toán</div>${esc(o.paymentMethod)}</div>${o.note ? `<div><div class="text-secondary">Ghi chú</div>${esc(o.note)}</div>` : ""}</div></div>
    <div class="col-lg-8"><div class="card-soft bg-white p-3 table-responsive"><table class="table mb-0"><thead><tr><th>Sản phẩm</th><th class="text-center">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th></tr></thead><tbody>
      ${items.map((i) => `<tr><td><img src="${img(i.imageUrl)}" class="thumb-sm me-2" alt="">${esc(i.name)}</td><td class="text-center">${i.quantity}</td><td class="text-end">${money(i.unitPrice)}</td><td class="text-end">${money(i.unitPrice * i.quantity)}</td></tr>`).join("")}
      </tbody><tfoot><tr><th colspan="3" class="text-end">Tổng cộng</th><th class="text-end price">${money(o.totalAmount)}</th></tr></tfoot></table></div></div></div>`;
  const save = $("#save");
  save.onclick = () => {
    const status = $("#st").value;
    if (status === "Đã hủy" && !confirm("Hủy đơn này? Tồn kho sẽ được hoàn lại.")) return;
    busy(save, async () => { try { await api(`/api/admin/orders/${id}/status`, { method: "PUT", body: { status } }); toast("Đã cập nhật trạng thái."); route(); } catch (e) { toast(e.message, "danger"); } });
  };
}

function productForm(p, categories) {
  openModal(`<form id="pf" novalidate><div class="modal-header"><h5 class="modal-title">${p ? "Sửa" : "Thêm"} sản phẩm</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
    <div class="modal-body row g-3">
      <div class="col-md-8"><label class="form-label">Tên sản phẩm *</label><input class="form-control" name="name" required maxlength="300" value="${esc(p?.name || "")}"></div>
      <div class="col-md-4"><label class="form-label">Danh mục *</label><select class="form-select" name="categoryId" required>${categories.map((c) => `<option value="${c.id}" ${c.id === p?.categoryId ? "selected" : ""}>${esc(c.name)}</option>`).join("")}</select></div>
      <div class="col-md-6"><label class="form-label">Giá bán (đ) *</label><input type="number" class="form-control" name="price" min="0" step="1000" required value="${p?.price ?? ""}"></div>
      <div class="col-md-6"><label class="form-label">Tồn kho ${p ? "" : "ban đầu"}</label><input type="number" class="form-control" name="stock" min="0" value="${p?.stock ?? 0}" ${p ? "disabled" : ""}>${p ? `<div class="form-text">Thay đổi tồn kho tại mục Kho hàng.</div>` : ""}</div>
      <div class="col-12"><label class="form-label">Mô tả</label><textarea class="form-control" name="description" rows="4" maxlength="2000">${esc(p?.description || "")}</textarea></div>
      <div class="col-12"><label class="form-label">Ảnh sản phẩm</label><div class="d-flex gap-3 align-items-center">
        <img id="prev" src="${img(p?.imageUrl)}" class="rounded img-cover" width="90" height="90" alt="">
        <div class="flex-grow-1"><input type="file" class="form-control mb-2" id="file" accept="image/jpeg,image/png,image/webp,image/gif"><input class="form-control form-control-sm" name="imageUrl" placeholder="hoặc dán đường dẫn ảnh" value="${esc(p?.imageUrl || "")}"></div></div></div>
      <div class="col-12"><div class="form-check form-switch"><input class="form-check-input" type="checkbox" name="isActive" id="pa" ${p?.isActive === false ? "" : "checked"}><label class="form-check-label" for="pa">Đang kinh doanh</label></div></div>
      <div class="col-12" id="err"></div></div>
    <div class="modal-footer"><button type="button" class="btn btn-light" data-bs-dismiss="modal">Hủy</button><button class="btn btn-primary" id="save">Lưu sản phẩm</button></div></form>`);
  const f = $("#pf");
  f.imageUrl.oninput = () => ($("#prev").src = f.imageUrl.value || img(""));
  $("#file").onchange = async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    const fd = new FormData(); fd.append("file", file);
    try { const { url } = await api("/api/images", { method: "POST", body: fd }); f.imageUrl.value = url; $("#prev").src = url; toast("Đã tải ảnh lên."); }
    catch (err) { toast(err.message, "danger"); e.target.value = ""; }
  };
  f.onsubmit = (e) => {
    e.preventDefault();
    f.classList.add("was-validated");
    if (!f.checkValidity()) return;
    const body = { name: f.name.value, categoryId: f.categoryId.value, price: f.price.value, stock: f.stock.value, description: f.description.value, imageUrl: f.imageUrl.value, isActive: f.isActive.checked };
    busy($("#save"), async () => { try {
      await api(`/api/admin/products${p ? `/${p.id}` : ""}`, { method: p ? "PUT" : "POST", body });
      modal.hide(); toast("Đã lưu sản phẩm."); route();
    } catch (err) { $("#err").innerHTML = `<div class="alert alert-danger small mb-0">${esc(err.message)}</div>`; } });
  };
}

async function route() {
  const [path] = location.hash.slice(1).split("?");
  let [tab, id] = path.split("/");
  if (!TABS.some(([k]) => k === tab)) tab = "dashboard";
  document.querySelectorAll("[data-tab]").forEach((a) => a.classList.toggle("active", a.dataset.tab === tab));
  view.onclick = view.onchange = null;
  view.innerHTML = loading();
  try { await VIEWS[tab](id); } catch (e) {
    if (e.status === 401) return location.replace("/login?next=/admin");
    view.innerHTML = errorBox(e.message);
  }
}
window.addEventListener("hashchange", route);
route();
