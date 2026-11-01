using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IOrderService
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize]
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC
using System.Security.Claims; // Nạp thư viện đọc thông tin định danh nhân viên đang đăng nhập

namespace AquariumStore.Controllers.Admin // Khai báo không gian tên cho các Controller Quản trị
{ // Mở khối không gian tên AquariumStore.Controllers.Admin
    /// <summary>
    /// Controller quản lý đơn hàng (Admin/Staff): Xem danh sách, lọc theo trạng thái, xem chi tiết và duyệt/cập nhật trạng thái đơn.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")] // Cho phép Admin và Staff xử lý đơn hàng
    [Route("Admin/[controller]")] // Định tuyến URL gốc là /Admin/OrderManagement
    public class OrderManagementController : Controller // Lớp điều khiển quản lý đơn hàng
    { // Mở khối lớp OrderManagementController
        private readonly IOrderService _orderService; // Dịch vụ xử lý nghiệp vụ đơn hàng

        public OrderManagementController(IOrderService orderService) // Hàm khởi tạo nhận IOrderService qua DI
        { // Mở thân hàm khởi tạo
            _orderService = orderService; // Gán dịch vụ đơn hàng vào biến thành viên
        } // Đóng thân hàm khởi tạo

        // GET: /Admin/OrderManagement
        [HttpGet("")] // Định tuyến mặc định /Admin/OrderManagement
        [HttpGet("Index")] // Định tuyến /Admin/OrderManagement/Index
        public async Task<IActionResult> Index(string? status) // Action hiển thị danh sách đơn hàng (hỗ trợ lọc theo trạng thái)
        { // Mở thân hàm Index
            ViewBag.CurrentStatus = status; // Lưu trạng thái đang lọc để đánh dấu nút active trên giao diện

            List<Models.Order> orders; // Khai báo biến danh sách đơn hàng kết quả
            if (!string.IsNullOrEmpty(status)) // Nếu người quản trị chọn lọc theo một trạng thái cụ thể
            { // Mở khối lọc theo trạng thái
                orders = await _orderService.GetByStatusAsync(status); // Truy vấn các đơn hàng có trạng thái tương ứng
            } // Đóng khối lọc theo trạng thái
            else // Nếu xem toàn bộ đơn hàng
            { // Mở khối lấy tất cả đơn hàng
                orders = await _orderService.GetAllAsync(); // Truy vấn toàn bộ đơn hàng mới nhất lên đầu
            } // Đóng khối lấy tất cả đơn hàng

            return View("~/Views/Admin/OrderManagement/Index.cshtml", orders); // Trả về View danh sách đơn hàng Admin
        } // Đóng thân hàm Index

        // GET: /Admin/OrderManagement/Details/5
        [HttpGet("Details/{id}")] // Định tuyến GET /Admin/OrderManagement/Details/{id}
        public async Task<IActionResult> Details(int id) // Action xem chi tiết đơn hàng và các mặt hàng bên trong
        { // Mở thân hàm Details
            var order = await _orderService.GetByIdAsync(id); // Tìm đơn hàng theo mã OrderId
            if (order == null) return NotFound(); // Trả về 404 nếu đơn hàng không tồn tại
            return View("~/Views/Admin/OrderManagement/Details.cshtml", order); // Trả về View chi tiết đơn hàng Admin
        } // Đóng thân hàm Details

        // POST: /Admin/OrderManagement/UpdateStatus
        [HttpPost("UpdateStatus")] // Định tuyến POST /Admin/OrderManagement/UpdateStatus
        [ValidateAntiForgeryToken] // Bảo vệ chống giả mạo CSRF
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus) // Action cập nhật trạng thái đơn hàng
        { // Mở thân hàm UpdateStatus
            try // Thử thực hiện cập nhật trạng thái (và hoàn kho nếu hủy đơn)
            { // Mở khối try
                var staffUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Lấy mã UserId của Admin/Staff đang thao tác
                await _orderService.UpdateStatusAsync(orderId, newStatus, staffUserId); // Gọi dịch vụ đổi trạng thái và ghi log hoàn kho (nếu hủy)
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành \"{newStatus}\"."; // Thông báo thành công
            } // Đóng khối try
            catch (InvalidOperationException ex) // Bắt các lỗi nghiệp vụ nếu có
            { // Mở khối catch
                TempData["ErrorMessage"] = ex.Message; // Hiển thị thông báo lỗi
            } // Đóng khối catch

            return RedirectToAction("Details", new { id = orderId }); // Quay lại trang chi tiết của đơn hàng vừa cập nhật
        } // Đóng thân hàm UpdateStatus
    } // Đóng khối lớp OrderManagementController
} // Đóng khối không gian tên AquariumStore.Controllers.Admin
