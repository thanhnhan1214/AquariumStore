using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa IOrderService, ICartService, IUserService
using AquariumStore.ViewModels; // Nạp không gian tên chứa CheckoutViewModel
using Microsoft.AspNetCore.Authorization; // Nạp thư viện phân quyền [Authorize] bắt buộc đăng nhập
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC
using System.Security.Claims; // Nạp thư viện đọc thông tin định danh người dùng (Claims) từ Cookie

namespace AquariumStore.Controllers // Khai báo không gian tên cho các Controller phía Khách hàng
{ // Mở khối không gian tên AquariumStore.Controllers
    /// <summary>
    /// Controller đơn hàng (phía khách hàng): Thanh toán, xác nhận đơn, xem lịch sử, chi tiết và hủy đơn.
    /// </summary>
    [Authorize] // Yêu cầu người dùng phải đăng nhập mới được phép truy cập các chức năng đặt hàng và xem đơn
    public class OrderController : Controller // Lớp điều khiển quy trình đặt hàng của khách
    { // Mở khối lớp OrderController
        private readonly IOrderService _orderService; // Dịch vụ xử lý đơn hàng và tồn kho
        private readonly ICartService _cartService; // Dịch vụ quản lý giỏ hàng trong Session
        private readonly IUserService _userService; // Dịch vụ truy xuất thông tin người dùng

        public OrderController(IOrderService orderService, ICartService cartService, IUserService userService) // Hàm khởi tạo nhận các dịch vụ qua DI
        { // Mở thân hàm khởi tạo
            _orderService = orderService; // Gán dịch vụ đơn hàng
            _cartService = cartService; // Gán dịch vụ giỏ hàng
            _userService = userService; // Gán dịch vụ người dùng
        } // Đóng thân hàm khởi tạo

        private int GetCurrentUserId() // Hàm nội bộ lấy mã UserId của tài khoản đang đăng nhập từ Claims
        { // Mở thân hàm GetCurrentUserId
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Đọc Claim NameIdentifier và chuyển sang kiểu số nguyên
        } // Đóng thân hàm GetCurrentUserId

        // GET: /Order/Checkout
        public async Task<IActionResult> Checkout() // Action hiển thị trang điền thông tin giao hàng và thanh toán
        { // Mở thân hàm Checkout
            var cartItems = _cartService.GetCart(HttpContext.Session); // Lấy danh sách sản phẩm trong giỏ hàng hiện tại
            if (!cartItems.Any()) // Nếu giỏ hàng đang trống
            { // Mở khối chặn thanh toán khi giỏ rỗng
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán."; // Thông báo nhắc khách thêm hàng
                return RedirectToAction("Index", "Cart"); // Chuyển hướng về trang giỏ hàng
            } // Đóng khối chặn thanh toán khi giỏ rỗng

            var user = await _userService.GetByIdAsync(GetCurrentUserId()); // Lấy thông tin mới nhất của người dùng từ CSDL để điền sẵn địa chỉ/SĐT

            var model = new CheckoutViewModel // Khởi tạo ViewModel cho trang thanh toán
            { // Mở khối gán thuộc tính CheckoutViewModel
                CartItems = cartItems, // Danh sách sản phẩm cần thanh toán
                ShippingAddress = user?.Address ?? User.FindFirstValue("Address") ?? string.Empty, // Tự động điền sẵn địa chỉ giao hàng mặc định
                RecipientPhone = user?.PhoneNumber ?? User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty // Tự động điền sẵn số điện thoại người nhận
            }; // Đóng khối gán thuộc tính CheckoutViewModel

            return View(model); // Trả về View Views/Order/Checkout.cshtml
        } // Đóng thân hàm Checkout

        // POST: /Order/PlaceOrder
        [HttpPost] // Chỉ nhận yêu cầu POST khi khách bấm nút Đặt hàng
        [ValidateAntiForgeryToken] // Kiểm tra CSRF Token bảo mật
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model) // Action xử lý lưu đơn hàng vào CSDL
        { // Mở thân hàm PlaceOrder
            var cartItems = _cartService.GetCart(HttpContext.Session); // Lấy lại danh sách sản phẩm trong giỏ hàng từ Session
            if (!cartItems.Any()) // Nếu phiên giỏ hàng đã hết hạn hoặc trống
            { // Mở khối xử lý giỏ hàng trống
                TempData["ErrorMessage"] = "Giỏ hàng trống."; // Báo lỗi giỏ hàng trống
                return RedirectToAction("Index", "Cart"); // Quay về trang giỏ hàng
            } // Đóng khối xử lý giỏ hàng trống

            model.CartItems = cartItems; // Gán lại danh sách sản phẩm vào model phòng trường hợp cần trả lại View khi lỗi

            if (!ModelState.IsValid) // Nếu thông tin địa chỉ hoặc số điện thoại không hợp lệ
                return View("Checkout", model); // Hiển thị lại trang Checkout kèm thông báo lỗi kiểm tra dữ liệu

            try // Thử tạo đơn hàng và trừ tồn kho trong Transaction
            { // Mở khối try
                var order = await _orderService.CreateOrderAsync( // Gọi dịch vụ tạo đơn hàng mới
                    GetCurrentUserId(), // Mã người dùng đặt đơn
                    model.ShippingAddress, // Địa chỉ nhận hàng
                    model.RecipientPhone, // Số điện thoại nhận hàng
                    model.PaymentMethod, // Phương thức thanh toán (COD / Chuyển khoản)
                    cartItems); // Danh sách các món hàng trong giỏ

                _cartService.ClearCart(HttpContext.Session); // Xóa sạch giỏ hàng trong Session sau khi tạo đơn thành công

                return RedirectToAction("Confirmation", new { id = order.OrderId }); // Chuyển sang trang xác nhận đặt hàng thành công (kèm mã QR nếu chuyển khoản)
            } // Đóng khối try
            catch (InvalidOperationException ex) // Bắt lỗi nghiệp vụ (ví dụ: sản phẩm vừa hết hàng trong kho)
            { // Mở khối catch
                TempData["ErrorMessage"] = ex.Message; // Hiển thị nguyên nhân lỗi cụ thể cho khách hàng
                return View("Checkout", model); // Quay lại trang Checkout để khách điều chỉnh
            } // Đóng khối catch
        } // Đóng thân hàm PlaceOrder

        // GET: /Order/Confirmation/5
        public async Task<IActionResult> Confirmation(int id) // Action hiển thị trang cảm ơn và xác nhận đơn hàng vừa đặt
        { // Mở thân hàm Confirmation
            var order = await _orderService.GetByIdAsync(id); // Truy vấn thông tin đơn hàng theo mã OrderId
            if (order == null || order.UserId != GetCurrentUserId()) // Đảm bảo đơn hàng tồn tại và thuộc về đúng khách hàng đang đăng nhập
                return NotFound(); // Trả về lỗi 404 nếu truy cập trái phép đơn của người khác

            return View(order); // Trả về View Views/Order/Confirmation.cshtml
        } // Đóng thân hàm Confirmation

        // GET: /Order/History
        public async Task<IActionResult> History() // Action hiển thị danh sách lịch sử đơn mua của khách hàng
        { // Mở thân hàm History
            var orders = await _orderService.GetByUserIdAsync(GetCurrentUserId()); // Lấy toàn bộ đơn hàng của tài khoản hiện tại
            return View(orders); // Trả về View Views/Order/History.cshtml
        } // Đóng thân hàm History

        // GET: /Order/Details/5
        public async Task<IActionResult> Details(int id) // Action xem chi tiết một đơn hàng đã đặt
        { // Mở thân hàm Details
            var order = await _orderService.GetByIdAsync(id); // Lấy thông tin đơn hàng kèm danh sách sản phẩm
            if (order == null || order.UserId != GetCurrentUserId()) // Kiểm tra quyền sở hữu đơn hàng
                return NotFound(); // Trả về 404 nếu không tìm thấy hoặc không phải chủ đơn

            return View(order); // Trả về View Views/Order/Details.cshtml
        } // Đóng thân hàm Details

        // POST: /Order/Cancel/5
        [HttpPost] // Chỉ nhận phương thức POST khi khách bấm hủy đơn
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> Cancel(int id) // Action cho phép khách tự hủy đơn khi đơn đang "Chờ xử lý"
        { // Mở thân hàm Cancel
            var userId = GetCurrentUserId(); // Lấy mã khách hàng hiện tại
            var order = await _orderService.GetByIdAsync(id); // Tìm đơn hàng theo mã id
            if (order == null || order.UserId != userId) // Kiểm tra đơn hàng có thuộc về khách hàng này không
                return NotFound(); // Trả về 404 nếu không hợp lệ

            if (order.Status != "Chờ xử lý") // Chỉ cho phép khách hủy khi cửa hàng chưa duyệt/giao đơn
            { // Mở khối từ chối hủy đơn đã duyệt
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái 'Chờ xử lý'."; // Thông báo lý do không thể hủy
                return RedirectToAction("Details", new { id }); // Quay lại trang chi tiết đơn hàng
            } // Đóng khối từ chối hủy đơn đã duyệt

            await _orderService.UpdateStatusAsync(id, "Đã hủy", userId); // Cập nhật trạng thái sang "Đã hủy" và tự động hoàn lại số lượng vào kho
            TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{id} và hoàn trả số lượng sản phẩm vào kho."; // Thông báo hủy đơn thành công
            return RedirectToAction("Details", new { id }); // Hiển thị lại trang chi tiết đơn hàng sau khi hủy
        } // Đóng thân hàm Cancel
    } // Đóng khối lớp OrderController
} // Đóng khối không gian tên AquariumStore.Controllers
