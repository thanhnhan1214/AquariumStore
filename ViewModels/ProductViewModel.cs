using System.ComponentModel.DataAnnotations; // Nạp thư viện Data Annotations để kiểm tra dữ liệu đầu vào
using Microsoft.AspNetCore.Http; // Nạp thư viện hỗ trợ kiểu IFormFile để tải tệp hình ảnh lên máy chủ

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho form tạo mới hoặc chỉnh sửa sản phẩm phía Quản trị (Admin/Staff).
    /// </summary>
    public class ProductViewModel // Lớp dữ liệu trung gian cho Form thêm/sửa sản phẩm
    { // Mở khối lớp ProductViewModel
        public int ProductId { get; set; } // Mã định danh sản phẩm (bằng 0 khi tạo mới, > 0 khi chỉnh sửa)

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")] // Bắt buộc người quản trị phải chọn một danh mục
        [Display(Name = "Danh mục")] // Nhãn hiển thị trên giao diện Form
        public int CategoryId { get; set; } // Mã danh mục sản phẩm được chọn

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")] // Bắt buộc nhập tên sản phẩm
        [StringLength(300)] // Giới hạn độ dài tên tối đa 300 ký tự
        [Display(Name = "Tên sản phẩm")] // Nhãn hiển thị trên giao diện Form
        public string ProductName { get; set; } = string.Empty; // Tên hiển thị của sản phẩm

        [StringLength(2000)] // Giới hạn độ dài mô tả tối đa 2000 ký tự
        [Display(Name = "Mô tả chi tiết")] // Nhãn hiển thị trên giao diện Form
        public string? Description { get; set; } // Nội dung mô tả đặc điểm, thông số của sản phẩm

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")] // Bắt buộc nhập giá bán
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải >= 0.")] // Kiểm tra giá bán không được âm
        [Display(Name = "Giá bán (VNĐ)")] // Nhãn hiển thị trên giao diện Form
        public decimal Price { get; set; } // Giá bán lẻ của sản phẩm (VNĐ)

        [Required] // Bắt buộc nhập số lượng tồn kho ban đầu
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải >= 0.")] // Số lượng tồn kho không được âm
        [Display(Name = "Tồn kho")] // Nhãn hiển thị trên giao diện Form
        public int StockQuantity { get; set; } // Số lượng tồn kho thực tế

        [Display(Name = "Hình ảnh sản phẩm")] // Nhãn hiển thị cho ô chọn tệp ảnh
        public IFormFile? ImageFile { get; set; } // Tệp hình ảnh tải lên từ máy tính người dùng (tùy chọn)

        /// <summary>
        /// Đường dẫn ảnh hiện tại (dùng khi chỉnh sửa để hiển thị lại ảnh cũ).
        /// </summary>
        public string? ExistingImageUrl { get; set; } // Đường dẫn URL của ảnh sản phẩm hiện có trên máy chủ

        [Display(Name = "Đang kinh doanh")] // Nhãn hiển thị cho công tắc bật/tắt kinh doanh
        public bool IsActive { get; set; } = true; // Trạng thái hiển thị sản phẩm ngoài cửa hàng (mặc định là true)
    } // Đóng khối lớp ProductViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
