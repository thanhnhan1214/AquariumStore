using AquariumStore.Data; // Nạp không gian tên chứa ApplicationDbContext
using AquariumStore.Models; // Nạp không gian tên chứa thực thể Product, Order, Category
using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IAiChatService
using AquariumStore.ViewModels; // Nạp không gian tên chứa các ViewModel của khung chat AI
using Microsoft.EntityFrameworkCore; // Nạp thư viện Entity Framework Core để truy vấn dữ liệu thời gian thực
using System.Globalization; // Nạp thư viện xử lý văn hóa và ký tự Unicode (bỏ dấu tiếng Việt)
using System.Text; // Nạp thư viện StringBuilder xử lý chuỗi hiệu năng cao
using System.Text.Json; // Nạp thư viện xử lý JSON khi gọi API Gemini
using System.Text.RegularExpressions; // Nạp thư viện Biểu thức chính quy (Regex) để bóc tách mã đơn và ngân sách

namespace AquariumStore.Services // Khai báo không gian tên cho tầng dịch vụ nghiệp vụ
{ // Mở khối không gian tên AquariumStore.Services
    /// <summary>
    /// Dịch vụ AI Tư vấn Thủy sinh & Sinh vật cảnh (AquaBot AI).
    /// Phân tích ngữ nghĩa câu hỏi (hỗ trợ cả tiếng Việt có dấu và không dấu),
    /// tra cứu trực tiếp CSDL (Sản phẩm, Danh mục, Đơn hàng) và trả lời trực tiếp mọi câu hỏi của khách hàng.
    /// </summary>
    public class AiChatService : IAiChatService // Lớp thực thi giao diện IAiChatService
    { // Mở khối lớp AiChatService
        private readonly ApplicationDbContext _context; // Ngữ cảnh cơ sở dữ liệu để tra cứu giá, tồn kho và đơn hàng thật
        private readonly IConfiguration _config; // Đối tượng đọc cấu hình từ appsettings.json (ví dụ: Gemini:ApiKey)
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(12) }; // HttpClient dùng chung để gọi API ngoài với thời gian chờ tối đa 12 giây

        public AiChatService(ApplicationDbContext context, IConfiguration config) // Hàm khởi tạo nhận DbContext và IConfiguration qua DI
        { // Mở thân hàm khởi tạo
            _context = context; // Gán đối tượng DbContext vào biến thành viên
            _config = config; // Gán đối tượng cấu hình vào biến thành viên
        } // Đóng thân hàm khởi tạo

        public async Task<AiChatResponseViewModel> AskAsync(string message, List<AiChatTurn>? history = null) // Phương thức chính xử lý câu hỏi của khách hàng
        { // Mở thân hàm AskAsync
            var userMsg = (message ?? string.Empty).Trim(); // Chuẩn hóa câu hỏi đầu vào và cắt khoảng trắng thừa
            var products = await _context.Products.AsNoTracking() // Truy vấn toàn bộ sản phẩm đang kinh doanh từ CSDL (không tracking)
                .Include(p => p.Category) // Nạp kèm thông tin danh mục sản phẩm
                .Where(p => p.IsActive && p.Category.IsActive) // Chỉ lấy các sản phẩm và danh mục đang hoạt động
                .OrderBy(p => p.ProductId) // Sắp xếp theo mã sản phẩm tăng dần
                .ToListAsync(); // Thực thi truy vấn bất đồng bộ

            if (string.IsNullOrWhiteSpace(userMsg)) // Nếu người dùng gửi tin nhắn rỗng
            { // Mở khối phản hồi lời chào mặc định
                return new AiChatResponseViewModel // Trả về đối tượng phản hồi chào mừng
                { // Mở khối khởi tạo AiChatResponseViewModel
                    Reply = "Xin chào! Mình là **AquaBot AI** — Chuyên gia tư vấn của **Aquarium Store**. Hãy đặt bất kỳ câu hỏi nào về cách nuôi cá, chọn cây thủy sinh, tính công suất lọc/đèn hoặc kiểm tra đơn hàng, mình sẽ trả lời ngay lập tức!", // Lời chào hướng dẫn
                    SuggestedProducts = MapProducts(products.Take(3)), // Đính kèm 3 sản phẩm tiêu biểu đầu tiên
                    FollowUpQuestions = DefaultFollowUps() // Đính kèm danh sách câu hỏi gợi ý nhanh
                }; // Đóng khối khởi tạo AiChatResponseViewModel
            } // Đóng khối phản hồi lời chào mặc định

            // 1. Nếu có cấu hình Gemini API Key thì ưu tiên gọi LLM kèm toàn bộ dữ liệu kho (RAG)
            var apiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY"); // Đọc khóa API Gemini từ cấu hình hoặc biến môi trường
            if (!string.IsNullOrWhiteSpace(apiKey)) // Nếu hệ thống có cấu hình khóa API Gemini
            { // Mở khối gọi Gemini API
                var geminiReply = await TryCallGeminiAsync(apiKey, userMsg, products, history); // Gọi Gemini 2.5 Flash với ngữ cảnh tồn kho thực tế
                if (!string.IsNullOrWhiteSpace(geminiReply)) // Nếu nhận được câu trả lời hợp lệ từ Gemini
                { // Mở khối trả về kết quả từ Gemini
                    var matched = FindRelevantProducts(userMsg + " " + geminiReply, products, 3); // Tìm tối đa 3 sản phẩm liên quan nhất tới nội dung tư vấn
                    if (!matched.Any()) matched = products.Take(3).ToList(); // Nếu chưa khớp sản phẩm cụ thể thì gợi ý 3 sản phẩm nổi bật
                    return new AiChatResponseViewModel // Trả về phản hồi cho khung chat
                    { // Mở khối khởi tạo kết quả
                        Reply = geminiReply, // Nội dung tư vấn từ Gemini
                        SuggestedProducts = MapProducts(matched), // Danh sách thẻ sản phẩm đính kèm
                        FollowUpQuestions = DefaultFollowUps() // Các câu hỏi gợi ý tiếp theo
                    }; // Đóng khối khởi tạo kết quả
                } // Đóng khối trả về kết quả từ Gemini
            } // Đóng khối gọi Gemini API

            // 2. Engine Trí tuệ Chuyên gia Thủy sinh & Tra cứu CSDL thời gian thực (Trả lời trực tiếp 100% câu hỏi)
            return await BuildDirectExpertAnswerAsync(userMsg, products); // Phân tích câu hỏi và trả lời trực tiếp bằng tri thức chuyên gia kết hợp CSDL
        } // Đóng thân hàm AskAsync

        private async Task<AiChatResponseViewModel> BuildDirectExpertAnswerAsync(string userMsg, List<Product> products) // Bộ máy phân tích và trả lời trực tiếp câu hỏi của khách hàng
        { // Mở thân hàm BuildDirectExpertAnswerAsync
            var lower = userMsg.ToLowerInvariant(); // Chuyển câu hỏi về chữ thường
            var noTone = RemoveDiacritics(lower); // Loại bỏ dấu tiếng Việt để khách gõ có dấu hay không dấu đều hiểu chính xác
            var sections = new List<string>(); // Danh sách các đoạn nội dung trả lời (hỗ trợ câu hỏi ghép nhiều ý)
            var suggested = new List<Product>(); // Danh sách các sản phẩm gợi ý tương ứng với câu trả lời

            // A. TRA CỨU ĐƠN HÀNG TRỰC TIẾP TRONG DATABASE (VD: "đơn hàng #3", "kiểm tra đơn 2", "đơn hàng của tôi")
            if (ContainsAny(noTone, "don hang", "ma don", "kiem tra don", "trang thai don", "dh-", "dh#")) // Kiểm tra nếu khách hỏi về đơn hàng
            { // Mở khối xử lý tra cứu đơn hàng
                var orderIdMatch = Regex.Match(noTone, @"#?(\d+)"); // Dùng Regex bóc tách con số mã đơn hàng trong câu hỏi
                if (orderIdMatch.Success && int.TryParse(orderIdMatch.Groups[1].Value, out int orderId)) // Nếu tìm thấy mã số đơn hàng hợp lệ
                { // Mở khối truy vấn đơn hàng theo mã
                    var order = await _context.Orders.AsNoTracking() // Truy vấn bảng Orders trong SQL Server
                        .Include(o => o.OrderDetails).ThenInclude(od => od.Product) // Nạp kèm chi tiết các mặt hàng trong đơn
                        .FirstOrDefaultAsync(o => o.OrderId == orderId); // Tìm đơn hàng theo OrderId

                    if (order != null) // Nếu tìm thấy đơn hàng trong CSDL
                    { // Mở khối trả về thông tin đơn hàng
                        var itemSummary = string.Join(", ", order.OrderDetails.Select(d => $"{d.Product.ProductName} (x{d.Quantity})")); // Ghép tên và số lượng các món trong đơn
                        return new AiChatResponseViewModel // Trả về chi tiết trạng thái đơn hàng ngay lập tức
                        { // Mở khối khởi tạo phản hồi đơn hàng
                            Reply = $"📦 **Kết quả tra cứu Đơn hàng #{order.OrderId}:**\n" + // Tiêu đề kết quả tra cứu
                                    $"- **Trạng thái hiện tại:** **{order.Status}**\n" + // Hiển thị trạng thái đơn (Chờ xử lý / Đang giao / Hoàn thành / Đã hủy)
                                    $"- **Ngày đặt:** {order.OrderDate:dd/MM/yyyy HH:mm}\n" + // Hiển thị ngày giờ đặt hàng
                                    $"- **Sản phẩm:** {itemSummary}\n" + // Hiển thị danh sách sản phẩm đã đặt
                                    $"- **Tổng thanh toán:** **{order.TotalAmount:N0} ₫** ({order.PaymentMethod})\n" + // Hiển thị tổng tiền và phương thức thanh toán
                                    $"- **Địa chỉ nhận:** {order.ShippingAddress}\n\n" + // Hiển thị địa chỉ nhận hàng
                                    (order.Status == "Chờ xử lý" // Kiểm tra nếu đơn đang chờ xử lý
                                        ? "💡 *Đơn hàng đang chờ xác nhận, bạn có thể xem chi tiết hoặc hủy đơn trong mục **Đơn mua** trên thanh menu.*" // Gợi ý thao tác cho đơn chờ xử lý
                                        : "💡 *Cảm ơn bạn đã mua sắm tại Aquarium Store!*"), // Lời cảm ơn khách hàng
                            SuggestedProducts = MapProducts(products.Take(2)), // Gợi ý 2 sản phẩm nổi bật
                            FollowUpQuestions = DefaultFollowUps() // Gợi ý câu hỏi tiếp theo
                        }; // Đóng khối khởi tạo phản hồi đơn hàng
                    } // Đóng khối trả về thông tin đơn hàng
                    else // Nếu không tìm thấy mã đơn hàng tương ứng
                    { // Mở khối báo không tìm thấy đơn
                        return new AiChatResponseViewModel // Trả về thông báo hướng dẫn kiểm tra lại mã đơn
                        { // Mở khối khởi tạo phản hồi
                            Reply = $"🔎 Mình đã kiểm tra trên hệ thống nhưng chưa tìm thấy **Đơn hàng #{orderId}**. Bạn vui lòng kiểm tra lại mã số đơn hàng hoặc vào mục **Đơn mua** trên thanh menu (sau khi đăng nhập) để xem toàn bộ lịch sử đơn nhé!", // Nội dung phản hồi
                            SuggestedProducts = MapProducts(products.Take(2)), // Gợi ý 2 sản phẩm
                            FollowUpQuestions = DefaultFollowUps() // Gợi ý câu hỏi tiếp theo
                        }; // Đóng khối khởi tạo phản hồi
                    } // Đóng khối báo không tìm thấy đơn
                } // Đóng khối truy vấn đơn hàng theo mã

                return new AiChatResponseViewModel // Trường hợp khách hỏi về đơn hàng nhưng chưa nhập kèm mã số đơn
                { // Mở khối hướng dẫn nhập mã đơn
                    Reply = "📦 **Hướng dẫn tra cứu đơn hàng nhanh:**\n" + // Tiêu đề hướng dẫn
                            "Bạn hãy nhắn cho mình kèm mã số đơn (ví dụ: **\"Kiểm tra đơn hàng #1\"** hoặc **\"Đơn hàng #2\"**), mình sẽ tra cứu trạng thái trực tiếp từ hệ thống ngay lập tức!", // Hướng dẫn cú pháp nhắn
                    SuggestedProducts = new(), // Không cần hiển thị sản phẩm khi đang hỏi mã đơn
                    FollowUpQuestions = new() { "Kiểm tra đơn hàng #1", "Kiểm tra đơn hàng #2", "Chính sách bảo hành cá sống 48h" } // Nút bấm nhanh để khách thử tra cứu đơn #1 hoặc #2
                }; // Đóng khối hướng dẫn nhập mã đơn
            } // Đóng khối xử lý tra cứu đơn hàng

            // B. KIỂM TRA CÂU HỎI VỀ TỪNG LOÀI CÁ / CÂY / THIẾT BỊ CỤ THỂ (Trả lời sâu & trực tiếp)
            if (ContainsAny(noTone, "betta", "ca choi", "halfmoon", "lia thia")) // Nếu khách hỏi về cá Betta / cá chọi / lia thia
            { // Mở khối tư vấn cá Betta
                var p = FindProductByKeyword(products, "Betta"); // Tìm sản phẩm Cá Betta trong CSDL
                if (p != null) suggested.Add(p); // Đưa Cá Betta vào danh sách thẻ sản phẩm gợi ý
                sections.Add( // Thêm đoạn tư vấn chuyên sâu về cá Betta
                    "🐟 **Tư vấn chuyên sâu về Cá Betta Halfmoon:**\n" + // Tiêu đề mục tư vấn Betta
                    (p != null ? $"- **Giá tại shop:** **{p.Price:N0} ₫/con** (Tồn kho: **{p.StockQuantity} con** — Đang sẵn hàng)\n" : "") + // Báo giá và tồn kho thời gian thực
                    "- **Môi trường sống:** Nhiệt độ 25–29°C, pH 6.5–7.5. Cá Betta có cơ quan hô hấp phụ (Labyrinth) lấy oxy trên mặt nước nên **nuôi tốt trong bể mini từ 3–15 lít mà không bắt buộc phải có máy sục khí**.\n" + // Giải thích kỹ thuật nuôi
                    "- **Tập tính ghép đàn:** Tuyệt đối **không nuôi chung 2 con Betta trống** vì chúng sẽ đá nhau. Có thể thả kèm rong đuôi chồn hoặc ráy nana để cá có chỗ nghỉ.\n" + // Lưu ý ghép đàn
                    "- **Thức ăn:** Ăn cám hạt nhỏ, thức ăn vảy **Tetra Min Flakes** hoặc trùng chỉ (cho ăn 1–2 lần/ngày, mỗi lần 3–5 hạt)."); // Chế độ ăn
            } // Đóng khối tư vấn cá Betta

            if (ContainsAny(noTone, "neon", "ca dan", "tetra")) // Nếu khách hỏi về cá Neon Tetra hoặc cá bơi theo đàn
            { // Mở khối tư vấn cá Neon
                var p = FindProductByKeyword(products, "Neon"); // Tìm sản phẩm Cá Neon Tetra trong CSDL
                if (p != null) suggested.Add(p); // Thêm vào danh sách gợi ý
                sections.Add( // Thêm nội dung tư vấn cá Neon
                    "✨ **Tư vấn chuyên sâu về Cá Neon Tetra:**\n" + // Tiêu đề
                    (p != null ? $"- **Giá tại shop:** **{p.Price:N0} ₫/con** (Tồn kho: **{p.StockQuantity} con**)\n" : "") + // Giá và tồn kho thực tế
                    "- **Cách nuôi đẹp nhất:** Cá Neon là dòng cá bơi theo đàn, bạn nên nuôi tối thiểu **10–20 con** trong bể có trồng cây thủy sinh và bật đèn LED để dải màu xanh – đỏ phản quang rực rỡ.\n" + // Mật độ đàn khuyên dùng
                    "- **Thông số nước:** Nhiệt độ mát 23–27°C, pH 6.0–7.0, cần nước sạch đã chạy lọc vi sinh ổn định.\n" + // Thông số nước
                    "- **Nuôi chung:** Cực kỳ hiền lành, nuôi chung rất tốt với cá Guppy, tép cảnh và mọi loại cây thủy sinh."); // Tập tính hòa đồng
            } // Đóng khối tư vấn cá Neon

            if (ContainsAny(noTone, "guppy", "bay mau", "7 mau", "full red")) // Nếu khách hỏi về cá Guppy / cá bảy màu
            { // Mở khối tư vấn cá Guppy
                var p = FindProductByKeyword(products, "Guppy"); // Tìm sản phẩm Cá Guppy trong CSDL
                if (p != null) suggested.Add(p); // Thêm vào danh sách gợi ý
                sections.Add( // Thêm nội dung tư vấn cá Guppy
                    "🌈 **Tư vấn chuyên sâu về Cá Guppy Full Red (Bảy màu):**\n" + // Tiêu đề
                    (p != null ? $"- **Giá tại shop:** **{p.Price:N0} ₫/con** (Tồn kho: **{p.StockQuantity} con**)\n" : "") + // Giá và số lượng tồn kho
                    "- **Đặc điểm:** Dòng cá khỏe nhất cho người mới, thích nghi tốt pH 6.5–7.8, nhiệt độ 24–29°C.\n" + // Thông số môi trường
                    "- **Sinh sản:** Cá đẻ con trực tiếp (chu kỳ ~28 ngày/lứa). Nên thả thêm **Cây Rong Đuôi Chồn** để cá bột mới đẻ có chỗ trú ẩn không bị cá lớn ăn."); // Kinh nghiệm nuôi sinh sản
            } // Đóng khối tư vấn cá Guppy

            if (ContainsAny(noTone, "ca dia", "discus")) // Nếu khách hỏi về cá Đĩa (Discus)
            { // Mở khối tư vấn cá Đĩa
                var p = FindProductByKeyword(products, "Discus") ?? FindProductByKeyword(products, "Đĩa"); // Tìm sản phẩm Cá Đĩa trong CSDL
                if (p != null) suggested.Add(p); // Thêm vào danh sách gợi ý
                sections.Add( // Thêm nội dung tư vấn cá Đĩa
                    "👑 **Tư vấn nuôi Cá Đĩa (Discus) Lam Tự Nhiên:**\n" + // Tiêu đề
                    (p != null ? $"- **Giá tại shop:** **{p.Price:N0} ₫/con** (Tồn kho: **{p.StockQuantity} con**)\n" : "") + // Giá và tồn kho
                    "- **Yêu cầu kỹ thuật:** Được mệnh danh là \"Vua cá cảnh nước ngọt\", cá Đĩa cần nước ấm ổn định **28–30°C** (nên có sưởi), pH 6.0–6.8 và hệ thống lọc vi sinh tốt (như **Sunsun HW-603B**).\n" + // Yêu cầu nhiệt độ và lọc
                    "- **Chăm sóc:** Nên nuôi theo đàn từ 4–6 con trong bể từ 60cm trở lên và thay 30% nước định kỳ mỗi 2–3 ngày."); // Chế độ thay nước
            } // Đóng khối tư vấn cá Đĩa

            if (ContainsAny(noTone, "nemo", "ca he", "clownfish", "blue tang", "dory", "ca bien", "nuoc man")) // Nếu khách hỏi về cá biển / Nemo / Dory
            { // Mở khối tư vấn cá cảnh biển
                var nemo = FindProductByKeyword(products, "Nemo"); // Tìm cá Nemo trong CSDL
                var tang = FindProductByKeyword(products, "Blue Tang"); // Tìm cá Blue Tang trong CSDL
                if (nemo != null) suggested.Add(nemo); // Gợi ý cá Nemo
                if (tang != null) suggested.Add(tang); // Gợi ý cá Blue Tang
                sections.Add( // Thêm nội dung tư vấn kỹ thuật nước mặn
                    "🌊 **Tư vấn Cá Cảnh Biển (Cá Hề Nemo & Blue Tang Dory):**\n" + // Tiêu đề
                    (nemo != null ? $"- **Cá Nemo (Clownfish):** **{nemo.Price:N0} ₫** (Còn {nemo.StockQuantity} con)\n" : "") + // Giá cá Nemo
                    (tang != null ? $"- **Cá Blue Tang (Dory):** **{tang.Price:N0} ₫** (Còn {tang.StockQuantity} con)\n" : "") + // Giá cá Blue Tang
                    "- **Lưu ý quan trọng:** Đây là **cá biển (nước mặn)**, bắt buộc phải nuôi trong nước pha muối biển chuyên dụng (độ mặn **1.023–1.025**, pH **8.1–8.4**, nhiệt độ 24–26°C). **Tuyệt đối không thả vào nước ngọt thông thường**."); // Cảnh báo quan trọng về độ mặn
            } // Đóng khối tư vấn cá cảnh biển

            if (ContainsAny(noTone, "tran chau", "rong duoi chon", "ray nana", "cay thuy sinh", "trong cay", "co2")) // Nếu khách hỏi về cây thủy sinh hoặc CO2
            { // Mở khối tư vấn cây thủy sinh
                var plants = products.Where(p => p.Category.CategoryName.Contains("thủy sinh", StringComparison.OrdinalIgnoreCase)).ToList(); // Lọc các sản phẩm thuộc nhóm Cây thủy sinh
                suggested.AddRange(plants.Take(3)); // Gợi ý tối đa 3 loại cây thủy sinh
                sections.Add( // Thêm hướng dẫn chọn cây theo điều kiện bể
                    "🌱 **Tư vấn chọn & chăm sóc Cây Thủy Sinh:**\n" + // Tiêu đề
                    "- **Nếu bể KHÔNG có bình CO2 (Low-tech):** Hãy chọn **Cây Rong Đuôi Chồn (20.000 ₫)** hoặc **Cây Ráy Nana Petite gắn lũa (95.000 ₫)**. Hai loại này cực khỏe, hấp thụ độc tố NO3 tốt và chỉ cần ánh sáng vừa phải.\n" + // Cây không cần CO2
                    "- **Nếu muốn trải thảm xanh mướt:** Chọn **Cây Trân Châu Ngọc Trai (35.000 ₫)** kết hợp **Đèn LED WRGB (bật 6–8h/ngày)** và phân nền dinh dưỡng."); // Cây trải thảm tiền cảnh
            } // Đóng khối tư vấn cây thủy sinh

            if (ContainsAny(noTone, "go lua", "redmoor", "lua ra mau", "nuoc vang")) // Nếu khách hỏi về gỗ lũa hoặc hiện tượng lũa ra màu vàng
            { // Mở khối tư vấn gỗ lũa
                var wood = FindProductByKeyword(products, "Gỗ lũa"); // Tìm sản phẩm Gỗ lũa Redmoor
                if (wood != null) suggested.Add(wood); // Thêm vào danh sách gợi ý
                sections.Add( // Thêm hướng dẫn xử lý lũa
                    "🪵 **Tư vấn về Gỗ Lũa Redmoor Tự Nhiên:**\n" + // Tiêu đề
                    (wood != null ? $"- **Giá tại shop:** **{wood.Price:N0} ₫** (Tồn kho: **{wood.StockQuantity}**)\n" : "") + // Giá và tồn kho
                    "- **Cách xử lý trước khi cho vào bể:** Ngâm nước sạch 3–5 ngày (hoặc luộc qua nước sôi 20 phút) để lũa chìm hoàn toàn và nhả bớt chất Tannin (màu vàng nâu tự nhiên). Chất Tannin thực chất rất tốt cho cá Betta, Neon và Cá Đĩa vì giúp kháng khuẩn nhẹ và ổn định pH!"); // Giải thích lợi ích của Tannin
            } // Đóng khối tư vấn gỗ lũa

            if (ContainsAny(noTone, "may loc", "sunsun", "hw-603", "loc thung", "loc nuoc", "den led", "wrgb", "chihiros", "be kinh", "be ca 60")) // Nếu khách hỏi về bể kính, máy lọc hoặc đèn LED
            { // Mở khối tư vấn thiết bị bể cá
                var equips = products.Where(p => p.CategoryId == 4).ToList(); // Lọc các sản phẩm thuộc danh mục Thiết bị bể cá (CategoryId = 4)
                suggested.AddRange(equips.Take(3)); // Gợi ý các thiết bị
                sections.Add( // Thêm thông số kỹ thuật chi tiết
                    "⚙️ **Tư vấn Thiết bị Bể cá (Bể kính, Máy lọc & Đèn LED):**\n" + // Tiêu đề
                    "- **Bể cá kính 60cm (450.000 ₫):** Kích thước 60×30×36cm (~65 lít nước), kính dày 6mm siêu trong.\n" + // Thông số bể kính
                    "- **Máy lọc thùng Sunsun HW-603B (380.000 ₫):** Công suất 400–600L/h, có sẵn bông lọc 3 tầng giúp nước trong vắt và nuôi cấy hệ vi sinh cho bể 40–80cm.\n" + // Thông số máy lọc
                    "- **Đèn LED Thủy Sinh WRGB Chihiros 60cm (890.000 ₫):** Quang phổ WRGB chuyên dụng giúp cây quang hợp mạnh và tôn màu cá đỏ/xanh rực rỡ mà không gây vàng nước."); // Thông số đèn LED
            } // Đóng khối tư vấn thiết bị bể cá

            if (ContainsAny(noTone, "thuc an", "cho an", "an gi", "tetra min", "cam ca")) // Nếu khách hỏi về thức ăn cho cá hoặc cách cho ăn
            { // Mở khối tư vấn dinh dưỡng
                var food = FindProductByKeyword(products, "Tetra Min"); // Tìm sản phẩm Thức ăn Tetra Min Flakes
                if (food != null) suggested.Add(food); // Thêm vào danh sách gợi ý
                sections.Add( // Thêm hướng dẫn cho cá ăn chuẩn
                    "🥫 **Tư vấn Chế độ Dinh dưỡng & Cho cá ăn đúng cách:**\n" + // Tiêu đề
                    (food != null ? $"- **Sản phẩm khuyên dùng:** **{food.ProductName}** — Giá **{food.Price:N0} ₫/hộp 52g** (Còn {food.StockQuantity} hộp).\n" : "") + // Giá hộp thức ăn
                    "- **Nguyên tắc vàng:** Chỉ cho ăn **1–2 lần/ngày**, lượng thức ăn vừa đủ để cá ăn hết sạch trong **2–3 phút**. Cho ăn dư thừa là nguyên nhân số 1 làm đục nước và gây sình bụng cá!"); // Nguyên tắc tránh đục nước
            } // Đóng khối tư vấn dinh dưỡng

            if (ContainsAny(noTone, "tep", "red cherry", "blue dream", "red bee", "prl", "pinto", "caridina", "neocaridina", "cau reu", "marimo", "tds")) // Nếu khách hỏi về Tép cảnh hoặc Cầu rêu Marimo
            { // Mở khối tư vấn Tép cảnh & Cầu rêu
                var shrimps = products.Where(p => p.Category.CategoryName.Contains("Tép", StringComparison.OrdinalIgnoreCase) || p.ProductName.Contains("Tép", StringComparison.OrdinalIgnoreCase) || p.ProductName.Contains("Cầu Rêu", StringComparison.OrdinalIgnoreCase)).ToList(); // Lọc các sản phẩm Tép cảnh & Cầu rêu trong kho
                suggested.AddRange(shrimps.Take(4)); // Đưa tối đa 4 sản phẩm Tép cảnh & Cầu rêu vào danh sách gợi ý
                sections.Add( // Thêm hướng dẫn kỹ thuật nuôi Tép cảnh chuẩn thông số
                    "🦐 **Tư vấn Chuyên sâu về Tép Cảnh (Neocaridina & Caridina) và Cầu Rêu Marimo:**\n" + // Tiêu đề tư vấn Tép cảnh
                    "- **Nhóm Tép Màu Dễ Nuôi (Neocaridina):** Gồm **Tép Đỏ Red Cherry (18.000 ₫)** và **Tép Xanh Blue Dream (25.000 ₫)**. Sống khỏe ở nhiệt độ phòng **22–28°C**, **pH 6.5–7.5**, **TDS 120–250 ppm**, không bắt buộc dùng Chiller.\n" + // Thông số nhóm Neocaridina
                    "- **Nhóm Tép Ong Cao Cấp (Caridina):** Gồm **Tép Ong Đỏ Red Bee PRL Grade SS (120.000 ₫)** và **Tép Galaxy Pinto (250.000 ₫)**. Cần nền công nghiệp (ADA/Benibachi), nước mát **22–25°C**, **pH 5.5–6.4**, **TDS 90–140 ppm**.\n" + // Thông số nhóm Caridina
                    "- **Cầu Rêu Marimo Nhật Bản (45.000 ₫):** Quả cầu rêu nhung tự nhiên giúp hấp thụ Nitrate và tạo lớp màng Biofilm vi sinh tự nhiên cho tép con ăn mỗi ngày!"); // Công dụng Cầu rêu Marimo
            } // Đóng khối tư vấn Tép cảnh & Cầu rêu

            if (ContainsAny(noTone, "can cau", "cau tom", "cau tep", "can lure", "vot tep", "vot bat tep", "vot 3d")) // Nếu khách hỏi về Cần câu tôm tép, Cần câu Lure hoặc Vợt bắt tép
            { // Mở khối tư vấn Cần câu & Dụng cụ
                var rods = products.Where(p => p.Category.CategoryName.Contains("Cần câu", StringComparison.OrdinalIgnoreCase) || p.ProductName.Contains("Cần Câu", StringComparison.OrdinalIgnoreCase) || p.ProductName.Contains("Vợt", StringComparison.OrdinalIgnoreCase)).ToList(); // Lọc nhóm Cần câu & Vợt tép trong kho
                suggested.AddRange(rods.Take(3)); // Đưa các sản phẩm Cần câu & Dụng cụ vào danh sách gợi ý
                sections.Add( // Thêm nội dung tư vấn chọn Cần câu & Phụ kiện
                    "🎣 **Tư vấn Cần Câu Tôm Tép, Cần Câu Lure & Phụ Kiện Thủy Sinh:**\n" + // Tiêu đề tư vấn Cần câu
                    "- **Cần Câu Tôm Tép Mini Carbon 1.5m (195.000 ₫):** Trọng lượng siêu nhẹ 38g, đọt Carbon dịu báo tín hiệu cực nhạy khi tôm/tép nhấp mồi, thu gọn 38cm bỏ balo tiện lợi.\n" + // Thông số Cần câu tôm tép
                    "- **Cần Câu Lure Carbon UL 2 Khúc 1.8m (460.000 ₫):** Sợi Carbon xoắn X chịu tải cao, khoen sứ chống xoắn dây, cảm giác dòng cá suối/cá rô/cá lóc cực phấn khích.\n" + // Thông số Cần câu Lure
                    "- **Vợt Bắt Tép Cảnh 3D Cán Rút Inox (35.000 ₫):** Lưới định hình 3D chống kẹt chân và gãy râu tép, cán Inox 304 kéo dài tới 55cm."); // Thông số Vợt bắt tép 3D
            } // Đóng khối tư vấn Cần câu & Dụng cụ

            // C. TÍNH TOÁN MẬT ĐỘ NUÔI CÁ / THỂ TÍCH BỂ (VD: "bể 30cm nuôi mấy con", "bể 50 lít nuôi bao nhiêu cá")
            if (ContainsAny(noTone, "may con", "bao nhieu con", "bao nhieu ca", "mat do", "be 30", "be 40", "be 50", "lit nuoc")) // Nếu khách hỏi về số lượng cá nuôi trong bể
            { // Mở khối tính mật độ cá
                sections.Add( // Thêm công thức tính số lượng cá theo kích thước bể
                    "📐 **Công thức tính Số lượng cá nuôi chuẩn theo kích thước bể:**\n" + // Tiêu đề
                    "Theo quy chuẩn thủy sinh (**1cm chiều dài cá trưởng thành cần 1.5 – 2 lít nước**):\n" + // Quy chuẩn quốc tế
                    "- **Bể mini để bàn (20–30cm ~ 10–15L):** Nuôi **1 cá Betta**, hoặc **6–8 con Guppy / Neon**.\n" + // Gợi ý cho bể nhỏ
                    "- **Bể tầm trung (40–50cm ~ 30–40L):** Nuôi **15–20 con cá nhỏ (Neon/Guppy)**.\n" + // Gợi ý cho bể vừa
                    "- **Bể tiêu chuẩn (60×30×36cm ~ 65L):** Nuôi **25–35 con Neon/Guppy** hoặc **4–5 con Cá Đĩa (Discus)** (kèm lọc thùng Sunsun HW-603B)."); // Gợi ý cho bể 60cm
                if (!suggested.Any()) suggested.AddRange(products.Take(3)); // Đính kèm sản phẩm nếu danh sách gợi ý đang trống
            } // Đóng khối tính mật độ cá

            // D. TƯ VẤN COMBO SETUP TRỌN BỘ
            if (ContainsAny(noTone, "setup", "combo", "tron bo", "moi bat dau can mua gi")) // Nếu khách hỏi combo trọn bộ setup bể
            { // Mở khối tư vấn combo setup
                var combo = new[] // Chọn 4 món cốt lõi tạo thành bộ bể thủy sinh hoàn chỉnh
                { // Mở mảng chọn sản phẩm combo
                    FindProductByKeyword(products, "Bể cá"), // 1. Bể cá kính 60cm
                    FindProductByKeyword(products, "Sunsun"), // 2. Máy lọc thùng Sunsun HW-603B
                    FindProductByKeyword(products, "Trân Châu"), // 3. Cây Trân Châu Ngọc Trai
                    FindProductByKeyword(products, "Gỗ lũa") // 4. Gỗ lũa Redmoor
                }.Where(p => p != null).Cast<Product>().ToList(); // Lọc bỏ các phần tử null

                suggested.AddRange(combo); // Thêm combo vào danh sách gợi ý
                var sum = combo.Sum(p => p.Price); // Tính tổng chi phí của trọn bộ combo
                sections.Add( // Thêm bảng báo giá combo
                    "🌿 **Gợi ý Trọn bộ Setup Bể Thủy Sinh 60cm Chuẩn Đẹp:**\n" + // Tiêu đề
                    "1. **Bể cá kính 60cm** (450.000 ₫)\n" + // Món số 1
                    "2. **Máy lọc thùng Sunsun HW-603B** (380.000 ₫)\n" + // Món số 2
                    "3. **Gỗ lũa Redmoor** (150.000 ₫) + **Cây Trân Châu Ngọc Trai** (35.000 ₫)\n" + // Món số 3 & 4
                    $"👉 **Tổng trọn gói chỉ: {sum:N0} ₫** (Đơn trên 500k được **Miễn phí vận chuyển**!)."); // Tổng tiền và ưu đãi freeship
            } // Đóng khối tư vấn combo setup

            // E. TƯ VẤN CÁ DỄ NUÔI CHO NGƯỜI MỚI
            if (ContainsAny(noTone, "nguoi moi", "de nuoi", "tap nuoi", "sinh vien", "khong can oxy", "ca gi khoe", "nen nuoi ca gi", "tu van chon ca")) // Nếu khách hỏi cá cho người mới tập nuôi
            { // Mở khối tư vấn người mới
                var easy = products.Where(p => p.ProductId <= 3).ToList(); // Lấy 3 dòng cá dễ nuôi nhất (Betta, Neon, Guppy)
                suggested.AddRange(easy); // Thêm vào danh sách gợi ý
                sections.Add( // Thêm nội dung so sánh 3 dòng cá
                    "🐠 **Gợi ý 3 dòng cá dễ nuôi, khỏe mạnh nhất cho người mới:**\n" + // Tiêu đề
                    "1. **Cá Neon Tetra (5.000 ₫/con):** Bơi theo đàn rất đẹp, hiền lành, không phá cây thủy sinh.\n" + // Gợi ý 1
                    "2. **Cá Guppy Full Red (15.000 ₫/con):** Khỏe, ăn tạp, màu đỏ toàn thân nổi bật, dễ đẻ.\n" + // Gợi ý 2
                    "3. **Cá Betta Halfmoon (85.000 ₫/con):** Đuôi xòe rực rỡ, sống khỏe trong bể nhỏ không cần máy oxy."); // Gợi ý 3
            } // Đóng khối tư vấn người mới

            // F. TƯ VẤN GHÉP ĐÀN / NUÔI CHUNG
            if (ContainsAny(noTone, "nuoi chung", "can nhau", "ghep dan", "hoa dong", "chung voi")) // Nếu khách hỏi cá nào nuôi chung được với nhau
            { // Mở khối tư vấn nuôi chung
                sections.Add( // Thêm nguyên tắc ghép đàn
                    "🤝 **Nguyên tắc nuôi chung các loài cá không bị cắn nhau:**\n" + // Tiêu đề
                    "- **Nhóm nuôi chung hòa thuận:** Cá Neon Tetra + Cá Guppy + Cá Đĩa (trong bể lớn) + Tép/Ốc cảnh.\n" + // Nhóm hòa đồng
                    "- **Nhóm cần nuôi riêng:** **Cá Betta trống** có tính lãnh thổ cao, sẽ cắn vây các loài cá đuôi dài (như Guppy) hoặc đá nhau với Betta khác.\n" + // Nhóm hiếu chiến
                    "- **Nhóm nước mặn:** **Cá Nemo & Blue Tang** chỉ sống trong bể nước biển, không nuôi chung với cá nước ngọt."); // Nhóm cá biển
                if (!suggested.Any()) suggested.AddRange(products.Take(3)); // Gợi ý 3 sản phẩm tiêu biểu
            } // Đóng khối tư vấn nuôi chung

            // G. XỬ LÝ NƯỚC ĐỤC, RÊU HẠI, BỆNH CÁ, THẢ CÁ MỚI
            if (ContainsAny(noTone, "nuoc duc", "bi duc", "reu", "nam trang", "benh", "chet", "tha ca", "soc nuoc", "thay nuoc", "khu clo", "vi sinh", "vang la")) // Nếu khách hỏi về sự cố bể cá hoặc bệnh cá
            { // Mở khối tư vấn xử lý sự cố
                sections.Add( // Thêm phác đồ xử lý nước và trị bệnh
                    "🩺 **Hướng dẫn Xử lý Nước & Chữa bệnh Cá cảnh:**\n" + // Tiêu đề
                    "- **Nước bể bị đục trắng:** Do hệ vi sinh chưa ổn định hoặc cho ăn quá nhiều. Cách xử lý: Bật máy lọc **24/24**, thay **30% nước đã phơi khử Clo** mỗi 2 ngày và ngưng cho ăn 1 ngày.\n" + // Xử lý nước đục
                    "- **Bể bị rêu hại (rêu tóc, rêu nâu):** Giảm thời gian bật đèn xuống **6 tiếng/ngày**, thả thêm **Rong Đuôi Chồn** để hút bớt dinh dưỡng thừa.\n" + // Xử lý rêu hại
                    "- **Cá bị nấm trắng / túm lắc:** Cắm sưởi **29–30°C** kết hợp muối hột (**150g / 100 lít nước**) và sục oxy mạnh trong 3–5 ngày.\n" + // Chữa nấm trắng
                    "- **Cách thả cá mới về:** Ngâm túi 15 phút cân bằng nhiệt → Múc nước bể châm vào túi 3 lần cách nhau 5 phút → Vớt cá thả vào bể."); // Quy trình thả cá chống sốc nước
                var filter = FindProductByKeyword(products, "Sunsun"); // Tìm máy lọc Sunsun
                var plant = FindProductByKeyword(products, "Rong Đuôi Chồn"); // Tìm cây Rong Đuôi Chồn
                if (filter != null) suggested.Add(filter); // Gợi ý máy lọc
                if (plant != null) suggested.Add(plant); // Gợi ý cây hút dinh dưỡng thừa
            } // Đóng khối tư vấn xử lý sự cố

            // H. TÌM SẢN PHẨM THEO GIÁ / NGÂN SÁCH (VD: "dưới 100k", "rẻ nhất", "bảng giá", "giá bao nhiêu")
            var priceMatch = Regex.Match(noTone, @"(\d+)\s*(k|ngan|nghin|000|d|vnd)"); // Bóc tách mức giá tiền trong câu hỏi của khách
            if (priceMatch.Success || ContainsAny(noTone, "gia re", "re nhat", "duoi ", "tam gia", "ngan sach", "bang gia", "bao nhieu tien")) // Nếu khách hỏi theo tầm giá hoặc bảng giá
            { // Mở khối lọc sản phẩm theo ngân sách
                decimal maxBudget = 150000m; // Ngân sách mặc định 150.000đ nếu khách chỉ nói "giá rẻ"
                if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, out var val)) // Nếu khách nhập con số cụ thể (ví dụ: 100k)
                { // Mở khối quy đổi đơn vị tiền
                    var unit = priceMatch.Groups[2].Value; // Lấy đơn vị tiền tệ (k, ngan, nghin, d, vnd)
                    maxBudget = (unit == "k" || unit == "ngan" || unit == "nghin") ? val * 1000m : (val < 1000 ? val * 1000m : val); // Quy đổi sang số tiền VNĐ chính xác
                } // Đóng khối quy đổi đơn vị tiền

                var matchedByPrice = products // Lọc danh sách sản phẩm trong kho theo mức ngân sách
                    .Where(p => p.Price <= maxBudget && p.StockQuantity > 0) // Chỉ lấy sản phẩm có giá <= ngân sách và còn hàng
                    .OrderBy(p => p.Price) // Sắp xếp từ rẻ nhất lên cao
                    .Take(5) // Lấy tối đa 5 sản phẩm
                    .ToList(); // Chuyển thành danh sách

                if (!matchedByPrice.Any()) // Nếu ngân sách quá nhỏ không có sản phẩm nào khớp
                    matchedByPrice = products.OrderBy(p => p.Price).Take(4).ToList(); // Lấy 4 sản phẩm có giá thấp nhất cửa hàng

                suggested.AddRange(matchedByPrice); // Đưa vào danh sách thẻ sản phẩm gợi ý
                var priceList = string.Join("\n", matchedByPrice.Select(p => $"- **{p.ProductName}**: **{p.Price:N0} ₫** (Còn {p.StockQuantity})")); // Tạo danh sách báo giá từng món
                sections.Add($"💰 **Bảng giá các sản phẩm phù hợp (≤ {maxBudget:N0} ₫) đang có sẵn:**\n{priceList}"); // Thêm vào nội dung câu trả lời
            } // Đóng khối lọc sản phẩm theo ngân sách

            // I. CHÍNH SÁCH BẢO HÀNH, SHIP, THANH TOÁN, ĐỊA CHỈ, GIỜ MỞ CỬA
            if (ContainsAny(noTone, "bao hanh", "giao hang", "ship", "van chuyen", "thanh toan", "chuyen khoan", "qr", "cod", "dia chi", "o dau", "lien he", "sdt", "hotline", "mo cua", "doi tra")) // Nếu khách hỏi về dịch vụ, vận chuyển hoặc địa chỉ
            { // Mở khối trả lời chính sách cửa hàng
                sections.Add( // Thêm thông tin chính sách và liên hệ
                    "🛡️ **Thông tin Dịch vụ & Chính sách tại Aquarium Store:**\n" + // Tiêu đề
                    "- **Bảo hành cá sống 100% (48h):** Hoàn tiền 100% hoặc gửi bù miễn phí nếu cá gặp sự cố khi vận chuyển.\n" + // Chính sách bảo hành DOA
                    "- **Giao hàng:** Đóng túi Oxy 3 lớp + thùng xốp. Giao nhanh 2h tại TP.HCM & COD toàn quốc (**Miễn phí ship đơn từ 500.000 ₫**).\n" + // Chính sách vận chuyển
                    "- **Thanh toán:** Tiền mặt khi nhận hàng (**COD**) hoặc quét mã **VietQR tự động điền số tiền**.\n" + // Phương thức thanh toán
                    "- **Địa chỉ:** 123 Nguyễn Huệ, Quận 1, TP.HCM — **Hotline:** 0901 234 567 (Mở cửa 08:00 – 21:30 tất cả các ngày)."); // Địa chỉ và Hotline
            } // Đóng khối trả lời chính sách cửa hàng

            // J. LỜI CHÀO / HỎI SHOP BÁN GÌ / DANH MỤC
            if (ContainsAny(noTone, "xin chao", "chao shop", "hello", "hi ", "ban la ai", "shop ban gi", "co nhung loai nao", "danh sach san pham")) // Nếu khách chào hoặc hỏi tổng quan cửa hàng
            { // Mở khối giới thiệu tổng quan cửa hàng
                sections.Add( // Thêm thông tin tổng hợp các nhóm hàng đang bán
                    $"👋 **Xin chào bạn đến với Aquarium Store!**\n" + // Lời chào
                    $"Hiện cửa hàng đang phân phối **{products.Count} mặt hàng** chia làm 6 nhóm chính:\n" + // Số lượng sản phẩm thực tế
                    "- **Cá cảnh nước ngọt:** Cá Betta (85k), Cá Neon (5k), Cá Guppy (15k), Cá Đĩa Discus (350k)\n" + // Nhóm cá nước ngọt
                    "- **Cá cảnh biển:** Cá Hề Nemo (120k), Cá Blue Tang Dory (420k)\n" + // Nhóm cá nước mặn
                    "- **Cây thủy sinh:** Trân Châu Ngọc Trai (35k), Rong Đuôi Chồn (20k), Ráy Nana gắn lũa (95k)\n" + // Nhóm cây thủy sinh
                    "- **Thiết bị & Phụ kiện:** Bể kính 60cm (450k), Lọc thùng Sunsun (380k), Đèn WRGB (890k), Gỗ lũa Redmoor (150k), Thức ăn Tetra Min (65k)."); // Nhóm thiết bị & phụ kiện
                suggested.AddRange(products.Take(3)); // Gợi ý 3 sản phẩm tiêu biểu
            } // Đóng khối giới thiệu tổng quan cửa hàng

            // K. Nếu đã khớp bất kỳ chủ đề nào ở trên -> Trả về câu trả lời trực tiếp ngay lập tức!
            if (sections.Any()) // Kiểm tra nếu đã thu thập được ít nhất một đoạn trả lời chuyên môn
            { // Mở khối tổng hợp kết quả trả về
                var distinctProducts = suggested // Lọc bỏ các sản phẩm bị trùng lặp trong danh sách gợi ý
                    .GroupBy(p => p.ProductId) // Nhóm theo mã ProductId
                    .Select(g => g.First()) // Lấy phần tử đầu tiên của mỗi nhóm
                    .Take(4) // Giới hạn tối đa 4 thẻ sản phẩm hiển thị gọn đẹp trong khung chat
                    .ToList(); // Chuyển thành danh sách

                return new AiChatResponseViewModel // Trả về phản hồi hoàn chỉnh
                { // Mở khối khởi tạo AiChatResponseViewModel
                    Reply = string.Join("\n\n", sections), // Ghép các đoạn tư vấn cách nhau bởi dòng trống
                    SuggestedProducts = MapProducts(distinctProducts), // Ánh xạ danh sách sản phẩm sang DTO
                    FollowUpQuestions = DefaultFollowUps() // Đính kèm các câu hỏi gợi ý tiếp theo
                }; // Đóng khối khởi tạo AiChatResponseViewModel
            } // Đóng khối tổng hợp kết quả trả về

            // L. Tìm kiếm mờ (Fuzzy keyword match) trên toàn bộ tên & mô tả sản phẩm
            var relevant = FindRelevantProducts(userMsg, products, 4); // Chấm điểm độ liên quan của câu hỏi với toàn bộ sản phẩm trong kho
            if (relevant.Any()) // Nếu tìm thấy sản phẩm có từ khóa liên quan
            { // Mở khối trả lời theo sản phẩm tìm được
                var sb = new StringBuilder(); // Khởi tạo bộ ghép chuỗi StringBuilder
                sb.AppendLine($"✅ **Thông tin chi tiết cho câu hỏi \"{userMsg}\" của bạn:**"); // Dòng tiêu đề trả lời
                foreach (var p in relevant) // Duyệt qua từng sản phẩm liên quan
                { // Mở vòng lặp liệt kê thông tin sản phẩm
                    sb.AppendLine($"- **{p.ProductName}** ({p.Category.CategoryName}): Giá **{p.Price:N0} ₫** — Kho còn **{p.StockQuantity}**. {p.Description}"); // Ghi rõ tên, danh mục, giá, tồn kho và mô tả
                } // Đóng vòng lặp liệt kê thông tin sản phẩm
                return new AiChatResponseViewModel // Trả về kết quả
                { // Mở khối khởi tạo phản hồi
                    Reply = sb.ToString(), // Chuỗi nội dung chi tiết
                    SuggestedProducts = MapProducts(relevant), // Danh sách thẻ sản phẩm tương ứng
                    FollowUpQuestions = DefaultFollowUps() // Câu hỏi gợi ý tiếp theo
                }; // Đóng khối khởi tạo phản hồi
            } // Đóng khối trả lời theo sản phẩm tìm được

            // M. Trả lời tư vấn trực tiếp cho mọi câu hỏi tự nhiên khác
            var topPicks = products.OrderBy(p => p.Price).Take(3).ToList(); // Chọn 3 sản phẩm dễ tiếp cận nhất
            return new AiChatResponseViewModel // Trả về hướng dẫn kỹ thuật nuôi cá tổng quát
            { // Mở khối khởi tạo phản hồi mặc định thông minh
                Reply = $"💡 **Tư vấn nhanh cho câu hỏi \"{userMsg}\":**\n" + // Tiêu đề
                        "- Nếu bạn đang tìm **cá cảnh khỏe, dễ chăm sóc và chi phí thấp**: Hãy bắt đầu với **Cá Neon Tetra (5.000 ₫)**, **Cá Guppy Full Red (15.000 ₫)** hoặc **Cá Betta Halfmoon (85.000 ₫)**.\n" + // Gợi ý cá
                        "- Nếu bạn muốn **cây thủy sinh dễ sống không cần bình CO2**: Chọn ngay **Cây Rong Đuôi Chồn (20.000 ₫)** hoặc **Ráy Nana Petite gắn lũa (95.000 ₫)**.\n" + // Gợi ý cây
                        "- **Thông số nước chuẩn chung:** Nhiệt độ 24–28°C, pH 6.5–7.5, thay 25–30% nước đã khử Clo mỗi tuần và bật lọc 24/24.\n\n" + // Thông số nước chuẩn
                        "Dưới đây là các sản phẩm phù hợp nhất đang sẵn hàng tại kho:", // Lời dẫn xuống danh sách thẻ sản phẩm
                SuggestedProducts = MapProducts(topPicks), // Hiển thị 3 thẻ sản phẩm
                FollowUpQuestions = DefaultFollowUps() // Hiển thị các nút câu hỏi gợi ý
            }; // Đóng khối khởi tạo phản hồi mặc định thông minh
        } // Đóng thân hàm BuildDirectExpertAnswerAsync

        private async Task<string?> TryCallGeminiAsync(string apiKey, string userMsg, List<Product> products, List<AiChatTurn>? history) // Hàm gọi API Google Gemini (nếu người dùng cấu hình ApiKey)
        { // Mở thân hàm TryCallGeminiAsync
            try // Bọc trong try-catch để nếu mất mạng hoặc hết hạn key thì tự động chuyển sang Engine nội bộ
            { // Mở khối try
                var catalogBuilder = new StringBuilder(); // Khởi tạo StringBuilder chứa danh mục sản phẩm làm ngữ cảnh (RAG)
                foreach (var p in products) // Duyệt qua từng sản phẩm trong kho
                { // Mở vòng lặp ghi thông tin sản phẩm
                    catalogBuilder.AppendLine($"- [ID:{p.ProductId}] {p.ProductName} ({p.Category.CategoryName}): Giá {p.Price:N0}đ, Tồn kho: {p.StockQuantity}. Mô tả: {p.Description}"); // Định dạng dòng dữ liệu sản phẩm
                } // Đóng vòng lặp ghi thông tin sản phẩm

                var systemInstruction = $@"Bạn là AquaBot AI, chuyên gia tư vấn cá cảnh & thủy sinh của cửa hàng Aquarium Store (123 Nguyễn Huệ, Q.1, TP.HCM - Hotline 0901 234 567).
Hãy trả lời TRỰC TIẾP, rõ ràng, đúng trọng tâm câu hỏi của khách hàng bằng tiếng Việt, kèm thông số kỹ thuật cụ thể (nhiệt độ, pH, cách nuôi, giá bán).
Danh sách sản phẩm thực tế trong kho:
{catalogBuilder}"; // Chỉ dẫn hệ thống (System Prompt) cung cấp toàn bộ dữ liệu thực tế cho AI

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}"; // Đường dẫn endpoint REST API của Gemini 2.5 Flash
                var payload = new // Tạo cấu trúc dữ liệu JSON gửi lên Gemini API
                { // Mở khối payload
                    system_instruction = new { parts = new[] { new { text = systemInstruction } } }, // Truyền System Instruction
                    contents = new[] // Truyền nội dung câu hỏi của người dùng
                    { // Mở mảng contents
                        new { role = "user", parts = new[] { new { text = userMsg } } } // Lượt tin nhắn của user
                    } // Đóng mảng contents
                }; // Đóng khối payload

                var json = JsonSerializer.Serialize(payload); // Tuần tự hóa đối tượng payload thành chuỗi JSON
                using var resp = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json")); // Gửi HTTP POST tới máy chủ Gemini
                if (!resp.IsSuccessStatusCode) // Nếu mã phản hồi HTTP không thành công (khác 2xx)
                    return null; // Trả về null để dùng Engine chuyên gia nội bộ

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()); // Phân tích cú pháp chuỗi JSON phản hồi
                return doc.RootElement // Truy cập phần tử gốc của JSON
                    .GetProperty("candidates")[0] // Lấy ứng viên trả lời đầu tiên
                    .GetProperty("content") // Truy cập đối tượng content
                    .GetProperty("parts")[0] // Lấy phần tử văn bản đầu tiên
                    .GetProperty("text") // Lấy thuộc tính text
                    .GetString(); // Trả về chuỗi câu trả lời
            } // Đóng khối try
            catch // Bắt mọi lỗi kết nối mạng hoặc lỗi phân tích JSON
            { // Mở khối catch
                return null; // Trả về null an toàn để chuyển sang chế độ trả lời trực tiếp nội bộ
            } // Đóng khối catch
        } // Đóng thân hàm TryCallGeminiAsync

        private static Product? FindProductByKeyword(List<Product> products, string keyword) // Hàm tìm nhanh một sản phẩm theo từ khóa tên (không phân biệt dấu)
        { // Mở thân hàm FindProductByKeyword
            var kwNoTone = RemoveDiacritics(keyword.ToLowerInvariant()); // Chuyển từ khóa tìm kiếm về chữ thường không dấu
            return products.FirstOrDefault(p => // Tìm sản phẩm đầu tiên thỏa mãn
                RemoveDiacritics(p.ProductName.ToLowerInvariant()).Contains(kwNoTone)); // Tên sản phẩm sau khi bỏ dấu chứa từ khóa
        } // Đóng thân hàm FindProductByKeyword

        private static List<Product> FindRelevantProducts(string text, List<Product> products, int maxCount) // Hàm chấm điểm và lọc ra các sản phẩm liên quan nhất tới văn bản
        { // Mở thân hàm FindRelevantProducts
            var normText = RemoveDiacritics(text.ToLowerInvariant()); // Chuẩn hóa văn bản đầu vào về chữ thường không dấu
            var words = normText // Tách văn bản thành danh sách các từ đơn
                .Split(new[] { ' ', ',', '.', '?', '!', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries) // Cắt theo khoảng trắng và dấu câu
                .Where(w => w.Length >= 2) // Chỉ giữ lại các từ có độ dài từ 2 ký tự trở lên
                .Distinct() // Loại bỏ các từ trùng lặp
                .ToList(); // Chuyển thành danh sách

            var scored = products // Duyệt qua từng sản phẩm để tính điểm tương đồng
                .Select(p => // Chiếu mỗi sản phẩm sang cặp (product, score)
                { // Mở khối tính điểm
                    var name = RemoveDiacritics(p.ProductName.ToLowerInvariant()); // Tên sản phẩm không dấu
                    var cat = RemoveDiacritics(p.Category.CategoryName.ToLowerInvariant()); // Tên danh mục không dấu
                    var desc = RemoveDiacritics((p.Description ?? string.Empty).ToLowerInvariant()); // Mô tả sản phẩm không dấu
                    int score = 0; // Khởi tạo điểm số bằng 0

                    if (normText.Contains(name)) // Nếu câu hỏi chứa trọn vẹn tên sản phẩm
                        score += 10; // Cộng 10 điểm ưu tiên tuyệt đối

                    foreach (var w in words) // Duyệt qua từng từ khóa trong câu hỏi
                    { // Mở vòng lặp chấm điểm từ khóa
                        if (name.Contains(w)) score += 3; // Từ khóa xuất hiện trong tên sản phẩm: +3 điểm
                        if (cat.Contains(w)) score += 2; // Từ khóa xuất hiện trong tên danh mục: +2 điểm
                        if (desc.Contains(w)) score += 1; // Từ khóa xuất hiện trong mô tả: +1 điểm
                    } // Đóng vòng lặp chấm điểm từ khóa
                    return (product: p, score); // Trả về bộ giá trị sản phẩm và điểm số
                }) // Đóng khối tính điểm
                .Where(x => x.score >= 2) // Chỉ giữ lại các sản phẩm đạt từ 2 điểm trở lên
                .OrderByDescending(x => x.score) // Sắp xếp sản phẩm có điểm cao nhất lên đầu
                .ThenBy(x => x.product.Price) // Nếu bằng điểm nhau thì ưu tiên sản phẩm giá tốt hơn
                .Select(x => x.product) // Chỉ lấy đối tượng Product
                .Take(maxCount) // Giới hạn số lượng kết quả trả về
                .ToList(); // Chuyển thành danh sách

            return scored; // Trả về danh sách sản phẩm liên quan
        } // Đóng thân hàm FindRelevantProducts

        private static List<AiChatProductDto> MapProducts(IEnumerable<Product> products) // Hàm chuyển đổi danh sách thực thể Product sang DTO gọn nhẹ cho JSON
        { // Mở thân hàm MapProducts
            return products.Select(p => new AiChatProductDto // Ánh xạ từng đối tượng Product sang AiChatProductDto
            { // Mở khối khởi tạo DTO
                ProductId = p.ProductId, // Mã sản phẩm
                ProductName = p.ProductName, // Tên sản phẩm
                CategoryName = p.Category?.CategoryName ?? string.Empty, // Tên danh mục
                Price = p.Price, // Giá bán
                StockQuantity = p.StockQuantity, // Số lượng tồn kho
                ImageUrl = p.ImageUrl // Đường dẫn ảnh
            }).ToList(); // Trả về danh sách DTO
        } // Đóng thân hàm MapProducts

        private static List<string> DefaultFollowUps() // Hàm trả về danh sách các câu hỏi gợi ý mẫu
        { // Mở thân hàm DefaultFollowUps
            return new List<string> // Khởi tạo danh sách 4 câu hỏi phổ biến nhất
            { // Mở danh sách
                "Cá Betta nuôi như thế nào?", // Gợi ý hỏi cách nuôi cá Betta
                "Bể 60cm nuôi được bao nhiêu con cá?", // Gợi ý hỏi công thức tính mật độ cá
                "Cách xử lý nước bể cá bị đục", // Gợi ý hỏi cách xử lý nước đục
                "Kiểm tra đơn hàng #1" // Gợi ý tra cứu trạng thái đơn hàng trực tiếp
            }; // Đóng danh sách
        } // Đóng thân hàm DefaultFollowUps

        private static bool ContainsAny(string input, params string[] keywords) // Hàm kiểm tra chuỗi đầu vào có chứa bất kỳ từ khóa nào trong mảng hay không
        { // Mở thân hàm ContainsAny
            foreach (var kw in keywords) // Duyệt qua từng từ khóa trong danh sách truyền vào
            { // Mở vòng lặp kiểm tra
                if (input.Contains(kw, StringComparison.OrdinalIgnoreCase)) // Kiểm tra chuỗi không phân biệt hoa thường
                    return true; // Trả về true ngay khi khớp một từ khóa
            } // Đóng vòng lặp kiểm tra
            return false; // Trả về false nếu không khớp từ khóa nào
        } // Đóng thân hàm ContainsAny

        private static string RemoveDiacritics(string text) // Hàm loại bỏ dấu tiếng Việt (chuyển "cá cảnh" -> "ca canh")
        { // Mở thân hàm RemoveDiacritics
            if (string.IsNullOrEmpty(text)) return string.Empty; // Nếu chuỗi rỗng hoặc null thì trả về chuỗi rỗng
            var normalized = text.Normalize(NormalizationForm.FormD); // Tách các ký tự có dấu thành ký tự gốc và dấu thanh rời nhau (FormD)
            var sb = new StringBuilder(); // Khởi tạo bộ ghép chuỗi
            foreach (var c in normalized) // Duyệt qua từng ký tự sau khi phân tách
            { // Mở vòng lặp lọc ký tự
                var uc = CharUnicodeInfo.GetUnicodeCategory(c); // Lấy phân loại Unicode của ký tự hiện tại
                if (uc != UnicodeCategory.NonSpacingMark) // Nếu không phải là dấu phụ (dấu sắc, huyền, hỏi, ngã, nặng, mũ, móc...)
                { // Mở khối giữ lại ký tự gốc
                    sb.Append(c); // Nối ký tự chữ cái gốc vào kết quả
                } // Đóng khối giữ lại ký tự gốc
            } // Đóng vòng lặp lọc ký tự
            return sb.ToString().Normalize(NormalizationForm.FormC) // Chuẩn hóa lại chuỗi về dạng FormC
                .Replace('đ', 'd').Replace('Đ', 'D'); // Thay thế riêng chữ đ/Đ tiếng Việt thành d/D
        } // Đóng thân hàm RemoveDiacritics
    } // Đóng khối lớp AiChatService
} // Đóng khối không gian tên AquariumStore.Services
