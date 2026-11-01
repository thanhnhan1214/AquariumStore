using AquariumStore.Models; // Nạp không gian tên chứa 7 lớp thực thể (Models) của hệ thống
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core để làm việc với cơ sở dữ liệu
using System.Security.Cryptography; // Nạp thư viện mã hóa để băm mật khẩu SHA256
using System.Text; // Nạp thư viện xử lý chuỗi và bảng mã UTF-8

namespace AquariumStore.Data // Khai báo không gian tên cho tầng truy cập dữ liệu (Data Access Layer)
{ // Mở khối không gian tên AquariumStore.Data
    /// <summary>
    /// DbContext chính của hệ thống Aquarium Store.
    /// Cấu hình Fluent API: decimal precision, cascade delete behavior, unique index và data seeding.
    /// </summary>
    public class ApplicationDbContext : DbContext // Lớp ngữ cảnh cơ sở dữ liệu kế thừa từ DbContext của EF Core
    { // Mở khối lớp ApplicationDbContext
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) // Hàm khởi tạo nhận các tùy chọn cấu hình DbContext từ DI Container
            : base(options) // Truyền tùy chọn cấu hình lên lớp cha DbContext
        { // Mở thân hàm khởi tạo
        } // Đóng thân hàm khởi tạo

        public DbSet<Role> Roles { get; set; } // Tập thực thể đại diện cho bảng Roles (Vai trò) trong SQL Server
        public DbSet<User> Users { get; set; } // Tập thực thể đại diện cho bảng Users (Người dùng) trong SQL Server
        public DbSet<Category> Categories { get; set; } // Tập thực thể đại diện cho bảng Categories (Danh mục) trong SQL Server
        public DbSet<Product> Products { get; set; } // Tập thực thể đại diện cho bảng Products (Sản phẩm) trong SQL Server
        public DbSet<Order> Orders { get; set; } // Tập thực thể đại diện cho bảng Orders (Đơn hàng) trong SQL Server
        public DbSet<OrderDetail> OrderDetails { get; set; } // Tập thực thể đại diện cho bảng OrderDetails (Chi tiết đơn hàng) trong SQL Server
        public DbSet<InventoryLog> InventoryLogs { get; set; } // Tập thực thể đại diện cho bảng InventoryLogs (Nhật ký kho) trong SQL Server

        protected override void OnModelCreating(ModelBuilder modelBuilder) // Ghi đè phương thức cấu hình mô hình dữ liệu bằng Fluent API
        { // Mở thân phương thức OnModelCreating
            base.OnModelCreating(modelBuilder); // Gọi phương thức OnModelCreating của lớp cơ sở DbContext

            // ==========================================
            // 1. CẤU HÌNH ĐỘ CHÍNH XÁC SỐ THẬP PHÂN (DECIMAL PRECISION)
            // ==========================================
            modelBuilder.Entity<Product>() // Chọn thực thể Product để cấu hình
                .Property(p => p.Price) // Chọn thuộc tính Price (Giá bán)
                .HasColumnType("decimal(18,2)"); // Định kiểu cột SQL là decimal(18,2) tránh làm tròn sai lệch tiền tệ

            modelBuilder.Entity<Order>() // Chọn thực thể Order để cấu hình
                .Property(o => o.TotalAmount) // Chọn thuộc tính TotalAmount (Tổng tiền đơn hàng)
                .HasColumnType("decimal(18,2)"); // Định kiểu cột SQL là decimal(18,2)

            modelBuilder.Entity<OrderDetail>() // Chọn thực thể OrderDetail để cấu hình
                .Property(od => od.UnitPrice) // Chọn thuộc tính UnitPrice (Đơn giá tại thời điểm mua)
                .HasColumnType("decimal(18,2)"); // Định kiểu cột SQL là decimal(18,2)

            // ==========================================
            // 2. CẤU HÌNH QUAN HỆ & HÀNH VI XÓA (TRÁNH MULTIPLE CASCADE PATHS)
            // ==========================================
            modelBuilder.Entity<User>() // Chọn thực thể User để cấu hình khóa ngoại tới Role
                .HasOne(u => u.Role) // Một User thuộc về một Role duy nhất
                .WithMany(r => r.Users) // Một Role có thể gắn với nhiều User
                .HasForeignKey(u => u.RoleId) // Khóa ngoại là cột RoleId trên bảng Users
                .OnDelete(DeleteBehavior.Restrict); // Chặn xóa Role nếu vẫn còn User thuộc vai trò đó

            modelBuilder.Entity<Product>() // Chọn thực thể Product để cấu hình khóa ngoại tới Category
                .HasOne(p => p.Category) // Một Product thuộc về một Category duy nhất
                .WithMany(c => c.Products) // Một Category bao gồm nhiều Product
                .HasForeignKey(p => p.CategoryId) // Khóa ngoại là cột CategoryId trên bảng Products
                .OnDelete(DeleteBehavior.Restrict); // Chặn xóa Category nếu vẫn còn Product thuộc danh mục đó

            modelBuilder.Entity<Order>() // Chọn thực thể Order để cấu hình khóa ngoại tới User
                .HasOne(o => o.User) // Một Order thuộc về một User đặt hàng
                .WithMany(u => u.Orders) // Một User có thể có nhiều Order
                .HasForeignKey(o => o.UserId) // Khóa ngoại là cột UserId trên bảng Orders
                .OnDelete(DeleteBehavior.Restrict); // Chặn xóa User nếu đã có lịch sử đơn hàng (tránh lỗi multiple cascade paths)

            modelBuilder.Entity<OrderDetail>() // Chọn thực thể OrderDetail để cấu hình khóa ngoại tới Order
                .HasOne(od => od.Order) // Một dòng OrderDetail thuộc về một Order
                .WithMany(o => o.OrderDetails) // Một Order chứa nhiều dòng OrderDetail
                .HasForeignKey(od => od.OrderId) // Khóa ngoại là cột OrderId trên bảng OrderDetails
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa Order thì tự động xóa các dòng OrderDetail đi kèm

            modelBuilder.Entity<OrderDetail>() // Chọn thực thể OrderDetail để cấu hình khóa ngoại tới Product
                .HasOne(od => od.Product) // Một dòng OrderDetail tham chiếu đến một Product
                .WithMany(p => p.OrderDetails) // Một Product có thể xuất hiện trong nhiều OrderDetail
                .HasForeignKey(od => od.ProductId) // Khóa ngoại là cột ProductId trên bảng OrderDetails
                .OnDelete(DeleteBehavior.Restrict); // Chặn xóa cứng Product nếu sản phẩm đã từng nằm trong đơn hàng

            modelBuilder.Entity<InventoryLog>() // Chọn thực thể InventoryLog để cấu hình khóa ngoại tới Product
                .HasOne(il => il.Product) // Một bản ghi nhật ký kho thuộc về một Product
                .WithMany(p => p.InventoryLogs) // Một Product có nhiều bản ghi biến động kho
                .HasForeignKey(il => il.ProductId) // Khóa ngoại là cột ProductId trên bảng InventoryLogs
                .OnDelete(DeleteBehavior.Restrict); // Chặn xóa cứng Product nếu đã có lịch sử nhật ký kho

            modelBuilder.Entity<InventoryLog>() // Chọn thực thể InventoryLog để cấu hình khóa ngoại tới User
                .HasOne(il => il.User) // Một bản ghi nhật ký kho do một User thực hiện
                .WithMany(u => u.InventoryLogs) // Một User có thể thực hiện nhiều giao dịch kho
                .HasForeignKey(il => il.UserId) // Khóa ngoại là cột UserId trên bảng InventoryLogs
                .OnDelete(DeleteBehavior.Restrict); // Chặn xóa cứng User nếu đã có lịch sử thao tác kho

            // ==========================================
            // 3. CẤU HÌNH CHỈ MỤC DUY NHẤT (UNIQUE INDEXES)
            // ==========================================
            modelBuilder.Entity<User>() // Chọn thực thể User
                .HasIndex(u => u.Username) // Tạo chỉ mục (Index) trên cột Username
                .IsUnique(); // Ràng buộc giá trị Username là duy nhất không trùng lặp

            modelBuilder.Entity<User>() // Chọn thực thể User
                .HasIndex(u => u.Email) // Tạo chỉ mục (Index) trên cột Email
                .IsUnique(); // Ràng buộc giá trị Email là duy nhất không trùng lặp

            modelBuilder.Entity<Category>() // Chọn thực thể Category
                .HasIndex(c => c.CategoryName) // Tạo chỉ mục (Index) trên cột CategoryName
                .IsUnique(); // Ràng buộc tên danh mục là duy nhất

            // ==========================================
            // 4. KHỞI TẠO DỮ LIỆU GỐC (DATA SEEDING)
            // ==========================================
            modelBuilder.Entity<Role>().HasData( // Nạp dữ liệu mặc định cho bảng Roles
                new Role { RoleId = 1, RoleName = "Admin" }, // Vai trò số 1: Quản trị viên (Admin)
                new Role { RoleId = 2, RoleName = "Staff" }, // Vai trò số 2: Nhân viên (Staff)
                new Role { RoleId = 3, RoleName = "Customer" } // Vai trò số 3: Khách hàng (Customer)
            ); // Kết thúc nạp dữ liệu Roles

            modelBuilder.Entity<User>().HasData( // Nạp tài khoản Quản trị viên mặc định (mật khẩu: Admin@123)
                new User // Khởi tạo đối tượng User Admin
                { // Mở khối khởi tạo thuộc tính User
                    UserId = 1, // Mã người dùng cố định là 1
                    RoleId = 1, // Gán vai trò Admin (RoleId = 1)
                    Username = "admin", // Tên đăng nhập: admin
                    PasswordHash = HashPassword("Admin@123"), // Mật khẩu Admin@123 đã băm bằng SHA256 Base64
                    FullName = "Quản trị viên hệ thống", // Họ và tên hiển thị của quản trị viên
                    Email = "admin@aquariumstore.vn", // Địa chỉ email quản trị viên
                    PhoneNumber = "0901234567", // Số điện thoại liên hệ
                    Address = "123 Nguyễn Huệ, Q.1, TP.HCM", // Địa chỉ mặc định
                    CreatedAt = new DateTime(2024, 1, 1), // Ngày tạo cố định phục vụ Migration
                    IsActive = true // Trạng thái tài khoản đang hoạt động
                } // Đóng khối khởi tạo thuộc tính User
            ); // Kết thúc nạp dữ liệu User Admin

            modelBuilder.Entity<Category>().HasData( // Nạp 6 danh mục sản phẩm mẫu ban đầu
                new Category { CategoryId = 1, CategoryName = "Cá cảnh nước ngọt", Description = "Các loại cá cảnh nước ngọt: Betta, Neon, Guppy, Discus, ...", IsActive = true }, // Danh mục 1: Cá cảnh nước ngọt
                new Category { CategoryId = 2, CategoryName = "Cá cảnh nước mặn", Description = "Các loại cá cảnh biển: Nemo, Tang, Angel, ...", IsActive = true }, // Danh mục 2: Cá cảnh nước mặn
                new Category { CategoryId = 3, CategoryName = "Cây thủy sinh", Description = "Các loại cây thủy sinh trang trí bể cá", IsActive = true }, // Danh mục 3: Cây thủy sinh
                new Category { CategoryId = 4, CategoryName = "Thiết bị bể cá", Description = "Bể kính, máy lọc, đèn LED, máy sục khí, ...", IsActive = true }, // Danh mục 4: Thiết bị bể cá
                new Category { CategoryId = 5, CategoryName = "Thức ăn & Thuốc", Description = "Thức ăn cá, thuốc xử lý nước, vitamin, ...", IsActive = true }, // Danh mục 5: Thức ăn & Thuốc
                new Category { CategoryId = 6, CategoryName = "Phụ kiện trang trí", Description = "Sỏi, đá, gỗ lũa, san hô giả, ...", IsActive = true } // Danh mục 6: Phụ kiện trang trí
            ); // Kết thúc nạp dữ liệu Categories

            modelBuilder.Entity<Product>().HasData( // Nạp 10 sản phẩm mẫu ban đầu khớp với ảnh thực tế trong wwwroot/images/products
                new Product { ProductId = 1, CategoryId = 1, ProductName = "Cá Betta Halfmoon", Description = "Cá Betta Halfmoon đuôi tròn, nhiều màu sắc rực rỡ. Dễ nuôi, phù hợp bể nhỏ.", Price = 85000m, StockQuantity = 50, ImageUrl = "/images/products/aba6fc46-7379-482e-96c8-f00bd88f82ea.jpg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 1: Cá Betta Halfmoon
                new Product { ProductId = 2, CategoryId = 1, ProductName = "Cá Neon Tetra", Description = "Cá Neon sọc xanh đỏ, nuôi theo đàn rất đẹp. Kích thước 2-3cm.", Price = 5000m, StockQuantity = 200, ImageUrl = "/images/products/86a23d4d-45f7-4f27-a9ee-26020fb2ef6b.jpg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 2: Cá Neon Tetra
                new Product { ProductId = 3, CategoryId = 1, ProductName = "Cá Guppy Full Red", Description = "Cá Guppy đỏ toàn thân, đuôi rộng. Dễ sinh sản.", Price = 15000m, StockQuantity = 150, ImageUrl = "/images/products/e2de0ebc-c429-4dbb-bbd9-79140928e5f3.jpg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 3: Cá Guppy Full Red
                new Product { ProductId = 4, CategoryId = 2, ProductName = "Cá Nemo (Clownfish)", Description = "Cá hề Nemo cam trắng, biểu tượng cá cảnh biển. Cần bể nước mặn.", Price = 120000m, StockQuantity = 30, ImageUrl = "/images/products/79537482-c5f9-4ad8-be92-d63a735bba76.jpg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 4: Cá Nemo (Clownfish)
                new Product { ProductId = 5, CategoryId = 3, ProductName = "Cây Trân Châu Ngọc Trai", Description = "Cây thủy sinh lá tròn nhỏ, trải thảm đẹp. Cần CO2 và ánh sáng mạnh.", Price = 35000m, StockQuantity = 80, ImageUrl = "/images/products/5aa87c4e-0ff5-4be3-b86a-8d5b41746963.png", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 5: Cây Trân Châu Ngọc Trai
                new Product { ProductId = 6, CategoryId = 3, ProductName = "Cây Rong Đuôi Chồn", Description = "Cây thủy sinh dạng thân, lá mềm mại tạo cảnh quan tự nhiên.", Price = 20000m, StockQuantity = 100, ImageUrl = "/images/products/c0128e7d-215d-4c35-862b-18bd41112678.png", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 6: Cây Rong Đuôi Chồn
                new Product { ProductId = 7, CategoryId = 4, ProductName = "Bể cá kính 60cm", Description = "Bể kính siêu trong 60x30x36cm, kính dày 6mm. Kèm nắp đậy.", Price = 450000m, StockQuantity = 20, ImageUrl = "/images/products/34ce8508-8ce6-4387-acf6-645870e7bd36.jpeg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 7: Bể cá kính 60cm
                new Product { ProductId = 8, CategoryId = 4, ProductName = "Máy lọc Sunsun HW-603B", Description = "Máy lọc thùng ngoài cho bể 60-80cm. Công suất 600L/h.", Price = 380000m, StockQuantity = 15, ImageUrl = "/images/products/b25193ff-dc9f-41ce-a169-22bf756af14b.png", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 8: Máy lọc Sunsun HW-603B
                new Product { ProductId = 9, CategoryId = 5, ProductName = "Thức ăn Tetra Min Flakes", Description = "Thức ăn dạng vảy cho cá cảnh nhiệt đới. Hộp 52g.", Price = 65000m, StockQuantity = 100, ImageUrl = "/images/products/c9ad1a96-8c24-4bd6-baee-ee17e027dbbf.jpg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true }, // Sản phẩm 9: Thức ăn Tetra Min Flakes
                new Product { ProductId = 10, CategoryId = 6, ProductName = "Gỗ lũa Redmoor", Description = "Gỗ lũa tự nhiên tạo cảnh quan bể thủy sinh. Kích thước 20-30cm.", Price = 150000m, StockQuantity = 3, ImageUrl = "/images/products/e3276c08-cce8-43a0-81bc-7abdc7326005.jpg", CreatedAt = new DateTime(2024, 1, 1), IsActive = true } // Sản phẩm 10: Gỗ lũa Redmoor
            ); // Kết thúc nạp dữ liệu Products
        } // Đóng thân phương thức OnModelCreating

        /// <summary>
        /// Băm mật khẩu bằng thuật toán SHA256 và chuyển sang chuỗi Base64.
        /// </summary>
        private static string HashPassword(string password) // Hàm tĩnh hỗ trợ băm mật khẩu khi khởi tạo dữ liệu mẫu
        { // Mở thân hàm HashPassword
            using var sha256 = SHA256.Create(); // Khởi tạo đối tượng băm SHA256 và tự động giải phóng bộ nhớ khi xong
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password)); // Chuyển chuỗi mật khẩu sang mảng byte UTF-8 rồi tính mã băm
            return Convert.ToBase64String(bytes); // Chuyển mảng byte kết quả băm sang chuỗi định dạng Base64
        } // Đóng thân hàm HashPassword
    } // Đóng khối lớp ApplicationDbContext
} // Đóng khối không gian tên AquariumStore.Data
