using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra ràng buộc dữ liệu
using System.ComponentModel.DataAnnotations.Schema; // Nạp thư viện cấu hình kiểu cột và khóa ngoại CSDL

namespace AquariumStore.Models // Khai báo namespace chứa các thực thể của hệ thống
{ // Mở khối namespace
    /// <summary>
    /// Thực thể đơn hàng: Lưu trữ thông tin giao dịch mua bán.
    /// </summary>
    public class Order // Khai báo lớp thực thể Order ánh xạ tới bảng Orders trong SQL Server
    { // Mở khối lớp Order
        [Key] // Đánh dấu OrderId là khóa chính (Primary Key)
        public int OrderId { get; set; } // Mã đơn hàng tự động tăng

        [Required] // Bắt buộc phải gắn với một tài khoản khách hàng
        public int UserId { get; set; } // Mã người dùng đặt hàng (Khóa ngoại tham chiếu tới bảng Users)

        [Display(Name = "Ngày đặt hàng")] // Nhãn hiển thị ngày tạo đơn
        public DateTime OrderDate { get; set; } = DateTime.Now; // Thời điểm khách hàng đặt đơn

        [Required] // Bắt buộc có tổng số tiền thanh toán
        [Column(TypeName = "decimal(18,2)")] // Định dạng kiểu dữ liệu tiền tệ decimal(18,2) trong SQL Server
        [Display(Name = "Tổng tiền (VNĐ)")] // Nhãn hiển thị tổng tiền
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)] // Định dạng hiển thị tiền tệ có dấu phân cách
        public decimal TotalAmount { get; set; } // Tổng giá trị thanh toán của toàn bộ đơn hàng

        [Required] // Bắt buộc có trạng thái xử lý đơn hàng
        [StringLength(50)] // Độ dài tối đa của chuỗi trạng thái là 50 ký tự
        [Display(Name = "Trạng thái")] // Nhãn hiển thị trạng thái đơn
        public string Status { get; set; } = "Chờ xử lý"; // Trạng thái đơn ('Chờ xử lý', 'Đã xác nhận', 'Đang giao', 'Hoàn thành', 'Đã hủy')

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống.")] // Bắt buộc nhập địa chỉ nhận hàng
        [StringLength(500)] // Giới hạn độ dài địa chỉ giao hàng tối đa 500 ký tự
        [Display(Name = "Địa chỉ giao hàng")] // Nhãn hiển thị địa chỉ nhận hàng
        public string ShippingAddress { get; set; } = string.Empty; // Địa chỉ nhận hàng chi tiết của khách

        [Required(ErrorMessage = "Số điện thoại người nhận không được để trống.")] // Bắt buộc nhập SĐT người nhận
        [StringLength(20)] // Độ dài tối đa của số điện thoại là 20 ký tự
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")] // Kiểm tra định dạng số điện thoại hợp lệ
        [Display(Name = "SĐT người nhận")] // Nhãn hiển thị số điện thoại nhận hàng
        public string RecipientPhone { get; set; } = string.Empty; // Số điện thoại người nhận hàng

        [Required] // Bắt buộc chọn phương thức thanh toán
        [StringLength(50)] // Độ dài tối đa của tên phương thức thanh toán là 50 ký tự
        [Display(Name = "Phương thức thanh toán")] // Nhãn hiển thị hình thức thanh toán
        public string PaymentMethod { get; set; } = "COD"; // Hình thức thanh toán ('COD' hoặc 'Chuyển khoản')

        // Các thuộc tính điều hướng (Navigation properties)
        [ForeignKey("UserId")] // Chỉ định khóa ngoại liên kết thông qua UserId
        public User User { get; set; } = null!; // Thông tin tài khoản khách hàng đã đặt đơn này

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>(); // Danh sách các mặt hàng chi tiết nằm trong đơn hàng này
    } // Đóng khối lớp Order
} // Đóng khối namespace
