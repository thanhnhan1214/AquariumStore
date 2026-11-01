using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra tính hợp lệ dữ liệu Data Annotations

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho form đăng ký tài khoản khách hàng mới.
    /// </summary>
    public class RegisterViewModel // Lớp dữ liệu nhận thông tin đăng ký tài khoản từ khách hàng
    { // Mở khối lớp RegisterViewModel
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")] // Bắt buộc nhập tên đăng nhập
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 100 ký tự.")] // Ràng buộc độ dài từ 3 đến 100 ký tự
        [Display(Name = "Tên đăng nhập")] // Nhãn hiển thị trên Form đăng ký
        public string Username { get; set; } = string.Empty; // Tên tài khoản đăng ký mới

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")] // Bắt buộc nhập mật khẩu
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")] // Ràng buộc mật khẩu tối thiểu 6 ký tự
        [DataType(DataType.Password)] // Định dạng trường mật khẩu ẩn ký tự
        [Display(Name = "Mật khẩu")] // Nhãn hiển thị trên Form đăng ký
        public string Password { get; set; } = string.Empty; // Mật khẩu do người dùng tạo

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")] // Bắt buộc nhập lại mật khẩu xác nhận
        [DataType(DataType.Password)] // Định dạng trường mật khẩu ẩn ký tự
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")] // So sánh phải trùng khớp với trường Password ở trên
        [Display(Name = "Xác nhận mật khẩu")] // Nhãn hiển thị trên Form đăng ký
        public string ConfirmPassword { get; set; } = string.Empty; // Chuỗi mật khẩu nhập lại để xác nhận

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")] // Bắt buộc nhập họ và tên
        [StringLength(150)] // Độ dài họ tên tối đa 150 ký tự
        [Display(Name = "Họ và tên")] // Nhãn hiển thị trên Form đăng ký
        public string FullName { get; set; } = string.Empty; // Họ và tên đầy đủ của khách hàng

        [Required(ErrorMessage = "Vui lòng nhập email.")] // Bắt buộc nhập địa chỉ Email
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")] // Kiểm tra đúng định dạng địa chỉ Email
        [Display(Name = "Email")] // Nhãn hiển thị trên Form đăng ký
        public string Email { get; set; } = string.Empty; // Địa chỉ Email của khách hàng

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")] // Kiểm tra định dạng số điện thoại
        [Display(Name = "Số điện thoại")] // Nhãn hiển thị trên Form đăng ký
        public string? PhoneNumber { get; set; } // Số điện thoại liên hệ (tùy chọn)

        [StringLength(500)] // Giới hạn độ dài địa chỉ tối đa 500 ký tự
        [Display(Name = "Địa chỉ")] // Nhãn hiển thị trên Form đăng ký
        public string? Address { get; set; } // Địa chỉ giao hàng (tùy chọn)
    } // Đóng khối lớp RegisterViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
