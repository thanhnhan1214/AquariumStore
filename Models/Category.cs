using System.ComponentModel.DataAnnotations; // Nạp thư viện Data Annotations để thiết lập ràng buộc dữ liệu

namespace AquariumStore.Models // Khai báo không gian tên chứa các thực thể dữ liệu
{ // Mở khối namespace
    /// <summary>
    /// Thực thể danh mục sản phẩm: Cá cảnh, Thủy sinh, Thiết bị, Phụ kiện, ...
    /// </summary>
    public class Category // Khai báo lớp Category ánh xạ tới bảng Categories trong CSDL
    { // Mở khối lớp Category
        [Key] // Đánh dấu CategoryId là khóa chính (Primary Key)
        public int CategoryId { get; set; } // Mã định danh danh mục sản phẩm (tự động tăng)

        [Required(ErrorMessage = "Tên danh mục không được để trống.")] // Bắt buộc nhập tên danh mục
        [StringLength(200)] // Giới hạn độ dài tên danh mục tối đa 200 ký tự
        [Display(Name = "Tên danh mục")] // Nhãn hiển thị trên giao diện
        public string CategoryName { get; set; } = string.Empty; // Tên danh mục sản phẩm (duy nhất)

        [StringLength(500)] // Giới hạn độ dài mô tả danh mục tối đa 500 ký tự
        [Display(Name = "Mô tả")] // Nhãn hiển thị cho phần mô tả
        public string? Description { get; set; } // Mô tả chi tiết về nhóm sản phẩm trong danh mục

        [Display(Name = "Đang hoạt động")] // Nhãn hiển thị trạng thái hoạt động
        public bool IsActive { get; set; } = true; // Trạng thái hiển thị danh mục (true = hiển thị, false = tạm ẩn)

        // Thuộc tính điều hướng (Navigation property): Một Danh mục có nhiều Sản phẩm (Quan hệ 1-n)
        public ICollection<Product> Products { get; set; } = new List<Product>(); // Danh sách các sản phẩm thuộc danh mục này
    } // Đóng khối lớp Category
} // Đóng khối namespace
