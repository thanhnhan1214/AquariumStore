import { $, renderLayout, BANK } from "../core.js";

await renderLayout("guide");
$("#app").innerHTML = `<div class="row justify-content-center"><div class="col-lg-9">
<h1 class="h3 mb-4">Hướng dẫn mua hàng</h1>
<div class="row g-3 mb-5">${[["1", "Chọn sản phẩm", "Tìm theo danh mục hoặc hỏi AquaBot để được gợi ý phù hợp với bể của bạn."], ["2", "Thêm vào giỏ", "Điều chỉnh số lượng trong giỏ hàng. Hệ thống tự kiểm tra tồn kho."], ["3", "Đặt hàng", "Đăng nhập, nhập thông tin giao hàng, chọn COD hoặc chuyển khoản VietQR."], ["4", "Nhận hàng", "Theo dõi trạng thái đơn trong mục Đơn hàng của tôi."]]
  .map(([n, t, d]) => `<div class="col-md-6"><div class="card-soft bg-white p-4 h-100 d-flex gap-3"><span class="badge rounded-circle bg-primary fs-5" style="width:44px;height:44px;line-height:32px">${n}</span><div><h2 class="h6">${t}</h2><p class="small text-secondary mb-0">${d}</p></div></div></div>`).join("")}</div>
<div class="card-soft bg-white p-4 mb-4"><h2 class="h5">Thanh toán</h2><ul class="mb-0"><li><strong>COD:</strong> thanh toán tiền mặt khi nhận hàng.</li>
<li><strong>Chuyển khoản VietQR:</strong> ${BANK.bank} Bank – STK <strong>${BANK.account}</strong> – ${BANK.name}. Nội dung chuyển khoản: <code>DH-[mã đơn]</code>. Mã QR có sẵn số tiền hiển thị ở trang chi tiết đơn hàng.</li></ul></div>
<div class="card-soft bg-white p-4" id="policy"><h2 class="h5">Chính sách đổi trả & bảo hành</h2><ul class="mb-0">
<li>Bảo hành sống 24 giờ với cá, tép và sinh vật cảnh: vui lòng quay video khi mở hàng.</li>
<li>Thiết bị (đèn, máy lọc...) được đổi mới trong 7 ngày nếu lỗi nhà sản xuất.</li>
<li>Bạn có thể tự hủy đơn khi đơn còn ở trạng thái <em>Chờ xử lý</em>; tồn kho sẽ được hoàn lại tự động.</li>
<li>Thông tin cá nhân chỉ được dùng để giao hàng và chăm sóc khách hàng.</li></ul></div>
</div></div>`;
