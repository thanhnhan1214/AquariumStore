using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext
using AquariumStore.Models; // Nạp không gian tên chứa thực thể Category
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện ICategoryService
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core

namespace AquariumStore.Services // Khai báo không gian tên cho tầng xử lý nghiệp vụ
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Service xử lý nghiệp vụ danh mục sản phẩm.
    /// </summary>
    public class CategoryService : ICategoryService // Lớp thực thi giao diện ICategoryService
    { // Mở khối lớp CategoryService
        private readonly ApplicationDbContext _context; // Biến ngữ cảnh cơ sở dữ liệu

        public CategoryService(ApplicationDbContext context) // Hàm khởi tạo nhận ApplicationDbContext qua DI
        { // Mở thân hàm khởi tạo
            _context = context; // Lưu tham chiếu DbContext vào biến thành viên
        } // Đóng thân hàm khởi tạo

        public async Task<List<Category>> GetAllAsync() // Lấy toàn bộ danh mục (dùng cho trang Quản trị)
        { // Mở thân hàm GetAllAsync
            return await _context.Categories.AsNoTracking() // Truy vấn không theo dõi thay đổi để tối ưu tốc độ
                .Include(c => c.Products) // Nạp kèm danh sách sản phẩm để đếm số lượng sản phẩm mỗi danh mục
                .OrderBy(c => c.CategoryId) // Sắp xếp theo mã danh mục tăng dần
                .ToListAsync(); // Thực thi truy vấn và trả về danh sách
        } // Đóng thân hàm GetAllAsync

        public async Task<List<Category>> GetActiveAsync() // Lấy các danh mục đang hoạt động (hiển thị ngoài trang chủ và bộ lọc)
        { // Mở thân hàm GetActiveAsync
            return await _context.Categories.AsNoTracking() // Truy vấn chỉ đọc với AsNoTracking
                .Include(c => c.Products) // Nạp kèm danh sách sản phẩm thuộc danh mục
                .Where(c => c.IsActive) // Chỉ lọc những danh mục có IsActive = true
                .OrderBy(c => c.CategoryId) // Sắp xếp theo thứ tự mã danh mục
                .ToListAsync(); // Trả về danh sách danh mục đang hiển thị
        } // Đóng thân hàm GetActiveAsync

        public async Task<Category?> GetByIdAsync(int id) // Tìm danh mục theo khóa chính CategoryId
        { // Mở thân hàm GetByIdAsync
            return await _context.Categories // Truy vấn bảng Categories
                .Include(c => c.Products) // Nạp kèm danh sách sản phẩm thuộc danh mục đó
                .FirstOrDefaultAsync(c => c.CategoryId == id); // Trả về danh mục khớp mã hoặc null nếu không tồn tại
        } // Đóng thân hàm GetByIdAsync

        public async Task CreateAsync(Category category) // Tạo mới một danh mục sản phẩm
        { // Mở thân hàm CreateAsync
            _context.Categories.Add(category); // Thêm đối tượng Category vào DbSet
            await _context.SaveChangesAsync(); // Lưu xuống cơ sở dữ liệu SQL Server
        } // Đóng thân hàm CreateAsync

        public async Task UpdateAsync(Category category) // Cập nhật thông tin danh mục sản phẩm
        { // Mở thân hàm UpdateAsync
            _context.Categories.Update(category); // Đánh dấu đối tượng Category cần cập nhật
            await _context.SaveChangesAsync(); // Lưu thay đổi xuống CSDL
        } // Đóng thân hàm UpdateAsync

        public async Task ToggleActiveAsync(int id) // Chuyển đổi trạng thái ẩn/hiện (xóa mềm) của danh mục
        { // Mở thân hàm ToggleActiveAsync
            var category = await _context.Categories.FindAsync(id); // Tìm danh mục theo khóa chính
            if (category != null) // Nếu danh mục tồn tại
            { // Mở khối đổi trạng thái
                category.IsActive = !category.IsActive; // Đảo ngược trạng thái hoạt động hiện tại
                await _context.SaveChangesAsync(); // Lưu thay đổi xuống CSDL
            } // Đóng khối đổi trạng thái
        } // Đóng thân hàm ToggleActiveAsync
    } // Đóng khối lớp CategoryService
} // Đóng khối không gian tên AquariumStore.Services
