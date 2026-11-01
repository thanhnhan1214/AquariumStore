using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra hợp lệ dữ liệu
using System.ComponentModel.DataAnnotations.Schema; // Nạp thư viện ánh xạ khóa ngoại CSDL

namespace AquariumStore.Models // Khai báo không gian tên chứa các thực thể dữ liệu
{ // Mở khối namespace
    /// <summary>
    /// Thực thể nhật ký kho: Ghi lại lịch sử nhập/xuất/hoàn kho sản phẩm.
    /// </summary>
    public class InventoryLog // Khai báo lớp InventoryLog ánh xạ tới bảng InventoryLogs trong SQL Server
    { // Mở khối lớp InventoryLog
        [Key] // Đánh dấu LogId là khóa chính (Primary Key)
        public int LogId { get; set; } // Mã bản ghi nhật ký kho (tự động tăng)

        [Required] // Bắt buộc gắn với một sản phẩm cụ thể
        public int ProductId { get; set; } // Mã sản phẩm biến động tồn kho (Khóa ngoại tới bảng Products)

        [Required] // Bắt buộc ghi nhận người thực hiện giao dịch kho
        public int UserId { get; set; } // Mã người dùng thực hiện (Khóa ngoại tới bảng Users)

        [Required] // Bắt buộc có số lượng biến động
        [Display(Name = "Số lượng thay đổi")] // Nhãn hiển thị số lượng tăng/giảm
        public int QuantityChange { get; set; } // Số lượng thay đổi (+ là nhập/hoàn kho, - là xuất bán)

        [Required(ErrorMessage = "Lý do không được để trống.")] // Bắt buộc ghi rõ lý do biến động kho
        [StringLength(500)] // Độ dài tối đa của nội dung lý do là 500 ký tự
        [Display(Name = "Lý do")] // Nhãn hiển thị cột lý do
        public string Reason { get; set; } = string.Empty; // Nội dung lý do nhập hàng, xuất đơn hoặc hoàn kho khi hủy đơn

        [Display(Name = "Thời gian")] // Nhãn hiển thị thời điểm ghi nhận
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày giờ phát sinh giao dịch kho

        // Các thuộc tính điều hướng (Navigation properties)
        [ForeignKey("ProductId")] // Liên kết khóa ngoại tới thực thể Product
        public Product Product { get; set; } = null!; // Thông tin sản phẩm có biến động tồn kho

        [ForeignKey("UserId")] // Liên kết khóa ngoại tới thực thể User
        public User User { get; set; } = null!; // Thông tin nhân viên/người dùng thực hiện thao tác
    } // Đóng khối lớp InventoryLog
} // Đóng khối namespace
