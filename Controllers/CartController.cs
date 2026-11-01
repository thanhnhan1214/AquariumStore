using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa ICartService và IProductService
using AquariumStore.ViewModels; // Nạp không gian tên chứa CartItemViewModel
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC

namespace AquariumStore.Controllers // Khai báo không gian tên cho các Controller phía Khách hàng
{ // Mở khối không gian tên AquariumStore.Controllers
    /// <summary>
    /// Controller giỏ hàng: Thêm, cập nhật, xóa sản phẩm trong giỏ (lưu trên Session).
    /// </summary>
    public class CartController : Controller // Lớp điều khiển các thao tác giỏ hàng của khách
    { // Mở khối lớp CartController
        private readonly ICartService _cartService; // Dịch vụ thao tác giỏ hàng trong Session
        private readonly IProductService _productService; // Dịch vụ kiểm tra thông tin và tồn kho sản phẩm

        public CartController(ICartService cartService, IProductService productService) // Hàm khởi tạo nhận dịch vụ qua DI
        { // Mở thân hàm khởi tạo
            _cartService = cartService; // Lưu tham chiếu dịch vụ giỏ hàng
            _productService = productService; // Lưu tham chiếu dịch vụ sản phẩm
        } // Đóng thân hàm khởi tạo

        // GET: /Cart
        public IActionResult Index() // Action hiển thị trang Giỏ hàng của khách
        { // Mở thân hàm Index
            var cart = _cartService.GetCart(HttpContext.Session); // Đọc danh sách sản phẩm trong giỏ từ Session
            return View(cart); // Trả về View Views/Cart/Index.cshtml
        } // Đóng thân hàm Index

        // POST: /Cart/Add
        [HttpPost] // Chỉ chấp nhận phương thức HTTP POST để thay đổi trạng thái giỏ hàng
        [ValidateAntiForgeryToken] // Kiểm tra mã chống giả mạo yêu cầu (CSRF Token)
        public async Task<IActionResult> Add(int productId, int quantity = 1) // Action thêm sản phẩm vào giỏ hàng
        { // Mở thân hàm Add
            if (quantity <= 0) quantity = 1; // Đảm bảo số lượng thêm vào tối thiểu là 1

            var product = await _productService.GetByIdAsync(productId); // Truy vấn sản phẩm từ CSDL theo mã ProductId
            if (product == null || !product.IsActive) // Nếu sản phẩm không tồn tại hoặc đã bị ẩn
            { // Mở khối báo lỗi sản phẩm không hợp lệ
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh."; // Lưu thông báo lỗi vào TempData
                return RedirectToAction("Index", "Product"); // Chuyển hướng về trang danh sách sản phẩm
            } // Đóng khối báo lỗi sản phẩm không hợp lệ

            var currentCart = _cartService.GetCart(HttpContext.Session); // Lấy giỏ hàng hiện tại để kiểm tra số lượng cộng dồn
            var existingQty = currentCart.FirstOrDefault(c => c.ProductId == productId)?.Quantity ?? 0; // Số lượng của sản phẩm này đã có sẵn trong giỏ

            if (existingQty + quantity > product.StockQuantity) // Nếu tổng số lượng trong giỏ cộng thêm vượt quá tồn kho thực tế
            { // Mở khối cảnh báo vượt tồn kho
                TempData["ErrorMessage"] = $"Sản phẩm '{product.ProductName}' chỉ còn {product.StockQuantity} trong kho (giỏ hàng của bạn đang có {existingQty})."; // Thông báo rõ tồn kho
                return RedirectToAction("Details", "Product", new { id = productId }); // Quay lại trang chi tiết sản phẩm
            } // Đóng khối cảnh báo vượt tồn kho

            var cartItem = new CartItemViewModel // Khởi tạo đối tượng mục giỏ hàng mới
            { // Mở khối gán thuộc tính CartItemViewModel
                ProductId = product.ProductId, // Mã sản phẩm
                ProductName = product.ProductName, // Tên sản phẩm
                Price = product.Price, // Giá bán hiện tại
                Quantity = quantity, // Số lượng thêm vào
                ImageUrl = product.ImageUrl // Đường dẫn ảnh đại diện
            }; // Đóng khối gán thuộc tính CartItemViewModel

            _cartService.AddToCart(HttpContext.Session, cartItem); // Lưu sản phẩm vào giỏ hàng trong Session
            TempData["SuccessMessage"] = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng."; // Thông báo thành công

            return RedirectToAction("Index"); // Chuyển hướng tới trang xem giỏ hàng
        } // Đóng thân hàm Add

        // POST: /Cart/Update
        [HttpPost] // Chỉ chấp nhận HTTP POST
        [ValidateAntiForgeryToken] // Bảo vệ chống tấn công CSRF
        public async Task<IActionResult> Update(int productId, int quantity) // Action cập nhật số lượng sản phẩm trong giỏ
        { // Mở thân hàm Update
            if (quantity > 0) // Nếu khách muốn cập nhật số lượng dương
            { // Mở khối kiểm tra tồn kho trước khi cập nhật
                var product = await _productService.GetByIdAsync(productId); // Lấy thông tin sản phẩm từ CSDL
                if (product != null && quantity > product.StockQuantity) // Nếu số lượng yêu cầu vượt quá số lượng tồn kho
                { // Mở khối giới hạn theo tồn kho tối đa
                    quantity = product.StockQuantity; // Giới hạn số lượng bằng đúng mức tồn kho tối đa hiện có
                    TempData["ErrorMessage"] = $"Kho chỉ còn tối đa {product.StockQuantity} sản phẩm \"{product.ProductName}\"."; // Thông báo cho khách hàng biết
                } // Đóng khối giới hạn theo tồn kho tối đa
            } // Đóng khối kiểm tra tồn kho trước khi cập nhật

            _cartService.UpdateQuantity(HttpContext.Session, productId, quantity); // Cập nhật số lượng mới vào Session (nếu <= 0 sẽ tự xóa)
            return RedirectToAction("Index"); // Tải lại trang giỏ hàng
        } // Đóng thân hàm Update

        // POST: /Cart/Remove
        [HttpPost] // Chỉ chấp nhận HTTP POST
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public IActionResult Remove(int productId) // Action xóa hẳn một sản phẩm khỏi giỏ hàng
        { // Mở thân hàm Remove
            _cartService.RemoveFromCart(HttpContext.Session, productId); // Xóa sản phẩm khỏi Session theo ProductId
            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng."; // Hiển thị thông báo đã xóa
            return RedirectToAction("Index"); // Quay lại trang giỏ hàng
        } // Đóng thân hàm Remove

        // GET: /Cart/GetCount (AJAX endpoint cập nhật số lượng trên biểu tượng giỏ hàng ở thanh điều hướng)
        [HttpGet] // Cho phép gọi bằng HTTP GET từ JavaScript fetch()
        public IActionResult GetCount() // Action trả về tổng số lượng sản phẩm trong giỏ dưới dạng JSON
        { // Mở thân hàm GetCount
            var count = _cartService.GetCartCount(HttpContext.Session); // Tính tổng số lượng sản phẩm trong Session
            return Json(new { count }); // Trả về chuỗi JSON { "count": ... }
        } // Đóng thân hàm GetCount
    } // Đóng khối lớp CartController
} // Đóng khối không gian tên AquariumStore.Controllers
