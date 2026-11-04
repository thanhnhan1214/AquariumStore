import { $, api, esc, renderLayout, requireLogin, busy, toast } from "../core.js";

await renderLayout();
const me = await requireLogin();
const app = $("#app");
app.innerHTML = `<h1 class="h3 mb-4">Hồ sơ cá nhân</h1><div class="row g-4"><div class="col-lg-8"><form id="f" class="card-soft bg-white p-4 row g-3" novalidate>
  <div class="col-md-6"><label class="form-label">Tên đăng nhập</label><input class="form-control" value="${esc(me.username)}" disabled></div>
  <div class="col-md-6"><label class="form-label">Vai trò</label><input class="form-control" value="${esc(me.role)}" disabled></div>
  <div class="col-md-6"><label class="form-label">Họ và tên *</label><input class="form-control" name="fullName" required value="${esc(me.fullName)}"></div>
  <div class="col-md-6"><label class="form-label">Email *</label><input type="email" class="form-control" name="email" required value="${esc(me.email)}"></div>
  <div class="col-md-6"><label class="form-label">Số điện thoại</label><input class="form-control" name="phone" pattern="0\\d{9,10}" value="${esc(me.phone || "")}"></div>
  <div class="col-md-6"><label class="form-label">Địa chỉ</label><input class="form-control" name="address" value="${esc(me.address || "")}"></div>
  <div class="col-12"><hr><h2 class="h6">Đổi mật khẩu <span class="text-secondary fw-normal">(để trống nếu không đổi)</span></h2></div>
  <div class="col-md-6"><label class="form-label">Mật khẩu hiện tại</label><input type="password" class="form-control" name="currentPassword" autocomplete="current-password"></div>
  <div class="col-md-6"><label class="form-label">Mật khẩu mới</label><input type="password" class="form-control" name="newPassword" minlength="6" autocomplete="new-password"></div>
  <div class="col-12" id="err"></div><div class="col-12"><button class="btn btn-primary rounded-pill px-4" id="submit">Lưu thay đổi</button></div>
</form></div><div class="col-lg-4"><div class="card-soft bg-foam p-4"><i class="bi bi-receipt fs-2 text-ocean"></i><h2 class="h6 mt-2">Đơn hàng của bạn</h2><p class="small text-secondary">Theo dõi trạng thái và lịch sử mua hàng.</p><a href="/orders" class="btn btn-outline-primary btn-sm">Xem đơn hàng</a></div></div></div>`;
const f = $("#f");
f.onsubmit = (e) => {
  e.preventDefault();
  f.classList.add("was-validated");
  if (!f.checkValidity()) return;
  $("#err").innerHTML = "";
  busy($("#submit"), async () => {
    try {
      await api("/api/auth/profile", { method: "PUT", body: Object.fromEntries(new FormData(f)) });
      f.currentPassword.value = f.newPassword.value = "";
      f.classList.remove("was-validated");
      toast("Đã cập nhật hồ sơ.");
    } catch (err) { $("#err").innerHTML = `<div class="alert alert-danger small">${esc(err.message)}</div>`; }
  });
};
