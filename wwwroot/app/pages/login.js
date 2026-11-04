import { $, api, esc, qs, renderLayout, busy, getMe, isStaff } from "../core.js";

const me = await renderLayout();
const safeNext = () => { const n = qs("next") || ""; return n.startsWith("/") && !n.startsWith("//") ? n : null; };
if (me) location.replace(safeNext() || (isStaff(me) ? "/admin" : "/"));
const app = $("#app");
app.innerHTML = `<div class="row justify-content-center"><div class="col-md-6 col-lg-5"><div class="card-soft bg-white p-4 p-md-5">
  <h1 class="h3 text-center mb-1">Đăng nhập</h1><p class="text-center text-secondary mb-4">Chào mừng bạn quay lại Aquarium Store</p>
  <form id="f"><div class="mb-3"><label class="form-label">Tên đăng nhập hoặc Email</label><input class="form-control" name="username" required autocomplete="username"></div>
  <div class="mb-3"><label class="form-label">Mật khẩu</label><input type="password" class="form-control" name="password" required autocomplete="current-password"></div>
  <div id="err"></div><button class="btn btn-primary w-100 rounded-pill py-2" id="submit">Đăng nhập</button></form>
  <p class="text-center mt-3 mb-0">Chưa có tài khoản? <a href="/register${qs("next") ? `?next=${encodeURIComponent(qs("next"))}` : ""}">Đăng ký ngay</a></p>
  <details class="mt-4 small text-secondary"><summary>Tài khoản dùng thử</summary><ul class="mb-0 mt-2">
    <li>Quản trị: <code>admin</code> / <code>Admin@123</code></li><li>Nhân viên: <code>staff</code> / <code>Staff@123</code></li><li>Khách hàng: <code>customer</code> / <code>Customer@123</code></li></ul></details>
</div></div></div>`;
$("#f").onsubmit = (e) => {
  e.preventDefault();
  const data = Object.fromEntries(new FormData(e.target));
  $("#err").innerHTML = "";
  busy($("#submit"), async () => {
    try {
      const { user } = await api("/api/auth/login", { method: "POST", body: data });
      location.href = safeNext() || (isStaff(user) ? "/admin" : "/");
    } catch (err) { $("#err").innerHTML = `<div class="alert alert-danger small">${esc(err.message)}</div>`; }
  });
};
