using AquariumStore.Models; // Nạp không gian tên chứa thực thể InventoryLog

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ quản lý kho hàng và lịch sử nhập/xuất kho.
    /// </summary>
    public interface IInventoryService // Giao diện định nghĩa các phương thức nghiệp vụ quản lý tồn kho
    { // Mở khối giao diện IInventoryService
        Task<List<InventoryLog>> GetAllLogsAsync(); // Lấy toàn bộ lịch sử biến động tồn kho
        Task<List<InventoryLog>> GetLogsByProductAsync(int productId); // Lấy lịch sử biến động kho của một sản phẩm cụ thể
        Task ImportStockAsync(int productId, int quantity, string reason, int userId); // Điều chỉnh tăng/giảm tồn kho và ghi nhận lịch sử
    } // Đóng khối giao diện IInventoryService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
