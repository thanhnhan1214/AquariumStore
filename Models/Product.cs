using System.ComponentModel.DataAnnotations; // Nạp thư viện kiểm tra hợp lệ dữ liệu (Validation)
using System.ComponentModel.DataAnnotations.Schema; // Nạp thư viện định nghĩa kiểu cột và khóa ngoại trong CSDL

namespace AquariumStore.Models // Khai báo không gian tên chứa các mô hình thực thể
{ // Mở khối namespace
    /// <summary>
    /// Thực thể sản phẩm: Cá cảnh, cây thủy sinh, thiết bị bể cá, phụ kiện.
    /// </summary>
    public class Product // Khai báo lớp thực thể Product ánh xạ tới bảng Products trong SQL Server
    { // Mở khối lớp Product
        [Key] // Đánh dấu ProductId là khóa chính (Primary Key)
        public int ProductId { get; set; } // Mã định danh duy nhất của sản phẩm

        [Required] // Bắt buộc sản phẩm phải thuộc về một danh mục
        [Display(Name = "Danh mục")] // Nhãn hiển thị trên giao diện
        public int CategoryId { get; set; } // Mã danh mục (Khóa ngoại tham chiếu tới bảng Categories)

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")] // Bắt buộc nhập tên sản phẩm
        [StringLength(300)] // Độ dài tối đa của tên sản phẩm là 300 ký tự
        [Display(Name = "Tên sản phẩm")] // Nhãn hiển thị cho tên sản phẩm
        public string ProductName { get; set; } = string.Empty; // Tên gọi thương mại của sản phẩm

        [StringLength(2000)] // Độ dài tối đa của phần mô tả là 2000 ký tự
        [Display(Name = "Mô tả chi tiết")] // Nhãn hiển thị cho mô tả chi tiết
        public string? Description { get; set; } // Thông tin mô tả đặc điểm, kích thước và cách chăm sóc sản phẩm

        [Required(ErrorMessage = "Giá bán không được để trống.")] // Bắt buộc phải nhập giá bán
        [Column(TypeName = "decimal(18,2)")] // Định dạng cột kiểu số thực chính xác decimal(18,2) trong SQL Server
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")] // Ràng buộc giá bán không được âm
        [Display(Name = "Giá bán (VNĐ)")] // Nhãn hiển thị đơn giá
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)] // Định dạng hiển thị số tiền có dấu phân cách hàng nghìn
        public decimal Price { get; set; } // Giá bán niêm yết của sản phẩm (đơn vị: VNĐ)

        [Required] // Bắt buộc có thông tin số lượng tồn kho
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0.")] // Ràng buộc tồn kho >= 0
        [Display(Name = "Tồn kho")] // Nhãn hiển thị số lượng tồn kho
        public int StockQuantity { get; set; } // Số lượng sản phẩm hiện còn trong kho

        [StringLength(500)] // Giới hạn độ dài đường dẫn ảnh tối đa 500 ký tự
        [Display(Name = "Hình ảnh")] // Nhãn hiển thị cho ảnh sản phẩm
        public string? ImageUrl { get; set; } // Đường dẫn tương đối tới tệp hình ảnh trong wwwroot/images/products

        [Display(Name = "Ngày tạo")] // Nhãn hiển thị ngày thêm sản phẩm
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày giờ tạo sản phẩm trên hệ thống

        [Display(Name = "Đang kinh doanh")] // Nhãn hiển thị trạng thái mở bán
        public bool IsActive { get; set; } = true; // Trạng thái kinh doanh (true = đang bán, false = ngừng bán)

        // Các thuộc tính điều hướng (Navigation properties)
        [ForeignKey("CategoryId")] // Khai báo khóa ngoại liên kết qua thuộc tính CategoryId
        public Category Category { get; set; } = null!; // Đối tượng danh mục chứa sản phẩm này

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>(); // Danh sách các dòng chi tiết đơn hàng có chứa sản phẩm này
        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>(); // Danh sách lịch sử biến động tồn kho của sản phẩm này
    } // Đóng khối lớp Product
} // Đóng khối namespace
