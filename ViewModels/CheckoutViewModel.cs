using System.ComponentModel.DataAnnotations; // Nạp thư viện Data Annotations để kiểm tra dữ liệu đầu vào trên Form

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho trang thanh toán đơn hàng (Checkout).
    /// </summary>
    public class CheckoutViewModel // Lớp dữ liệu chứa thông tin giao hàng và giỏ hàng khi đặt đơn
    { // Mở khối lớp CheckoutViewModel
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")] // Bắt buộc người dùng phải nhập địa chỉ nhận hàng
        [StringLength(500)] // Giới hạn độ dài địa chỉ tối đa 500 ký tự
        [Display(Name = "Địa chỉ giao hàng")] // Nhãn hiển thị trên giao diện Form
        public string ShippingAddress { get; set; } = string.Empty; // Địa chỉ giao hàng của người nhận

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận.")] // Bắt buộc nhập số điện thoại liên hệ
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")] // Kiểm tra định dạng chuỗi số điện thoại hợp lệ
        [Display(Name = "Số điện thoại người nhận")] // Nhãn hiển thị trên giao diện Form
        public string RecipientPhone { get; set; } = string.Empty; // Số điện thoại người nhận hàng

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")] // Bắt buộc chọn hình thức thanh toán
        [Display(Name = "Phương thức thanh toán")] // Nhãn hiển thị trên giao diện Form
        public string PaymentMethod { get; set; } = "COD"; // Phương thức thanh toán (mặc định là COD - Thanh toán khi nhận hàng)

        /// <summary>
        /// Danh sách sản phẩm trong giỏ hàng (chỉ đọc, hiển thị lại trên trang thanh toán).
        /// </summary>
        public List<CartItemViewModel> CartItems { get; set; } = new(); // Danh sách các món hàng trong giỏ hiện tại

        /// <summary>
        /// Tổng tiền giỏ hàng.
        /// </summary>
        public decimal TotalAmount => CartItems.Sum(x => x.SubTotal); // Tính tổng thành tiền của toàn bộ sản phẩm trong giỏ
    } // Đóng khối lớp CheckoutViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
