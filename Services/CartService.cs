using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện ICartService
using AquariumStore.ViewModels; // Nạp không gian tên chứa CartItemViewModel
using System.Text.Json; // Nạp thư viện tuần tự hóa JSON (JsonSerializer) của .NET

namespace AquariumStore.Services // Khai báo không gian tên cho tầng xử lý nghiệp vụ
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Service giỏ hàng: Lưu trữ và thao tác giỏ hàng trên Session dưới dạng chuỗi JSON.
    /// </summary>
    public class CartService : ICartService // Lớp thực thi giao diện ICartService
    { // Mở khối lớp CartService
        private const string CartSessionKey = "ShoppingCart"; // Khóa cố định dùng để lưu và đọc giỏ hàng trong Session

        public List<CartItemViewModel> GetCart(ISession session) // Lấy danh sách các mặt hàng trong giỏ từ Session hiện tại
        { // Mở thân hàm GetCart
            var cartJson = session.GetString(CartSessionKey); // Đọc chuỗi JSON của giỏ hàng từ Session
            if (string.IsNullOrEmpty(cartJson)) // Nếu chưa có dữ liệu giỏ hàng trong Session
                return new List<CartItemViewModel>(); // Trả về một danh sách rỗng mới

            return JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) // Giải mã chuỗi JSON thành danh sách CartItemViewModel
                   ?? new List<CartItemViewModel>(); // Nếu giải mã trả về null thì trả về danh sách rỗng
        } // Đóng thân hàm GetCart

        public void SaveCart(ISession session, List<CartItemViewModel> cart) // Lưu danh sách mặt hàng giỏ hàng vào lại Session
        { // Mở thân hàm SaveCart
            var cartJson = JsonSerializer.Serialize(cart); // Chuyển đổi danh sách đối tượng thành chuỗi JSON
            session.SetString(CartSessionKey, cartJson); // Ghi chuỗi JSON vào Session với khóa "ShoppingCart"
        } // Đóng thân hàm SaveCart

        public void AddToCart(ISession session, CartItemViewModel item) // Thêm một mặt hàng vào giỏ
        { // Mở thân hàm AddToCart
            var cart = GetCart(session); // Lấy danh sách giỏ hàng hiện tại từ Session
            var existingItem = cart.FirstOrDefault(c => c.ProductId == item.ProductId); // Kiểm tra sản phẩm đã có sẵn trong giỏ hay chưa

            if (existingItem != null) // Nếu sản phẩm đã có trong giỏ hàng
            { // Mở khối cộng dồn số lượng
                existingItem.Quantity += item.Quantity; // Cộng thêm số lượng mới vào số lượng hiện có
            } // Đóng khối cộng dồn số lượng
            else // Nếu sản phẩm chưa có trong giỏ hàng
            { // Mở khối thêm mới
                cart.Add(item); // Thêm mục sản phẩm mới vào danh sách giỏ hàng
            } // Đóng khối thêm mới

            SaveCart(session, cart); // Lưu danh sách giỏ hàng đã cập nhật vào lại Session
        } // Đóng thân hàm AddToCart

        public void RemoveFromCart(ISession session, int productId) // Xóa một sản phẩm khỏi giỏ hàng theo mã ProductId
        { // Mở thân hàm RemoveFromCart
            var cart = GetCart(session); // Lấy giỏ hàng hiện tại từ Session
            cart.RemoveAll(c => c.ProductId == productId); // Xóa tất cả phần tử có ProductId trùng khớp
            SaveCart(session, cart); // Lưu lại giỏ hàng sau khi xóa vào Session
        } // Đóng thân hàm RemoveFromCart

        public void UpdateQuantity(ISession session, int productId, int quantity) // Cập nhật số lượng của một sản phẩm trong giỏ
        { // Mở thân hàm UpdateQuantity
            var cart = GetCart(session); // Lấy giỏ hàng hiện tại từ Session
            var item = cart.FirstOrDefault(c => c.ProductId == productId); // Tìm mặt hàng cần thay đổi số lượng

            if (item != null) // Nếu tìm thấy mặt hàng trong giỏ
            { // Mở khối kiểm tra số lượng mới
                if (quantity <= 0) // Nếu số lượng mới nhỏ hơn hoặc bằng 0
                { // Mở khối xóa mặt hàng
                    cart.Remove(item); // Xóa luôn mặt hàng đó khỏi giỏ hàng
                } // Đóng khối xóa mặt hàng
                else // Nếu số lượng mới lớn hơn 0
                { // Mở khối gán số lượng
                    item.Quantity = quantity; // Cập nhật lại số lượng mới cho mặt hàng
                } // Đóng khối gán số lượng
            } // Đóng khối kiểm tra số lượng mới

            SaveCart(session, cart); // Lưu lại giỏ hàng vào Session
        } // Đóng thân hàm UpdateQuantity

        public void ClearCart(ISession session) // Xóa toàn bộ giỏ hàng khỏi Session (dùng sau khi đặt hàng thành công)
        { // Mở thân hàm ClearCart
            session.Remove(CartSessionKey); // Xóa khóa "ShoppingCart" khỏi bộ nhớ Session
        } // Đóng thân hàm ClearCart

        public int GetCartCount(ISession session) // Tính tổng số lượng đơn vị sản phẩm đang có trong giỏ
        { // Mở thân hàm GetCartCount
            var cart = GetCart(session); // Đọc giỏ hàng từ Session
            return cart.Sum(c => c.Quantity); // Cộng tổng thuộc tính Quantity của tất cả mặt hàng
        } // Đóng thân hàm GetCartCount

        public decimal GetCartTotal(ISession session) // Tính tổng tiền tạm tính của toàn bộ giỏ hàng
        { // Mở thân hàm GetCartTotal
            var cart = GetCart(session); // Đọc giỏ hàng từ Session
            return cart.Sum(c => c.SubTotal); // Cộng tổng thành tiền (SubTotal) của từng dòng sản phẩm
        } // Đóng thân hàm GetCartTotal
    } // Đóng khối lớp CartService
} // Đóng khối không gian tên AquariumStore.Services
