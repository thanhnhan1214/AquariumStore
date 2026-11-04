import { $, api, money, fdate, statusBadge, renderLayout, requireLogin, loading, errorBox } from "../core.js";

await renderLayout();
await requireLogin();
const app = $("#app");
app.innerHTML = loading();
try {
  const { orders } = await api("/api/orders");
  app.innerHTML = `<h1 class="h3 mb-4">Đơn hàng của tôi</h1>` + (orders.length ? `<div class="card-soft bg-white table-responsive"><table class="table table-hover mb-0">
    <thead class="table-light"><tr><th>Mã đơn</th><th>Ngày đặt</th><th>Số SP</th><th>Thanh toán</th><th>Tổng tiền</th><th>Trạng thái</th><th></th></tr></thead><tbody>
    ${orders.map((o) => `<tr><td class="fw-semibold">#${o.id}</td><td>${fdate(o.orderDate)}</td><td>${o.itemCount}</td><td>${o.paymentMethod}</td><td class="price">${money(o.totalAmount)}</td><td>${statusBadge(o.status)}</td>
    <td><a class="btn btn-sm btn-outline-primary" href="/order?id=${o.id}">Chi tiết</a></td></tr>`).join("")}</tbody></table></div>`
    : `<div class="empty-state"><i class="bi bi-receipt"></i><p class="mt-2">Bạn chưa có đơn hàng nào.</p><a href="/products" class="btn btn-primary rounded-pill">Mua sắm ngay</a></div>`);
} catch (e) { app.innerHTML = errorBox(e.message); }
