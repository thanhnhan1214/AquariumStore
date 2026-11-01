using AquariumStore.Models; // Nạp không gian tên chứa thực thể User

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ quản lý người dùng & xác thực tài khoản.
    /// </summary>
    public interface IUserService // Giao diện định nghĩa các phương thức nghiệp vụ cho tài khoản người dùng
    { // Mở khối giao diện IUserService
        Task<User?> AuthenticateAsync(string username, string password); // Xác thực thông tin đăng nhập (Username và Password)
        Task<User?> GetByIdAsync(int id); // Tìm người dùng theo khóa chính UserId
        Task<User?> GetByUsernameAsync(string username); // Tìm người dùng theo tên đăng nhập Username
        Task<bool> IsUsernameExistsAsync(string username); // Kiểm tra tên đăng nhập đã tồn tại trong CSDL hay chưa
        Task<bool> IsEmailExistsAsync(string email); // Kiểm tra địa chỉ Email đã được sử dụng hay chưa
        Task<User> RegisterAsync(ViewModels.RegisterViewModel model); // Đăng ký tài khoản khách hàng mới
        Task<List<User>> GetAllAsync(); // Lấy danh sách toàn bộ người dùng kèm vai trò
        Task ToggleActiveAsync(int userId); // Khóa hoặc mở khóa tài khoản người dùng
        Task<int> GetCustomerCountAsync(); // Đếm tổng số lượng tài khoản khách hàng đang hoạt động
        Task UpdateProfileAsync(int userId, string fullName, string? phoneNumber, string? address); // Cập nhật thông tin hồ sơ cá nhân
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword); // Đổi mật khẩu sau khi xác minh mật khẩu hiện tại
        string HashPassword(string password); // Băm mật khẩu bằng thuật toán SHA256
    } // Đóng khối giao diện IUserService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
