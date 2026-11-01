using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IUserService
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize]
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC
using System.Security.Claims; // Nạp thư viện đọc thông tin định danh tài khoản đang đăng nhập

namespace AquariumStore.Controllers.Admin // Khai báo không gian tên cho các Controller Quản trị
{ // Mở khối không gian tên AquariumStore.Controllers.Admin
    /// <summary>
    /// Controller quản lý người dùng (chỉ dành cho Admin): Xem danh sách tài khoản, khóa hoặc mở khóa tài khoản.
    /// </summary>
    [Authorize(Roles = "Admin")] // Chỉ Quản trị viên cao nhất (Admin) mới có quyền quản lý người dùng
    [Route("Admin/[controller]")] // Định tuyến URL gốc là /Admin/UserManagement
    public class UserManagementController : Controller // Lớp điều khiển quản lý người dùng
    { // Mở khối lớp UserManagementController
        private readonly IUserService _userService; // Dịch vụ xử lý nghiệp vụ người dùng

        public UserManagementController(IUserService userService) // Hàm khởi tạo nhận IUserService qua DI
        { // Mở thân hàm khởi tạo
            _userService = userService; // Gán dịch vụ người dùng vào biến thành viên
        } // Đóng thân hàm khởi tạo

        // GET: /Admin/UserManagement
        [HttpGet("")] // Định tuyến mặc định /Admin/UserManagement
        [HttpGet("Index")] // Định tuyến /Admin/UserManagement/Index
        public async Task<IActionResult> Index() // Action hiển thị danh sách toàn bộ tài khoản người dùng
        { // Mở thân hàm Index
            var users = await _userService.GetAllAsync(); // Truy vấn toàn bộ người dùng kèm vai trò (Role)
            return View("~/Views/Admin/UserManagement/Index.cshtml", users); // Trả về View quản lý người dùng
        } // Đóng thân hàm Index

        // POST: /Admin/UserManagement/ToggleActive/5
        [HttpPost("ToggleActive/{id}")] // Định tuyến POST /Admin/UserManagement/ToggleActive/{id}
        [ValidateAntiForgeryToken] // Bảo vệ chống giả mạo CSRF
        public async Task<IActionResult> ToggleActive(int id) // Action khóa hoặc mở khóa tài khoản người dùng
        { // Mở thân hàm ToggleActive
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Lấy mã UserId của Admin đang đăng nhập
            if (id == currentAdminId) // Ngăn chặn Admin tự khóa chính tài khoản đang sử dụng của mình
            { // Mở khối cảnh báo tự khóa tài khoản
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản Quản trị viên đang đăng nhập."; // Hiển thị cảnh báo an toàn
                return RedirectToAction("Index"); // Quay lại danh sách người dùng
            } // Đóng khối cảnh báo tự khóa tài khoản

            await _userService.ToggleActiveAsync(id); // Đảo trạng thái hoạt động (IsActive) của tài khoản mục tiêu
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái hoạt động của tài khoản."; // Thông báo thành công
            return RedirectToAction("Index"); // Tải lại danh sách người dùng
        } // Đóng thân hàm ToggleActive
    } // Đóng khối lớp UserManagementController
} // Đóng khối không gian tên AquariumStore.Controllers.Admin
