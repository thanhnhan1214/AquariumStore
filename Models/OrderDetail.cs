using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra ràng buộc dữ liệu
using System.ComponentModel.DataAnnotations.Schema; // Nạp thư viện cấu hình ánh xạ bảng/cột trong CSDL

namespace AquariumStore.Models // Khai báo không gian tên chứa các thực thể dữ liệu
{ // Mở khối namespace
    /// <summary>
    /// Thực thể chi tiết đơn hàng: Lưu từng sản phẩm trong đơn.
    /// </summary>
    public class OrderDetail // Khai báo lớp OrderDetail ánh xạ tới bảng OrderDetails trong SQL Server
    { // Mở khối lớp OrderDetail
        [Key] // Đánh dấu OrderDetailId là khóa chính (Primary Key)
        public int OrderDetailId { get; set; } // Mã định danh dòng chi tiết đơn hàng

        [Required] // Bắt buộc phải thuộc về một đơn hàng
        public int OrderId { get; set; } // Mã đơn hàng (Khóa ngoại tham chiếu tới bảng Orders)

        [Required] // Bắt buộc phải tham chiếu tới một sản phẩm
        public int ProductId { get; set; } // Mã sản phẩm (Khóa ngoại tham chiếu tới bảng Products)

        [Required] // Bắt buộc nhập số lượng đặt mua
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")] // Ràng buộc số lượng mua tối thiểu là 1
        [Display(Name = "Số lượng")] // Nhãn hiển thị cột số lượng
        public int Quantity { get; set; } // Số lượng sản phẩm khách đặt mua

        [Required] // Bắt buộc lưu đơn giá tại thời điểm mua
        [Column(TypeName = "decimal(18,2)")] // Định dạng cột kiểu decimal(18,2) trong SQL Server
        [Display(Name = "Đơn giá (VNĐ)")] // Nhãn hiển thị cột đơn giá
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)] // Định dạng hiển thị số tiền
        public decimal UnitPrice { get; set; } // Đơn giá sản phẩm chốt tại thời điểm tạo đơn hàng

        /// <summary>
        /// Thành tiền = Quantity × UnitPrice (thuộc tính tính toán, không lưu DB).
        /// </summary>
        [NotMapped] // Không tạo cột SubTotal trong CSDL vì đây là giá trị tính toán động
        [Display(Name = "Thành tiền")] // Nhãn hiển thị thành tiền của dòng sản phẩm
        public decimal SubTotal => Quantity * UnitPrice; // Công thức tính thành tiền = Số lượng nhân Đơn giá

        // Các thuộc tính điều hướng (Navigation properties)
        [ForeignKey("OrderId")] // Khai báo khóa ngoại liên kết tới đơn hàng cha
        public Order Order { get; set; } = null!; // Đối tượng đơn hàng chứa dòng chi tiết này

        [ForeignKey("ProductId")] // Khai báo khóa ngoại liên kết tới sản phẩm được mua
        public Product Product { get; set; } = null!; // Đối tượng sản phẩm tương ứng với dòng chi tiết này
    } // Đóng khối lớp OrderDetail
} // Đóng khối namespace
