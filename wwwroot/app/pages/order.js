import { $, api, esc, img, qs, money, fdate, statusBadge, renderLayout, requireLogin, loading, errorBox, qrUrl, BANK, busy, toast } from "../core.js";

await renderLayout();
await requireLogin();
const app = $("#app");
const id = qs("id");

async function load() {
  app.innerHTML = loading();
  try {
    if (!/^\d+$/.test(id || "")) throw new Error("Không tìm thấy đơn hàng.");
    const { order: o, items } = await api(`/api/orders/${id}`);
    const isNew = qs("new") === "1";
    const transfer = o.paymentMethod === "Chuyển khoản" && o.status !== "Đã hủy";
    app.innerHTML = `
    ${isNew ? `<div class="alert alert-success d-flex align-items-center gap-2"><i class="bi bi-check-circle-fill fs-4"></i><div><strong>Đặt hàng thành công!</strong> Cảm ơn bạn đã mua sắm tại Aquarium Store. Chúng tôi sẽ liên hệ xác nhận sớm nhất.</div></div>` : ""}
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2">
      <h1 class="h3 mb-0">Đơn hàng #${o.id} ${statusBadge(o.status)}</h1>
      <div class="d-flex gap-2 no-print"><button class="btn btn-outline-secondary btn-sm" onclick="print()"><i class="bi bi-printer me-1"></i>In hóa đơn</button>
        ${o.status === "Chờ xử lý" ? `<button class="btn btn-outline-danger btn-sm" id="cancel">Hủy đơn hàng</button>` : ""}<a href="/orders" class="btn btn-primary btn-sm">Đơn hàng của tôi</a></div>
    </div>
    <div class="row g-4">
      <div class="col-lg-${transfer ? 8 : 12}"><div class="card-soft bg-white p-4">
        <div class="row small mb-3">
          <div class="col-sm-6 mb-2"><div class="text-secondary">Người nhận</div><strong>${esc(o.recipientName || "")}</strong> · ${esc(o.recipientPhone)}</div>
          <div class="col-sm-6 mb-2"><div class="text-secondary">Ngày đặt</div>${fdate(o.orderDate)}</div>
          <div class="col-sm-6 mb-2"><div class="text-secondary">Địa chỉ</div>${esc(o.shippingAddress)}</div>
          <div class="col-sm-6 mb-2"><div class="text-secondary">Thanh toán</div>${esc(o.paymentMethod)}</div>
          ${o.note ? `<div class="col-12"><div class="text-secondary">Ghi chú</div>${esc(o.note)}</div>` : ""}
        </div>
        <table class="table"><thead class="table-light"><tr><th>Sản phẩm</th><th class="text-center">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th></tr></thead><tbody>
          ${items.map((i) => `<tr><td><div class="d-flex align-items-center gap-2"><img src="${img(i.imageUrl)}" class="thumb-sm" alt=""><a href="/product?id=${i.productId}">${esc(i.name)}</a></div></td><td class="text-center">${i.quantity}</td><td class="text-end">${money(i.unitPrice)}</td><td class="text-end">${money(i.unitPrice * i.quantity)}</td></tr>`).join("")}
        </tbody><tfoot><tr><th colspan="3" class="text-end">Tổng cộng</th><th class="text-end price fs-5">${money(o.totalAmount)}</th></tr></tfoot></table>
      </div></div>
      ${transfer ? `<div class="col-lg-4"><div class="card-soft bg-white p-4 text-center">
        <h2 class="h6">Quét mã để chuyển khoản</h2><img src="${qrUrl(o.totalAmount, `DH-${o.id}`)}" alt="Mã VietQR đơn hàng" class="img-fluid border rounded my-2">
        <div class="small text-start"><div>Ngân hàng: <strong>${BANK.bank} Bank</strong></div><div>Số tài khoản: <strong>${BANK.account}</strong></div><div>Chủ tài khoản: <strong>${BANK.name}</strong></div>
        <div>Số tiền: <strong class="price">${money(o.totalAmount)}</strong></div><div>Nội dung: <strong>DH-${o.id}</strong></div></div></div></div>` : ""}
    </div>`;
    const c = $("#cancel");
    if (c) c.onclick = () => confirm("Bạn chắc chắn muốn hủy đơn hàng này?") && busy(c, async () => {
      try { await api(`/api/orders/${o.id}/cancel`, { method: "POST" }); toast("Đã hủy đơn hàng."); history.replaceState(null, "", `/order?id=${o.id}`); load(); }
      catch (e) { toast(e.message, "danger"); }
    });
  } catch (e) { app.innerHTML = errorBox(e.message) + `<a href="/orders" class="btn btn-primary">Đơn hàng của tôi</a>`; }
}
load();
