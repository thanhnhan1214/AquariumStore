using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa IInventoryService và IProductService
using AquariumStore.ViewModels; // Nạp không gian tên chứa InventoryImportViewModel
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize]
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc.Rendering; // Nạp thư viện SelectList phục vụ Dropdown chọn sản phẩm
using System.Security.Claims; // Nạp thư viện đọc thông tin người dùng từ Claims

namespace AquariumStore.Controllers.Admin // Khai báo không gian tên cho các Controller Quản trị
{ // Mở khối không gian tên AquariumStore.Controllers.Admin
    /// <summary>
    /// Controller quản lý kho hàng (Admin/Staff): Lập phiếu nhập hàng và theo dõi lịch sử biến động tồn kho.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")] // Cho phép Admin và Staff thao tác kho hàng
    [Route("Admin/[controller]")] // Định tuyến URL gốc là /Admin/Inventory
    public class InventoryController : Controller // Lớp điều khiển nghiệp vụ kho hàng
    { // Mở khối lớp InventoryController
        private readonly IInventoryService _inventoryService; // Dịch vụ xử lý nhập kho và nhật ký kho
        private readonly IProductService _productService; // Dịch vụ truy xuất danh sách sản phẩm

        public InventoryController(IInventoryService inventoryService, IProductService productService) // Hàm khởi tạo nhận các dịch vụ qua DI
        { // Mở thân hàm khởi tạo
            _inventoryService = inventoryService; // Gán dịch vụ kho hàng
            _productService = productService; // Gán dịch vụ sản phẩm
        } // Đóng thân hàm khởi tạo

        // GET: /Admin/Inventory
        [HttpGet("")] // Định tuyến mặc định /Admin/Inventory
        [HttpGet("Index")] // Định tuyến /Admin/Inventory/Index
        public async Task<IActionResult> Index() // Action hiển thị bảng lịch sử biến động tồn kho (Nhập / Xuất / Hoàn kho)
        { // Mở thân hàm Index
            var logs = await _inventoryService.GetAllLogsAsync(); // Lấy toàn bộ bản ghi nhật ký kho mới nhất lên đầu
            return View("~/Views/Admin/Inventory/Index.cshtml", logs); // Trả về View nhật ký kho hàng
        } // Đóng thân hàm Index

        // GET: /Admin/Inventory/Import
        [HttpGet("Import")] // Định tuyến GET /Admin/Inventory/Import
        public async Task<IActionResult> Import() // Action hiển thị Form lập phiếu nhập thêm hàng vào kho
        { // Mở thân hàm Import (GET)
            await PopulateProductDropdown(); // Nạp danh sách sản phẩm kèm số lượng tồn hiện tại vào Dropdown
            return View("~/Views/Admin/Inventory/Import.cshtml", new InventoryImportViewModel()); // Trả về View phiếu nhập kho
        } // Đóng thân hàm Import (GET)

        // POST: /Admin/Inventory/Import
        [HttpPost("Import")] // Định tuyến POST /Admin/Inventory/Import
        [ValidateAntiForgeryToken] // Bảo vệ chống giả mạo CSRF
        public async Task<IActionResult> Import(InventoryImportViewModel model) // Action xử lý lưu phiếu nhập kho
        { // Mở thân hàm Import (POST)
            if (!ModelState.IsValid) // Nếu chưa chọn sản phẩm hoặc số lượng nhập <= 0
            { // Mở khối trả lại Form khi dữ liệu lỗi
                await PopulateProductDropdown(); // Nạp lại Dropdown sản phẩm
                return View("~/Views/Admin/Inventory/Import.cshtml", model); // Hiển thị lại Form kèm thông báo lỗi
            } // Đóng khối trả lại Form khi dữ liệu lỗi

            try // Thử thực hiện cộng tồn kho và ghi nhật ký trong Transaction
            { // Mở khối try
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Lấy mã UserId của nhân viên/Admin đang nhập kho
                await _inventoryService.ImportStockAsync(model.ProductId, model.Quantity, model.Reason.Trim(), userId); // Gọi dịch vụ cộng kho và lưu InventoryLog
                TempData["SuccessMessage"] = "Nhập hàng vào kho thành công!"; // Lưu thông báo thành công
                return RedirectToAction("Index"); // Chuyển về trang lịch sử biến động kho
            } // Đóng khối try
            catch (InvalidOperationException ex) // Bắt lỗi nếu không tìm thấy sản phẩm hoặc dữ liệu bất thường
            { // Mở khối catch
                TempData["ErrorMessage"] = ex.Message; // Lưu thông báo lỗi
                await PopulateProductDropdown(); // Nạp lại Dropdown sản phẩm
                return View("~/Views/Admin/Inventory/Import.cshtml", model); // Trả lại Form nhập kho
            } // Đóng khối catch
        } // Đóng thân hàm Import (POST)

        private async Task PopulateProductDropdown() // Hàm nội bộ chuẩn bị danh sách chọn sản phẩm kèm tồn kho hiện tại
        { // Mở thân hàm PopulateProductDropdown
            var products = await _productService.GetAllAsync(); // Lấy toàn bộ sản phẩm trong cửa hàng
            ViewBag.Products = new SelectList( // Tạo đối tượng SelectList truyền qua ViewBag
                products.Select(p => new { p.ProductId, Display = $"{p.ProductName} (Tồn hiện tại: {p.StockQuantity})" }), // Hiển thị rõ tên kèm số tồn kho
                "ProductId", "Display"); // Trường giá trị là ProductId, trường hiển thị là Display
        } // Đóng thân hàm PopulateProductDropdown
    } // Đóng khối lớp InventoryController
} // Đóng khối không gian tên AquariumStore.Controllers.Admin
