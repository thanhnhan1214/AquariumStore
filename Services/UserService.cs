using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext
using AquariumStore.Models; // Nạp không gian tên chứa thực thể User và Role
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IUserService
using AquariumStore.ViewModels; // Nạp không gian tên chứa RegisterViewModel
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core

namespace AquariumStore.Services // Khai báo không gian tên cho tầng xử lý nghiệp vụ
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Service xử lý nghiệp vụ người dùng: xác thực đăng nhập, đăng ký, cập nhật hồ sơ và băm mật khẩu.
    /// </summary>
    public class UserService : IUserService // Lớp thực thi giao diện IUserService
    { // Mở khối lớp UserService
        private readonly ApplicationDbContext _context; // Biến ngữ cảnh cơ sở dữ liệu

        public UserService(ApplicationDbContext context) // Hàm khởi tạo nhận ApplicationDbContext
        { // Mở thân hàm khởi tạo
            _context = context; // Gán đối tượng DbContext vào biến thành viên
        } // Đóng thân hàm khởi tạo

        public async Task<User?> AuthenticateAsync(string username, string password) // Xác thực thông tin đăng nhập của người dùng
        { // Mở thân hàm AuthenticateAsync
            var hashedPassword = HashPassword(password); // Băm mật khẩu người dùng nhập vào bằng SHA256 để so khớp
            return await _context.Users.AsNoTracking() // Truy vấn chỉ đọc bảng Users
                .Include(u => u.Role) // Nạp kèm thông tin Role (Admin/Staff/Customer) để cấp quyền Cookie
                .FirstOrDefaultAsync(u => u.Username == username // Khớp chính xác tên đăng nhập
                                         && u.PasswordHash == hashedPassword // Khớp mã băm mật khẩu
                                         && u.IsActive); // Tài khoản phải đang ở trạng thái hoạt động (chưa bị khóa)
        } // Đóng thân hàm AuthenticateAsync

        public async Task<User?> GetByIdAsync(int id) // Lấy thông tin người dùng theo mã UserId
        { // Mở thân hàm GetByIdAsync
            return await _context.Users.AsNoTracking() // Truy vấn chỉ đọc bảng Users
                .Include(u => u.Role) // Nạp kèm thông tin vai trò của người dùng
                .FirstOrDefaultAsync(u => u.UserId == id); // Trả về người dùng khớp mã hoặc null
        } // Đóng thân hàm GetByIdAsync

        public async Task<User?> GetByUsernameAsync(string username) // Tìm người dùng theo tên đăng nhập Username
        { // Mở thân hàm GetByUsernameAsync
            return await _context.Users.AsNoTracking() // Truy vấn chỉ đọc bảng Users
                .Include(u => u.Role) // Nạp kèm thông tin vai trò
                .FirstOrDefaultAsync(u => u.Username == username); // Trả về người dùng khớp Username hoặc null
        } // Đóng thân hàm GetByUsernameAsync

        public async Task<bool> IsUsernameExistsAsync(string username) // Kiểm tra tên đăng nhập đã có người sử dụng chưa
        { // Mở thân hàm IsUsernameExistsAsync
            return await _context.Users.AsNoTracking().AnyAsync(u => u.Username == username); // Trả về true nếu đã tồn tại trong CSDL
        } // Đóng thân hàm IsUsernameExistsAsync

        public async Task<bool> IsEmailExistsAsync(string email) // Kiểm tra địa chỉ Email đã được đăng ký chưa
        { // Mở thân hàm IsEmailExistsAsync
            return await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email); // Trả về true nếu Email đã tồn tại
        } // Đóng thân hàm IsEmailExistsAsync

        public async Task<User> RegisterAsync(RegisterViewModel model) // Tạo mới tài khoản khách hàng từ dữ liệu Form đăng ký
        { // Mở thân hàm RegisterAsync
            var user = new User // Khởi tạo thực thể User mới
            { // Mở khối gán thuộc tính User
                RoleId = 3, // Gán mặc định vai trò là Customer (RoleId = 3)
                Username = model.Username.Trim(), // Tên đăng nhập đã cắt khoảng trắng thừa
                PasswordHash = HashPassword(model.Password), // Mật khẩu được băm bảo mật bằng SHA256
                FullName = model.FullName.Trim(), // Họ và tên đầy đủ của khách hàng
                Email = model.Email.Trim(), // Địa chỉ Email liên hệ
                PhoneNumber = model.PhoneNumber?.Trim(), // Số điện thoại (nếu có)
                Address = model.Address?.Trim(), // Địa chỉ giao hàng mặc định (nếu có)
                CreatedAt = DateTime.Now, // Thời điểm tạo tài khoản
                IsActive = true // Kích hoạt tài khoản ngay sau khi đăng ký
            }; // Đóng khối gán thuộc tính User

            _context.Users.Add(user); // Thêm đối tượng User mới vào DbContext
            await _context.SaveChangesAsync(); // Lưu xuống cơ sở dữ liệu SQL Server
            return user; // Trả về đối tượng User vừa tạo
        } // Đóng thân hàm RegisterAsync

        public async Task<List<User>> GetAllAsync() // Lấy danh sách toàn bộ người dùng cho trang Quản trị
        { // Mở thân hàm GetAllAsync
            return await _context.Users.AsNoTracking() // Truy vấn chỉ đọc bảng Users
                .Include(u => u.Role) // Nạp kèm thông tin vai trò (Role)
                .OrderByDescending(u => u.CreatedAt) // Sắp xếp tài khoản mới đăng ký lên đầu
                .ToListAsync(); // Trả về danh sách người dùng
        } // Đóng thân hàm GetAllAsync

        public async Task ToggleActiveAsync(int userId) // Khóa hoặc mở khóa tài khoản người dùng
        { // Mở thân hàm ToggleActiveAsync
            var user = await _context.Users.FindAsync(userId); // Tìm người dùng theo khóa chính UserId
            if (user != null) // Nếu tìm thấy người dùng
            { // Mở khối cập nhật trạng thái
                user.IsActive = !user.IsActive; // Đảo trạng thái hoạt động (true <-> false)
                await _context.SaveChangesAsync(); // Lưu thay đổi xuống CSDL
            } // Đóng khối cập nhật trạng thái
        } // Đóng thân hàm ToggleActiveAsync

        public async Task<int> GetCustomerCountAsync() // Đếm tổng số lượng khách hàng đang hoạt động
        { // Mở thân hàm GetCustomerCountAsync
            return await _context.Users.AsNoTracking().CountAsync(u => u.RoleId == 3 && u.IsActive); // Đếm các User có RoleId = 3 (Customer) và IsActive = true
        } // Đóng thân hàm GetCustomerCountAsync

        public async Task UpdateProfileAsync(int userId, string fullName, string? phoneNumber, string? address) // Cập nhật thông tin hồ sơ cá nhân
        { // Mở thân hàm UpdateProfileAsync
            var user = await _context.Users.FindAsync(userId); // Tìm bản ghi người dùng theo UserId
            if (user != null) // Nếu người dùng tồn tại
            { // Mở khối cập nhật thông tin
                user.FullName = fullName.Trim(); // Cập nhật họ và tên mới
                user.PhoneNumber = phoneNumber?.Trim(); // Cập nhật số điện thoại mới
                user.Address = address?.Trim(); // Cập nhật địa chỉ giao hàng mặc định mới
                await _context.SaveChangesAsync(); // Lưu thay đổi xuống cơ sở dữ liệu
            } // Đóng khối cập nhật thông tin
        } // Đóng thân hàm UpdateProfileAsync

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword) // Đổi mật khẩu tài khoản
        { // Mở thân hàm ChangePasswordAsync
            var user = await _context.Users.FindAsync(userId); // Tìm tài khoản người dùng trong CSDL
            if (user == null) // Nếu không tìm thấy người dùng
                return false; // Trả về kết quả thất bại

            if (user.PasswordHash != HashPassword(currentPassword)) // Kiểm tra mật khẩu hiện tại có khớp với CSDL không
                return false; // Trả về thất bại nếu nhập sai mật khẩu hiện tại

            user.PasswordHash = HashPassword(newPassword); // Băm mật khẩu mới và cập nhật vào tài khoản
            await _context.SaveChangesAsync(); // Lưu mật khẩu mới xuống CSDL
            return true; // Trả về kết quả đổi mật khẩu thành công
        } // Đóng thân hàm ChangePasswordAsync

        /// <summary>
        /// Băm mật khẩu bằng thuật toán SHA256 và mã hóa sang chuỗi Base64.
        /// </summary>
        public string HashPassword(string password) // Phương thức băm mật khẩu một chiều
        { // Mở thân hàm HashPassword
            using var sha256 = System.Security.Cryptography.SHA256.Create(); // Khởi tạo bộ băm SHA256
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)); // Chuyển chuỗi mật khẩu thành byte UTF-8 và tính mã băm
            return Convert.ToBase64String(bytes); // Chuyển kết quả mảng byte sang chuỗi Base64
        } // Đóng thân hàm HashPassword
    } // Đóng khối lớp UserService
} // Đóng khối không gian tên AquariumStore.Services
