using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext
using AquariumStore.Models; // Nạp không gian tên chứa các thực thể Order, OrderDetail, Product, InventoryLog
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IOrderService
using AquariumStore.ViewModels; // Nạp không gian tên chứa CartItemViewModel
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core

namespace AquariumStore.Services // Khai báo không gian tên cho tầng xử lý nghiệp vụ
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Service xử lý nghiệp vụ đơn hàng: tạo đơn, cập nhật trạng thái, hoàn kho tự động và thống kê doanh thu.
    /// </summary>
    public class OrderService : IOrderService // Lớp thực thi giao diện IOrderService
    { // Mở khối lớp OrderService
        private readonly ApplicationDbContext _context; // Biến ngữ cảnh cơ sở dữ liệu

        public OrderService(ApplicationDbContext context) // Hàm khởi tạo nhận ApplicationDbContext
        { // Mở thân hàm khởi tạo
            _context = context; // Gán DbContext vào biến thành viên
        } // Đóng thân hàm khởi tạo

        public async Task<List<Order>> GetAllAsync() // Lấy toàn bộ danh sách đơn hàng cho Admin/Staff
        { // Mở thân hàm GetAllAsync
            return await _context.Orders.AsNoTracking() // Truy vấn chỉ đọc không theo dõi thực thể để tăng tốc độ tải
                .Include(o => o.User) // Nạp kèm thông tin tài khoản khách hàng đặt đơn
                .Include(o => o.OrderDetails) // Nạp kèm danh sách chi tiết đơn hàng
                    .ThenInclude(od => od.Product) // Nạp tiếp thông tin sản phẩm trong từng dòng chi tiết
                .OrderByDescending(o => o.OrderDate) // Sắp xếp đơn hàng mới nhất lên đầu
                .ToListAsync(); // Thực thi truy vấn và trả về danh sách
        } // Đóng thân hàm GetAllAsync

        public async Task<List<Order>> GetByStatusAsync(string status) // Lọc danh sách đơn hàng theo trạng thái
        { // Mở thân hàm GetByStatusAsync
            return await _context.Orders.AsNoTracking() // Truy vấn chỉ đọc với AsNoTracking
                .Include(o => o.User) // Nạp kèm thông tin khách hàng
                .Include(o => o.OrderDetails) // Nạp kèm chi tiết đơn hàng
                    .ThenInclude(od => od.Product) // Nạp kèm thông tin sản phẩm
                .Where(o => o.Status == status) // Lọc theo đúng chuỗi trạng thái yêu cầu
                .OrderByDescending(o => o.OrderDate) // Sắp xếp theo ngày đặt giảm dần
                .ToListAsync(); // Trả về danh sách đơn hàng thỏa điều kiện
        } // Đóng thân hàm GetByStatusAsync

        public async Task<List<Order>> GetByUserIdAsync(int userId) // Lấy lịch sử đơn hàng của một khách hàng cụ thể
        { // Mở thân hàm GetByUserIdAsync
            return await _context.Orders.AsNoTracking() // Truy vấn chỉ đọc giúp trang lịch sử đơn tải nhanh
                .Include(o => o.OrderDetails) // Nạp kèm các dòng chi tiết đơn hàng
                    .ThenInclude(od => od.Product) // Nạp kèm tên và ảnh sản phẩm
                .Where(o => o.UserId == userId) // Chỉ lấy các đơn hàng thuộc về mã người dùng này
                .OrderByDescending(o => o.OrderDate) // Sắp xếp đơn mới đặt lên trên cùng
                .ToListAsync(); // Trả về danh sách đơn hàng của khách
        } // Đóng thân hàm GetByUserIdAsync

        public async Task<Order?> GetByIdAsync(int id) // Lấy thông tin chi tiết của một đơn hàng theo mã OrderId
        { // Mở thân hàm GetByIdAsync
            return await _context.Orders.AsNoTracking() // Truy vấn không theo dõi phục vụ hiển thị chi tiết đơn hàng
                .Include(o => o.User) // Nạp kèm thông tin khách hàng đặt đơn
                .Include(o => o.OrderDetails) // Nạp kèm danh sách sản phẩm trong đơn
                    .ThenInclude(od => od.Product) // Nạp chi tiết từng sản phẩm
                .FirstOrDefaultAsync(o => o.OrderId == id); // Trả về đơn hàng khớp mã hoặc null
        } // Đóng thân hàm GetByIdAsync

        /// <summary>
        /// Tạo đơn hàng mới: tính tổng tiền, trừ tồn kho, ghi nhận InventoryLog xuất kho trong cùng 1 Transaction.
        /// </summary>
        public async Task<Order> CreateOrderAsync(int userId, string shippingAddress, string recipientPhone, string paymentMethod, List<CartItemViewModel> cartItems) // Phương thức tạo đơn hàng an toàn giao dịch
        { // Mở thân hàm CreateOrderAsync
            using var transaction = await _context.Database.BeginTransactionAsync(); // Bắt đầu một Transaction CSDL để đảm bảo tính toàn vẹn (ACID)
            try // Khối thử thực thi các thao tác tạo đơn và trừ kho
            { // Mở khối try
                var order = new Order // Khởi tạo đối tượng Order mới
                { // Mở khối gán thuộc tính Order
                    UserId = userId, // Gán mã khách hàng đặt hàng
                    OrderDate = DateTime.Now, // Ghi nhận thời điểm đặt hàng hiện tại
                    Status = "Chờ xử lý", // Trạng thái khởi tạo mặc định là "Chờ xử lý"
                    ShippingAddress = shippingAddress, // Địa chỉ giao hàng khách nhập
                    RecipientPhone = recipientPhone, // Số điện thoại người nhận
                    PaymentMethod = paymentMethod, // Phương thức thanh toán (COD hoặc Chuyển khoản)
                    TotalAmount = 0 // Khởi tạo tổng tiền bằng 0, sẽ cập nhật sau khi duyệt từng món
                }; // Đóng khối gán thuộc tính Order

                _context.Orders.Add(order); // Thêm đơn hàng vào DbContext
                await _context.SaveChangesAsync(); // Lưu xuống CSDL để lấy mã OrderId tự tăng

                decimal totalAmount = 0; // Biến cộng dồn tổng giá trị đơn hàng

                foreach (var item in cartItems) // Duyệt qua từng mặt hàng trong giỏ hàng của khách
                { // Mở vòng lặp xử lý từng mặt hàng
                    var product = await _context.Products.FindAsync(item.ProductId); // Tìm sản phẩm trong CSDL để kiểm tra tồn kho thực tế
                    if (product == null || !product.IsActive || product.StockQuantity < item.Quantity) // Nếu sản phẩm không tồn tại, ngừng bán hoặc không đủ tồn kho
                    { // Mở khối báo lỗi thiếu hàng
                        throw new InvalidOperationException( // Ném ngoại lệ nghiệp vụ để hủy giao dịch
                            $"Sản phẩm '{item.ProductName}' không đủ số lượng tồn kho. Còn lại: {product?.StockQuantity ?? 0}."); // Thông báo rõ số lượng còn lại
                    } // Đóng khối báo lỗi thiếu hàng

                    var orderDetail = new OrderDetail // Tạo bản ghi chi tiết đơn hàng cho sản phẩm này
                    { // Mở khối thuộc tính OrderDetail
                        OrderId = order.OrderId, // Liên kết tới mã đơn hàng vừa tạo
                        ProductId = item.ProductId, // Mã sản phẩm được mua
                        Quantity = item.Quantity, // Số lượng mua
                        UnitPrice = product.Price // Lấy đơn giá chuẩn từ CSDL tại thời điểm mua
                    }; // Đóng khối thuộc tính OrderDetail
                    _context.OrderDetails.Add(orderDetail); // Thêm dòng chi tiết vào DbContext

                    product.StockQuantity -= item.Quantity; // Trừ trực tiếp số lượng tồn kho của sản phẩm

                    _context.InventoryLogs.Add(new InventoryLog // Tự động tạo bản ghi nhật ký xuất kho
                    { // Mở khối thuộc tính InventoryLog
                        ProductId = item.ProductId, // Mã sản phẩm biến động kho
                        UserId = userId, // Người dùng phát sinh giao dịch
                        QuantityChange = -item.Quantity, // Số lượng thay đổi mang dấu âm (xuất kho)
                        Reason = $"Xuất kho - Đơn hàng #{order.OrderId}", // Lý do xuất kho kèm mã đơn hàng
                        CreatedAt = DateTime.Now // Thời điểm ghi nhận log
                    }); // Đóng khối thuộc tính InventoryLog

                    totalAmount += product.Price * item.Quantity; // Cộng dồn thành tiền của dòng sản phẩm vào tổng đơn
                } // Đóng vòng lặp xử lý từng mặt hàng

                order.TotalAmount = totalAmount; // Cập nhật tổng số tiền chính xác cho đơn hàng
                await _context.SaveChangesAsync(); // Lưu toàn bộ chi tiết đơn, tồn kho mới và nhật ký kho xuống CSDL
                await transaction.CommitAsync(); // Xác nhận hoàn tất Transaction thành công

                return order; // Trả về đối tượng đơn hàng vừa tạo
            } // Đóng khối try
            catch // Bắt mọi lỗi phát sinh trong quá trình tạo đơn
            { // Mở khối catch
                await transaction.RollbackAsync(); // Hoàn tác (Rollback) toàn bộ thay đổi để không bị lệch tồn kho
                throw; // Ném lại ngoại lệ lên tầng Controller để hiển thị thông báo cho người dùng
            } // Đóng khối catch
        } // Đóng thân hàm CreateOrderAsync

        /// <summary>
        /// Cập nhật trạng thái đơn hàng. Nếu chuyển sang "Đã hủy" → tự động hoàn trả tồn kho và ghi log.
        /// </summary>
        public async Task UpdateStatusAsync(int orderId, string newStatus, int staffUserId) // Phương thức đổi trạng thái đơn hàng
        { // Mở thân hàm UpdateStatusAsync
            using var transaction = await _context.Database.BeginTransactionAsync(); // Mở Transaction bảo vệ quá trình cập nhật và hoàn kho
            try // Khối thử thực thi
            { // Mở khối try
                var order = await _context.Orders // Truy vấn đơn hàng cần cập nhật (có tracking để sửa đổi)
                    .Include(o => o.OrderDetails) // Nạp kèm danh sách chi tiết sản phẩm của đơn
                    .FirstOrDefaultAsync(o => o.OrderId == orderId); // Tìm theo khóa chính OrderId

                if (order == null) // Nếu không tìm thấy đơn hàng
                    throw new InvalidOperationException("Không tìm thấy đơn hàng."); // Báo lỗi ngoại lệ

                var oldStatus = order.Status; // Lưu lại trạng thái cũ trước khi thay đổi
                order.Status = newStatus; // Gán trạng thái mới cho đơn hàng

                // Nếu đơn chuyển sang "Đã hủy" và trước đó chưa từng bị hủy -> hoàn trả lại tồn kho
                if (newStatus == "Đã hủy" && oldStatus != "Đã hủy") // Kiểm tra điều kiện hoàn kho
                { // Mở khối hoàn trả tồn kho
                    foreach (var detail in order.OrderDetails) // Duyệt qua từng sản phẩm trong đơn hàng bị hủy
                    { // Mở vòng lặp hoàn kho từng món
                        var product = await _context.Products.FindAsync(detail.ProductId); // Tìm sản phẩm tương ứng trong bảng Products
                        if (product != null) // Nếu sản phẩm tồn tại
                        { // Mở khối cộng lại kho và ghi log
                            product.StockQuantity += detail.Quantity; // Cộng trả lại số lượng hàng vào tồn kho

                            _context.InventoryLogs.Add(new InventoryLog // Ghi bản ghi nhật ký hoàn kho
                            { // Mở khối thuộc tính InventoryLog
                                ProductId = detail.ProductId, // Mã sản phẩm được hoàn kho
                                UserId = staffUserId, // Mã người thực hiện hủy đơn (Nhân viên/Admin hoặc Khách hàng)
                                QuantityChange = detail.Quantity, // Số lượng cộng lại mang dấu dương (+)
                                Reason = $"Hoàn kho - Đơn hàng #{orderId} bị hủy", // Lý do hoàn kho rõ ràng
                                CreatedAt = DateTime.Now // Thời gian hoàn kho
                            }); // Đóng khối thuộc tính InventoryLog
                        } // Đóng khối cộng lại kho và ghi log
                    } // Đóng vòng lặp hoàn kho từng món
                } // Đóng khối hoàn trả tồn kho

                await _context.SaveChangesAsync(); // Lưu trạng thái đơn hàng và tồn kho mới xuống CSDL
                await transaction.CommitAsync(); // Xác nhận hoàn tất giao dịch
            } // Đóng khối try
            catch // Nếu có lỗi bất kỳ xảy ra
            { // Mở khối catch
                await transaction.RollbackAsync(); // Hoàn tác toàn bộ giao dịch
                throw; // Ném lại lỗi để tầng trên xử lý
            } // Đóng khối catch
        } // Đóng thân hàm UpdateStatusAsync

        /// <summary>
        /// Thống kê doanh thu theo tháng (chỉ tính các đơn hàng đã "Hoàn thành").
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int months = 12) // Lấy dữ liệu biểu đồ doanh thu N tháng gần nhất
        { // Mở thân hàm GetMonthlyRevenueAsync
            var startDate = DateTime.Now.AddMonths(-months + 1); // Tính mốc tháng bắt đầu thống kê
            startDate = new DateTime(startDate.Year, startDate.Month, 1); // Đưa về ngày mùng 1 đầu tháng đó

            var data = await _context.Orders.AsNoTracking() // Truy vấn chỉ đọc bảng Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= startDate) // Chỉ lọc đơn đã "Hoàn thành" từ mốc thời gian bắt đầu
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month }) // Nhóm các đơn hàng theo Năm và Tháng
                .Select(g => new // Chiếu sang đối tượng ẩn danh chứa Năm, Tháng và Tổng doanh thu
                { // Mở khối Select
                    g.Key.Year, // Năm của nhóm
                    g.Key.Month, // Tháng của nhóm
                    Total = g.Sum(o => o.TotalAmount) // Tổng tiền các đơn hoàn thành trong tháng đó
                }) // Đóng khối Select
                .OrderBy(x => x.Year).ThenBy(x => x.Month) // Sắp xếp theo trình tự thời gian tăng dần
                .ToListAsync(); // Thực thi truy vấn SQL

            var result = new Dictionary<string, decimal>(); // Khởi tạo từ điển kết quả ("MM/yyyy" -> Doanh thu)
            for (int i = 0; i < months; i++) // Duyệt đủ từng tháng trong khoảng thống kê (kể cả tháng doanh thu bằng 0)
            { // Mở vòng lặp điền dữ liệu từng tháng
                var date = startDate.AddMonths(i); // Tính tháng thứ i tính từ mốc bắt đầu
                var key = date.ToString("MM/yyyy"); // Định dạng nhãn tháng/năm (ví dụ: "10/2026")
                var found = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month); // Tìm dữ liệu doanh thu của tháng đó
                result[key] = found?.Total ?? 0; // Nếu có thì lấy tổng doanh thu, nếu không có đơn nào thì gán bằng 0
            } // Đóng vòng lặp điền dữ liệu từng tháng

            return result; // Trả về từ điển doanh thu theo tháng phục vụ vẽ biểu đồ Chart.js
        } // Đóng thân hàm GetMonthlyRevenueAsync

        public async Task<decimal> GetTotalRevenueAsync() // Tính tổng doanh thu toàn thời gian của cửa hàng
        { // Mở thân hàm GetTotalRevenueAsync
            return await _context.Orders.AsNoTracking() // Truy vấn chỉ đọc bảng Orders
                .Where(o => o.Status == "Hoàn thành") // Chỉ tính các đơn hàng đã giao thành công ("Hoàn thành")
                .SumAsync(o => o.TotalAmount); // Cộng tổng cột TotalAmount trên SQL Server
        } // Đóng thân hàm GetTotalRevenueAsync

        public async Task<int> GetNewOrdersCountAsync() // Đếm số lượng đơn hàng mới đang chờ duyệt
        { // Mở thân hàm GetNewOrdersCountAsync
            return await _context.Orders.AsNoTracking() // Truy vấn chỉ đọc bảng Orders
                .CountAsync(o => o.Status == "Chờ xử lý"); // Đếm số đơn có trạng thái "Chờ xử lý"
        } // Đóng thân hàm GetNewOrdersCountAsync
    } // Đóng khối lớp OrderService
} // Đóng khối không gian tên AquariumStore.Services
