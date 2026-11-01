using AquariumStore.Models; // Nạp không gian tên chứa các thực thể Product và Order

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho trang Dashboard quản trị.
    /// Tổng hợp dữ liệu thống kê doanh thu, đơn hàng, cảnh báo tồn kho.
    /// </summary>
    public class DashboardViewModel // Lớp dữ liệu tổng hợp toàn bộ chỉ số hiển thị trên trang Bảng điều khiển Admin
    { // Mở khối lớp DashboardViewModel
        public decimal TotalRevenue { get; set; } // Tổng doanh thu thực tế (chỉ tính các đơn hàng ở trạng thái "Hoàn thành")
        public int NewOrdersCount { get; set; } // Số lượng đơn hàng mới đang ở trạng thái "Chờ xử lý"
        public int TotalProducts { get; set; } // Tổng số sản phẩm đang kinh doanh trong cửa hàng
        public int TotalCustomers { get; set; } // Tổng số tài khoản khách hàng đã đăng ký
        public List<Product> LowStockProducts { get; set; } = new(); // Danh sách các sản phẩm sắp hết hàng (tồn kho <= 5)
        public List<Order> RecentOrders { get; set; } = new(); // Danh sách 5 đơn hàng gần nhất để theo dõi nhanh
        public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new(); // Dữ liệu doanh thu theo tháng ("MM/yyyy" -> Tổng tiền) vẽ biểu đồ cột
        public int TotalOrders { get; set; } // Tổng số lượng đơn hàng trên toàn hệ thống
        public Dictionary<string, int> OrdersByStatus { get; set; } = new(); // Thống kê số lượng đơn hàng theo từng trạng thái để vẽ biểu đồ tròn
    } // Đóng khối lớp DashboardViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
