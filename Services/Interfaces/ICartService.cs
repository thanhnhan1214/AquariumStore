using AquariumStore.ViewModels; // Nạp không gian tên chứa CartItemViewModel

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ giỏ hàng lưu trữ trong Session.
    /// </summary>
    public interface ICartService // Giao diện định nghĩa các thao tác quản lý giỏ hàng của khách
    { // Mở khối giao diện ICartService
        List<CartItemViewModel> GetCart(ISession session); // Đọc danh sách sản phẩm trong giỏ hàng từ Session
        void SaveCart(ISession session, List<CartItemViewModel> cart); // Lưu danh sách sản phẩm giỏ hàng vào Session dưới dạng JSON
        void AddToCart(ISession session, CartItemViewModel item); // Thêm sản phẩm vào giỏ (cộng dồn số lượng nếu đã có)
        void RemoveFromCart(ISession session, int productId); // Xóa một sản phẩm khỏi giỏ hàng theo ProductId
        void UpdateQuantity(ISession session, int productId, int quantity); // Cập nhật số lượng của một sản phẩm trong giỏ
        void ClearCart(ISession session); // Xóa sạch giỏ hàng sau khi đặt đơn thành công
        int GetCartCount(ISession session); // Tính tổng số lượng sản phẩm hiện có trong giỏ
        decimal GetCartTotal(ISession session); // Tính tổng số tiền của toàn bộ giỏ hàng
    } // Đóng khối giao diện ICartService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
