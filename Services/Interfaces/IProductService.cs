using AquariumStore.Models; // Nạp không gian tên chứa thực thể Product

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ (Service Interfaces)
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ quản lý sản phẩm.
    /// </summary>
    public interface IProductService // Giao diện định nghĩa các phương thức nghiệp vụ cho sản phẩm
    { // Mở khối giao diện IProductService
        Task<List<Product>> GetAllAsync(); // Lấy toàn bộ danh sách sản phẩm (dùng cho trang Quản trị)
        Task<List<Product>> GetActiveAsync(); // Lấy danh sách các sản phẩm đang kinh doanh (IsActive = true)
        Task<List<Product>> GetByCategoryAsync(int categoryId); // Lấy danh sách sản phẩm đang bán theo mã danh mục
        Task<List<Product>> SearchAsync(string keyword); // Tìm kiếm sản phẩm theo từ khóa tên hoặc mô tả
        Task<List<Product>> GetLowStockAsync(int threshold = 5); // Lấy danh sách sản phẩm sắp hết hàng (tồn kho <= ngưỡng)
        Task<Product?> GetByIdAsync(int id); // Lấy thông tin chi tiết một sản phẩm theo mã định danh
        Task<Dictionary<int, int>> GetBestSellingCountsAsync(int topCount = 8); // Lấy từ điển (ProductId -> Tổng số lượng đã bán) của các sản phẩm bán chạy nhất
        Task CreateAsync(Product product); // Thêm mới một sản phẩm vào cơ sở dữ liệu
        Task UpdateAsync(Product product); // Cập nhật thông tin một sản phẩm hiện có
        Task ToggleActiveAsync(int id); // Đảo trạng thái ẩn/hiện (xóa mềm) của một sản phẩm
    } // Đóng khối giao diện IProductService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
