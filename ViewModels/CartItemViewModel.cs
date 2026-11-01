namespace AquariumStore.ViewModels // Khai báo không gian tên chứa các ViewModel truyền dữ liệu ra View
{ // Mở khối không gian tên AquariumStore.ViewModels
    /// <summary>
    /// ViewModel biểu diễn một mục trong giỏ hàng (được lưu trữ trong Session).
    /// </summary>
    public class CartItemViewModel // Lớp dữ liệu đại diện cho 1 dòng sản phẩm trong giỏ hàng
    { // Mở khối lớp CartItemViewModel
        public int ProductId { get; set; } // Mã định danh sản phẩm được chọn mua
        public string ProductName { get; set; } = string.Empty; // Tên hiển thị của sản phẩm
        public decimal Price { get; set; } // Đơn giá bán của sản phẩm (VNĐ)
        public int Quantity { get; set; } // Số lượng sản phẩm khách chọn mua
        public string? ImageUrl { get; set; } // Đường dẫn hình ảnh thu nhỏ của sản phẩm

        /// <summary>
        /// Thành tiền của mục giỏ hàng = Price × Quantity.
        /// </summary>
        public decimal SubTotal => Price * Quantity; // Thuộc tính tự động tính thành tiền bằng đơn giá nhân số lượng
    } // Đóng khối lớp CartItemViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
