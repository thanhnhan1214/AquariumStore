using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa các giao diện IOrderService, IProductService, IUserService
using AquariumStore.ViewModels; // Nạp không gian tên chứa DashboardViewModel
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize]
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC

namespace AquariumStore.Controllers.Admin // Khai báo không gian tên cho các Controller thuộc phân hệ Quản trị (Admin)
{ // Mở khối không gian tên AquariumStore.Controllers.Admin
    /// <summary>
    /// Controller Dashboard quản trị: Thống kê tổng quan, biểu đồ doanh thu và tỷ lệ đơn hàng.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")] // Chỉ cho phép tài khoản có vai trò Admin hoặc Staff truy cập
    [Route("Admin/[controller]")] // Định tuyến URL theo dạng /Admin/Dashboard
    public class DashboardController : Controller // Lớp điều khiển trang Tổng quan Quản trị
    { // Mở khối lớp DashboardController
        private readonly IOrderService _orderService; // Dịch vụ xử lý đơn hàng và thống kê doanh thu
        private readonly IProductService _productService; // Dịch vụ quản lý sản phẩm và kiểm tra tồn kho
        private readonly IUserService _userService; // Dịch vụ quản lý người dùng

        public DashboardController(IOrderService orderService, IProductService productService, IUserService userService) // Hàm khởi tạo nhận các dịch vụ qua DI
        { // Mở thân hàm khởi tạo
            _orderService = orderService; // Gán dịch vụ đơn hàng
            _productService = productService; // Gán dịch vụ sản phẩm
            _userService = userService; // Gán dịch vụ người dùng
        } // Đóng thân hàm khởi tạo

        // GET: /Admin hoặc /Admin/Dashboard hoặc /Admin/Dashboard/Index
        [HttpGet("/Admin")] // Hỗ trợ truy cập ngắn gọn qua đường dẫn /Admin
        [HttpGet("")] // Định tuyến mặc định cho /Admin/Dashboard
        [HttpGet("Index")] // Định tuyến bổ sung cho /Admin/Dashboard/Index
        public async Task<IActionResult> Index() // Action tổng hợp số liệu hiển thị trên Bảng điều khiển
        { // Mở thân hàm Index
            var allOrders = await _orderService.GetAllAsync(); // Lấy toàn bộ danh sách đơn hàng từ CSDL
            var statuses = new[] { "Chờ xử lý", "Đã xác nhận", "Đang giao", "Hoàn thành", "Đã hủy" }; // Danh sách 5 trạng thái đơn hàng chuẩn

            var model = new DashboardViewModel // Khởi tạo đối tượng DashboardViewModel chứa toàn bộ chỉ số
            { // Mở khối gán thuộc tính DashboardViewModel
                TotalRevenue = await _orderService.GetTotalRevenueAsync(), // Tổng doanh thu từ các đơn hàng đã "Hoàn thành"
                NewOrdersCount = await _orderService.GetNewOrdersCountAsync(), // Số lượng đơn hàng mới đang "Chờ xử lý"
                TotalProducts = (await _productService.GetActiveAsync()).Count, // Tổng số mặt hàng đang kinh doanh
                TotalCustomers = await _userService.GetCustomerCountAsync(), // Tổng số tài khoản khách hàng đang hoạt động
                LowStockProducts = await _productService.GetLowStockAsync(5), // Danh sách các sản phẩm sắp hết hàng (tồn kho <= 5)
                MonthlyRevenue = await _orderService.GetMonthlyRevenueAsync(12), // Doanh thu từng tháng trong 12 tháng gần nhất (vẽ biểu đồ cột)
                TotalOrders = allOrders.Count, // Tổng số đơn hàng trên toàn hệ thống
                RecentOrders = allOrders.Take(6).ToList(), // 6 đơn hàng mới đặt gần đây nhất
                OrdersByStatus = statuses.ToDictionary(s => s, s => allOrders.Count(o => o.Status == s)) // Đếm số đơn theo từng trạng thái (vẽ biểu đồ tròn)
            }; // Đóng khối gán thuộc tính DashboardViewModel

            return View("~/Views/Admin/Dashboard/Index.cshtml", model); // Trả về View Dashboard kèm mô hình dữ liệu thống kê
        } // Đóng thân hàm Index
    } // Đóng khối lớp DashboardController
} // Đóng khối không gian tên AquariumStore.Controllers.Admin
