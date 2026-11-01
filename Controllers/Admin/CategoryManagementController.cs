using AquariumStore.Models; // Nạp không gian tên chứa thực thể Category
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện ICategoryService
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize]
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC

namespace AquariumStore.Controllers.Admin // Khai báo không gian tên cho các Controller Quản trị
{ // Mở khối không gian tên AquariumStore.Controllers.Admin
    /// <summary>
    /// Controller quản lý danh mục sản phẩm (dành riêng cho Admin).
    /// </summary>
    [Authorize(Roles = "Admin")] // Chỉ tài khoản Admin cao nhất mới được thêm/sửa/ẩn danh mục
    [Route("Admin/[controller]")] // Định tuyến URL gốc là /Admin/CategoryManagement
    public class CategoryManagementController : Controller // Lớp điều khiển quản lý danh mục
    { // Mở khối lớp CategoryManagementController
        private readonly ICategoryService _categoryService; // Dịch vụ xử lý nghiệp vụ danh mục

        public CategoryManagementController(ICategoryService categoryService) // Hàm khởi tạo nhận ICategoryService qua DI
        { // Mở thân hàm khởi tạo
            _categoryService = categoryService; // Gán dịch vụ danh mục vào biến thành viên
        } // Đóng thân hàm khởi tạo

        // GET: /Admin/CategoryManagement
        [HttpGet("")] // Định tuyến mặc định /Admin/CategoryManagement
        [HttpGet("Index")] // Định tuyến /Admin/CategoryManagement/Index
        public async Task<IActionResult> Index() // Action hiển thị danh sách tất cả danh mục
        { // Mở thân hàm Index
            var categories = await _categoryService.GetAllAsync(); // Lấy toàn bộ danh mục kèm số lượng sản phẩm bên trong
            return View("~/Views/Admin/CategoryManagement/Index.cshtml", categories); // Trả về View danh sách danh mục
        } // Đóng thân hàm Index

        // GET: /Admin/CategoryManagement/Create
        [HttpGet("Create")] // Định tuyến GET /Admin/CategoryManagement/Create
        public IActionResult Create() // Action hiển thị Form thêm mới danh mục
        { // Mở thân hàm Create (GET)
            return View("~/Views/Admin/CategoryManagement/Create.cshtml", new Category()); // Trả về View tạo mới với đối tượng Category rỗng
        } // Đóng thân hàm Create (GET)

        // POST: /Admin/CategoryManagement/Create
        [HttpPost("Create")] // Định tuyến POST /Admin/CategoryManagement/Create
        [ValidateAntiForgeryToken] // Bảo vệ chống giả mạo CSRF
        public async Task<IActionResult> Create(Category category) // Action lưu danh mục mới vào CSDL
        { // Mở thân hàm Create (POST)
            if (!ModelState.IsValid) // Nếu tên danh mục bị bỏ trống hoặc vượt quá độ dài cho phép
                return View("~/Views/Admin/CategoryManagement/Create.cshtml", category); // Hiển thị lại Form kèm thông báo lỗi

            await _categoryService.CreateAsync(category); // Lưu danh mục mới vào CSDL
            TempData["SuccessMessage"] = "Thêm danh mục thành công!"; // Thông báo thành công
            return RedirectToAction("Index"); // Quay về trang danh sách danh mục
        } // Đóng thân hàm Create (POST)

        // GET: /Admin/CategoryManagement/Edit/5
        [HttpGet("Edit/{id}")] // Định tuyến GET /Admin/CategoryManagement/Edit/{id}
        public async Task<IActionResult> Edit(int id) // Action hiển thị Form chỉnh sửa danh mục
        { // Mở thân hàm Edit (GET)
            var category = await _categoryService.GetByIdAsync(id); // Tìm danh mục theo mã CategoryId
            if (category == null) return NotFound(); // Trả về 404 nếu không tồn tại
            return View("~/Views/Admin/CategoryManagement/Edit.cshtml", category); // Trả về View chỉnh sửa danh mục
        } // Đóng thân hàm Edit (GET)

        // POST: /Admin/CategoryManagement/Edit/5
        [HttpPost("Edit/{id}")] // Định tuyến POST /Admin/CategoryManagement/Edit/{id}
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> Edit(int id, Category category) // Action lưu thay đổi danh mục
        { // Mở thân hàm Edit (POST)
            if (id != category.CategoryId) return BadRequest(); // Kiểm tra mã id hợp lệ
            if (!ModelState.IsValid) // Nếu dữ liệu nhập không hợp lệ
                return View("~/Views/Admin/CategoryManagement/Edit.cshtml", category); // Trả lại Form chỉnh sửa

            await _categoryService.UpdateAsync(category); // Cập nhật danh mục xuống CSDL
            TempData["SuccessMessage"] = "Cập nhật danh mục thành công!"; // Thông báo thành công
            return RedirectToAction("Index"); // Quay về danh sách danh mục
        } // Đóng thân hàm Edit (POST)

        // POST: /Admin/CategoryManagement/ToggleActive/5
        [HttpPost("ToggleActive/{id}")] // Định tuyến POST /Admin/CategoryManagement/ToggleActive/{id}
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> ToggleActive(int id) // Action ẩn hoặc hiện danh mục sản phẩm
        { // Mở thân hàm ToggleActive
            await _categoryService.ToggleActiveAsync(id); // Đảo trạng thái IsActive của danh mục
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái danh mục."; // Thông báo thành công
            return RedirectToAction("Index"); // Tải lại trang danh sách danh mục
        } // Đóng thân hàm ToggleActive
    } // Đóng khối lớp CategoryManagementController
} // Đóng khối không gian tên AquariumStore.Controllers.Admin
