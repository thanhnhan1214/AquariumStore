using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra hợp lệ dữ liệu (Validation)
using System.ComponentModel.DataAnnotations.Schema; // Nạp thư viện ánh xạ khóa ngoại và cấu trúc bảng CSDL

namespace AquariumStore.Models // Khai báo namespace chứa các lớp mô hình dữ liệu
{ // Mở khối namespace
    /// <summary>
    /// Thực thể người dùng hệ thống: Quản trị viên, Nhân viên, Khách hàng.
    /// </summary>
    public class User // Khai báo lớp thực thể User ánh xạ tới bảng Users trong SQL Server
    { // Mở khối lớp User
        [Key] // Đánh dấu UserId là khóa chính (Primary Key) của bảng Users
        public int UserId { get; set; } // Mã định danh duy nhất của người dùng

        [Required] // Ràng buộc bắt buộc phải có mã vai trò
        public int RoleId { get; set; } // Mã vai trò (Khóa ngoại tham chiếu tới bảng Roles)

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")] // Bắt buộc nhập tên đăng nhập
        [StringLength(100)] // Giới hạn độ dài tối đa 100 ký tự
        [Display(Name = "Tên đăng nhập")] // Nhãn hiển thị trên biểu mẫu (Form)
        public string Username { get; set; } = string.Empty; // Tên tài khoản dùng để đăng nhập (duy nhất)

        [Required(ErrorMessage = "Mật khẩu không được để trống.")] // Bắt buộc có chuỗi băm mật khẩu
        [StringLength(256)] // Giới hạn độ dài chuỗi băm SHA-256 tối đa 256 ký tự
        public string PasswordHash { get; set; } = string.Empty; // Mật khẩu đã được mã hóa SHA-256 kèm Base64

        [Required(ErrorMessage = "Họ tên không được để trống.")] // Bắt buộc nhập họ và tên người dùng
        [StringLength(150)] // Giới hạn độ dài họ tên tối đa 150 ký tự
        [Display(Name = "Họ và tên")] // Nhãn hiển thị trên giao diện
        public string FullName { get; set; } = string.Empty; // Họ và tên đầy đủ của người dùng

        [Required(ErrorMessage = "Email không được để trống.")] // Bắt buộc nhập địa chỉ Email
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")] // Kiểm tra đúng định dạng địa chỉ Email
        [StringLength(200)] // Giới hạn độ dài Email tối đa 200 ký tự
        public string Email { get; set; } = string.Empty; // Địa chỉ thư điện tử của người dùng (duy nhất)

        [StringLength(20)] // Giới hạn độ dài số điện thoại tối đa 20 ký tự
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")] // Kiểm tra định dạng số điện thoại hợp lệ
        [Display(Name = "Số điện thoại")] // Nhãn hiển thị cho trường số điện thoại
        public string? PhoneNumber { get; set; } // Số điện thoại liên hệ của người dùng (có thể để trống)

        [StringLength(500)] // Giới hạn độ dài địa chỉ tối đa 500 ký tự
        [Display(Name = "Địa chỉ")] // Nhãn hiển thị cho trường địa chỉ
        public string? Address { get; set; } // Địa chỉ nhận hàng mặc định của người dùng

        [Display(Name = "Ngày tạo")] // Nhãn hiển thị ngày đăng ký tài khoản
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Thời điểm tạo tài khoản, mặc định lấy giờ hệ thống hiện tại

        [Display(Name = "Đang hoạt động")] // Nhãn hiển thị trạng thái kích hoạt
        public bool IsActive { get; set; } = true; // Trạng thái tài khoản (true = hoạt động, false = bị khóa)

        // Các thuộc tính điều hướng (Navigation properties) phục vụ truy vấn liên kết bảng
        [ForeignKey("RoleId")] // Chỉ định khóa ngoại liên kết thông qua thuộc tính RoleId
        public Role Role { get; set; } = null!; // Đối tượng vai trò của người dùng này

        public ICollection<Order> Orders { get; set; } = new List<Order>(); // Danh sách các đơn hàng mà người dùng đã đặt
        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>(); // Danh sách các bản ghi lịch sử kho do người dùng thực hiện
    } // Đóng khối lớp User
} // Đóng khối namespace
