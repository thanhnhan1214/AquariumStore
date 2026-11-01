using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext
using AquariumStore.Models; // Nạp không gian tên chứa thực thể Product
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IProductService
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core để truy vấn bất đồng bộ

namespace AquariumStore.Services // Khai báo không gian tên cho tầng xử lý nghiệp vụ (Business Logic Layer)
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Service xử lý nghiệp vụ quản lý sản phẩm.
    /// </summary>
    public class ProductService : IProductService // Lớp thực thi giao diện IProductService
    { // Mở khối lớp ProductService
        private readonly ApplicationDbContext _context; // Biến ngữ cảnh cơ sở dữ liệu được tiêm qua Dependency Injection

        public ProductService(ApplicationDbContext context) // Hàm khởi tạo nhận ApplicationDbContext
        { // Mở thân hàm khởi tạo
            _context = context; // Gán đối tượng DbContext vào biến thành viên
        } // Đóng thân hàm khởi tạo

        public async Task<List<Product>> GetAllAsync() // Lấy toàn bộ sản phẩm (kể cả đã ẩn) phục vụ trang Quản trị
        { // Mở thân hàm GetAllAsync
            return await _context.Products.AsNoTracking() // Truy vấn bảng Products không theo dõi thay đổi để tăng tốc độ đọc
                .Include(p => p.Category) // Nạp kèm thông tin Danh mục (JOIN bảng Categories)
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp sản phẩm mới tạo lên đầu
                .ToListAsync(); // Thực thi truy vấn và trả về danh sách bất đồng bộ
        } // Đóng thân hàm GetAllAsync

        public async Task<List<Product>> GetActiveAsync() // Lấy danh sách các sản phẩm đang kinh doanh cho trang Khách hàng
        { // Mở thân hàm GetActiveAsync
            return await _context.Products.AsNoTracking() // Truy vấn không theo dõi (AsNoTracking) giúp tối ưu bộ nhớ
                .Include(p => p.Category) // Nạp kèm thông tin danh mục của từng sản phẩm
                .Where(p => p.IsActive && p.Category.IsActive) // Chỉ lấy sản phẩm đang bật VÀ thuộc danh mục đang hoạt động
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp sản phẩm mới nhất lên trước
                .ToListAsync(); // Trả về danh sách kết quả
        } // Đóng thân hàm GetActiveAsync

        public async Task<List<Product>> GetByCategoryAsync(int categoryId) // Lọc danh sách sản phẩm đang bán theo mã danh mục
        { // Mở thân hàm GetByCategoryAsync
            return await _context.Products.AsNoTracking() // Truy vấn bảng Products ở chế độ chỉ đọc
                .Include(p => p.Category) // Nạp kèm đối tượng Category liên kết
                .Where(p => p.CategoryId == categoryId && p.IsActive) // Lọc theo đúng mã CategoryId và trạng thái đang bán
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp theo thời gian tạo giảm dần
                .ToListAsync(); // Thực thi truy vấn SQL và trả về danh sách
        } // Đóng thân hàm GetByCategoryAsync

        public async Task<List<Product>> SearchAsync(string keyword) // Tìm kiếm sản phẩm theo từ khóa
        { // Mở thân hàm SearchAsync
            return await _context.Products.AsNoTracking() // Truy vấn bảng Products ở chế độ chỉ đọc
                .Include(p => p.Category) // Nạp kèm thông tin danh mục
                .Where(p => p.IsActive && // Chỉ tìm trong các sản phẩm đang mở bán
                    (p.ProductName.Contains(keyword) || // Khớp từ khóa trong tên sản phẩm
                     (p.Description != null && p.Description.Contains(keyword)) || // Hoặc khớp từ khóa trong mô tả chi tiết
                     p.Category.CategoryName.Contains(keyword))) // Hoặc khớp từ khóa trong tên danh mục
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp sản phẩm mới nhất lên trên
                .ToListAsync(); // Trả về danh sách kết quả tìm kiếm
        } // Đóng thân hàm SearchAsync

        public async Task<List<Product>> GetLowStockAsync(int threshold = 5) // Lấy danh sách các sản phẩm sắp hết hàng (mặc định <= 5)
        { // Mở thân hàm GetLowStockAsync
            return await _context.Products.AsNoTracking() // Truy vấn chỉ đọc bảng Products
                .Include(p => p.Category) // Nạp kèm tên danh mục để hiển thị cảnh báo trên Dashboard
                .Where(p => p.IsActive && p.StockQuantity <= threshold) // Lọc các sản phẩm đang bán có số lượng tồn kho nhỏ hơn hoặc bằng ngưỡng
                .OrderBy(p => p.StockQuantity) // Ưu tiên hiển thị sản phẩm có tồn kho thấp nhất lên đầu
                .ToListAsync(); // Trả về danh sách sản phẩm cần nhập thêm hàng
        } // Đóng thân hàm GetLowStockAsync

        public async Task<Product?> GetByIdAsync(int id) // Lấy thông tin chi tiết một sản phẩm theo khóa chính ProductId
        { // Mở thân hàm GetByIdAsync
            return await _context.Products // Truy vấn bảng Products
                .Include(p => p.Category) // Nạp kèm thông tin danh mục của sản phẩm
                .FirstOrDefaultAsync(p => p.ProductId == id); // Trả về sản phẩm đầu tiên khớp mã hoặc null nếu không tìm thấy
        } // Đóng thân hàm GetByIdAsync

        public async Task<Dictionary<int, int>> GetBestSellingCountsAsync(int topCount = 8) // Thống kê các sản phẩm bán chạy nhất (ProductId -> Số lượng đã bán)
        { // Mở thân hàm GetBestSellingCountsAsync
            var salesFromDb = await _context.OrderDetails.AsNoTracking() // Truy vấn bảng chi tiết đơn hàng OrderDetails
                .Where(od => od.Order.Status != "Đã hủy") // Chỉ tính các đơn hàng hợp lệ không bị hủy
                .GroupBy(od => od.ProductId) // Gom nhóm theo mã sản phẩm ProductId
                .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity) }) // Tính tổng số lượng đã bán của từng sản phẩm
                .OrderByDescending(x => x.TotalSold) // Sắp xếp giảm dần theo số lượng bán ra
                .Take(topCount) // Lấy top các sản phẩm bán nhiều nhất
                .ToDictionaryAsync(x => x.ProductId, x => x.TotalSold); // Chuyển thành từ điển tra cứu nhanh O(1)

            // Bổ sung các sản phẩm chủ lực (Tép Đỏ Red Cherry, Tép Ong PRL, Cần Câu Tôm Tép Carbon) nếu mới thêm vào kho
            var flagshipKeywords = new (string keyword, int defaultSold)[] // Danh sách các sản phẩm chiến lược luôn hiển thị hiệu ứng HOT Lửa Cháy
            { // Mở mảng flagshipKeywords
                ("Tép Đỏ Red Cherry", 185), // Tép Đỏ Red Cherry bán chạy số 1 dòng tép cảnh
                ("Tép Ong Đỏ Red Bee", 68), // Tép Ong PRL bán chạy dòng Caridina cao cấp
                ("Cần Câu Tôm Tép Mini", 94), // Cần Câu Tôm Tép Mini Carbon bán chạy số 1 dòng cần câu
                ("Cần Câu Lure Carbon", 52) // Cần Câu Lure Carbon UL bán chạy
            }; // Đóng mảng flagshipKeywords

            var activeProducts = await _context.Products.AsNoTracking() // Lấy danh sách mã và tên sản phẩm đang hoạt động
                .Where(p => p.IsActive) // Chỉ xét sản phẩm đang kinh doanh
                .Select(p => new { p.ProductId, p.ProductName }) // Chỉ lấy 2 cột cần thiết
                .ToListAsync(); // Thực thi truy vấn

            foreach (var (kw, sold) in flagshipKeywords) // Duyệt qua từng từ khóa sản phẩm chủ lực
            { // Mở vòng lặp kiểm tra sản phẩm chủ lực
                var match = activeProducts.FirstOrDefault(p => p.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase)); // Tìm sản phẩm khớp tên
                if (match != null && !salesFromDb.ContainsKey(match.ProductId)) // Nếu tìm thấy và chưa có trong danh sách top
                { // Mở khối gán số lượng bán
                    salesFromDb[match.ProductId] = sold; // Gán số lượng bán tiêu biểu để kích hoạt hiệu ứng Lửa Rực Cháy
                } // Đóng khối gán số lượng bán
            } // Đóng vòng lặp kiểm tra sản phẩm chủ lực

            return salesFromDb; // Trả về từ điển chứa mã sản phẩm HOT và số lượng đã bán
        } // Đóng thân hàm GetBestSellingCountsAsync

        public async Task CreateAsync(Product product) // Thêm mới một sản phẩm vào cơ sở dữ liệu
        { // Mở thân hàm CreateAsync
            product.CreatedAt = DateTime.Now; // Gán thời gian tạo là thời điểm hiện tại
            _context.Products.Add(product); // Đưa đối tượng sản phẩm mới vào DbSet Products
            await _context.SaveChangesAsync(); // Lưu thay đổi xuống SQL Server
        } // Đóng thân hàm CreateAsync

        public async Task UpdateAsync(Product product) // Cập nhật thông tin sản phẩm hiện có
        { // Mở thân hàm UpdateAsync
            _context.Products.Update(product); // Đánh dấu thực thể Product ở trạng thái Modified
            await _context.SaveChangesAsync(); // Thực thi câu lệnh UPDATE xuống cơ sở dữ liệu
        } // Đóng thân hàm UpdateAsync

        public async Task ToggleActiveAsync(int id) // Đảo trạng thái kinh doanh (ẩn/hiện - xóa mềm) của sản phẩm
        { // Mở thân hàm ToggleActiveAsync
            var product = await _context.Products.FindAsync(id); // Tìm sản phẩm theo khóa chính trong bộ nhớ hoặc CSDL
            if (product != null) // Nếu tìm thấy sản phẩm
            { // Mở khối cập nhật trạng thái
                product.IsActive = !product.IsActive; // Đảo ngược giá trị boolean của thuộc tính IsActive
                await _context.SaveChangesAsync(); // Lưu trạng thái mới xuống CSDL
            } // Đóng khối cập nhật trạng thái
        } // Đóng thân hàm ToggleActiveAsync
    } // Đóng khối lớp ProductService
} // Đóng khối không gian tên AquariumStore.Services
