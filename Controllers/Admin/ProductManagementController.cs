using AquariumStore.Models; // Nạp không gian tên chứa thực thể Product
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa IProductService và ICategoryService
using AquariumStore.ViewModels; // Nạp không gian tên chứa ProductViewModel
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize]
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc.Rendering; // Nạp thư viện SelectList phục vụ Dropdown danh mục

namespace AquariumStore.Controllers.Admin // Khai báo không gian tên cho các Controller Quản trị
{ // Mở khối không gian tên AquariumStore.Controllers.Admin
    /// <summary>
    /// Controller quản lý sản phẩm (Admin/Staff): Xem danh sách, thêm mới, chỉnh sửa, tải ảnh và ẩn/hiện sản phẩm.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")] // Cho phép Admin và Staff quản lý sản phẩm
    [Route("Admin/[controller]")] // Định tuyến URL gốc là /Admin/ProductManagement
    public class ProductManagementController : Controller // Lớp điều khiển quản lý sản phẩm
    { // Mở khối lớp ProductManagementController
        private readonly IProductService _productService; // Dịch vụ xử lý nghiệp vụ sản phẩm
        private readonly ICategoryService _categoryService; // Dịch vụ xử lý nghiệp vụ danh mục
        private readonly IWebHostEnvironment _environment; // Môi trường máy chủ web dùng để lấy đường dẫn vật lý thư mục wwwroot

        public ProductManagementController( // Hàm khởi tạo nhận các dịch vụ qua Dependency Injection
            IProductService productService, // Dịch vụ sản phẩm
            ICategoryService categoryService, // Dịch vụ danh mục
            IWebHostEnvironment environment) // Môi trường lưu trữ web
        { // Mở thân hàm khởi tạo
            _productService = productService; // Gán dịch vụ sản phẩm
            _categoryService = categoryService; // Gán dịch vụ danh mục
            _environment = environment; // Gán đối tượng môi trường web
        } // Đóng thân hàm khởi tạo

        // GET: /Admin/ProductManagement
        [HttpGet("")] // Định tuyến mặc định /Admin/ProductManagement
        [HttpGet("Index")] // Định tuyến /Admin/ProductManagement/Index
        public async Task<IActionResult> Index() // Action hiển thị toàn bộ danh sách sản phẩm trong hệ thống
        { // Mở thân hàm Index
            var products = await _productService.GetAllAsync(); // Lấy tất cả sản phẩm (bao gồm cả đang bán và đã ẩn)
            return View("~/Views/Admin/ProductManagement/Index.cshtml", products); // Trả về View danh sách sản phẩm Admin
        } // Đóng thân hàm Index

        // GET: /Admin/ProductManagement/Create
        [HttpGet("Create")] // Định tuyến GET /Admin/ProductManagement/Create
        public async Task<IActionResult> Create() // Action hiển thị Form thêm sản phẩm mới
        { // Mở thân hàm Create (GET)
            await PopulateCategoryDropdown(); // Nạp danh sách danh mục vào ViewBag.Categories cho thẻ <select>
            return View("~/Views/Admin/ProductManagement/Create.cshtml", new ProductViewModel()); // Trả về View tạo mới sản phẩm
        } // Đóng thân hàm Create (GET)

        // POST: /Admin/ProductManagement/Create
        [HttpPost("Create")] // Định tuyến POST /Admin/ProductManagement/Create
        [ValidateAntiForgeryToken] // Bảo vệ chống giả mạo yêu cầu CSRF
        public async Task<IActionResult> Create(ProductViewModel model) // Action xử lý lưu sản phẩm mới từ Form
        { // Mở thân hàm Create (POST)
            if (!ModelState.IsValid) // Kiểm tra tính hợp lệ của dữ liệu nhập vào
            { // Mở khối trả lại Form khi dữ liệu lỗi
                await PopulateCategoryDropdown(); // Nạp lại Dropdown danh mục
                return View("~/Views/Admin/ProductManagement/Create.cshtml", model); // Hiển thị lại Form kèm thông báo lỗi
            } // Đóng khối trả lại Form khi dữ liệu lỗi

            var product = new Product // Khởi tạo thực thể Product mới từ dữ liệu ViewModel
            { // Mở khối gán thuộc tính Product
                CategoryId = model.CategoryId, // Mã danh mục sản phẩm
                ProductName = model.ProductName.Trim(), // Tên sản phẩm
                Description = model.Description?.Trim(), // Mô tả chi tiết sản phẩm
                Price = model.Price, // Giá bán (VNĐ)
                StockQuantity = model.StockQuantity, // Số lượng tồn kho ban đầu
                IsActive = model.IsActive, // Trạng thái mở bán
                CreatedAt = DateTime.Now // Ngày giờ tạo sản phẩm
            }; // Đóng khối gán thuộc tính Product

            if (model.ImageFile != null && model.ImageFile.Length > 0) // Nếu người dùng có chọn tải lên tệp hình ảnh
            { // Mở khối lưu ảnh tải lên
                product.ImageUrl = await SaveImageAsync(model.ImageFile); // Lưu tệp ảnh vào wwwroot/images/products/ và gán đường dẫn URL
            } // Đóng khối lưu ảnh tải lên

            await _productService.CreateAsync(product); // Lưu sản phẩm mới vào cơ sở dữ liệu
            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!"; // Lưu thông báo thành công
            return RedirectToAction("Index"); // Chuyển hướng về trang danh sách sản phẩm
        } // Đóng thân hàm Create (POST)

        // GET: /Admin/ProductManagement/Edit/5
        [HttpGet("Edit/{id}")] // Định tuyến GET /Admin/ProductManagement/Edit/{id}
        public async Task<IActionResult> Edit(int id) // Action hiển thị Form chỉnh sửa thông tin sản phẩm
        { // Mở thân hàm Edit (GET)
            var product = await _productService.GetByIdAsync(id); // Tìm sản phẩm theo mã ProductId
            if (product == null) return NotFound(); // Trả về lỗi 404 nếu không tìm thấy sản phẩm

            var model = new ProductViewModel // Ánh xạ dữ liệu từ thực thể Product sang ProductViewModel
            { // Mở khối khởi tạo ProductViewModel
                ProductId = product.ProductId, // Mã sản phẩm
                CategoryId = product.CategoryId, // Mã danh mục hiện tại
                ProductName = product.ProductName, // Tên sản phẩm hiện tại
                Description = product.Description, // Mô tả hiện tại
                Price = product.Price, // Giá bán hiện tại
                StockQuantity = product.StockQuantity, // Tồn kho hiện tại
                IsActive = product.IsActive, // Trạng thái kinh doanh hiện tại
                ExistingImageUrl = product.ImageUrl // Đường dẫn ảnh hiện tại để hiển thị bản xem trước
            }; // Đóng khối khởi tạo ProductViewModel

            await PopulateCategoryDropdown(); // Nạp danh sách danh mục cho Dropdown
            return View("~/Views/Admin/ProductManagement/Edit.cshtml", model); // Trả về View chỉnh sửa sản phẩm
        } // Đóng thân hàm Edit (GET)

        // POST: /Admin/ProductManagement/Edit/5
        [HttpPost("Edit/{id}")] // Định tuyến POST /Admin/ProductManagement/Edit/{id}
        [ValidateAntiForgeryToken] // Kiểm tra CSRF Token
        public async Task<IActionResult> Edit(int id, ProductViewModel model) // Action lưu thông tin sản phẩm sau khi chỉnh sửa
        { // Mở thân hàm Edit (POST)
            if (id != model.ProductId) return BadRequest(); // Kiểm tra mã id trên URL và trong Form phải khớp nhau

            if (!ModelState.IsValid) // Nếu dữ liệu nhập trên Form không hợp lệ
            { // Mở khối trả lại Form khi lỗi
                await PopulateCategoryDropdown(); // Nạp lại Dropdown danh mục
                return View("~/Views/Admin/ProductManagement/Edit.cshtml", model); // Hiển thị lại Form chỉnh sửa
            } // Đóng khối trả lại Form khi lỗi

            var product = await _productService.GetByIdAsync(id); // Truy vấn thực thể Product gốc từ CSDL
            if (product == null) return NotFound(); // Trả về 404 nếu sản phẩm không tồn tại

            product.CategoryId = model.CategoryId; // Cập nhật mã danh mục
            product.ProductName = model.ProductName.Trim(); // Cập nhật tên sản phẩm
            product.Description = model.Description?.Trim(); // Cập nhật mô tả chi tiết
            product.Price = model.Price; // Cập nhật giá bán
            product.StockQuantity = model.StockQuantity; // Cập nhật số lượng tồn kho
            product.IsActive = model.IsActive; // Cập nhật trạng thái kinh doanh

            if (model.ImageFile != null && model.ImageFile.Length > 0) // Nếu người quản trị tải lên một tệp ảnh mới thay thế
            { // Mở khối thay thế ảnh mới
                await DeleteOldImageSafelyAsync(product.ImageUrl, product.ProductId); // Xóa tệp ảnh cũ (chỉ xóa nếu không có sản phẩm nào khác dùng chung ảnh đó)
                product.ImageUrl = await SaveImageAsync(model.ImageFile); // Lưu tệp ảnh mới và cập nhật đường dẫn ImageUrl
            } // Đóng khối thay thế ảnh mới

            await _productService.UpdateAsync(product); // Lưu thay đổi sản phẩm xuống cơ sở dữ liệu
            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!"; // Thông báo thành công
            return RedirectToAction("Index"); // Quay về danh sách sản phẩm
        } // Đóng thân hàm Edit (POST)

        // POST: /Admin/ProductManagement/ToggleActive/5
        [HttpPost("ToggleActive/{id}")] // Định tuyến POST /Admin/ProductManagement/ToggleActive/{id}
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> ToggleActive(int id) // Action bật/tắt trạng thái kinh doanh của sản phẩm
        { // Mở thân hàm ToggleActive
            await _productService.ToggleActiveAsync(id); // Đảo trạng thái IsActive trong CSDL
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái sản phẩm."; // Lưu thông báo thành công
            return RedirectToAction("Index"); // Tải lại danh sách sản phẩm
        } // Đóng thân hàm ToggleActive

        /// <summary>
        /// Lưu tệp ảnh tải lên vào thư mục wwwroot/images/products/ với tên GUID duy nhất.
        /// </summary>
        private async Task<string> SaveImageAsync(IFormFile imageFile) // Hàm nội bộ lưu tệp ảnh vào ổ cứng máy chủ
        { // Mở thân hàm SaveImageAsync
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products"); // Tạo đường dẫn tuyệt đối tới thư mục wwwroot/images/products
            if (!Directory.Exists(uploadsFolder)) // Nếu thư mục chưa tồn tại
                Directory.CreateDirectory(uploadsFolder); // Tự động tạo thư mục

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName); // Sinh tên tệp duy nhất bằng GUID kèm phần mở rộng gốc (.jpg, .png...)
            var filePath = Path.Combine(uploadsFolder, uniqueFileName); // Đường dẫn vật lý đầy đủ của tệp ảnh mới

            using (var fileStream = new FileStream(filePath, FileMode.Create)) // Mở luồng ghi tệp xuống ổ đĩa
            { // Mở khối using FileStream
                await imageFile.CopyToAsync(fileStream); // Sao chép dữ liệu nhị phân của ảnh tải lên vào tệp
            } // Đóng khối using và tự động đóng luồng tệp

            return "/images/products/" + uniqueFileName; // Trả về đường dẫn tương đối để lưu vào cột ImageUrl trong CSDL
        } // Đóng thân hàm SaveImageAsync

        /// <summary>
        /// Xóa tệp ảnh cũ khỏi hệ thống một cách an toàn (không xóa nếu có sản phẩm khác đang dùng chung đường dẫn ảnh).
        /// </summary>
        private async Task DeleteOldImageSafelyAsync(string? imageUrl, int currentProductId) // Hàm xóa ảnh cũ an toàn
        { // Mở thân hàm DeleteOldImageSafelyAsync
            if (string.IsNullOrEmpty(imageUrl)) return; // Nếu sản phẩm trước đó chưa có ảnh thì bỏ qua

            var allProducts = await _productService.GetAllAsync(); // Lấy danh sách sản phẩm để kiểm tra có sản phẩm nào khác dùng chung ảnh không
            var isShared = allProducts.Any(p => p.ProductId != currentProductId && p.ImageUrl == imageUrl); // Kiểm tra trùng lặp đường dẫn ảnh
            if (isShared) return; // Nếu ảnh đang được dùng chung bởi sản phẩm mẫu khác thì giữ nguyên tệp vật lý

            var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); // Chuyển URL tương đối thành đường dẫn tệp vật lý
            if (System.IO.File.Exists(filePath)) // Kiểm tra tệp có thực sự tồn tại trên ổ đĩa không
            { // Mở khối xóa tệp
                System.IO.File.Delete(filePath); // Xóa tệp ảnh cũ khỏi thư mục wwwroot/images/products
            } // Đóng khối xóa tệp
        } // Đóng thân hàm DeleteOldImageSafelyAsync

        private async Task PopulateCategoryDropdown() // Hàm nội bộ chuẩn bị dữ liệu danh mục cho thẻ <select>
        { // Mở thân hàm PopulateCategoryDropdown
            var categories = await _categoryService.GetActiveAsync(); // Lấy danh sách các danh mục đang hoạt động
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName"); // Đóng gói thành SelectList truyền qua ViewBag
        } // Đóng thân hàm PopulateCategoryDropdown
    } // Đóng khối lớp ProductManagementController
} // Đóng khối không gian tên AquariumStore.Controllers.Admin
