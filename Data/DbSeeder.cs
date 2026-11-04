using AquariumStore.Models; // Nạp không gian tên chứa các lớp thực thể (Models)
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core để truy vấn và cập nhật CSDL

namespace AquariumStore.Data // Khai báo không gian tên cho tầng truy cập dữ liệu
{ // Mở khối không gian tên AquariumStore.Data
    /// <summary>
    /// Khởi tạo dữ liệu mẫu phong phú khi ứng dụng chạy (Idempotent - chạy nhiều lần không bị trùng).
    /// Bổ sung tài khoản Staff/Customer, danh mục & sản phẩm Tép Cảnh - Cầu Rêu - Cần Câu và đơn hàng theo tháng.
    /// </summary>
    public static class DbSeeder // Lớp tĩnh chịu trách nhiệm nạp dữ liệu mẫu tự động khi khởi động
    { // Mở khối lớp DbSeeder
        public static async Task SeedAsync(ApplicationDbContext context) // Phương thức bất đồng bộ thực thi migrate và seed dữ liệu
        { // Mở thân phương thức SeedAsync
            await context.Database.MigrateAsync(); // Tự động áp dụng mọi Migration chưa chạy để tạo hoặc cập nhật bảng trong SQL Server

            // 1. Đảm bảo bảng Roles luôn có đủ 3 vai trò cơ bản
            if (!await context.Roles.AnyAsync()) // Kiểm tra nếu bảng Roles đang trống
            { // Mở khối thêm Roles
                context.Roles.AddRange( // Thêm hàng loạt 3 vai trò vào DbContext
                    new Role { RoleName = "Admin" }, // Vai trò Quản trị viên
                    new Role { RoleName = "Staff" }, // Vai trò Nhân viên
                    new Role { RoleName = "Customer" }); // Vai trò Khách hàng
                await context.SaveChangesAsync(); // Lưu 3 vai trò xuống cơ sở dữ liệu
            } // Đóng khối thêm Roles

            var roles = await context.Roles.AsNoTracking().ToDictionaryAsync(r => r.RoleName, r => r.RoleId); // Tải danh sách Role thành từ điển (RoleName -> RoleId) không theo dõi

            // 2. Bổ sung các tài khoản mẫu nếu chưa tồn tại (tối ưu truy vấn 1 lần lấy danh sách Username hiện có)
            var existingUsernames = await context.Users.AsNoTracking().Select(u => u.Username).ToHashSetAsync(); // Lấy tập hợp tất cả Username đã có trong CSDL
            var sampleUsers = new[] // Khai báo mảng các tài khoản mẫu cần bổ sung
            { // Mở mảng sampleUsers
                new User // Tài khoản mẫu Nhân viên (Staff)
                { // Mở khối khởi tạo Staff
                    RoleId = roles["Staff"], // Gán RoleId của vai trò Staff
                    Username = "staff", // Tên đăng nhập: staff
                    PasswordHash = Hash("Staff@123"), // Mật khẩu Staff@123 đã băm SHA256
                    FullName = "Nguyễn Hoàng Nam (Nhân viên)", // Họ tên đầy đủ của nhân viên
                    Email = "staff@aquariumstore.vn", // Email của nhân viên
                    PhoneNumber = "0902345678", // Số điện thoại của nhân viên
                    Address = "45 Lê Lợi, Quận 1, TP.HCM", // Địa chỉ của nhân viên
                    CreatedAt = DateTime.Now.AddMonths(-4), // Ngày tạo cách đây 4 tháng
                    IsActive = true // Tài khoản đang hoạt động
                }, // Đóng khối khởi tạo Staff
                new User // Tài khoản mẫu Khách hàng 1
                { // Mở khối khởi tạo Khách hàng 1
                    RoleId = roles["Customer"], // Gán RoleId của vai trò Customer
                    Username = "customer", // Tên đăng nhập: customer
                    PasswordHash = Hash("Customer@123"), // Mật khẩu Customer@123 đã băm SHA256
                    FullName = "Trần Minh Anh", // Họ tên khách hàng 1
                    Email = "minhanh@gmail.com", // Email khách hàng 1
                    PhoneNumber = "0918223344", // Số điện thoại khách hàng 1
                    Address = "88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM", // Địa chỉ khách hàng 1
                    CreatedAt = DateTime.Now.AddMonths(-3), // Ngày tạo cách đây 3 tháng
                    IsActive = true // Tài khoản đang hoạt động
                }, // Đóng khối khởi tạo Khách hàng 1
                new User // Tài khoản mẫu Khách hàng 2
                { // Mở khối khởi tạo Khách hàng 2
                    RoleId = roles["Customer"], // Gán RoleId của vai trò Customer
                    Username = "khachhang2", // Tên đăng nhập: khachhang2
                    PasswordHash = Hash("Customer@123"), // Mật khẩu Customer@123 đã băm SHA256
                    FullName = "Lê Quốc Bảo", // Họ tên khách hàng 2
                    Email = "quocbao.aqua@gmail.com", // Email khách hàng 2
                    PhoneNumber = "0987654321", // Số điện thoại khách hàng 2
                    Address = "210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM", // Địa chỉ khách hàng 2
                    CreatedAt = DateTime.Now.AddMonths(-2), // Ngày tạo cách đây 2 tháng
                    IsActive = true // Tài khoản đang hoạt động
                }, // Đóng khối khởi tạo Khách hàng 2
                new User // Tài khoản mẫu Khách hàng 3
                { // Mở khối khởi tạo Khách hàng 3
                    RoleId = roles["Customer"], // Gán RoleId của vai trò Customer
                    Username = "khachhang3", // Tên đăng nhập: khachhang3
                    PasswordHash = Hash("Customer@123"), // Mật khẩu Customer@123 đã băm SHA256
                    FullName = "Phạm Thu Hà", // Họ tên khách hàng 3
                    Email = "thuha.aquascape@gmail.com", // Email khách hàng 3
                    PhoneNumber = "0933445566", // Số điện thoại khách hàng 3
                    Address = "15 Phan Xích Long, Quận Phú Nhuận, TP.HCM", // Địa chỉ khách hàng 3
                    CreatedAt = DateTime.Now.AddMonths(-1), // Ngày tạo cách đây 1 tháng
                    IsActive = true // Tài khoản đang hoạt động
                } // Đóng khối khởi tạo Khách hàng 3
            }; // Đóng mảng sampleUsers

            var addedAnyUser = false; // Biến cờ đánh dấu có tài khoản mới nào cần lưu hay không
            foreach (var u in sampleUsers) // Duyệt qua từng tài khoản mẫu
            { // Mở vòng lặp kiểm tra tài khoản
                if (!existingUsernames.Contains(u.Username)) // Nếu Username chưa tồn tại trong CSDL
                { // Mở khối thêm tài khoản mới
                    context.Users.Add(u); // Đưa tài khoản mới vào bộ nhớ đệm của DbContext
                    addedAnyUser = true; // Đánh dấu đã có thay đổi cần lưu
                } // Đóng khối thêm tài khoản mới
            } // Đóng vòng lặp kiểm tra tài khoản
            if (addedAnyUser) // Nếu có ít nhất một tài khoản mới được thêm
            { // Mở khối lưu tài khoản
                await context.SaveChangesAsync(); // Thực thi lệnh INSERT xuống SQL Server một lần duy nhất
            } // Đóng khối lưu tài khoản

            // 3. Đảm bảo có đủ 2 danh mục chuyên biệt: "Tép cảnh & Cầu rêu" và "Cần câu & Dụng cụ"
            var existingCatNames = await context.Categories.AsNoTracking().Select(c => c.CategoryName).ToHashSetAsync(); // Lấy danh sách tên các danh mục hiện có
            var extraCategories = new[] // Khai báo mảng danh mục mở rộng chuyên Tép Cảnh & Cần Câu
            { // Mở mảng extraCategories
                new Category // Danh mục Tép cảnh & Cầu rêu thủy sinh
                { // Mở khối khởi tạo danh mục Tép cảnh
                    CategoryName = "Tép cảnh & Cầu rêu", // Tên danh mục hiển thị
                    Description = "Các dòng tép màu Neocaridina, tép ong Caridina cao cấp và cầu rêu Marimo Nhật Bản", // Mô tả chi tiết danh mục
                    IsActive = true // Trạng thái đang hoạt động
                }, // Đóng khối khởi tạo danh mục Tép cảnh
                new Category // Danh mục Cần câu & Dụng cụ thủy sinh
                { // Mở khối khởi tạo danh mục Cần câu
                    CategoryName = "Cần câu & Dụng cụ", // Tên danh mục hiển thị
                    Description = "Cần câu tôm tép giải trí Carbon, cần câu Lure UL, vợt bắt tép 3D Inox và dụng cụ chăm sóc bể", // Mô tả chi tiết danh mục
                    IsActive = true // Trạng thái đang hoạt động
                } // Đóng khối khởi tạo danh mục Cần câu
            }; // Đóng mảng extraCategories

            var addedAnyCategory = false; // Biến cờ kiểm tra xem có danh mục mới nào được thêm không
            foreach (var cat in extraCategories) // Duyệt qua từng danh mục mở rộng
            { // Mở vòng lặp kiểm tra danh mục
                if (!existingCatNames.Contains(cat.CategoryName)) // Nếu tên danh mục chưa có trong CSDL
                { // Mở khối thêm danh mục mới
                    context.Categories.Add(cat); // Thêm danh mục vào DbContext
                    addedAnyCategory = true; // Bật cờ cần lưu thay đổi
                } // Đóng khối thêm danh mục mới
            } // Đóng vòng lặp kiểm tra danh mục
            if (addedAnyCategory) // Nếu có danh mục mới được thêm
            { // Mở khối lưu danh mục
                await context.SaveChangesAsync(); // Lưu danh mục mới xuống SQL Server
            } // Đóng khối lưu danh mục

            // 4. Bổ sung đầy đủ các sản phẩm mẫu (bao gồm Cá cảnh, Cây thủy sinh, Tép cảnh, Cầu rêu & Cần câu) theo tên không trùng lặp
            var catDict = await context.Categories.AsNoTracking().ToDictionaryAsync(c => c.CategoryName, c => c.CategoryId); // Tạo từ điển ánh xạ Tên danh mục -> CategoryId
            var existingProdNames = await context.Products.AsNoTracking().Select(p => p.ProductName).ToHashSetAsync(); // Lấy tập hợp tên sản phẩm đã tồn tại trong CSDL

            int catFreshwater = catDict.GetValueOrDefault("Cá cảnh nước ngọt", 1); // Lấy mã danh mục Cá cảnh nước ngọt
            int catSaltwater = catDict.GetValueOrDefault("Cá cảnh nước mặn", 2); // Lấy mã danh mục Cá cảnh nước mặn
            int catPlant = catDict.GetValueOrDefault("Cây thủy sinh", 3); // Lấy mã danh mục Cây thủy sinh
            int catEquipment = catDict.GetValueOrDefault("Thiết bị bể cá", 4); // Lấy mã danh mục Thiết bị bể cá
            int catShrimp = catDict.GetValueOrDefault("Tép cảnh & Cầu rêu", catFreshwater); // Lấy mã danh mục Tép cảnh & Cầu rêu
            int catRod = catDict.GetValueOrDefault("Cần câu & Dụng cụ", catEquipment); // Lấy mã danh mục Cần câu & Dụng cụ

            var additionalProducts = new[] // Khai báo danh sách các sản phẩm mở rộng cần đảm bảo luôn có trong cửa hàng
            { // Mở mảng additionalProducts
                new Product // Sản phẩm mở rộng 1: Cá Đĩa Lam Tự Nhiên
                { // Mở khối thuộc tính
                    CategoryId = catFreshwater, // Thuộc danh mục Cá cảnh nước ngọt
                    ProductName = "Cá Đĩa (Discus) Lam Tự Nhiên", // Tên sản phẩm
                    Description = "Vua của các loài cá cảnh nước ngọt. Dáng tròn chuẩn, vây căng, màu xanh lam ánh kim nổi bật trong bể thủy sinh.", // Mô tả chi tiết
                    Price = 350000m, // Giá bán 350.000đ
                    StockQuantity = 18, // Số lượng tồn kho
                    ImageUrl = "/images/products/0a29b17d-1db2-4d16-a637-bbcbe7d3b710.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-20), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm mở rộng 2: Cá Blue Tang
                { // Mở khối thuộc tính
                    CategoryId = catSaltwater, // Thuộc danh mục Cá cảnh nước mặn
                    ProductName = "Cá Blue Tang (Dory Biển)", // Tên sản phẩm
                    Description = "Cá cảnh biển màu xanh coban rực rỡ, tính tình hiền lành, bơi năng động. Phù hợp bể cá biển từ 100L trở lên.", // Mô tả chi tiết
                    Price = 420000m, // Giá bán 420.000đ
                    StockQuantity = 12, // Số lượng tồn kho
                    ImageUrl = "/images/products/b38b0b67-b08d-4c43-8bc3-dc478f517fd4.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-15), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm mở rộng 3: Cây Ráy Nana Petite Gắn Lũa
                { // Mở khối thuộc tính
                    CategoryId = catPlant, // Thuộc danh mục Cây thủy sinh
                    ProductName = "Cây Ráy Nana Petite Gắn Lũa", // Tên sản phẩm
                    Description = "Ráy thủy sinh lá nhỏ gắn sẵn trên giá thể lũa tự nhiên. Không cần CO2 cao, cực kỳ bền khỏe và rất hợp cho bể tép cảnh.", // Mô tả chi tiết
                    Price = 95000m, // Giá bán 95.000đ
                    StockQuantity = 25, // Số lượng tồn kho
                    ImageUrl = "/images/products/fe590f58-f6db-4451-9745-4217b52b5e28.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-10), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm mở rộng 4: Đèn LED Thủy Sinh WRGB Chihiros 60cm
                { // Mở khối thuộc tính
                    CategoryId = catEquipment, // Thuộc danh mục Thiết bị bể cá
                    ProductName = "Đèn LED Thủy Sinh WRGB Chihiros 60cm", // Tên sản phẩm
                    Description = "Đèn LED quang phổ WRGB giúp cây thủy sinh lên màu đỏ căng và tép cảnh rực rỡ. Tích hợp điều khiển tăng giảm độ sáng.", // Mô tả chi tiết
                    Price = 890000m, // Giá bán 890.000đ
                    StockQuantity = 10, // Số lượng tồn kho
                    ImageUrl = "/images/products/4c7df9cb-a31d-48c8-b153-eabd2c5888eb.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-5), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Tép cảnh 1: Tép Đỏ Red Cherry (Fire Red)
                { // Mở khối thuộc tính
                    CategoryId = catShrimp, // Thuộc danh mục Tép cảnh & Cầu rêu
                    ProductName = "Tép Đỏ Red Cherry (Fire Red Neocaridina)", // Tên sản phẩm
                    Description = "Dòng tép màu Neocaridina đỏ rực toàn thân, sức sống cực khỏe, chăm dọn rêu hại trên lá cây thủy sinh và sinh sản nhanh. Thông số: pH 6.5-7.5, TDS 120-250, nhiệt độ 22-28°C.", // Mô tả chi tiết
                    Price = 18000m, // Giá bán 18.000đ / con
                    StockQuantity = 120, // Tồn kho 120 con
                    ImageUrl = "/images/products/9334c666-78c8-4f0f-98a2-67a3a8f45865.png", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-4), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Tép cảnh 2: Tép Xanh Blue Dream Sapphire
                { // Mở khối thuộc tính
                    CategoryId = catShrimp, // Thuộc danh mục Tép cảnh & Cầu rêu
                    ProductName = "Tép Xanh Blue Dream Sapphire", // Tên sản phẩm
                    Description = "Tép màu xanh ngọc bích đậm sâu nổi bật trên nền cát trắng hoặc rêu xanh. Dễ nuôi không cần Chiller, thích hợp cho cả bể nano để bàn.", // Mô tả chi tiết
                    Price = 25000m, // Giá bán 25.000đ / con
                    StockQuantity = 90, // Tồn kho 90 con
                    ImageUrl = "/images/products/874c7573-624a-4bcc-95fc-46907955cfbd.JPG", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-3), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Tép cảnh 3: Tép Ong Đỏ Red Bee PRL Grade SS
                { // Mở khối thuộc tính
                    CategoryId = catShrimp, // Thuộc danh mục Tép cảnh & Cầu rêu
                    ProductName = "Tép Ong Đỏ Red Bee PRL (Grade SS)", // Tên sản phẩm
                    Description = "Dòng tép ong Caridina Pure Red Line thuần chủng với các khoang trắng sứ dày xen kẽ đỏ tươi sắc nét. Phù hợp bể nền công nghiệp (ADA/Benibachi), pH 5.8-6.5, TDS 100-140, nhiệt độ 22-25°C.", // Mô tả chi tiết
                    Price = 120000m, // Giá bán 120.000đ / con
                    StockQuantity = 35, // Tồn kho 35 con
                    ImageUrl = "/images/products/d68d454c-b7b8-4a12-96b4-79beae404318.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-3), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Tép cảnh 4: Tép Galaxy Pinto Black & Red
                { // Mở khối thuộc tính
                    CategoryId = catShrimp, // Thuộc danh mục Tép cảnh & Cầu rêu
                    ProductName = "Tép Galaxy Pinto Black & Red Cao Cấp", // Tên sản phẩm
                    Description = "Siêu phẩm tép cảnh dòng Caridina cao cấp với chấm sao Galaxy hai bên má và sọc lưng Zebra sắc sảo. Tuyển chọn từng cá thể khỏe mạnh, chân đều màu.", // Mô tả chi tiết
                    Price = 250000m, // Giá bán 250.000đ / con
                    StockQuantity = 18, // Tồn kho 18 con
                    ImageUrl = "/images/products/ad4606ff-f34e-4a96-8845-08137cb84942.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-2), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Cầu rêu: Cầu Rêu Marimo Nhật Bản
                { // Mở khối thuộc tính
                    CategoryId = catShrimp, // Thuộc danh mục Tép cảnh & Cầu rêu
                    ProductName = "Cầu Rêu Marimo Nhật Bản (Ổ Vi Sinh Bể Tép)", // Tên sản phẩm
                    Description = "Quả cầu rêu nhung xanh tròn tự nhiên đường kính 4-5cm. Giúp hấp thụ Nitrate, tạo nơi trú ẩn và nguồn thức ăn vi sinh tự nhiên tuyệt vời cho tép cảnh và tép con.", // Mô tả chi tiết
                    Price = 45000m, // Giá bán 45.000đ / quả
                    StockQuantity = 65, // Tồn kho 65 quả
                    ImageUrl = "/images/products/c28623f8-536c-44c9-b573-6f3fcbdc1e8b.jpg", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-2), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Cần câu 1: Cần Câu Tôm Tép Mini Carbon 1.5m
                { // Mở khối thuộc tính
                    CategoryId = catRod, // Thuộc danh mục Cần câu & Dụng cụ
                    ProductName = "Cần Câu Tôm Tép Mini Carbon Siêu Nhẹ 1.5m", // Tên sản phẩm
                    Description = "Cần câu tôm/tép giải trí chất liệu sợi Carbon cao cấp siêu nhẹ (chỉ 38g), đọt dịu cảm giác cực nhạy khi tôm tép nhấp mồi. Thu gọn 38cm tiện lợi mang đi.", // Mô tả chi tiết
                    Price = 195000m, // Giá bán 195.000đ / chiếc
                    StockQuantity = 25, // Tồn kho 25 chiếc
                    ImageUrl = "/images/products/342df03d-388b-422a-960b-7a9460c05b3a.png", // Đường dẫn ảnh thực tế (PNG 32-bit đã tách nền)
                    CreatedAt = DateTime.Now.AddDays(-1), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Cần câu 2: Cần Câu Lure Carbon UL 2 Khúc 1.8m
                { // Mở khối thuộc tính
                    CategoryId = catRod, // Thuộc danh mục Cần câu & Dụng cụ
                    ProductName = "Cần Câu Lure Carbon UL 2 Khúc 1.8m", // Tên sản phẩm
                    Description = "Cần câu cá Lure Ultra-Light (UL) sợi Carbon xoắn X chịu tải cao, dàn khoen sứ chống xoắn dây, tay cầm bần êm ái chuyên trị cá suối, cá rô, cá lóc nhỏ.", // Mô tả chi tiết
                    Price = 460000m, // Giá bán 460.000đ / chiếc
                    StockQuantity = 15, // Tồn kho 15 chiếc
                    ImageUrl = "/images/products/7599a23e-022b-4f2f-84f9-fc8af2c51f24.png", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-1), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                }, // Đóng khối thuộc tính
                new Product // Sản phẩm Dụng cụ tép: Vợt Bắt Tép Cảnh 3D Cán Rút Inox
                { // Mở khối thuộc tính
                    CategoryId = catRod, // Thuộc danh mục Cần câu & Dụng cụ
                    ProductName = "Vợt Bắt Tép Cảnh 3D Cán Rút Inox Không Gỉ", // Tên sản phẩm
                    Description = "Vợt lưới 3D giữ form cứng cáp dưới nước, mắt lưới siêu mịn không làm gãy râu hay tổn thương chân tép cảnh. Cán Inox 304 rút gọn 18cm – kéo dài 55cm.", // Mô tả chi tiết
                    Price = 35000m, // Giá bán 35.000đ / chiếc
                    StockQuantity = 50, // Tồn kho 50 chiếc
                    ImageUrl = "/images/products/6034ce93-d457-4bfb-b1aa-cbfb766d3237.png", // Đường dẫn ảnh thực tế
                    CreatedAt = DateTime.Now.AddDays(-1), // Ngày tạo
                    IsActive = true // Đang kinh doanh
                } // Đóng khối thuộc tính
            }; // Đóng mảng additionalProducts

            var addedAnyProduct = false; // Biến cờ kiểm tra xem có sản phẩm mới nào cần lưu không
            foreach (var p in additionalProducts) // Duyệt qua danh sách sản phẩm mở rộng
            { // Mở vòng lặp kiểm tra sản phẩm
                if (!existingProdNames.Contains(p.ProductName)) // Nếu sản phẩm chưa có trong CSDL
                { // Mở khối thêm sản phẩm mới
                    context.Products.Add(p); // Đưa sản phẩm vào bộ nhớ đệm của DbContext
                    addedAnyProduct = true; // Đánh dấu cần lưu xuống CSDL
                } // Đóng khối thêm sản phẩm mới
            } // Đóng vòng lặp kiểm tra sản phẩm
            if (addedAnyProduct) // Nếu có ít nhất 1 sản phẩm mới được thêm
            { // Mở khối lưu sản phẩm
                await context.SaveChangesAsync(); // Thực thi lệnh INSERT xuống SQL Server
            } // Đóng khối lưu sản phẩm

            // 5. Bổ sung đơn hàng mẫu theo nhiều tháng để biểu đồ doanh thu Dashboard sinh động
            if (await context.Orders.CountAsync() < 6) // Nếu tổng số đơn hàng trong hệ thống ít hơn 6 đơn
            { // Mở khối tạo đơn hàng mẫu
                var customer = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == "customer") // Tìm khách hàng mẫu 1
                               ?? await context.Users.AsNoTracking().FirstAsync(); // Nếu không có thì lấy người dùng đầu tiên
                var customer2 = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == "khachhang2") ?? customer; // Tìm khách hàng mẫu 2
                var customer3 = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == "khachhang3") ?? customer; // Tìm khách hàng mẫu 3
                var products = await context.Products.AsNoTracking().OrderBy(p => p.ProductId).ToListAsync(); // Lấy danh sách sản phẩm sắp xếp theo mã

                if (products.Count >= 8) // Đảm bảo có đủ ít nhất 8 sản phẩm để tạo chi tiết đơn hàng
                { // Mở khối khởi tạo danh sách đơn hàng mẫu
                    var now = DateTime.Now; // Lấy mốc thời gian hiện tại
                    var seedOrders = new List<(User user, DateTime date, string status, string payMethod, List<(Product prod, int qty)> items)> // Khai báo cấu trúc dữ liệu đơn hàng mẫu
                    { // Mở danh sách 6 đơn hàng mẫu trải đều các tháng
                        (customer, now.AddMonths(-4).AddDays(-3), "Hoàn thành", "Chuyển khoản", // Đơn hàng cách đây 4 tháng
                            new() { (products[6], 1), (products[7], 1), (products[4], 3) }), // Mua bể cá, máy lọc và cây thủy sinh
                        (customer2, now.AddMonths(-3).AddDays(-5), "Hoàn thành", "COD", // Đơn hàng cách đây 3 tháng
                            new() { (products[0], 4), (products[1], 20), (products[8], 2) }), // Mua cá Betta, đàn cá Neon và thức ăn
                        (customer3, now.AddMonths(-2).AddDays(-2), "Hoàn thành", "Chuyển khoản", // Đơn hàng cách đây 2 tháng
                            new() { (products[6], 1), (products[3], 3), (products[9], 2) }), // Mua bể cá, cá Nemo và gỗ lũa
                        (customer, now.AddMonths(-1).AddDays(-6), "Hoàn thành", "Chuyển khoản", // Đơn hàng cách đây 1 tháng
                            new() { (products[7], 2), (products[4], 5), (products[5], 5) }), // Mua máy lọc và combo cây thủy sinh
                        (customer2, now.AddDays(-3), "Đang giao", "Chuyển khoản", // Đơn hàng cách đây 3 ngày đang giao
                            new() { (products[0], 2), (products[2], 10), (products[8], 1) }), // Mua cá Betta, Guppy và thức ăn
                        (customer3, now.AddHours(-6), "Chờ xử lý", "COD", // Đơn hàng mới đặt cách đây 6 giờ
                            new() { (products[3], 2), (products[4], 2) }) // Mua cá Nemo và cây Trân Châu
                    }; // Đóng danh sách 6 đơn hàng mẫu

                    foreach (var so in seedOrders) // Duyệt qua từng đơn hàng mẫu để thêm vào CSDL
                    { // Mở vòng lặp tạo đơn hàng
                        var total = so.items.Sum(i => i.prod.Price * i.qty); // Tính tổng tiền của đơn hàng từ danh sách sản phẩm
                        var order = new Order // Khởi tạo đối tượng Order
                        { // Mở khối gán thuộc tính Order
                            UserId = so.user.UserId, // Mã khách hàng đặt đơn
                            OrderDate = so.date, // Ngày đặt đơn hàng
                            Status = so.status, // Trạng thái đơn hàng
                            ShippingAddress = so.user.Address ?? "123 Nguyễn Huệ, Q.1, TP.HCM", // Địa chỉ giao hàng
                            RecipientPhone = so.user.PhoneNumber ?? "0901234567", // Số điện thoại người nhận
                            PaymentMethod = so.payMethod, // Phương thức thanh toán (COD / Chuyển khoản)
                            TotalAmount = total // Tổng thành tiền của đơn hàng
                        }; // Đóng khối gán thuộc tính Order
                        context.Orders.Add(order); // Thêm đơn hàng vào DbContext
                        await context.SaveChangesAsync(); // Lưu để nhận về OrderId tự tăng từ SQL Server

                        foreach (var (prod, qty) in so.items) // Duyệt qua từng sản phẩm trong đơn hàng mẫu
                        { // Mở vòng lặp thêm chi tiết đơn và nhật ký kho
                            context.OrderDetails.Add(new OrderDetail // Thêm bản ghi chi tiết đơn hàng
                            { // Mở khối thuộc tính OrderDetail
                                OrderId = order.OrderId, // Gắn với mã đơn hàng vừa tạo
                                ProductId = prod.ProductId, // Mã sản phẩm được mua
                                Quantity = qty, // Số lượng mua
                                UnitPrice = prod.Price // Đơn giá sản phẩm
                            }); // Đóng khối thuộc tính OrderDetail

                            context.InventoryLogs.Add(new InventoryLog // Thêm bản ghi nhật ký xuất kho tương ứng
                            { // Mở khối thuộc tính InventoryLog
                                ProductId = prod.ProductId, // Mã sản phẩm xuất kho
                                UserId = so.user.UserId, // Mã người dùng liên quan
                                QuantityChange = -qty, // Số lượng giảm trừ (số âm)
                                Reason = $"Xuất kho - Đơn hàng #{order.OrderId}", // Lý do xuất kho
                                CreatedAt = so.date // Thời gian ghi nhận trùng với ngày đặt đơn
                            }); // Đóng khối thuộc tính InventoryLog
                        } // Đóng vòng lặp thêm chi tiết đơn và nhật ký kho
                        await context.SaveChangesAsync(); // Lưu toàn bộ chi tiết đơn và nhật ký kho của đơn hàng này
                    } // Đóng vòng lặp tạo đơn hàng
                } // Đóng khối kiểm tra số lượng sản phẩm
            } // Đóng khối tạo đơn hàng mẫu
        } // Đóng thân phương thức SeedAsync

        private static string Hash(string password) // Hàm tĩnh hỗ trợ băm mật khẩu SHA256 sang Base64
        { // Mở thân hàm Hash
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)); // Băm mảng byte UTF-8 của mật khẩu bằng SHA256
            return Convert.ToBase64String(bytes); // Trả về chuỗi mã hóa Base64
        } // Đóng thân hàm Hash
    } // Đóng khối lớp DbSeeder
} // Đóng khối không gian tên AquariumStore.Data
