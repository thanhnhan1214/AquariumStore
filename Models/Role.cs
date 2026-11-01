using System.ComponentModel.DataAnnotations; // Nạp thư viện Data Annotations để kiểm tra ràng buộc dữ liệu

namespace AquariumStore.Models // Khai báo không gian tên chứa các thực thể dữ liệu (Entities)
{ // Mở khối namespace
    /// <summary>
    /// Thực thể vai trò người dùng: Admin, Staff, Customer.
    /// </summary>
    public class Role // Khai báo lớp thực thể Role ánh xạ tới bảng Roles trong CSDL
    { // Mở khối lớp Role
        [Key] // Đánh dấu thuộc tính RoleId là khóa chính (Primary Key)
        public int RoleId { get; set; } // Mã định danh vai trò (tự động tăng)

        [Required(ErrorMessage = "Tên vai trò không được để trống.")] // Ràng buộc bắt buộc nhập tên vai trò
        [StringLength(50)] // Giới hạn độ dài tối đa trong CSDL là 50 ký tự
        [Display(Name = "Tên vai trò")] // Nhãn hiển thị trên giao diện người dùng
        public string RoleName { get; set; } = string.Empty; // Tên vai trò ('Admin', 'Staff', 'Customer')

        // Thuộc tính điều hướng (Navigation property): Một vai trò (Role) có nhiều người dùng (Users)
        public ICollection<User> Users { get; set; } = new List<User>(); // Danh sách các tài khoản thuộc vai trò này
    } // Đóng khối lớp Role
} // Đóng khối namespace
