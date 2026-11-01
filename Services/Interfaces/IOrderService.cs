using AquariumStore.Models; // Nạp không gian tên chứa thực thể Order

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ quản lý đơn hàng và thống kê doanh thu.
    /// </summary>
    public interface IOrderService // Giao diện định nghĩa các phương thức nghiệp vụ xử lý đơn hàng
    { // Mở khối giao diện IOrderService
        Task<List<Order>> GetAllAsync(); // Lấy toàn bộ danh sách đơn hàng sắp xếp mới nhất trước
        Task<List<Order>> GetByStatusAsync(string status); // Lọc danh sách đơn hàng theo trạng thái cụ thể
        Task<List<Order>> GetByUserIdAsync(int userId); // Lấy danh sách đơn hàng của một khách hàng cụ thể
        Task<Order?> GetByIdAsync(int id); // Lấy chi tiết một đơn hàng kèm danh sách sản phẩm bên trong
        Task<Order> CreateOrderAsync(int userId, string shippingAddress, string recipientPhone, string paymentMethod, List<ViewModels.CartItemViewModel> cartItems); // Tạo đơn hàng mới, trừ tồn kho và ghi log trong 1 Transaction
        Task UpdateStatusAsync(int orderId, string newStatus, int staffUserId); // Cập nhật trạng thái đơn hàng (hoàn trả tồn kho nếu hủy đơn)
        Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int months = 12); // Thống kê doanh thu theo từng tháng để vẽ biểu đồ
        Task<decimal> GetTotalRevenueAsync(); // Tính tổng doanh thu các đơn hàng đã "Hoàn thành"
        Task<int> GetNewOrdersCountAsync(); // Đếm số lượng đơn hàng mới đang "Chờ xử lý"
    } // Đóng khối giao diện IOrderService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
