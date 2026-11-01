using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IProductService và ICategoryService
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC

namespace AquariumStore.Controllers // Khai báo không gian tên cho các Controller phía Khách hàng
{ // Mở khối không gian tên AquariumStore.Controllers
    /// <summary>
    /// Controller sản phẩm (phía khách hàng): Xem danh sách, kết hợp lọc đa tiêu chí, sắp xếp, xem chi tiết.
    /// </summary>
    public class ProductController : Controller // Lớp điều khiển hiển thị và tra cứu sản phẩm
    { // Mở khối lớp ProductController
        private readonly IProductService _productService; // Dịch vụ truy xuất dữ liệu sản phẩm
        private readonly ICategoryService _categoryService; // Dịch vụ truy xuất dữ liệu danh mục

        public ProductController(IProductService productService, ICategoryService categoryService) // Hàm khởi tạo nhận dịch vụ qua DI
        { // Mở thân hàm khởi tạo
            _productService = productService; // Lưu tham chiếu dịch vụ sản phẩm
            _categoryService = categoryService; // Lưu tham chiếu dịch vụ danh mục
        } // Đóng thân hàm khởi tạo

        // GET: /Product
        public async Task<IActionResult> Index( // Action hiển thị danh sách sản phẩm kèm bộ lọc đa tiêu chí
            int? categoryId, // Tham số lọc theo mã danh mục (tùy chọn)
            string? search, // Tham số từ khóa tìm kiếm theo tên/mô tả (tùy chọn)
            string? sortBy = "newest", // Tham số tiêu chí sắp xếp (mặc định: mới nhất)
            decimal? minPrice = null, // Tham số lọc mức giá tối thiểu (tùy chọn)
            decimal? maxPrice = null, // Tham số lọc mức giá tối đa (tùy chọn)
            bool inStockOnly = false) // Tham số chỉ hiển thị sản phẩm còn hàng trong kho
        { // Mở thân hàm Index
            var categories = await _categoryService.GetActiveAsync(); // Lấy danh sách các danh mục đang hoạt động cho cột bộ lọc bên trái
            ViewBag.Categories = categories; // Truyền danh sách danh mục sang View
            ViewBag.CurrentCategoryId = categoryId; // Lưu mã danh mục đang chọn để đánh dấu active trên giao diện
            ViewBag.CurrentSearch = search; // Lưu từ khóa tìm kiếm hiện tại để hiển thị lại trên ô tìm kiếm
            ViewBag.CurrentSort = sortBy; // Lưu tùy chọn sắp xếp hiện tại
            ViewBag.MinPrice = minPrice; // Lưu giá trị lọc giá thấp nhất
            ViewBag.MaxPrice = maxPrice; // Lưu giá trị lọc giá cao nhất
            ViewBag.InStockOnly = inStockOnly; // Lưu trạng thái hộp kiểm chỉ xem hàng còn tồn kho

            var products = await _productService.GetActiveAsync(); // Lấy toàn bộ sản phẩm đang kinh doanh
            var bestSellers = await _productService.GetBestSellingCountsAsync(8); // Lấy danh sách sản phẩm bán chạy nhất kèm số lượng đã bán
            ViewBag.BestSellers = bestSellers; // Truyền danh sách bán chạy xuống View để hiển thị hiệu ứng Lửa Rực Cháy

            if (categoryId.HasValue) // Nếu người dùng chọn lọc theo một danh mục cụ thể
            { // Mở khối lọc theo danh mục
                products = products.Where(p => p.CategoryId == categoryId.Value).ToList(); // Lọc các sản phẩm thuộc mã danh mục đó
                var category = categories.FirstOrDefault(c => c.CategoryId == categoryId.Value); // Tìm đối tượng danh mục tương ứng
                ViewBag.Title = category?.CategoryName ?? "Sản phẩm"; // Đặt tiêu đề trang theo tên danh mục
            } // Đóng khối lọc theo danh mục
            else // Nếu không lọc theo danh mục
            { // Mở khối đặt tiêu đề mặc định
                ViewBag.Title = "Tất cả sản phẩm"; // Tiêu đề mặc định khi xem toàn bộ cửa hàng
            } // Đóng khối đặt tiêu đề mặc định

            if (!string.IsNullOrWhiteSpace(search)) // Nếu người dùng nhập từ khóa tìm kiếm
            { // Mở khối lọc theo từ khóa
                var kw = search.Trim(); // Cắt bỏ khoảng trắng thừa ở hai đầu từ khóa
                products = products.Where(p => // Lọc danh sách sản phẩm khớp từ khóa (không phân biệt chữ hoa/thường)
                    p.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase) || // Khớp trong tên sản phẩm
                    (p.Description != null && p.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)) || // Khớp trong mô tả sản phẩm
                    p.Category.CategoryName.Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList(); // Khớp trong tên danh mục

                ViewBag.Title = $"Kết quả tìm kiếm: \"{kw}\""; // Cập nhật tiêu đề trang hiển thị từ khóa vừa tìm
            } // Đóng khối lọc theo từ khóa

            if (minPrice.HasValue) // Nếu có giới hạn mức giá tối thiểu
                products = products.Where(p => p.Price >= minPrice.Value).ToList(); // Chỉ giữ lại sản phẩm có giá >= minPrice

            if (maxPrice.HasValue) // Nếu có giới hạn mức giá tối đa
                products = products.Where(p => p.Price <= maxPrice.Value).ToList(); // Chỉ giữ lại sản phẩm có giá <= maxPrice

            if (inStockOnly) // Nếu người dùng bật tùy chọn chỉ xem sản phẩm còn hàng
                products = products.Where(p => p.StockQuantity > 0).ToList(); // Loại bỏ các sản phẩm có tồn kho bằng 0

            products = sortBy switch // Sắp xếp danh sách sản phẩm theo tiêu chí người dùng chọn
            { // Mở biểu thức switch sắp xếp
                "price_asc" => products.OrderBy(p => p.Price).ToList(), // Giá tăng dần (từ thấp đến cao)
                "price_desc" => products.OrderByDescending(p => p.Price).ToList(), // Giá giảm dần (từ cao xuống thấp)
                "name_asc" => products.OrderBy(p => p.ProductName).ToList(), // Tên sản phẩm theo bảng chữ cái A -> Z
                _ => products.OrderByDescending(p => p.CreatedAt).ToList() // Mặc định: sản phẩm mới nhất lên đầu
            }; // Đóng biểu thức switch sắp xếp

            return View(products); // Trả về View Views/Product/Index.cshtml kèm danh sách sản phẩm đã lọc
        } // Đóng thân hàm Index

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id) // Action xem thông tin chi tiết một sản phẩm
        { // Mở thân hàm Details
            var product = await _productService.GetByIdAsync(id); // Tìm sản phẩm theo mã ProductId
            if (product == null || !product.IsActive) // Nếu sản phẩm không tồn tại hoặc đã ngừng kinh doanh
                return NotFound(); // Trả về lỗi HTTP 404 Not Found

            var relatedProducts = await _productService.GetByCategoryAsync(product.CategoryId); // Lấy các sản phẩm cùng danh mục
            var bestSellers = await _productService.GetBestSellingCountsAsync(8); // Lấy danh sách sản phẩm bán chạy nhất
            ViewBag.BestSellers = bestSellers; // Truyền từ điển bán chạy xuống trang Chi tiết sản phẩm
            ViewBag.RelatedProducts = relatedProducts // Gán danh sách sản phẩm liên quan vào ViewBag
                .Where(p => p.ProductId != id) // Loại trừ chính sản phẩm đang xem
                .Take(4) // Lấy tối đa 4 sản phẩm gợi ý cùng danh mục
                .ToList(); // Chuyển thành danh sách

            return View(product); // Trả về View Views/Product/Details.cshtml kèm mô hình sản phẩm
        } // Đóng thân hàm Details
    } // Đóng khối lớp ProductController
} // Đóng khối không gian tên AquariumStore.Controllers
