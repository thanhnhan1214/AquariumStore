using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext và DbSeeder
using AquariumStore.Services; // Nạp không gian tên chứa các lớp thực thi nghiệp vụ (Services)
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa các giao diện dịch vụ (Interfaces)
using Microsoft.AspNetCore.Authentication.Cookies; // Nạp thư viện xác thực bằng Cookie của ASP.NET Core
using Microsoft.EntityFrameworkCore; // Nạp thư viện ORM Entity Framework Core
using System.Diagnostics; // Nạp thư viện quản lý tiến trình hệ thống
using System.Net.NetworkInformation; // Nạp thư viện kiểm tra trạng thái cổng mạng TCP
using System.Runtime.InteropServices; // Nạp thư viện P/Invoke gọi hàm hệ thống Windows tắt QuickEdit Console

// Vô hiệu hóa chế độ QuickEdit của cửa sổ Console Windows để tránh việc vô tình click chuột vào cửa sổ đen làm treo máy chủ web
DisableConsoleQuickEdit(); // Gọi hàm tắt QuickEdit Mode ngay khi khởi động

// Tự động giải phóng cổng 5080 nếu có tiến trình chạy nền cũ đang chiếm cổng khi bấm F5 trong Visual Studio
FreePortIfOccupied(5080); // Kiểm tra và giải phóng cổng 5080 trước khi khởi tạo Kestrel Server

var builder = WebApplication.CreateBuilder(args); // Khởi tạo bộ thiết lập ứng dụng web với các tham số dòng lệnh

// ==========================================
// 1. ĐĂNG KÝ DỊCH VỤ (Dependency Injection)
// ==========================================

// Đăng ký ApplicationDbContext sử dụng hệ quản trị CSDL SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options => // Cấu hình tùy chọn cho DbContext
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")) // Lấy chuỗi kết nối DefaultConnection từ appsettings.json
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))); // Bỏ qua cảnh báo thay đổi snapshot khi seed dữ liệu

// Đăng ký các Service theo vòng đời Scoped (mỗi HTTP request tạo một instance riêng)
builder.Services.AddScoped<IProductService, ProductService>(); // Đăng ký dịch vụ xử lý nghiệp vụ sản phẩm
builder.Services.AddScoped<ICategoryService, CategoryService>(); // Đăng ký dịch vụ xử lý nghiệp vụ danh mục
builder.Services.AddScoped<IOrderService, OrderService>(); // Đăng ký dịch vụ xử lý nghiệp vụ đơn hàng
builder.Services.AddScoped<IUserService, UserService>(); // Đăng ký dịch vụ xử lý nghiệp vụ người dùng
builder.Services.AddScoped<IInventoryService, InventoryService>(); // Đăng ký dịch vụ xử lý nghiệp vụ nhập/xuất kho
builder.Services.AddScoped<IAiChatService, AiChatService>(); // Đăng ký dịch vụ trợ lý AI tư vấn thủy sinh
builder.Services.AddSingleton<ICartService, CartService>(); // Đăng ký dịch vụ giỏ hàng dùng chung (Singleton vì lưu trạng thái trong Session)

// Cấu hình bộ nhớ đệm và Session (dùng để lưu giỏ hàng của khách)
builder.Services.AddDistributedMemoryCache(); // Đăng ký bộ nhớ đệm phân tán trên RAM để lưu dữ liệu Session
builder.Services.AddSession(options => // Thiết lập các thông số cho Session
{ // Mở khối cấu hình Session
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn Session sau 30 phút không thao tác
    options.Cookie.HttpOnly = true; // Chỉ cho phép truy cập Cookie qua HTTP, chặn script đọc để chống XSS
    options.Cookie.IsEssential = true; // Đánh dấu Cookie là thiết yếu để luôn hoạt động
    options.Cookie.Name = ".AquariumStore.Session"; // Đặt tên định danh riêng cho Cookie lưu Session giỏ hàng
}); // Đóng khối cấu hình Session

// Cấu hình cơ chế xác thực người dùng bằng Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // Chọn lược đồ xác thực mặc định là Cookie
    .AddCookie(options => // Cấu hình chi tiết cho Cookie Authentication
    { // Mở khối cấu hình Cookie
        options.LoginPath = "/Account/Login"; // Đường dẫn tự động chuyển hướng tới khi người dùng chưa đăng nhập
        options.LogoutPath = "/Account/Logout"; // Đường dẫn xử lý đăng xuất tài khoản
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn chuyển hướng tới khi không đủ quyền (Role) truy cập
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Thời hạn hiệu lực của phiên đăng nhập là 7 ngày
        options.SlidingExpiration = true; // Tự động gia hạn thời gian sống của Cookie khi người dùng còn hoạt động
        options.Cookie.Name = ".AquariumStore.Auth"; // Đặt tên Cookie lưu thông tin xác thực
    }); // Đóng khối cấu hình Cookie

builder.Services.AddControllersWithViews(); // Đăng ký mô hình MVC (Controllers kết hợp Razor Views)

// Đăng ký dịch vụ nén phản hồi HTTP (Gzip/Brotli) cho môi trường Production
builder.Services.AddResponseCompression(options => options.EnableForHttps = true); // Cấu hình nén dữ liệu HTTP/HTTPS

var app = builder.Build(); // Đóng gói và xây dựng đối tượng ứng dụng WebApplication

// Tự động thực thi Migration tạo CSDL và nạp dữ liệu mẫu (Data Seeding) khi khởi động
using (var scope = app.Services.CreateScope()) // Tạo một phạm vi dịch vụ (Service Scope) tạm thời lúc khởi chạy
{ // Mở khối khởi tạo dữ liệu
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Lấy thực thể ApplicationDbContext từ DI Container
    await DbSeeder.SeedAsync(db); // Gọi hàm migrate và nạp dữ liệu mẫu (tài khoản, danh mục, sản phẩm, đơn hàng)
} // Giải phóng scope sau khi seed xong

// ==========================================
// 2. CẤU HÌNH MIDDLEWARE PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment()) // Kiểm tra nếu môi trường chạy không phải là Development (tránh xung đột BrowserLink của Visual Studio)
{ // Mở khối cấu hình Production
    app.UseExceptionHandler("/Home/Error"); // Chuyển hướng lỗi hệ thống về trang thông báo lỗi thân thiện
    app.UseResponseCompression(); // Chỉ bật nén Brotli/Gzip ở Production để tương thích hoàn toàn với BrowserRefresh của Visual Studio
} // Đóng khối cấu hình Production

// Kích hoạt phục vụ các tệp tĩnh (CSS, JS, Hình ảnh trong thư mục wwwroot)
app.UseStaticFiles(new StaticFileOptions // Cấu hình tùy chọn cho tệp tĩnh
{ // Mở khối cấu hình StaticFiles
    OnPrepareResponse = ctx => // Sự kiện chuẩn bị phản hồi cho tệp tĩnh
    { // Mở hàm xử lý header
        if (!app.Environment.IsDevelopment()) // Nếu chạy ở môi trường thực tế (không phải lúc đang sửa code)
            ctx.Context.Response.Headers.CacheControl = "public,max-age=604800"; // Lưu cache tệp tĩnh 7 ngày trên trình duyệt
    } // Đóng hàm xử lý header
}); // Đóng cấu hình StaticFiles

app.UseRouting(); // Kích hoạt Middleware định tuyến URL (Routing) tới các Controller

// Thứ tự Middleware bắt buộc: Session → Authentication → Authorization
app.UseSession(); // Kích hoạt Middleware quản lý phiên làm việc (Session giỏ hàng)
app.UseAuthentication(); // Kích hoạt Middleware nhận diện danh tính người dùng (đọc Cookie đăng nhập)
app.UseAuthorization(); // Kích hoạt Middleware kiểm tra quyền truy cập ([Authorize], Roles)

app.MapControllers(); // Ánh xạ trực tiếp các Controller dùng Attribute Routing (như /Admin/Dashboard, /Admin/ProductManagement, /Chat/Ask)

app.MapControllerRoute( // Định nghĩa quy tắc định tuyến mặc định cho các Controller phía Khách hàng
    name: "default", // Tên định tuyến mặc định là default
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Mẫu URL chuẩn trỏ về HomeController và hàm Index

app.Run(); // Chính thức khởi chạy máy chủ Kestrel và lắng nghe các yêu cầu HTTP

// Hàm hỗ trợ: Vô hiệu hóa QuickEdit Mode trên cửa sổ Console để click chuột vào cửa sổ đen không làm đứng web
static void DisableConsoleQuickEdit() // Hàm tĩnh tắt chế độ chọn văn bản làm tạm dừng tiến trình Console
{ // Mở thân hàm DisableConsoleQuickEdit
    if (!OperatingSystem.IsWindows()) return; // Chỉ áp dụng trên hệ điều hành Windows
    try // Bọc trong try-catch để an toàn khi chạy dưới IIS hoặc dịch vụ nền không có Console
    { // Mở khối try
        const int STD_INPUT_HANDLE = -10; // Mã định danh luồng đầu vào chuẩn (Standard Input) của Console
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040; // Cờ bật chế độ QuickEdit (click chuột làm đóng băng tiến trình)
        const uint ENABLE_EXTENDED_FLAGS = 0x0080; // Cờ mở rộng bắt buộc khi thay đổi thuộc tính QuickEdit
        var handle = GetStdHandle(STD_INPUT_HANDLE); // Lấy con trỏ Handle của cửa sổ Console hiện tại
        if (handle != IntPtr.Zero && GetConsoleMode(handle, out uint mode)) // Đọc chế độ hiện tại của Console
        { // Mở khối cập nhật chế độ Console
            mode &= ~ENABLE_QUICK_EDIT_MODE; // Tắt cờ QuickEdit Mode
            mode |= ENABLE_EXTENDED_FLAGS; // Bật cờ Extended Flags để áp dụng thay đổi
            SetConsoleMode(handle, mode); // Ghi chế độ mới xuống cửa sổ Console
        } // Đóng khối cập nhật chế độ Console
    } // Đóng khối try
    catch { /* Bỏ qua nếu không có cửa sổ Console */ } // Không gây ảnh hưởng tới ứng dụng
} // Đóng thân hàm DisableConsoleQuickEdit

// Hàm hỗ trợ: Tự động giải phóng cổng TCP nếu có tiến trình chạy nền cũ đang giữ cổng đó
static void FreePortIfOccupied(int port) // Nhận vào số hiệu cổng cần kiểm tra (5080)
{ // Mở thân hàm FreePortIfOccupied
    try // Bọc trong khối try-catch để đảm bảo an toàn tuyệt đối khi khởi động
    { // Mở khối try
        var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners(); // Lấy danh sách các cổng TCP đang lắng nghe trên máy
        if (!listeners.Any(ep => ep.Port == port)) return; // Nếu cổng 5080 đang trống thì thoát hàm ngay

        var currentPid = Environment.ProcessId; // Lấy mã tiến trình (PID) của ứng dụng hiện tại đang khởi chạy
        var psi = new ProcessStartInfo("netstat", "-ano -p tcp") // Chuẩn bị lệnh netstat để tìm PID đang giữ cổng 5080
        { // Mở cấu hình ProcessStartInfo
            RedirectStandardOutput = true, // Chuyển hướng đầu ra chuẩn để đọc kết quả
            UseShellExecute = false, // Không dùng giao diện shell của Windows
            CreateNoWindow = true // Chạy ngầm không hiện cửa sổ dòng lệnh
        }; // Đóng cấu hình ProcessStartInfo

        using var proc = Process.Start(psi); // Thực thi lệnh netstat
        if (proc == null) return; // Nếu không gọi được lệnh thì bỏ qua
        var output = proc.StandardOutput.ReadToEnd(); // Đọc toàn bộ kết quả trả về từ lệnh netstat
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) // Duyệt qua từng dòng kết quả
        { // Mở vòng lặp duyệt dòng
            if (line.Contains($":{port} ") && line.Contains("LISTENING")) // Tìm dòng có chứa cổng 5080 ở trạng thái LISTENING
            { // Mở khối xử lý khi tìm thấy cổng
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries); // Tách các cột trong dòng kết quả
                if (int.TryParse(parts.LastOrDefault(), out int pid) && pid > 0 && pid != currentPid) // Lấy cột cuối cùng là PID và khác PID hiện tại
                { // Mở khối dừng tiến trình cũ
                    Process.GetProcessById(pid).Kill(true); // Dừng tiến trình chạy nền cũ để nhường cổng 5080 cho tiến trình mới (F5 Visual Studio)
                    Thread.Sleep(250); // Chờ 250ms để hệ điều hành giải phóng hoàn toàn socket
                } // Đóng khối dừng tiến trình cũ
            } // Đóng khối xử lý khi tìm thấy cổng
        } // Đóng vòng lặp duyệt dòng
    } // Đóng khối try
    catch { /* Bỏ qua nếu không cần thiết hoặc không đủ quyền */ } // Đảm bảo không bao giờ gây lỗi dừng ứng dụng
} // Đóng thân hàm FreePortIfOccupied

[DllImport("kernel32.dll", SetLastError = true)] // Khai báo hàm Win32 lấy Handle luồng Console
static extern IntPtr GetStdHandle(int nStdHandle); // Nguyên mẫu hàm GetStdHandle

[DllImport("kernel32.dll", SetLastError = true)] // Khai báo hàm Win32 đọc chế độ Console
static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode); // Nguyên mẫu hàm GetConsoleMode

[DllImport("kernel32.dll", SetLastError = true)] // Khai báo hàm Win32 thiết lập chế độ Console
static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode); // Nguyên mẫu hàm SetConsoleMode
