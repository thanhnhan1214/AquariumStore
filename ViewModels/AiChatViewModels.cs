namespace AquariumStore.ViewModels // Khai báo không gian tên chứa các ViewModel cho trợ lý AI AquaBot
{ // Mở khối không gian tên AquariumStore.ViewModels
    public class AiChatTurn // Lớp biểu diễn một lượt hội thoại (câu hỏi của khách hoặc câu trả lời của bot)
    { // Mở khối lớp AiChatTurn
        public string Role { get; set; } = "user"; // Vai trò người nói: "user" (khách hàng) hoặc "assistant" (AquaBot)
        public string Content { get; set; } = string.Empty; // Nội dung văn bản của lượt tin nhắn
    } // Đóng khối lớp AiChatTurn

    public class AiChatRequest // Lớp dữ liệu nhận yêu cầu đặt câu hỏi từ trình duyệt gửi lên API /Chat/Ask
    { // Mở khối lớp AiChatRequest
        public string Message { get; set; } = string.Empty; // Câu hỏi hiện tại khách hàng vừa nhập
        public List<AiChatTurn>? History { get; set; } // Lịch sử các câu hội thoại trước đó để giữ ngữ cảnh tư vấn
    } // Đóng khối lớp AiChatRequest

    public class AiChatProductDto // Lớp DTO gọn nhẹ chứa thông tin sản phẩm gợi ý kèm theo câu trả lời của AI
    { // Mở khối lớp AiChatProductDto
        public int ProductId { get; set; } // Mã sản phẩm để tạo đường dẫn xem chi tiết hoặc thêm vào giỏ
        public string ProductName { get; set; } = string.Empty; // Tên sản phẩm cá cảnh / thủy sinh / thiết bị
        public string CategoryName { get; set; } = string.Empty; // Tên danh mục của sản phẩm
        public decimal Price { get; set; } // Giá bán hiện tại (VNĐ)
        public int StockQuantity { get; set; } // Số lượng tồn kho thực tế trong CSDL
        public string? ImageUrl { get; set; } // Đường dẫn ảnh sản phẩm hiển thị trong khung chat
    } // Đóng khối lớp AiChatProductDto

    public class AiChatResponseViewModel // Lớp dữ liệu phản hồi JSON trả về cho khung chat AquaBot ở giao diện
    { // Mở khối lớp AiChatResponseViewModel
        public string Reply { get; set; } = string.Empty; // Nội dung câu trả lời tư vấn chi tiết của AI (hỗ trợ Markdown/HTML cơ bản)
        public List<AiChatProductDto> SuggestedProducts { get; set; } = new(); // Danh sách các thẻ sản phẩm gợi ý liên quan trực tiếp tới câu hỏi
        public List<string> FollowUpQuestions { get; set; } = new(); // Danh sách các câu hỏi gợi ý tiếp theo để khách bấm nhanh
    } // Đóng khối lớp AiChatResponseViewModel
} // Đóng khối không gian tên AquariumStore.ViewModels
