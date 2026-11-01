using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext
using AquariumStore.Models; // Nạp không gian tên chứa thực thể InventoryLog và Product
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IInventoryService
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core

namespace AquariumStore.Services // Khai báo không gian tên cho tầng xử lý nghiệp vụ
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Service xử lý nghiệp vụ kho hàng: nhập/xuất hàng thủ công và truy xuất nhật ký kho.
    /// </summary>
    public class InventoryService : IInventoryService // Lớp thực thi giao diện IInventoryService
    { // Mở khối lớp InventoryService
        private readonly ApplicationDbContext _context; // Biến ngữ cảnh cơ sở dữ liệu

        public InventoryService(ApplicationDbContext context) // Hàm khởi tạo nhận ApplicationDbContext
        { // Mở thân hàm khởi tạo
            _context = context; // Gán đối tượng DbContext vào biến thành viên
        } // Đóng thân hàm khởi tạo

        public async Task<List<InventoryLog>> GetAllLogsAsync() // Lấy toàn bộ lịch sử biến động tồn kho
        { // Mở thân hàm GetAllLogsAsync
            return await _context.InventoryLogs.AsNoTracking() // Truy vấn chỉ đọc giúp tăng tốc độ tải danh sách log
                .Include(l => l.Product) // Nạp kèm thông tin sản phẩm
                .Include(l => l.User) // Nạp kèm thông tin người thực hiện giao dịch kho
                .OrderByDescending(l => l.CreatedAt) // Sắp xếp giao dịch mới nhất lên đầu
                .ToListAsync(); // Trả về danh sách nhật ký kho
        } // Đóng thân hàm GetAllLogsAsync

        public async Task<List<InventoryLog>> GetLogsByProductAsync(int productId) // Lấy lịch sử biến động kho của một sản phẩm cụ thể
        { // Mở thân hàm GetLogsByProductAsync
            return await _context.InventoryLogs.AsNoTracking() // Truy vấn chỉ đọc bảng InventoryLogs
                .Include(l => l.Product) // Nạp kèm thông tin sản phẩm
                .Include(l => l.User) // Nạp kèm thông tin người thực hiện
                .Where(l => l.ProductId == productId) // Lọc theo đúng mã sản phẩm cần tra cứu
                .OrderByDescending(l => l.CreatedAt) // Sắp xếp theo thời gian mới nhất trước
                .ToListAsync(); // Trả về danh sách nhật ký kho của sản phẩm đó
        } // Đóng thân hàm GetLogsByProductAsync

        /// <summary>
        /// Điều chỉnh tồn kho: cập nhật StockQuantity và tự động sinh bản ghi InventoryLog trong cùng Transaction.
        /// </summary>
        public async Task ImportStockAsync(int productId, int quantity, string reason, int userId) // Phương thức nhập thêm hoặc điều chỉnh giảm tồn kho
        { // Mở thân hàm ImportStockAsync
            using var transaction = await _context.Database.BeginTransactionAsync(); // Mở Transaction CSDL để đảm bảo tính đồng bộ giữa tồn kho và log
            try // Khối thử thực thi
            { // Mở khối try
                var product = await _context.Products.FindAsync(productId); // Tìm sản phẩm cần điều chỉnh tồn kho
                if (product == null) // Nếu không tìm thấy sản phẩm
                    throw new InvalidOperationException("Không tìm thấy sản phẩm."); // Ném lỗi ngoại lệ

                if (product.StockQuantity + quantity < 0) // Kiểm tra nếu điều chỉnh giảm vượt quá số lượng tồn kho hiện có
                    throw new InvalidOperationException($"Không thể giảm quá số lượng tồn kho hiện tại ({product.StockQuantity})."); // Chặn tồn kho âm

                product.StockQuantity += quantity; // Cộng (hoặc trừ nếu quantity âm) số lượng tồn kho của sản phẩm

                var log = new InventoryLog // Khởi tạo bản ghi nhật ký biến động tồn kho
                { // Mở khối gán thuộc tính InventoryLog
                    ProductId = productId, // Mã sản phẩm được điều chỉnh
                    UserId = userId, // Mã nhân viên hoặc Admin thực hiện
                    QuantityChange = quantity, // Số lượng biến động (+ nhập kho / - xuất hủy)
                    Reason = reason, // Lý do điều chỉnh kho
                    CreatedAt = DateTime.Now // Thời gian thực hiện giao dịch
                }; // Đóng khối gán thuộc tính InventoryLog
                _context.InventoryLogs.Add(log); // Thêm bản ghi nhật ký vào DbContext

                await _context.SaveChangesAsync(); // Lưu thay đổi của cả bảng Products và InventoryLogs xuống CSDL
                await transaction.CommitAsync(); // Xác nhận hoàn tất giao dịch
            } // Đóng khối try
            catch // Bắt lỗi nếu có sự cố xảy ra
            { // Mở khối catch
                await transaction.RollbackAsync(); // Hoàn tác mọi thay đổi trong Transaction
                throw; // Ném lại ngoại lệ lên Controller
            } // Đóng khối catch
        } // Đóng thân hàm ImportStockAsync
    } // Đóng khối lớp InventoryService
} // Đóng khối không gian tên AquariumStore.Services
