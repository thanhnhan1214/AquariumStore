using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa các giao diện IProductService và ICategoryService
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC cho Controller và IActionResult

namespace AquariumStore.Controllers // Khai báo không gian tên cho các Controller phía Khách hàng
{ // Mở khối không gian tên AquariumStore.Controllers
    /// <summary>
    /// Controller trang chủ: Hiển thị banner, Landing Page Tép cảnh &amp; Cần câu, danh mục, sản phẩm nổi bật, cẩm nang và chính sách.
    /// </summary>
    public class HomeController : Controller // Lớp điều khiển trang chủ kế thừa từ Controller của MVC
    { // Mở khối lớp HomeController
        private readonly IProductService _productService; // Dịch vụ xử lý nghiệp vụ sản phẩm
        private readonly ICategoryService _categoryService; // Dịch vụ xử lý nghiệp vụ danh mục

        public HomeController(IProductService productService, ICategoryService categoryService) // Hàm khởi tạo nhận các dịch vụ qua Dependency Injection
        { // Mở thân hàm khởi tạo
            _productService = productService; // Gán dịch vụ sản phẩm vào biến thành viên
            _categoryService = categoryService; // Gán dịch vụ danh mục vào biến thành viên
        } // Đóng thân hàm khởi tạo

        // GET: / hoặc /Home/Index
        public async Task<IActionResult> Index() // Action hiển thị Trang chủ cửa hàng
        { // Mở thân hàm Index
            var products = await _productService.GetActiveAsync(); // Lấy danh sách các sản phẩm đang kinh doanh
            var categories = await _categoryService.GetActiveAsync(); // Lấy danh sách các danh mục đang hoạt động
            var bestSellers = await _productService.GetBestSellingCountsAsync(8); // Lấy danh sách sản phẩm bán chạy nhất kèm số lượng đã bán

            ViewBag.Categories = categories; // Truyền danh sách danh mục qua ViewBag để hiển thị các thẻ danh mục
            ViewBag.BestSellers = bestSellers; // Truyền từ điển sản phẩm bán chạy nhất để hiển thị hiệu ứng Lửa Rực Cháy
            ViewBag.FeaturedProducts = products // Sắp xếp ưu tiên sản phẩm bán chạy nhất lên đầu khu vực Sản phẩm nổi bật
                .OrderByDescending(p => bestSellers.GetValueOrDefault(p.ProductId, 0)) // Ưu tiên số lượng bán cao nhất
                .Take(8) // Lấy 8 sản phẩm nổi bật nhất
                .ToList(); // Chuyển thành danh sách List<Product>
            ViewBag.ShrimpAndRodProducts = products // Lọc bộ sưu tập Tép cảnh, Cầu rêu & Cần câu nổi bật cho khu vực Showcase
                .Where(p => p.Category.CategoryName.Contains("Tép", StringComparison.OrdinalIgnoreCase) // Thuộc danh mục Tép cảnh & Cầu rêu
                         || p.Category.CategoryName.Contains("Cần câu", StringComparison.OrdinalIgnoreCase) // Hoặc thuộc danh mục Cần câu & Dụng cụ
                         || p.ProductName.Contains("Tép", StringComparison.OrdinalIgnoreCase) // Hoặc tên sản phẩm có chứa chữ Tép
                         || p.ProductName.Contains("Cần Câu", StringComparison.OrdinalIgnoreCase)) // Hoặc tên sản phẩm có chứa chữ Cần Câu
                .Take(8) // Lấy tối đa 8 sản phẩm tiêu biểu
                .ToList(); // Chuyển kết quả truy vấn thành danh sách List<Product>
            ViewBag.BeginnerProducts = products // Lọc danh sách sản phẩm gợi ý cho người mới tập nuôi
                .Where(p => p.Price <= 120000 && p.StockQuantity > 0) // Chỉ chọn sản phẩm giá dễ tiếp cận (<= 120.000đ) và còn hàng
                .OrderBy(p => p.Price) // Sắp xếp theo mức giá từ thấp đến cao
                .Take(4) // Lấy 4 sản phẩm tiêu biểu
                .ToList(); // Chuyển kết quả thành danh sách

            return View(products); // Trả về View Views/Home/Index.cshtml kèm danh sách sản phẩm
        } // Đóng thân hàm Index

        // GET: /Home/Landing
        public async Task<IActionResult> Landing() // Action hiển thị chuyên trang Landing Page Tép Cảnh, Cầu Rêu & Cần Câu Giải Trí
        { // Mở thân hàm Landing
            var products = await _productService.GetActiveAsync(); // Lấy toàn bộ sản phẩm đang kinh doanh từ CSDL
            var categories = await _categoryService.GetActiveAsync(); // Lấy toàn bộ danh mục đang hoạt động
            var bestSellers = await _productService.GetBestSellingCountsAsync(8); // Lấy từ điển sản phẩm bán chạy nhất để bật hiệu ứng HOT Lửa Cháy

            ViewBag.Categories = categories; // Truyền danh sách danh mục xuống View
            ViewBag.BestSellers = bestSellers; // Truyền từ điển sản phẩm bán chạy xuống Landing Page
            ViewBag.ShrimpProducts = products // Lọc riêng nhóm sản phẩm Tép cảnh & Cầu rêu Marimo
                .Where(p => p.Category.CategoryName.Contains("Tép", StringComparison.OrdinalIgnoreCase) // Kiểm tra tên danh mục chứa từ Tép
                         || p.ProductName.Contains("Tép", StringComparison.OrdinalIgnoreCase) // Hoặc tên sản phẩm chứa từ Tép
                         || p.ProductName.Contains("Cầu Rêu", StringComparison.OrdinalIgnoreCase)) // Hoặc là Cầu Rêu Marimo
                .OrderBy(p => p.Price) // Sắp xếp theo giá tăng dần (từ dòng Neocaridina phổ thông đến Caridina cao cấp)
                .ToList(); // Chuyển thành danh sách List<Product>

            ViewBag.RodProducts = products // Lọc riêng nhóm sản phẩm Cần câu & Dụng cụ bắt tép
                .Where(p => p.Category.CategoryName.Contains("Cần câu", StringComparison.OrdinalIgnoreCase) // Kiểm tra tên danh mục Cần câu & Dụng cụ
                         || p.ProductName.Contains("Cần Câu", StringComparison.OrdinalIgnoreCase) // Hoặc tên sản phẩm có chữ Cần Câu
                         || p.ProductName.Contains("Vợt", StringComparison.OrdinalIgnoreCase)) // Hoặc là Vợt bắt tép cảnh 3D
                .OrderBy(p => p.Price) // Sắp xếp theo giá từ thấp đến cao
                .ToList(); // Chuyển thành danh sách List<Product>

            ViewBag.TankSetupProducts = products // Lọc nhóm Bể kính, Đèn WRGB, Máy lọc và Cây ráy gắn lũa hỗ trợ setup bể tép
                .Where(p => p.Category.CategoryName.Contains("Thiết bị", StringComparison.OrdinalIgnoreCase) // Thuộc danh mục Thiết bị bể cá
                         || p.Category.CategoryName.Contains("Cây thủy sinh", StringComparison.OrdinalIgnoreCase)) // Hoặc thuộc danh mục Cây thủy sinh
                .Take(4) // Lấy 4 sản phẩm cốt lõi cho combo bể tép
                .ToList(); // Chuyển thành danh sách List<Product>

            return View(products); // Trả về View Views/Home/Landing.cshtml kèm toàn bộ danh sách sản phẩm
        } // Đóng thân hàm Landing

        // GET: /Home/Guide
        public IActionResult Guide() // Action hiển thị trang Cẩm nang Thủy sinh & Chăm sóc cá cảnh
        { // Mở thân hàm Guide
            return View(); // Trả về View Views/Home/Guide.cshtml
        } // Đóng thân hàm Guide

        // GET: /Home/Policy?topic=huong-dan-mua-hang
        public IActionResult Policy(string? topic = "huong-dan-mua-hang") // Action hiển thị 11 bài Hướng dẫn & Chính sách của cửa hàng
        { // Mở thân hàm Policy
            ViewBag.CurrentTopic = string.IsNullOrWhiteSpace(topic) ? "huong-dan-mua-hang" : topic.Trim().ToLowerInvariant(); // Chuẩn hóa mã chủ đề (slug) cần hiển thị
            return View(); // Trả về View Views/Home/Policy.cshtml
        } // Đóng thân hàm Policy
    } // Đóng khối lớp HomeController
} // Đóng khối không gian tên AquariumStore.Controllers
