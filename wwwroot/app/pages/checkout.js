import { $, api, esc, img, money, cart, renderLayout, requireLogin, busy, qrUrl, BANK } from "../core.js";

await renderLayout();
const me = await requireLogin();
const app = $("#app");
const items = cart.items();
if (!items.length) location.replace("/cart");

app.innerHTML = `<h1 class="h3 mb-4">Thanh toán</h1>
<form id="f" class="row g-4" novalidate>
  <div class="col-lg-7"><div class="card-soft bg-white p-4">
    <h2 class="h5 mb-3"><i class="bi bi-geo-alt me-1"></i>Thông tin giao hàng</h2>
    <div class="row g-3">
      <div class="col-md-6"><label class="form-label">Người nhận *</label><input class="form-control" name="recipientName" required maxlength="150" value="${esc(me.fullName)}"></div>
      <div class="col-md-6"><label class="form-label">Số điện thoại *</label><input class="form-control" name="recipientPhone" required pattern="0\\d{9,10}" value="${esc(me.phone || "")}"><div class="invalid-feedback">Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0.</div></div>
      <div class="col-12"><label class="form-label">Địa chỉ giao hàng *</label><input class="form-control" name="shippingAddress" required minlength="5" maxlength="500" value="${esc(me.address || "")}"><div class="invalid-feedback">Vui lòng nhập địa chỉ đầy đủ.</div></div>
      <div class="col-12"><label class="form-label">Ghi chú</label><textarea class="form-control" name="note" rows="2" maxlength="500" placeholder="Ví dụ: giao giờ hành chính"></textarea></div>
    </div>
    <h2 class="h5 mt-4 mb-3"><i class="bi bi-wallet2 me-1"></i>Phương thức thanh toán</h2>
    <div class="form-check border rounded p-3 ps-5 mb-2"><input class="form-check-input" type="radio" name="paymentMethod" id="pm1" value="COD" checked><label class="form-check-label w-100" for="pm1"><strong>Thanh toán khi nhận hàng (COD)</strong><div class="small text-secondary">Trả tiền mặt cho nhân viên giao hàng</div></label></div>
    <div class="form-check border rounded p-3 ps-5"><input class="form-check-input" type="radio" name="paymentMethod" id="pm2" value="Chuyển khoản"><label class="form-check-label w-100" for="pm2"><strong>Chuyển khoản ngân hàng (VietQR)</strong><div class="small text-secondary">Mã QR kèm đúng số tiền và nội dung sẽ hiển thị sau khi đặt hàng</div></label></div>
    <div id="qrPreview" class="text-center mt-3 d-none"><img src="${qrUrl(cart.total(), "THANH TOAN AQUARIUM")}" alt="Mã VietQR" width="220" class="border rounded"><div class="small text-secondary mt-1">${BANK.bank} · ${BANK.account} · ${BANK.name}</div></div>
  </div></div>
  <div class="col-lg-5"><div class="card-soft bg-white p-4">
    <h2 class="h5 mb-3">Đơn hàng (${cart.count()} sản phẩm)</h2>
    ${items.map((i) => `<div class="d-flex gap-2 align-items-center mb-2"><img src="${img(i.imageUrl)}" class="thumb-sm" alt=""><div class="flex-grow-1 small">${esc(i.name)}<div class="text-secondary">x${i.qty}</div></div><div class="small fw-semibold">${money(i.price * i.qty)}</div></div>`).join("")}
    <hr><div class="d-flex justify-content-between fs-5 fw-bold mb-3"><span>Tổng cộng</span><span class="price">${money(cart.total())}</span></div>
    <div id="err"></div>
    <button class="btn btn-coral w-100 rounded-pill py-2" id="submit">Đặt hàng</button>
    <a href="/cart" class="btn btn-link w-100 mt-1">Quay lại giỏ hàng</a></div></div>
</form>`;
const f = $("#f");
f.addEventListener("change", () => $("#qrPreview").classList.toggle("d-none", f.paymentMethod.value !== "Chuyển khoản"));
f.onsubmit = async (e) => {
  e.preventDefault();
  f.classList.add("was-validated");
  if (!f.checkValidity()) return;
  const data = Object.fromEntries(new FormData(f));
  data.items = cart.items().map((i) => ({ productId: i.id, quantity: i.qty }));
  $("#err").innerHTML = "";
  await busy($("#submit"), async () => {
    try {
      const { orderId } = await api("/api/orders", { method: "POST", body: data });
      cart.clear();
      location.href = `/order?id=${orderId}&new=1`;
    } catch (err) {
      if (err.status === 401) return (location.href = "/login?next=/checkout");
      $("#err").innerHTML = `<div class="alert alert-danger small">${esc(err.message)} <a href="/cart">Kiểm tra giỏ hàng</a></div>`;
    }
  });
};
