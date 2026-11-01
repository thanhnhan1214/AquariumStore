using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra tính hợp lệ dữ liệu trên Form

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho form đăng nhập hệ thống.
    /// </summary>
    public class LoginViewModel // Lớp dữ liệu nhận thông tin đăng nhập từ người dùng
    { // Mở khối lớp LoginViewModel
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")] // Bắt buộc nhập tên tài khoản
        [Display(Name = "Tên đăng nhập")] // Nhãn hiển thị cho ô nhập Username
        public string Username { get; set; } = string.Empty; // Tên đăng nhập của người dùng

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")] // Bắt buộc nhập mật khẩu
        [DataType(DataType.Password)] // Định dạng trường nhập liệu ẩn ký tự mật khẩu
        [Display(Name = "Mật khẩu")] // Nhãn hiển thị cho ô nhập Password
        public string Password { get; set; } = string.Empty; // Mật khẩu đăng nhập của người dùng

        [Display(Name = "Ghi nhớ đăng nhập")] // Nhãn hiển thị cho hộp kiểm ghi nhớ phiên đăng nhập
        public bool RememberMe { get; set; } // Cờ lưu Cookie đăng nhập dài hạn trên trình duyệt
    } // Đóng khối lớp LoginViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
