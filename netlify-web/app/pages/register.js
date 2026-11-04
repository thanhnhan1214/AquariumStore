import { $, api, esc, qs, renderLayout, busy } from "../core.js";

const me = await renderLayout();
if (me) location.replace("/");
const app = $("#app");
app.innerHTML = `<div class="row justify-content-center"><div class="col-md-8 col-lg-6"><div class="card-soft bg-white p-4 p-md-5">
  <h1 class="h3 text-center mb-4">Tạo tài khoản</h1>
  <form id="f" class="row g-3" novalidate>
    <div class="col-md-6"><label class="form-label">Tên đăng nhập *</label><input class="form-control" name="username" required pattern="[a-z0-9_.]{3,50}" autocomplete="username"><div class="form-text">3-50 ký tự: chữ thường không dấu, số, _ hoặc .</div></div>
    <div class="col-md-6"><label class="form-label">Họ và tên *</label><input class="form-control" name="fullName" required maxlength="150"></div>
    <div class="col-md-6"><label class="form-label">Email *</label><input type="email" class="form-control" name="email" required autocomplete="email"></div>
    <div class="col-md-6"><label class="form-label">Số điện thoại</label><input class="form-control" name="phone" pattern="0\\d{9,10}"><div class="invalid-feedback">10-11 chữ số, bắt đầu bằng 0.</div></div>
    <div class="col-12"><label class="form-label">Địa chỉ</label><input class="form-control" name="address" maxlength="500"></div>
    <div class="col-md-6"><label class="form-label">Mật khẩu *</label><input type="password" class="form-control" name="password" required minlength="6" autocomplete="new-password"><div class="invalid-feedback">Tối thiểu 6 ký tự.</div></div>
    <div class="col-md-6"><label class="form-label">Nhập lại mật khẩu *</label><input type="password" class="form-control" name="confirmPassword" required minlength="6" autocomplete="new-password"></div>
    <div class="col-12" id="err"></div>
    <div class="col-12"><button class="btn btn-primary w-100 rounded-pill py-2" id="submit">Đăng ký</button></div>
  </form><p class="text-center mt-3 mb-0">Đã có tài khoản? <a href="/login">Đăng nhập</a></p></div></div></div>`;
const f = $("#f");
f.onsubmit = (e) => {
  e.preventDefault();
  f.username.value = f.username.value.trim().toLowerCase();
  f.classList.add("was-validated");
  if (!f.checkValidity()) return;
  const data = Object.fromEntries(new FormData(f));
  if (data.password !== data.confirmPassword) return ($("#err").innerHTML = `<div class="alert alert-danger small">Mật khẩu xác nhận không khớp.</div>`);
  $("#err").innerHTML = "";
  busy($("#submit"), async () => {
    try {
      await api("/api/auth/register", { method: "POST", body: data });
      const n = qs("next") || "";
      location.href = n.startsWith("/") && !n.startsWith("//") ? n : "/";
    } catch (err) { $("#err").innerHTML = `<div class="alert alert-danger small">${esc(err.message)}</div>`; }
  });
};
