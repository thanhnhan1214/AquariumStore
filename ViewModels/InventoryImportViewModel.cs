using System.ComponentModel.DataAnnotations; // Nạp thư viện Data Annotations để kiểm tra tính hợp lệ dữ liệu

namespace AquariumStore.ViewModels // Khai báo không gian tên cho các ViewModel
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel cho form lập phiếu nhập kho hàng hóa.
    /// </summary>
    public class InventoryImportViewModel // Lớp dữ liệu nhận thông tin nhập kho từ Admin/Staff
    { // Mở khối lớp InventoryImportViewModel
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")] // Bắt buộc chọn sản phẩm cần nhập kho
        [Display(Name = "Sản phẩm")] // Nhãn hiển thị trên giao diện Form
        public int ProductId { get; set; } // Mã định danh sản phẩm được nhập thêm hàng

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")] // Bắt buộc nhập số lượng hàng nhập vào
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0.")] // Ràng buộc số lượng nhập phải từ 1 trở lên
        [Display(Name = "Số lượng nhập")] // Nhãn hiển thị trên giao diện Form
        public int Quantity { get; set; } // Số lượng đơn vị sản phẩm nhập thêm vào kho

        [Required(ErrorMessage = "Vui lòng nhập lý do.")] // Bắt buộc ghi rõ lý do nhập kho để đối soát
        [StringLength(500)] // Giới hạn độ dài lý do tối đa 500 ký tự
        [Display(Name = "Lý do / Ghi chú")] // Nhãn hiển thị trên giao diện Form
        public string Reason { get; set; } = "Nhập hàng từ nhà cung cấp"; // Nội dung lý do nhập kho (có giá trị gợi ý mặc định)
    } // Đóng khối lớp InventoryImportViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
