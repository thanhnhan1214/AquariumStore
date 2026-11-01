using AquariumStore.Models; // Nạp không gian tên chứa thực thể Category

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ quản lý danh mục sản phẩm.
    /// </summary>
    public interface ICategoryService // Giao diện định nghĩa các phương thức nghiệp vụ cho danh mục sản phẩm
    { // Mở khối giao diện ICategoryService
        Task<List<Category>> GetAllAsync(); // Lấy toàn bộ danh sách danh mục kèm sản phẩm (cho Admin)
        Task<List<Category>> GetActiveAsync(); // Lấy danh sách các danh mục đang hiển thị (IsActive = true)
        Task<Category?> GetByIdAsync(int id); // Tìm và trả về một danh mục theo mã CategoryId
        Task CreateAsync(Category category); // Thêm mới một danh mục sản phẩm
        Task UpdateAsync(Category category); // Cập nhật thông tin một danh mục sản phẩm
        Task ToggleActiveAsync(int id); // Chuyển đổi trạng thái hoạt động (ẩn/hiện) của danh mục
    } // Đóng khối giao diện ICategoryService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
