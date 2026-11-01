using AquariumStore.ViewModels; // Nạp không gian tên chứa các ViewModel của khung chat AI

namespace AquariumStore.Services.Interfaces // Khai báo không gian tên cho các giao diện dịch vụ
{ // Mở khối không gian tên AquariumStore.Services.Interfaces
    /// <summary>
    /// Interface dịch vụ Trợ lý AI tư vấn cá cảnh, thủy sinh và gợi ý sản phẩm theo dữ liệu thực tế.
    /// </summary>
    public interface IAiChatService // Giao diện định nghĩa dịch vụ trả lời tự động của trợ lý AquaBot AI
    { // Mở khối giao diện IAiChatService
        Task<AiChatResponseViewModel> AskAsync(string message, List<AiChatTurn>? history = null); // Xử lý câu hỏi của khách và trả về câu trả lời kèm sản phẩm gợi ý
    } // Đóng khối giao diện IAiChatService
} // Đóng khối không gian tên AquariumStore.Services.Interfaces
