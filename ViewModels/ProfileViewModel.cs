using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra dữ liệu đầu vào Data Annotations

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho trang Hồ sơ cá nhân & Đổi mật khẩu của người dùng.
    /// </summary>
    public class ProfileViewModel // Lớp dữ liệu hiển thị và cập nhật thông tin cá nhân của tài khoản đang đăng nhập
    { // Mở khối lớp ProfileViewModel
        public string Username { get; set; } = string.Empty; // Tên đăng nhập (chỉ đọc)
        public string Email { get; set; } = string.Empty; // Địa chỉ Email (chỉ đọc)
        public string RoleName { get; set; } = string.Empty; // Tên vai trò của tài khoản (Admin/Staff/Customer)
        public DateTime CreatedAt { get; set; } // Thời điểm đăng ký tài khoản

        [Required(ErrorMessage = "Họ và tên không được để trống.")] // Bắt buộc nhập họ và tên đầy đủ
        [StringLength(150)] // Giới hạn độ dài họ tên tối đa 150 ký tự
        [Display(Name = "Họ và tên")] // Nhãn hiển thị trên giao diện
        public string FullName { get; set; } = string.Empty; // Họ và tên người dùng có thể cập nhật

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")] // Kiểm tra định dạng số điện thoại
        [StringLength(20)] // Giới hạn độ dài tối đa 20 ký tự
        [Display(Name = "Số điện thoại")] // Nhãn hiển thị trên giao diện
        public string? PhoneNumber { get; set; } // Số điện thoại liên hệ mặc định

        [StringLength(500)] // Giới hạn độ dài địa chỉ tối đa 500 ký tự
        [Display(Name = "Địa chỉ giao hàng mặc định")] // Nhãn hiển thị trên giao diện
        public string? Address { get; set; } // Địa chỉ nhận hàng mặc định khi thanh toán
    } // Đóng khối lớp ProfileViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
