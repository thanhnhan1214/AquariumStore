using AquariumStore.Services.Interfaces; // Nạp không gian tên chứa giao diện IAiChatService
using AquariumStore.ViewModels; // Nạp không gian tên chứa AiChatRequest và AiChatResponseViewModel
using Microsoft.AspNetCore.Mvc; // Nạp thư viện ASP.NET Core MVC

namespace AquariumStore.Controllers // Khai báo không gian tên cho các Controller
{ // Mở khối không gian tên AquariumStore.Controllers
    /// <summary>
    /// API Controller phục vụ khung chat Trợ lý AI Thủy sinh (AquaBot AI).
    /// </summary>
    [Route("[controller]")] // Định tuyến gốc của Controller là /Chat
    public class ChatController : Controller // Lớp điều khiển xử lý yêu cầu hội thoại AI
    { // Mở khối lớp ChatController
        private readonly IAiChatService _aiChatService; // Dịch vụ xử lý trí tuệ nhân tạo AquaBot AI

        public ChatController(IAiChatService aiChatService) // Hàm khởi tạo nhận IAiChatService qua Dependency Injection
        { // Mở thân hàm khởi tạo
            _aiChatService = aiChatService; // Gán dịch vụ AI vào biến thành viên
        } // Đóng thân hàm khởi tạo

        // POST: /Chat/Ask
        [HttpPost("Ask")] // Định tuyến endpoint POST /Chat/Ask nhận dữ liệu JSON từ trình duyệt
        public async Task<IActionResult> Ask([FromBody] AiChatRequest request) // Nhận đối tượng AiChatRequest từ phần thân (Body) của HTTP Request
        { // Mở thân hàm Ask
            var result = await _aiChatService.AskAsync(request?.Message ?? string.Empty, request?.History); // Gọi dịch vụ phân tích câu hỏi và tạo câu trả lời trực tiếp
            return Json(result); // Trả về đối tượng phản hồi dưới định dạng JSON cho khung chat giao diện
        } // Đóng thân hàm Ask
    } // Đóng khối lớp ChatController
} // Đóng khối không gian tên AquariumStore.Controllers
