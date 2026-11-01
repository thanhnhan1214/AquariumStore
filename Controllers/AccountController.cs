using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IUserService
using AquariumStore.ViewModels; // Nạp không gian tên chứa LoginViewModel, RegisterViewModel, ProfileViewModel
using Microsoft.AspNetCore.Authentication; // Nạp thư viện xử lý đăng nhập/đăng xuất HttpContext.SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Nạp hằng số cấu hình CookieAuthenticationDefaults
using Microsoft.AspNetCore.Authorization; // Nạp thuộc tính [Authorize] bảo vệ trang Hồ sơ cá nhân
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC
using System.Security.Claims; // Nạp thư viện khởi tạo danh sách thông tin định danh (Claims)

namespace AquariumStore.Controllers // Khai báo không gian tên cho các Controller
{ // Mở khối không gian tên AquariumStore.Controllers
    /// <summary>
    /// Controller xác thực & tài khoản: Đăng nhập, Đăng ký, Đăng xuất, Hồ sơ cá nhân và Đổi mật khẩu.
    /// Sử dụng cơ chế Cookie Authentication.
    /// </summary>
    public class AccountController : Controller // Lớp điều khiển tài khoản người dùng
    { // Mở khối lớp AccountController
        private readonly IUserService _userService; // Dịch vụ xử lý nghiệp vụ người dùng

        public AccountController(IUserService userService) // Hàm khởi tạo nhận IUserService qua DI
        { // Mở thân hàm khởi tạo
            _userService = userService; // Gán dịch vụ người dùng vào biến thành viên
        } // Đóng thân hàm khởi tạo

        // GET: /Account/Login
        [HttpGet] // Hiển thị giao diện đăng nhập
        public IActionResult Login(string? returnUrl = null) // Nhận kèm đường dẫn returnUrl nếu bị chuyển hướng từ trang yêu cầu đăng nhập
        { // Mở thân hàm Login (GET)
            if (User.Identity?.IsAuthenticated == true) // Nếu người dùng đã đăng nhập rồi
                return RedirectToAction("Index", "Home"); // Chuyển hướng thẳng về Trang chủ

            ViewBag.ReturnUrl = returnUrl; // Lưu returnUrl vào ViewBag để giữ lại trong Form đăng nhập
            return View(); // Trả về View Views/Account/Login.cshtml
        } // Đóng thân hàm Login (GET)

        // POST: /Account/Login
        [HttpPost] // Xử lý dữ liệu khi người dùng nhấn nút Đăng nhập
        [ValidateAntiForgeryToken] // Kiểm tra mã chống giả mạo CSRF
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) // Nhận thông tin đăng nhập từ Form
        { // Mở thân hàm Login (POST)
            ViewBag.ReturnUrl = returnUrl; // Giữ lại đường dẫn returnUrl nếu cần trả lại View khi lỗi
            if (!ModelState.IsValid) // Nếu người dùng bỏ trống tên đăng nhập hoặc mật khẩu
                return View(model); // Hiển thị lại Form kèm thông báo lỗi

            var user = await _userService.AuthenticateAsync(model.Username.Trim(), model.Password); // Kiểm tra thông tin đăng nhập với CSDL
            if (user == null) // Nếu sai tài khoản/mật khẩu hoặc tài khoản đã bị khóa
            { // Mở khối báo lỗi đăng nhập
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng (hoặc tài khoản đã bị khóa)."); // Thêm lỗi chung vào ModelState
                return View(model); // Trả về lại trang đăng nhập
            } // Đóng khối báo lỗi đăng nhập

            // Tạo danh sách Claims lưu trữ thông tin định danh và phân quyền trong Cookie
            var claims = new List<Claim> // Khởi tạo danh sách các Claim
            { // Mở danh sách Claims
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Lưu mã định danh UserId
                new Claim(ClaimTypes.Name, user.Username), // Lưu tên đăng nhập Username
                new Claim("FullName", user.FullName), // Lưu họ và tên hiển thị trên thanh điều hướng
                new Claim(ClaimTypes.Role, user.Role.RoleName), // Lưu vai trò (Admin / Staff / Customer) để phân quyền [Authorize(Roles=...)]
                new Claim(ClaimTypes.Email, user.Email), // Lưu địa chỉ Email
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty), // Lưu số điện thoại
                new Claim("Address", user.Address ?? string.Empty) // Lưu địa chỉ giao hàng mặc định
            }; // Đóng danh sách Claims

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); // Tạo đối tượng định danh theo chuẩn Cookie
            var authProperties = new AuthenticationProperties // Cấu hình thuộc tính phiên đăng nhập Cookie
            { // Mở khối cấu hình AuthenticationProperties
                IsPersistent = model.RememberMe, // Lưu Cookie bền vững qua các lần đóng trình duyệt nếu chọn "Ghi nhớ đăng nhập"
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Thời hạn hiệu lực của Cookie là 7 ngày
            }; // Đóng khối cấu hình AuthenticationProperties

            await HttpContext.SignInAsync( // Ghi Cookie xác thực đã mã hóa xuống trình duyệt của người dùng
                CookieAuthenticationDefaults.AuthenticationScheme, // Tên cơ chế xác thực Cookie
                new ClaimsPrincipal(claimsIdentity), // Đối tượng người dùng chứa danh sách Claims
                authProperties); // Các thuộc tính thời hạn của Cookie

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) // Nếu có đường dẫn nội bộ cần quay lại trước khi đăng nhập
                return Redirect(returnUrl); // Chuyển hướng người dùng quay lại đúng trang đó

            // Điều hướng tự động theo vai trò nếu không có returnUrl cụ thể
            if (user.Role.RoleName == "Admin" || user.Role.RoleName == "Staff") // Nếu tài khoản đăng nhập là Quản trị viên hoặc Nhân viên
            { // Mở khối chuyển hướng trang quản trị
                return Redirect("/Admin/Dashboard"); // Chuyển hướng trực tiếp vào trang Bảng điều khiển Quản trị (/Admin/Dashboard)
            } // Đóng khối chuyển hướng trang quản trị

            return RedirectToAction("Index", "Home"); // Nếu là Khách hàng thì chuyển hướng về Trang chủ cửa hàng
        } // Đóng thân hàm Login (POST)

        // GET: /Account/Register
        [HttpGet] // Hiển thị giao diện đăng ký tài khoản
        public IActionResult Register() // Action mở Form đăng ký khách hàng mới
        { // Mở thân hàm Register (GET)
            if (User.Identity?.IsAuthenticated == true) // Nếu người dùng đã đăng nhập rồi
                return RedirectToAction("Index", "Home"); // Chuyển hướng về Trang chủ

            return View(); // Trả về View Views/Account/Register.cshtml
        } // Đóng thân hàm Register (GET)

        // POST: /Account/Register
        [HttpPost] // Xử lý dữ liệu khi người dùng nhấn nút Đăng ký
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> Register(RegisterViewModel model) // Nhận dữ liệu từ Form đăng ký
        { // Mở thân hàm Register (POST)
            if (!ModelState.IsValid) // Kiểm tra các ràng buộc dữ liệu đầu vào (độ dài mật khẩu, khớp mật khẩu, định dạng email...)
                return View(model); // Trả lại View nếu dữ liệu chưa hợp lệ

            if (await _userService.IsUsernameExistsAsync(model.Username.Trim())) // Kiểm tra tên đăng nhập đã tồn tại trong CSDL chưa
            { // Mở khối báo trùng Username
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại."); // Gắn lỗi trực tiếp vào trường Username
                return View(model); // Hiển thị lại Form đăng ký
            } // Đóng khối báo trùng Username

            if (await _userService.IsEmailExistsAsync(model.Email.Trim())) // Kiểm tra địa chỉ Email đã được đăng ký chưa
            { // Mở khối báo trùng Email
                ModelState.AddModelError("Email", "Email đã được sử dụng."); // Gắn lỗi trực tiếp vào trường Email
                return View(model); // Hiển thị lại Form đăng ký
            } // Đóng khối báo trùng Email

            await _userService.RegisterAsync(model); // Lưu tài khoản khách hàng mới vào cơ sở dữ liệu
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập."; // Lưu thông báo chúc mừng
            return RedirectToAction("Login"); // Chuyển hướng sang trang Đăng nhập
        } // Đóng thân hàm Register (POST)

        // POST: /Account/Logout
        [HttpPost] // Thực hiện đăng xuất qua phương thức POST để an toàn
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> Logout() // Action đăng xuất khỏi hệ thống
        { // Mở thân hàm Logout
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Xóa Cookie xác thực khỏi trình duyệt
            return RedirectToAction("Index", "Home"); // Chuyển hướng về Trang chủ
        } // Đóng thân hàm Logout

        // GET: /Account/Profile
        [Authorize] // Bắt buộc phải đăng nhập mới xem được hồ sơ cá nhân
        [HttpGet] // Truy cập bằng phương thức GET
        public async Task<IActionResult> Profile() // Action hiển thị trang Hồ sơ cá nhân & Đổi mật khẩu
        { // Mở thân hàm Profile
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Lấy mã UserId từ Cookie hiện tại
            var user = await _userService.GetByIdAsync(userId); // Truy vấn thông tin người dùng từ CSDL
            if (user == null) // Nếu không tìm thấy người dùng
                return NotFound(); // Trả về 404

            var model = new ProfileViewModel // Khởi tạo ViewModel chứa thông tin hồ sơ
            { // Mở khối gán thuộc tính ProfileViewModel
                Username = user.Username, // Tên đăng nhập
                Email = user.Email, // Địa chỉ Email
                RoleName = user.Role.RoleName, // Vai trò tài khoản
                CreatedAt = user.CreatedAt, // Ngày tham gia hệ thống
                FullName = user.FullName, // Họ và tên hiện tại
                PhoneNumber = user.PhoneNumber, // Số điện thoại hiện tại
                Address = user.Address // Địa chỉ giao hàng mặc định hiện tại
            }; // Đóng khối gán thuộc tính ProfileViewModel

            return View(model); // Trả về View Views/Account/Profile.cshtml
        } // Đóng thân hàm Profile

        // POST: /Account/UpdateProfile
        [Authorize] // Yêu cầu đã đăng nhập
        [HttpPost] // Nhận dữ liệu cập nhật qua POST
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model) // Action lưu thay đổi thông tin cá nhân
        { // Mở thân hàm UpdateProfile
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Lấy mã UserId đang đăng nhập
            var user = await _userService.GetByIdAsync(userId); // Lấy thông tin gốc từ CSDL
            if (user == null) // Nếu không tồn tại
                return NotFound(); // Trả về 404

            if (!ModelState.IsValid) // Nếu dữ liệu nhập vào không hợp lệ (ví dụ: bỏ trống Họ tên)
            { // Mở khối gán lại các trường chỉ đọc trước khi trả về View
                model.Username = user.Username; // Giữ lại Username hiển thị
                model.Email = user.Email; // Giữ lại Email hiển thị
                model.RoleName = user.Role.RoleName; // Giữ lại tên vai trò
                model.CreatedAt = user.CreatedAt; // Giữ lại ngày tạo
                return View("Profile", model); // Hiển thị lại trang Profile kèm lỗi
            } // Đóng khối gán lại các trường chỉ đọc

            await _userService.UpdateProfileAsync(userId, model.FullName, model.PhoneNumber, model.Address); // Cập nhật Họ tên, SĐT và Địa chỉ vào CSDL
            TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ cá nhân thành công!"; // Thông báo thành công
            return RedirectToAction("Profile"); // Tải lại trang Hồ sơ cá nhân
        } // Đóng thân hàm UpdateProfile

        // POST: /Account/ChangePassword
        [Authorize] // Yêu cầu đã đăng nhập
        [HttpPost] // Nhận yêu cầu đổi mật khẩu qua POST
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword) // Action xử lý đổi mật khẩu
        { // Mở thân hàm ChangePassword
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6) // Kiểm tra độ dài mật khẩu mới tối thiểu 6 ký tự
            { // Mở khối báo lỗi độ dài mật khẩu
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự."; // Thông báo lỗi
                return RedirectToAction("Profile"); // Quay lại trang Profile
            } // Đóng khối báo lỗi độ dài mật khẩu

            if (newPassword != confirmPassword) // Kiểm tra mật khẩu mới và mật khẩu xác nhận có trùng khớp không
            { // Mở khối báo lỗi không khớp
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp."; // Thông báo lỗi
                return RedirectToAction("Profile"); // Quay lại trang Profile
            } // Đóng khối báo lỗi không khớp

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Lấy mã UserId đang đăng nhập
            var ok = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword); // Gọi dịch vụ kiểm tra mật khẩu cũ và lưu mật khẩu mới
            if (!ok) // Nếu mật khẩu hiện tại nhập không đúng
            { // Mở khối báo sai mật khẩu cũ
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác."; // Thông báo lỗi
                return RedirectToAction("Profile"); // Quay lại trang Profile
            } // Đóng khối báo sai mật khẩu cũ

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!"; // Thông báo đổi mật khẩu thành công
            return RedirectToAction("Profile"); // Quay lại trang Profile
        } // Đóng thân hàm ChangePassword

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied() // Action hiển thị trang thông báo Từ chối truy cập (khi không đủ quyền Role)
        { // Mở thân hàm AccessDenied
            return View(); // Trả về View Views/Account/AccessDenied.cshtml
        } // Đóng thân hàm AccessDenied
    } // Đóng khối lớp AccountController
} // Đóng khối không gian tên AquariumStore.Controllers
