// wwwroot/js/site.js

// Toggle admin sidebar collapse/expand
document.addEventListener('DOMContentLoaded', function () {
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.admin-sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function (e) {
            e.preventDefault();
            const collapsed = sidebar.classList.toggle('collapsed');
            localStorage.setItem('adminSidebarCollapsed', collapsed);
        });
        const collapsed = localStorage.getItem('adminSidebarCollapsed') === 'true';
        if (collapsed) sidebar.classList.add('collapsed');
    }

    initAquaBotChat(); // Khởi tạo trợ lý ảo AI tư vấn thủy sinh
    initLandingEffects(); // Khởi tạo hiệu ứng cuộn trang mượt và bộ chọn Combo trên Landing Page
    initAutoBackgroundRemoval(); // Khởi tạo bộ lọc AI Canvas tự động tách nền trắng trên toàn bộ ảnh sản phẩm
    init360Viewer(); // Khởi tạo phòng Studio xoay 360 độ & soi chi tiết sản phẩm / cần câu
});

function formatAiMarkdown(text) {
    if (!text) return '';
    let safe = text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
    safe = safe.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    safe = safe.replace(/\*(.+?)\*/g, '<em>$1</em>');
    safe = safe.replace(/\n/g, '<br/>');
    return safe;
}

function initAquaBotChat() {
    const toggleBtn = document.getElementById('ai-chat-toggle');
    const closeBtn = document.getElementById('ai-chat-close');
    const clearBtn = document.getElementById('ai-chat-clear');
    const chatWin = document.getElementById('ai-chat-window');
    const msgContainer = document.getElementById('ai-chat-messages');
    const form = document.getElementById('ai-chat-form');
    const input = document.getElementById('ai-chat-input');
    const quickBar = document.getElementById('ai-chat-quick');

    if (!toggleBtn || !chatWin || !msgContainer || !form) return;

    const STORAGE_KEY = 'aquabot_chat_history_v1';

    function renderWelcome() {
        msgContainer.innerHTML = '';
        appendBotMessage(
            "Xin chào! 👋 Mình là **AquaBot AI** — Chuyên gia tư vấn cá cảnh & thủy sinh của **Aquarium Store**.\n" +
            "Mình có thể giúp bạn:\n" +
            "• Chọn dòng cá dễ nuôi, hợp mệnh, ghép đàn không cắn nhau\n" +
            "• Lên cấu hình & báo giá **Setup bể thủy sinh 30cm / 60cm**\n" +
            "• Hướng dẫn xử lý **nước đục, rêu hại, nấm trắng**\n" +
            "• Tra cứu giá & tồn kho thực tế tại cửa hàng!",
            []
        );
    }

    function appendUserMessage(text, save = true) {
        const div = document.createElement('div');
        div.className = 'ai-msg ai-msg-user';
        div.textContent = text;
        msgContainer.appendChild(div);
        msgContainer.scrollTop = msgContainer.scrollHeight;
        if (save) saveHistory();
    }

    function appendBotMessage(reply, products, save = true) {
        const div = document.createElement('div');
        div.className = 'ai-msg ai-msg-bot';

        let html = `<div>${formatAiMarkdown(reply)}</div>`;
        if (products && products.length > 0) {
            html += `<div class="ai-product-list">`;
            for (const p of products) {
                const priceStr = new Intl.NumberFormat('vi-VN').format(p.price) + ' ₫';
                const img = p.imageUrl || '/favicon.ico';
                const stockBadge = p.stockQuantity > 0
                    ? `<span class="badge bg-success-subtle text-success border border-success-subtle" style="font-size:0.68rem;">Kho: ${p.stockQuantity}</span>`
                    : `<span class="badge bg-danger" style="font-size:0.68rem;">Hết hàng</span>`;
                html += `
                    <a href="/Product/Details/${p.productId}" class="ai-product-item">
                        <img src="${img}" alt="${p.productName}" />
                        <div class="flex-grow-1 overflow-hidden">
                            <div class="fw-semibold small text-truncate text-dark">${p.productName}</div>
                            <div class="d-flex justify-content-between align-items-center mt-1">
                                <span class="text-danger fw-bold small">${priceStr}</span>
                                ${stockBadge}
                            </div>
                        </div>
                        <i class="bi bi-chevron-right text-primary small"></i>
                    </a>`;
            }
            html += `</div>`;
        }

        div.innerHTML = html;
        msgContainer.appendChild(div);
        msgContainer.scrollTop = msgContainer.scrollHeight;
        if (save) saveHistory();
    }

    function saveHistory() {
        try {
            sessionStorage.setItem(STORAGE_KEY, msgContainer.innerHTML);
        } catch (e) { }
    }

    const saved = sessionStorage.getItem(STORAGE_KEY);
    if (saved) {
        msgContainer.innerHTML = saved;
    } else {
        renderWelcome();
    }

    toggleBtn.addEventListener('click', () => {
        const isHidden = chatWin.style.display === 'none';
        chatWin.style.display = isHidden ? 'flex' : 'none';
        if (isHidden) {
            msgContainer.scrollTop = msgContainer.scrollHeight;
            input.focus();
        }
    });

    closeBtn?.addEventListener('click', () => {
        chatWin.style.display = 'none';
    });

    clearBtn?.addEventListener('click', () => {
        sessionStorage.removeItem(STORAGE_KEY);
        renderWelcome();
    });

    async function sendPrompt(text) {
        const clean = (text || '').trim();
        if (!clean) return;

        appendUserMessage(clean);
        input.value = '';

        const typing = document.createElement('div');
        typing.className = 'ai-msg ai-msg-bot text-muted fst-italic';
        typing.innerHTML = '<i class="bi bi-three-dots me-1"></i>AquaBot đang phân tích & tra cứu kho...';
        msgContainer.appendChild(typing);
        msgContainer.scrollTop = msgContainer.scrollHeight;

        try {
            const resp = await fetch('/Chat/Ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: clean })
            });
            const data = await resp.json();
            typing.remove();
            appendBotMessage(data.reply, data.suggestedProducts || []);

            if (data.followUpQuestions && data.followUpQuestions.length > 0 && quickBar) {
                quickBar.innerHTML = data.followUpQuestions
                    .map(q => `<button type="button" class="ai-chip" data-prompt="${q}">${q}</button>`)
                    .join('');
            }
        } catch (err) {
            typing.remove();
            appendBotMessage("Xin lỗi, kết nối tạm thời gián đoạn. Bạn vui lòng thử lại nhé!", []);
        }
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        sendPrompt(input.value);
    });

    quickBar?.addEventListener('click', function (e) {
        const chip = e.target.closest('.ai-chip');
        if (chip) {
            chatWin.style.display = 'flex';
            sendPrompt(chip.getAttribute('data-prompt'));
        }
    });

    window.askAquaBot = function (question) { // Hàm toàn cục cho phép các nút trên Landing Page gọi thẳng câu hỏi vào khung Chat AI
        chatWin.style.display = 'flex'; // Mở cửa sổ Chat AI
        sendPrompt(question); // Gửi câu hỏi tư vấn ngay lập tức
    }; // Đóng hàm askAquaBot
} // Đóng hàm initAquaBotChat

// Hàm khởi tạo hiệu ứng tương tác cho Landing Page Tép Cảnh & Cần Câu
function initLandingEffects() { // Khai báo hàm khởi tạo hiệu ứng giao diện
    // 1. Chuyển đổi các Tab cấu hình Combo trên Landing Page
    const comboBtns = document.querySelectorAll('.combo-tab-btn'); // Lấy tất cả nút chọn gói Combo
    if (comboBtns.length > 0) { // Nếu đang ở trang có bộ chọn Combo
        comboBtns.forEach(function (btn) { // Gắn sự kiện click cho từng nút Tab
            btn.addEventListener('click', function () { // Xử lý khi người dùng bấm chọn gói Combo
                comboBtns.forEach(function (b) { // Đặt lại trạng thái nút về viền mảnh (outline)
                    b.classList.remove('active', 'btn-primary'); // Bỏ lớp đang chọn
                    b.classList.add('btn-outline-primary'); // Thêm lớp viền mặc định
                }); // Kết thúc vòng lặp đặt lại nút
                btn.classList.remove('btn-outline-primary'); // Bỏ lớp viền ở nút vừa bấm
                btn.classList.add('active', 'btn-primary'); // Đánh dấu nút vừa bấm là đang kích hoạt

                const targetKey = btn.getAttribute('data-combo'); // Đọc mã định danh gói Combo (starter / pro / fishing)
                document.querySelectorAll('.combo-panel').forEach(function (panel) { // Duyệt qua tất cả các khung nội dung Combo
                    panel.classList.add('d-none'); // Ẩn tất cả các khung Combo
                }); // Kết thúc ẩn khung
                const activePanel = document.getElementById('combo-' + targetKey); // Tìm khung nội dung tương ứng với nút vừa bấm
                if (activePanel) { // Nếu tìm thấy khung nội dung
                    activePanel.classList.remove('d-none'); // Hiển thị khung nội dung đó lên màn hình
                } // Đóng kiểm tra activePanel
            }); // Đóng sự kiện click
        }); // Đóng vòng lặp comboBtns
    } // Đóng kiểm tra comboBtns

    // 2. Hiệu ứng xuất hiện mượt khi cuộn trang (Scroll Reveal bằng IntersectionObserver)
    const revealItems = document.querySelectorAll('.reveal-on-scroll'); // Lấy các phần tử có lớp hiệu ứng cuộn
    if ('IntersectionObserver' in window && revealItems.length > 0) { // Kiểm tra trình duyệt hỗ trợ IntersectionObserver
        const observer = new IntersectionObserver(function (entries) { // Khởi tạo bộ quan sát khung nhìn
            entries.forEach(function (entry) { // Duyệt qua từng phần tử thay đổi trạng thái hiển thị
                if (entry.isIntersecting) { // Khi phần tử đi vào vùng nhìn thấy của màn hình
                    entry.target.classList.remove('reveal-hidden'); // Gỡ bỏ lớp ẩn
                    entry.target.classList.add('reveal-visible'); // Thêm lớp hiển thị mượt
                    observer.unobserve(entry.target); // Ngừng theo dõi phần tử đã hiển thị để tối ưu hiệu năng
                } // Đóng kiểm tra isIntersecting
            }); // Đóng vòng lặp entries
        }, { threshold: 0.08 }); // Kích hoạt ngay khi 8% phần tử lọt vào khung nhìn

        revealItems.forEach(function (el) { // Duyệt qua từng phần tử cần hiệu ứng
            el.classList.add('reveal-hidden'); // Gán trạng thái chuẩn bị hiệu ứng
            observer.observe(el); // Bắt đầu theo dõi phần tử
        }); // Đóng vòng lặp revealItems
    } // Đóng kiểm tra IntersectionObserver
} // Đóng hàm initLandingEffects

// Bộ nhớ đệm lưu kết quả tách nền AI Canvas để không phải xử lý lại cùng một bức ảnh
const bgCutoutCache = new Map(); // Khởi tạo Map lưu cặp (imgUrl + chế độ -> DataURL ảnh đã tách nền)

// Hàm AI HTML5 Canvas tự động phát hiện & xóa nền trắng/sáng, làm mịn viền và căn giữa cân đối
function processSmartCutout(imgUrl, forceStudioCutout, callback) { // Khai báo hàm tách nền thông minh
    if (!imgUrl) { // Nếu không có đường dẫn ảnh
        callback(imgUrl, false); // Trả về nguyên trạng
        return; // Kết thúc sớm
    } // Đóng kiểm tra imgUrl
    const cacheKey = `${imgUrl}__${forceStudioCutout ? 'force' : 'auto'}`; // Tạo khóa lưu đệm duy nhất
    if (bgCutoutCache.has(cacheKey)) { // Nếu ảnh này đã được tách nền trước đó
        const cached = bgCutoutCache.get(cacheKey); // Lấy dữ liệu từ bộ nhớ đệm
        callback(cached.url, cached.didCutout); // Trả kết quả tức thì (0ms)
        return; // Kết thúc hàm
    } // Đóng kiểm tra bộ nhớ đệm

    const probeImg = new Image(); // Tạo đối tượng tải ảnh ngầm trong bộ nhớ
    probeImg.crossOrigin = 'anonymous'; // Cho phép đọc dữ liệu điểm ảnh (Pixel) trên Canvas
    probeImg.onload = function () { // Khi ảnh đã tải xong
        try { // Bắt đầu khối xử lý điểm ảnh an toàn
            const maxDim = 500; // Giới hạn kích thước xử lý tối đa 500px để đạt tốc độ siêu nhanh
            const scale = Math.min(1, maxDim / Math.max(probeImg.naturalWidth || 1, probeImg.naturalHeight || 1)); // Tính tỷ lệ thu phóng hợp lý
            const w = Math.max(1, Math.round((probeImg.naturalWidth || 300) * scale)); // Chiều rộng Canvas xử lý
            const h = Math.max(1, Math.round((probeImg.naturalHeight || 300) * scale)); // Chiều cao Canvas xử lý

            const canvas = document.createElement('canvas'); // Tạo thẻ Canvas ẩn
            canvas.width = w; // Gán chiều rộng Canvas
            canvas.height = h; // Gán chiều cao Canvas
            const ctx = canvas.getContext('2d', { willReadFrequently: true }); // Lấy ngữ cảnh đồ họa 2D tối ưu đọc Pixel
            ctx.drawImage(probeImg, 0, 0, w, h); // Vẽ ảnh gốc lên Canvas

            const imgData = ctx.getImageData(0, 0, w, h); // Trích xuất mảng dữ liệu RGBA của toàn bộ điểm ảnh
            const data = imgData.data; // Mảng phẳng chứa [R, G, B, A, R, G, B, A, ...]

            // Bước 1: Kiểm tra xem ảnh đã có sẵn nền trong suốt (PNG 32-bit) hay chưa
            let transparentBorderCount = 0; // Đếm số điểm ảnh trong suốt ở viền ngoài
            let borderSamples = 0; // Tổng số điểm ảnh lấy mẫu ở 4 cạnh viền
            let sumR = 0, sumG = 0, sumB = 0, lightBorderCount = 0; // Biến thống kê màu nền sáng ở 4 cạnh viền

            function samplePixel(x, y) { // Hàm lấy mẫu 1 điểm ảnh tại tọa độ (x, y)
                const idx = (y * w + x) * 4; // Tính vị trí chỉ số trong mảng RGBA
                const r = data[idx], g = data[idx + 1], b = data[idx + 2], a = data[idx + 3]; // Đọc 4 kênh màu
                borderSamples++; // Tăng tổng số điểm lấy mẫu
                if (a < 180) { // Nếu điểm ảnh đã trong suốt sẵn
                    transparentBorderCount++; // Tăng bộ đếm trong suốt
                    return; // Không cộng vào màu nền
                } // Đóng kiểm tra alpha
                const lum = 0.299 * r + 0.587 * g + 0.114 * b; // Tính độ sáng cảm nhận (Luminance)
                const maxC = Math.max(r, g, b), minC = Math.min(r, g, b); // Tìm kênh màu lớn nhất và nhỏ nhất
                const sat = maxC - minC; // Độ bão hòa màu (ảnh nền trắng/xám/tường sáng sẽ có độ bão hòa thấp)
                if (lum > 188 && sat < 48) { // Nếu điểm viền là màu trắng, xám sáng hoặc tường kem sáng
                    lightBorderCount++; // Tăng số điểm viền sáng
                    sumR += r; // Cộng dồn kênh Đỏ
                    sumG += g; // Cộng dồn kênh Xanh lá
                    sumB += b; // Cộng dồn kênh Xanh dương
                } // Đóng kiểm tra điểm viền sáng
            } // Đóng hàm samplePixel

            for (let x = 0; x < w; x += 2) { // Quét dọc theo cạnh viền trên cùng và dưới cùng
                samplePixel(x, 0); // Lấy mẫu cạnh trên
                samplePixel(x, h - 1); // Lấy mẫu cạnh dưới
            } // Đóng vòng lặp cạnh ngang
            for (let y = 0; y < h; y += 2) { // Quét dọc theo cạnh viền bên trái và bên phải
                samplePixel(0, y); // Lấy mẫu cạnh trái
                samplePixel(w - 1, y); // Lấy mẫu cạnh phải
            } // Đóng vòng lặp cạnh dọc

            const alreadyTransparent = (transparentBorderCount / Math.max(1, borderSamples)) > 0.25; // Ảnh đã tách nền sẵn nếu >25% viền trong suốt
            const lightBorderRatio = lightBorderCount / Math.max(1, borderSamples); // Tỷ lệ viền là nền trắng/sáng
            const shouldRemoveBg = !alreadyTransparent && (lightBorderRatio > 0.22 || forceStudioCutout); // Quyết định có chạy thuật toán xóa nền hay không

            if (!alreadyTransparent && !shouldRemoveBg) { // Nếu là ảnh chụp cảnh hồ thủy sinh tối/đầy khung và không ép tách nền
                const result = { url: imgUrl, didCutout: false }; // Giữ nguyên ảnh cảnh hồ tự nhiên
                bgCutoutCache.set(cacheKey, result); // Lưu vào bộ nhớ đệm
                callback(result.url, result.didCutout); // Trả về ảnh gốc
                return; // Kết thúc
            } // Đóng kiểm tra giữ ảnh gốc

            if (shouldRemoveBg) { // Nếu phát hiện nền trắng/sáng hoặc đang bật chế độ tách nền AI trong Studio 360°
                const bgR = lightBorderCount > 0 ? (sumR / lightBorderCount) : 245; // Màu Đỏ trung bình của nền
                const bgG = lightBorderCount > 0 ? (sumG / lightBorderCount) : 245; // Màu Xanh lá trung bình của nền
                const bgB = lightBorderCount > 0 ? (sumB / lightBorderCount) : 245; // Màu Xanh dương trung bình của nền
                const colorTol = forceStudioCutout ? 64 : 52; // Ngưỡng dung sai màu nền (chặt chẽ hơn trên thẻ, rộng hơn trong Studio)

                function isBackgroundPixel(idx) { // Hàm kiểm tra một điểm ảnh có phải là nền trắng/sáng cần xóa hay không
                    const r = data[idx], g = data[idx + 1], b = data[idx + 2], a = data[idx + 3]; // Đọc RGBA
                    if (a < 30) return true; // Đã trong suốt sẵn
                    const maxC = Math.max(r, g, b), minC = Math.min(r, g, b); // Độ chênh kênh màu
                    const sat = maxC - minC; // Độ bão hòa màu
                    const lum = 0.299 * r + 0.587 * g + 0.114 * b; // Độ sáng
                    // Nếu là màu trắng/xám rất sáng và ít màu sắc (nền trắng Studio điển hình)
                    if (lum > 226 && sat < 30) return true; // Xóa ngay nền trắng Studio
                    // Hoặc nếu màu gần trùng với màu tường/phông nền sáng ở viền ảnh
                    const dist = Math.abs(r - bgR) * 0.35 + Math.abs(g - bgG) * 0.4 + Math.abs(b - bgB) * 0.25; // Khoảng cách màu có trọng số
                    return (dist <= colorTol && lum > 168 && sat < 52); // Trả về true nếu khớp phông nền sáng
                } // Đóng hàm isBackgroundPixel

                // Bước 2: Thuật toán Loang (Flood-Fill BFS) từ 4 cạnh viền vào tâm để xóa sạch nền bao quanh sản phẩm
                const visited = new Uint8Array(w * h); // Mảng đánh dấu các điểm ảnh đã duyệt
                const queue = new Int32Array(w * h); // Hàng đợi BFS tốc độ cao bằng TypedArray
                let head = 0, tail = 0; // Con trỏ đầu và cuối hàng đợi

                function tryEnqueue(x, y) { // Hàm đưa điểm ảnh vào hàng đợi nếu thuộc vùng nền
                    const p = y * w + x; // Chỉ số điểm ảnh 1 chiều
                    if (visited[p]) return; // Bỏ qua nếu đã duyệt
                    if (isBackgroundPixel(p * 4)) { // Nếu đúng là điểm ảnh nền
                        visited[p] = 1; // Đánh dấu là nền cần xóa
                        queue[tail++] = p; // Đưa vào hàng đợi để loang tiếp
                    } // Đóng kiểm tra nền
                } // Đóng hàm tryEnqueue

                for (let x = 0; x < w; x++) { // Khởi tạo hạt giống loang từ toàn bộ viền trên và viền dưới
                    tryEnqueue(x, 0); // Viền trên
                    tryEnqueue(x, h - 1); // Viền dưới
                } // Đóng vòng lặp ngang
                for (let y = 0; y < h; y++) { // Khởi tạo hạt giống loang từ toàn bộ viền trái và viền phải
                    tryEnqueue(0, y); // Viền trái
                    tryEnqueue(w - 1, y); // Viền phải
                } // Đóng vòng lặp dọc

                while (head < tail) { // Vòng lặp loang BFS xóa toàn bộ mảng nền trắng/sáng liên thông
                    const p = queue[head++]; // Lấy điểm ảnh tiếp theo từ hàng đợi
                    const idx = p * 4; // Vị trí trong mảng RGBA
                    data[idx + 3] = 0; // Biến điểm nền thành trong suốt hoàn toàn (Alpha = 0)
                    const x = p % w; // Tọa độ X hiện tại
                    const y = (p / w) | 0; // Tọa độ Y hiện tại
                    if (x > 0) tryEnqueue(x - 1, y); // Loang sang trái
                    if (x + 1 < w) tryEnqueue(x + 1, y); // Loang sang phải
                    if (y > 0) tryEnqueue(x, y - 1); // Loang lên trên
                    if (y + 1 < h) tryEnqueue(x, y + 1); // Loang xuống dưới
                } // Đóng vòng lặp BFS

                // Bước 3: Xóa tiếp các lỗ nền trắng khép kín bên trong (ví dụ khoảng trắng giữa khoen cần câu hoặc tay cầm)
                for (let p = 0; p < w * h; p++) { // Duyệt qua toàn bộ điểm ảnh
                    if (visited[p]) continue; // Bỏ qua điểm đã xóa
                    const idx = p * 4; // Chỉ số RGBA
                    const r = data[idx], g = data[idx + 1], b = data[idx + 2]; // Đọc RGB
                    const sat = Math.max(r, g, b) - Math.min(r, g, b); // Độ bão hòa màu
                    if (r > 238 && g > 238 && b > 238 && sat < 18) { // Nếu là điểm trắng tinh nằm kẹt giữa các chi tiết cần câu/vợt
                        data[idx + 3] = 0; // Xóa trong suốt luôn
                        visited[p] = 1; // Đánh dấu đã xóa
                    } // Đóng kiểm tra lỗ trắng
                } // Đóng vòng lặp xóa lỗ trắng

                // Bước 4: Khử răng cưa & làm mềm viền (Alpha Feathering) cho các điểm ảnh giáp ranh vùng vừa tách nền
                for (let y = 1; y < h - 1; y++) { // Duyệt các dòng bên trong
                    for (let x = 1; x < w - 1; x++) { // Duyệt các cột bên trong
                        const p = y * w + x; // Chỉ số điểm hiện tại
                        if (visited[p]) continue; // Chỉ xử lý điểm thuộc vật thể
                        const transparentNeighbors = visited[p - 1] + visited[p + 1] + visited[p - w] + visited[p + w]; // Đếm số điểm nền sát cạnh
                        if (transparentNeighbors > 0) { // Nếu điểm này nằm ngay sát đường viền cắt
                            const idx = p * 4; // Chỉ số RGBA
                            const r = data[idx], g = data[idx + 1], b = data[idx + 2]; // Đọc màu tại viền
                            const lum = 0.299 * r + 0.587 * g + 0.114 * b; // Độ sáng điểm viền
                            if (lum > 205) { // Nếu viền còn hơi ám trắng từ nền cũ
                                data[idx + 3] = Math.round(data[idx + 3] * (0.35 / transparentNeighbors)); // Làm mờ mềm viền để triệt tiêu viền răng cưa trắng
                            } else { // Nếu là viền màu đậm của sản phẩm
                                data[idx + 3] = Math.round(data[idx + 3] * 0.88); // Làm dịu nhẹ 12% cho đường cắt mượt như Photoshop
                            } // Đóng kiểm tra độ sáng viền
                        } // Đóng kiểm tra điểm giáp ranh
                    } // Đóng vòng lặp X
                } // Đóng vòng lặp Y
            } // Đóng khối xử lý tách nền

            // Bước 5: Tìm khung bao (Bounding Box) của chủ thể sản phẩm để cắt bỏ khoảng trống thừa và căn giữa cân đối
            let minX = w, minY = h, maxX = 0, maxY = 0, fgCount = 0; // Biến tìm tọa độ bao quanh vật thể
            for (let y = 0; y < h; y++) { // Duyệt theo chiều dọc
                for (let x = 0; x < w; x++) { // Duyệt theo chiều ngang
                    const a = data[(y * w + x) * 4 + 3]; // Đọc độ đục Alpha sau khi đã tách nền
                    if (a > 35) { // Nếu là điểm ảnh thuộc chủ thể sản phẩm
                        fgCount++; // Tăng số điểm chủ thể
                        if (x < minX) minX = x; // Cập nhật biên trái
                        if (x > maxX) maxX = x; // Cập nhật biên phải
                        if (y < minY) minY = y; // Cập nhật biên trên
                        if (y > maxY) maxY = y; // Cập nhật biên dưới
                    } // Đóng kiểm tra điểm chủ thể
                } // Đóng vòng lặp X
            } // Đóng vòng lặp Y

            // Kiểm tra an toàn: nếu chủ thể quá nhỏ (<3% diện tích) do ảnh đặc biệt thì giữ nguyên ảnh gốc để tránh mất hình
            if (fgCount < (w * h * 0.03) || maxX <= minX || maxY <= minY) { // Nếu vùng chủ thể không hợp lệ
                const fallback = { url: imgUrl, didCutout: false }; // Quay về ảnh gốc an toàn
                bgCutoutCache.set(cacheKey, fallback); // Lưu vào bộ nhớ đệm
                callback(fallback.url, fallback.didCutout); // Trả về ảnh gốc
                return; // Kết thúc
            } // Đóng kiểm tra an toàn

            ctx.putImageData(imgData, 0, 0); // Cập nhật lại hình ảnh đã xóa nền lên Canvas gốc

            // Bước 6: Tạo Canvas đầu ra cân bằng hoàn hảo (cắt gọn viền trống và đặt chủ thể vào chính giữa với khoảng thở 7%)
            const cropW = maxX - minX + 1; // Chiều rộng thực tế của chủ thể sản phẩm
            const cropH = maxY - minY + 1; // Chiều cao thực tế của chủ thể sản phẩm
            const outW = Math.round(cropW * 1.14); // Chiều rộng khung mới (thêm 7% lề mỗi bên cho cân đối)
            const outH = Math.round(cropH * 1.14); // Chiều cao khung mới (thêm 7% lề trên/dưới cho cân đối)

            const outCanvas = document.createElement('canvas'); // Tạo Canvas kết quả cuối cùng
            outCanvas.width = outW; // Gán chiều rộng cân đối
            outCanvas.height = outH; // Gán chiều cao cân đối
            const outCtx = outCanvas.getContext('2d'); // Lấy ngữ cảnh đồ họa 2D
            const offsetX = Math.round((outW - cropW) / 2); // Vị trí X căn giữa tuyệt đối
            const offsetY = Math.round((outH - cropH) / 2); // Vị trí Y căn giữa tuyệt đối
            outCtx.drawImage(canvas, minX, minY, cropW, cropH, offsetX, offsetY, cropW, cropH); // Vẽ chủ thể đã tách nền vào chính giữa khung mới

            const dataUrl = outCanvas.toDataURL('image/png'); // Xuất ra định dạng ảnh PNG trong suốt chất lượng cao
            const finalResult = { url: dataUrl, didCutout: true }; // Đóng gói kết quả thành công
            bgCutoutCache.set(cacheKey, finalResult); // Lưu vào bộ nhớ đệm
            callback(finalResult.url, finalResult.didCutout); // Trả về ảnh PNG đã tách nền & căn giữa
        } catch (err) { // Nếu gặp lỗi bảo mật CORS hoặc lỗi đọc ảnh
            callback(imgUrl, false); // Trả về ảnh gốc an toàn
        } // Đóng khối try-catch
    }; // Đóng sự kiện onload
    probeImg.onerror = function () { // Nếu đường dẫn ảnh bị lỗi
        callback(imgUrl, false); // Trả về nguyên trạng
    }; // Đóng sự kiện onerror
    probeImg.src = imgUrl; // Bắt đầu tải ảnh để xử lý
} // Đóng hàm processSmartCutout

// Hàm tự động quét và tách nền trắng cho toàn bộ ảnh sản phẩm trên Trang chủ, Landing Page, Danh mục & Chi tiết
function initAutoBackgroundRemoval() { // Khai báo hàm tự động tách nền toàn trang
    const productImages = document.querySelectorAll('.product-card img, .product-detail-img, .combo-preview-img'); // Chọn tất cả ảnh sản phẩm trên giao diện
    productImages.forEach(function (img) { // Duyệt qua từng thẻ ảnh sản phẩm
        const originalSrc = img.getAttribute('src'); // Lấy đường dẫn ảnh gốc
        if (!originalSrc || originalSrc.startsWith('data:')) return; // Bỏ qua nếu rỗng hoặc đã là ảnh DataURL
        img.setAttribute('data-original-src', originalSrc); // Lưu lại đường dẫn gốc để dùng khi cần
        processSmartCutout(originalSrc, false, function (newUrl, didCutout) { // Chạy thuật toán phát hiện & xóa nền trắng tự động
            if (didCutout && newUrl) { // Nếu ảnh có nền trắng/sáng và đã được tách nền thành công
                img.src = newUrl; // Thay thế bằng ảnh PNG trong suốt đã căn giữa cân đối
                img.classList.add('bg-cutout-done'); // Gắn class CSS để tạo bóng đổ 3D ôm sát vật thể
            } // Đóng kiểm tra didCutout
        }); // Đóng lời gọi processSmartCutout
    }); // Đóng vòng lặp productImages
} // Đóng hàm initAutoBackgroundRemoval

// Hàm khởi tạo trình xem xoay 360 độ 3D & phóng to chi tiết (360° Product Studio tích hợp AI Tách Nền)
function init360Viewer() { // Khai báo hàm khởi tạo Studio 360 độ
    const modalEl = document.getElementById('viewer360Modal'); // Lấy phần tử Modal 360 độ trong _Layout.cshtml
    if (!modalEl) return; // Thoát nếu không tìm thấy Modal

    const stage = document.getElementById('v360-stage'); // Vùng kéo chuột xoay 360 độ
    const turntable = document.getElementById('v360-turntable'); // Khung 3D gắn hình sản phẩm
    const imgEl = document.getElementById('v360-img'); // Thẻ hình ảnh sản phẩm
    const glareEl = document.getElementById('v360-glare'); // Lớp ánh sáng phản quang khi xoay
    const ringEl = document.getElementById('v360-ring'); // Vòng la bàn 3D dưới đáy
    const slider = document.getElementById('v360-slider'); // Thanh trượt góc 0 - 360 độ
    const angleBadge = document.getElementById('v360-angle-badge'); // Huy hiệu hiển thị số độ hiện tại
    const bgRemoveBtn = document.getElementById('v360-bgremove-btn'); // Nút bật/tắt chế độ Tách nền AI trong Studio 360°
    const autoBtn = document.getElementById('v360-autorotate-btn'); // Nút bật/tắt tự động xoay 360 độ
    const resetBtn = document.getElementById('v360-reset-btn'); // Nút đặt lại góc xoay ban đầu
    const detailBox = document.getElementById('v360-detail-box'); // Hộp mô tả chi tiết bộ phận đang soi

    let currentAngle = 0; // Góc xoay hiện tại (0 -> 360 độ)
    let currentZoom = 1; // Độ phóng đại hiện tại (1x, 1.6x, 2.2x)
    let currentOrigin = '50% 50%'; // Tâm phóng đại khi soi chi tiết
    let isAutoRotating = true; // Cờ trạng thái tự động xoay tròn 360 độ
    let isBgRemoveEnabled = true; // Mặc định BẬT tự động tách nền trắng AI khi mở Studio 360°
    let rawImageUrl = ''; // Lưu đường dẫn ảnh gốc của sản phẩm đang mở trong Studio
    let isDragging = false; // Cờ trạng thái người dùng đang nhấn giữ chuột để kéo xoay
    let startX = 0; // Tọa độ X lúc bắt đầu kéo chuột
    let animFrameId = null; // Mã định danh vòng lặp hoạt ảnh requestAnimationFrame

    function applyStudioImageSource() { // Hàm áp dụng ảnh đã tách nền AI hoặc ảnh gốc lên bàn xoay 360°
        if (!rawImageUrl) return; // Kiểm tra đường dẫn hợp lệ
        if (bgRemoveBtn) { // Cập nhật giao diện nút Tách nền AI
            bgRemoveBtn.className = isBgRemoveEnabled ? 'btn btn-info text-dark fw-bold' : 'btn btn-outline-info'; // Đổi màu nút theo trạng thái
            bgRemoveBtn.innerHTML = isBgRemoveEnabled
                ? '<i class="bi bi-magic me-1"></i>Tách nền AI: BẬT'
                : '<i class="bi bi-image me-1"></i>Ảnh gốc (Chưa tách)'; // Cập nhật nhãn nút rõ ràng
        } // Đóng cập nhật nút
        if (!isBgRemoveEnabled) { // Nếu người dùng chọn xem ảnh gốc
            imgEl.src = rawImageUrl; // Gán lại đường dẫn ảnh gốc
            return; // Kết thúc
        } // Đóng kiểm tra tắt tách nền
        processSmartCutout(rawImageUrl, true, function (cutoutUrl) { // Chạy tách nền AI chế độ Studio
            imgEl.src = cutoutUrl || rawImageUrl; // Hiển thị hình ảnh sản phẩm nổi 3D không còn nền trắng
        }); // Đóng lời gọi processSmartCutout
    } // Đóng hàm applyStudioImageSource

    function configureDynamicHotspots(productName, categoryName) { // Hàm tự động đổi nhãn 4 góc soi chi tiết phù hợp với từng loại sản phẩm
        const text = `${productName || ''} ${categoryName || ''}`.toLowerCase(); // Gộp tên sản phẩm và danh mục để nhận diện
        const hsBtns = document.querySelectorAll('.v360-hotspot'); // Lấy danh sách 4 nút Hotspot
        if (hsBtns.length < 4) return; // Đảm bảo đủ 4 nút

        const hs2Title = document.getElementById('v360-hs2-title'); // Tiêu đề Hotspot số 2
        const hs2Sub = document.getElementById('v360-hs2-sub'); // Phụ đề Hotspot số 2
        const hs3Title = document.getElementById('v360-hs3-title'); // Tiêu đề Hotspot số 3
        const hs3Sub = document.getElementById('v360-hs3-sub'); // Phụ đề Hotspot số 3
        const hs4Title = document.getElementById('v360-hs4-title'); // Tiêu đề Hotspot số 4
        const hs4Sub = document.getElementById('v360-hs4-sub'); // Phụ đề Hotspot số 4

        if (text.includes('cần') || text.includes('lure') || text.includes('vợt') || text.includes('dụng cụ')) { // Nếu là Cần câu hoặc Dụng cụ
            if (hs2Title) hs2Title.innerHTML = '<i class="bi bi-hand-index-thumb me-1"></i>2. Soi Cận Cảnh Tay Cầm & Khớp Khóa'; // Nhãn cần câu 2
            if (hs2Sub) hs2Sub.textContent = 'Phóng to 2.0x phần cán bần & pat máy Inox'; // Phụ đề cần câu 2
            hsBtns[1].setAttribute('data-desc', 'Soi cận cảnh Tay cầm & Pat máy (Zoom 2.0x): Cán gỗ bần / EVA chống trượt, khớp khóa vặn Inox 304 chắc chắn.'); // Mô tả 2
            if (hs3Title) hs3Title.innerHTML = '<i class="bi bi-layers-half me-1"></i>3. Soi Mặt Sau 180° & Phôi Carbon'; // Nhãn cần câu 3
            if (hs3Sub) hs3Sub.textContent = 'Lật góc 180° kiểm tra vân sợi Carbon xoắn X'; // Phụ đề cần câu 3
            hsBtns[2].setAttribute('data-desc', 'Góc lật mặt sau 180° (Zoom 1.6x): Phôi sợi Carbon xoắn X siêu nhẹ, chịu lực uốn cong đàn hồi cao.'); // Mô tả 3
            if (hs4Title) hs4Title.innerHTML = '<i class="bi bi-lightning-charge me-1"></i>4. Soi Cận Cảnh Đọt Nhạy & Đầu Khoen'; // Nhãn cần câu 4
            if (hs4Sub) hs4Sub.textContent = 'Phóng to 2.2x kiểm tra khoen sứ & ngọn cần'; // Phụ đề cần câu 4
            hsBtns[3].setAttribute('data-desc', 'Soi cận cảnh Ngọn cần & Đầu khoen (Zoom 2.2x): Khoen sứ Ceramic chống xoắn dây, đọt dịu báo tín hiệu cực nhạy.'); // Mô tả 4
        } else if (text.includes('tép') || text.includes('cá') || text.includes('ốc')) { // Nếu là Tép cảnh hoặc Cá cảnh
            if (hs2Title) hs2Title.innerHTML = '<i class="bi bi-gem me-1"></i>2. Soi Cận Cảnh Đầu & Sắc Tố Giáp'; // Nhãn tép/cá 2
            if (hs2Sub) hs2Sub.textContent = 'Phóng to 2.0x kiểm tra độ dày vỏ & màu thuần chủng'; // Phụ đề tép/cá 2
            hsBtns[1].setAttribute('data-desc', 'Soi cận cảnh Phần đầu & Giáp lưng (Zoom 2.0x): Sắc tố vỏ dày bóng, lên màu đậm đều không bị loang nhạt.'); // Mô tả 2
            if (hs3Title) hs3Title.innerHTML = '<i class="bi bi-arrow-repeat me-1"></i>3. Góc Nghiêng 180° & Dáng Bơi'; // Nhãn tép/cá 3
            if (hs3Sub) hs3Sub.textContent = 'Lật góc 180° quan sát toàn bộ cơ thể hai bên'; // Phụ đề tép/cá 3
            hsBtns[2].setAttribute('data-desc', 'Góc quan sát đối xứng 180° (Zoom 1.6x): Dáng bơi cân đối, khỏe mạnh, đã thuần nước ổn định tại trại.'); // Mô tả 3
            if (hs4Title) hs4Title.innerHTML = '<i class="bi bi-stars me-1"></i>4. Soi Cận Cảnh Đuôi, Vây & Chân Bơi'; // Nhãn tép/cá 4
            if (hs4Sub) hs4Sub.textContent = 'Phóng to 2.2x kiểm tra chân bò và vây đuôi'; // Phụ đề tép/cá 4
            hsBtns[3].setAttribute('data-desc', 'Soi cận cảnh Vây đuôi & Chân bơi (Zoom 2.2x): Đầy đủ râu, chân bò và vây đuôi xòe đẹp, không dị tật.'); // Mô tả 4
        } else if (text.includes('cây') || text.includes('rêu') || text.includes('lũa')) { // Nếu là Cây thủy sinh hoặc Cầu rêu
            if (hs2Title) hs2Title.innerHTML = '<i class="bi bi-flower1 me-1"></i>2. Soi Cận Cảnh Tán Lá & Đỉnh Ngọn'; // Nhãn cây/rêu 2
            if (hs2Sub) hs2Sub.textContent = 'Phóng to 2.0x kiểm tra mật độ lá xanh mướt'; // Phụ đề cây/rêu 2
            hsBtns[1].setAttribute('data-desc', 'Soi cận cảnh Tán lá thủy sinh (Zoom 2.0x): Lá nước khỏe mạnh, xanh mướt tự nhiên, sạch rêu hại và ốc hại.'); // Mô tả 2
            if (hs3Title) hs3Title.innerHTML = '<i class="bi bi-tree me-1"></i>3. Góc Xoay 180° & Bố Cục Dáng Lũa'; // Nhãn cây/rêu 3
            if (hs3Sub) hs3Sub.textContent = 'Lật góc 180° ngắm trọn vẹn thế dáng từ mặt sau'; // Phụ đề cây/rêu 3
            hsBtns[2].setAttribute('data-desc', 'Góc xoay 180° (Zoom 1.6x): Bố cục tán cây và dáng lũa hài hòa ở mọi góc nhìn trong bể thủy sinh.'); // Mô tả 3
            if (hs4Title) hs4Title.innerHTML = '<i class="bi bi-moisture me-1"></i>4. Soi Cận Cảnh Bộ Rễ & Giá Thể'; // Nhãn cây/rêu 4
            if (hs4Sub) hs4Sub.textContent = 'Phóng to 2.2x kiểm tra rễ bám chắc vào lũa/đá'; // Phụ đề cây/rêu 4
            hsBtns[3].setAttribute('data-desc', 'Soi cận cảnh Bộ rễ & Giá thể (Zoom 2.2x): Rễ đã bám chặt vào lũa/giá thể, chỉ cần đặt vào bể là phát triển ngay.'); // Mô tả 4
        } else { // Các thiết bị lọc, đèn LED, phụ kiện, thức ăn
            if (hs2Title) hs2Title.innerHTML = '<i class="bi bi-cpu me-1"></i>2. Soi Cận Cảnh Thân Máy & Vật Liệu'; // Nhãn thiết bị 2
            if (hs2Sub) hs2Sub.textContent = 'Phóng to 2.0x kiểm tra độ hoàn thiện vỏ ngoài'; // Phụ đề thiết bị 2
            hsBtns[1].setAttribute('data-desc', 'Soi cận cảnh Thân sản phẩm (Zoom 2.0x): Vật liệu nhựa ABS / hợp kim cao cấp, gia công sắc sảo chống rò nước.'); // Mô tả 2
            if (hs3Title) hs3Title.innerHTML = '<i class="bi bi-box me-1"></i>3. Góc Xoay 180° Mặt Sau Sản Phẩm'; // Nhãn thiết bị 3
            if (hs3Sub) hs3Sub.textContent = 'Lật góc 180° quan sát thiết kế tổng thể hai mặt'; // Phụ đề thiết bị 3
            hsBtns[2].setAttribute('data-desc', 'Góc xoay 180° (Zoom 1.6x): Thiết kế gọn gàng, tối ưu không gian lắp đặt cho bể cá và bể tép.'); // Mô tả 3
            if (hs4Title) hs4Title.innerHTML = '<i class="bi bi-sliders me-1"></i>4. Soi Cận Cảnh Đầu Kết Nối & Phụ Kiện'; // Nhãn thiết bị 4
            if (hs4Sub) hs4Sub.textContent = 'Phóng to 2.2x kiểm tra khớp nối, van và thông số'; // Phụ đề thiết bị 4
            hsBtns[3].setAttribute('data-desc', 'Soi cận cảnh Chi tiết kỹ thuật (Zoom 2.2x): Đầy đủ khớp nối tiêu chuẩn, vận hành êm ái và tiết kiệm điện.'); // Mô tả 4
        } // Đóng phân nhánh danh mục
    } // Đóng hàm configureDynamicHotspots

    function applyTransform() { // Hàm cập nhật góc xoay 3D thực tế lên phần tử giao diện
        const normAngle = ((Math.round(currentAngle) % 360) + 360) % 360; // Chuẩn hóa góc luôn nằm trong khoảng 0 - 359 độ
        const rad = (normAngle * Math.PI) / 180; // Đổi từ độ sang Radian để tính hiệu ứng ánh sáng
        const tiltX = Math.sin(rad) * 5; // Độ nghiêng trục X nhẹ tạo cảm giác lượn 3D chân thực
        const flipScaleX = Math.cos(rad) < 0 ? -1 : 1; // Tự động lật mặt hình ảnh khi xoay sang nửa vòng sau (90° -> 270°)

        turntable.style.transform = `rotateX(${tiltX.toFixed(1)}deg) rotateY(${normAngle}deg)`; // Xoay bàn xoay 360 độ trong không gian 3D
        imgEl.style.transformOrigin = currentOrigin; // Đặt tâm phóng đại (cán cần / phôi cần / đọt cần)
        imgEl.style.transform = `scaleX(${flipScaleX}) scale(${currentZoom})`; // Kết hợp lật mặt sau chuẩn xác và phóng to Zoom

        const glarePos = ((normAngle / 360) * 140) - 20; // Tính vị trí vệt sáng phản quang chạy dọc sản phẩm
        glareEl.style.background = `linear-gradient(115deg, transparent ${glarePos - 25}%, rgba(56,189,248,0.22) ${glarePos}%, transparent ${glarePos + 25}%)`; // Cập nhật ánh sáng xanh dịu
        ringEl.style.transform = `rotateZ(${normAngle}deg)`; // Xoay vòng la bàn dưới đáy đồng bộ với sản phẩm

        if (slider) slider.value = normAngle; // Đồng bộ vị trí con trượt 0 - 360
        if (angleBadge) angleBadge.innerHTML = `<i class="bi bi-compass me-1"></i>Góc xoay: <strong>${normAngle}°</strong> / 360° (Zoom ${currentZoom}x)`; // Hiển thị số độ thời gian thực
    } // Đóng hàm applyTransform

    function updateAutoButtonUI() { // Hàm cập nhật giao diện nút Tự động xoay
        if (!autoBtn) return; // Kiểm tra nút tồn tại
        if (isAutoRotating) { // Nếu đang bật tự xoay
            autoBtn.className = 'btn btn-warning fw-bold text-dark'; // Đổi màu vàng nổi bật
            autoBtn.innerHTML = '<i class="bi bi-pause-circle-fill me-1"></i>Đang tự xoay 360°'; // Nhãn đang xoay
        } else { // Nếu đang dừng xoay để soi chi tiết
            autoBtn.className = 'btn btn-outline-warning'; // Đổi sang nút viền
            autoBtn.innerHTML = '<i class="bi bi-play-circle-fill me-1"></i>Bật tự xoay 360°'; // Nhãn bật xoay
        } // Đóng kiểm tra trạng thái
    } // Đóng hàm updateAutoButtonUI

    function tickAutoRotate() { // Vòng lặp xoay tự động mượt 60 khung hình/giây
        if (isAutoRotating && !isDragging) { // Chỉ tự tăng góc khi đang bật tự xoay và người dùng không giữ chuột
            currentAngle = (currentAngle + 0.65) % 360; // Tăng góc 0.65 độ mỗi khung hình
            applyTransform(); // Áp dụng góc xoay mới
        } // Đóng kiểm tra
        animFrameId = requestAnimationFrame(tickAutoRotate); // Lặp lại ở khung hình tiếp theo
    } // Đóng hàm tickAutoRotate

    // Sự kiện nút Bật / Tắt chế độ Tách nền AI ngay trong Studio 360°
    bgRemoveBtn?.addEventListener('click', function () { // Khi người dùng bấm nút Tách nền AI
        isBgRemoveEnabled = !isBgRemoveEnabled; // Đảo trạng thái bật/tắt tách nền
        applyStudioImageSource(); // Cập nhật lại hình ảnh trên bàn xoay ngay lập tức
    }); // Đóng sự kiện bgRemoveBtn

    // Sự kiện kéo chuột trái/phải trên bàn xoay 360 độ
    stage?.addEventListener('mousedown', function (e) { // Khi nhấn chuột xuống buồng xoay
        isDragging = true; // Bật cờ đang kéo
        isAutoRotating = false; // Tạm dừng tự xoay để người dùng chủ động điều khiển góc
        updateAutoButtonUI(); // Cập nhật nút giao diện
        startX = e.clientX; // Lưu tọa độ chuột ban đầu
    }); // Đóng sự kiện mousedown

    window.addEventListener('mousemove', function (e) { // Khi di chuyển chuột để quay sản phẩm
        if (!isDragging) return; // Bỏ qua nếu không nhấn giữ chuột
        const deltaX = e.clientX - startX; // Tính khoảng cách chuột di chuyển ngang
        startX = e.clientX; // Cập nhật mốc tọa độ mới
        currentAngle = (currentAngle + deltaX * 0.85) % 360; // Quy đổi khoảng kéo chuột thành góc xoay độ
        applyTransform(); // Cập nhật hình ảnh ngay lập tức
    }); // Đóng sự kiện mousemove

    window.addEventListener('mouseup', function () { // Khi nhả chuột
        isDragging = false; // Tắt cờ đang kéo
    }); // Đóng sự kiện mouseup

    // Hỗ trợ cảm ứng vuốt tay trên màn hình điện thoại / máy tính bảng
    stage?.addEventListener('touchstart', function (e) { // Khi chạm ngón tay vào hình
        if (e.touches.length === 1) { // Nếu chạm 1 ngón
            isDragging = true; // Bật cờ kéo
            isAutoRotating = false; // Tạm dừng tự xoay
            updateAutoButtonUI(); // Cập nhật nút
            startX = e.touches[0].clientX; // Lưu vị trí chạm
        } // Đóng kiểm tra
    }, { passive: true }); // Tối ưu cuộn cảm ứng

    stage?.addEventListener('touchmove', function (e) { // Khi vuốt ngón tay ngang màn hình
        if (!isDragging || e.touches.length !== 1) return; // Bỏ qua nếu không hợp lệ
        const deltaX = e.touches[0].clientX - startX; // Độ lệch ngang
        startX = e.touches[0].clientX; // Cập nhật vị trí mới
        currentAngle = (currentAngle + deltaX * 0.9) % 360; // Cập nhật góc xoay
        applyTransform(); // Vẽ lại góc xoay
    }, { passive: true }); // Tối ưu hiệu năng cảm ứng

    stage?.addEventListener('touchend', function () { // Khi nhấc ngón tay khỏi màn hình
        isDragging = false; // Kết thúc kéo
    }); // Đóng sự kiện touchend

    // Sự kiện kéo thanh trượt góc 0 - 360 độ
    slider?.addEventListener('input', function () { // Khi người dùng kéo thanh trượt
        isAutoRotating = false; // Tạm dừng tự xoay
        updateAutoButtonUI(); // Cập nhật nút
        currentAngle = parseFloat(slider.value) || 0; // Đọc góc từ thanh trượt
        applyTransform(); // Xoay sản phẩm tới đúng góc đã chọn
    }); // Đóng sự kiện input

    // Sự kiện nút Bật / Tắt tự động xoay 360 độ
    autoBtn?.addEventListener('click', function () { // Khi bấm nút Tự xoay
        isAutoRotating = !isAutoRotating; // Đảo trạng thái tự xoay
        updateAutoButtonUI(); // Cập nhật nhãn nút
    }); // Đóng sự kiện autoBtn

    // Sự kiện nút Đặt lại góc ban đầu
    resetBtn?.addEventListener('click', function () { // Khi bấm nút Đặt lại
        currentAngle = 0; // Đưa góc về 0 độ
        currentZoom = 1; // Đưa độ phóng đại về 1x
        currentOrigin = '50% 50%'; // Đưa tâm về chính giữa
        isAutoRotating = true; // Bật lại tự xoay
        updateAutoButtonUI(); // Cập nhật nút
        applyTransform(); // Áp dụng thay đổi
    }); // Đóng sự kiện resetBtn

    // Sự kiện các nút chọn mức phóng đại Zoom (1x, 1.6x, 2.2x)
    document.querySelectorAll('.v360-zoom-btn').forEach(function (zBtn) { // Duyệt qua từng nút Zoom
        zBtn.addEventListener('click', function () { // Khi bấm vào nút Zoom
            document.querySelectorAll('.v360-zoom-btn').forEach(b => b.classList.remove('active')); // Bỏ active cũ
            zBtn.classList.add('active'); // Đánh dấu nút Zoom đang chọn
            currentZoom = parseFloat(zBtn.getAttribute('data-zoom')) || 1; // Lấy hệ số phóng đại
            applyTransform(); // Phóng to hình ảnh sản phẩm
        }); // Đóng sự kiện click Zoom
    }); // Đóng vòng lặp nút Zoom

    // Sự kiện 4 nút Hotspot soi cận cảnh từng bộ phận của Cần câu / Sản phẩm
    document.querySelectorAll('.v360-hotspot').forEach(function (hBtn) { // Duyệt qua các nút Hotspot
        hBtn.addEventListener('click', function () { // Khi người dùng chọn một góc soi chi tiết
            document.querySelectorAll('.v360-hotspot').forEach(b => b.classList.remove('active')); // Bỏ trạng thái chọn cũ
            hBtn.classList.add('active'); // Đánh dấu mục đang soi

            const targetAngle = parseFloat(hBtn.getAttribute('data-angle')) || 0; // Góc xoay mục tiêu
            const targetZoom = parseFloat(hBtn.getAttribute('data-zoom')) || 1; // Độ phóng đại mục tiêu
            const targetOrigin = hBtn.getAttribute('data-origin') || '50% 50%'; // Vị trí bộ phận cần soi cận cảnh
            const desc = hBtn.getAttribute('data-desc') || ''; // Mô tả kỹ thuật của bộ phận

            currentAngle = targetAngle; // Chuyển tới góc mục tiêu
            currentZoom = targetZoom; // Phóng đại tới bộ phận đó
            currentOrigin = targetOrigin; // Đặt tâm phóng đại chuẩn xác
            isAutoRotating = (targetZoom === 1); // Nếu xem toàn cảnh 1x thì tự xoay, nếu đang soi cận cảnh 2x thì dừng lại để xem rõ
            updateAutoButtonUI(); // Cập nhật trạng thái nút tự xoay

            document.querySelectorAll('.v360-zoom-btn').forEach(b => { // Đồng bộ nút hiển thị mức Zoom
                b.classList.toggle('active', parseFloat(b.getAttribute('data-zoom')) === currentZoom); // Bật sáng mức Zoom tương ứng
            }); // Kết thúc đồng bộ nút Zoom

            if (detailBox) detailBox.textContent = desc; // Hiển thị thuyết minh kỹ thuật của bộ phận đang soi
            applyTransform(); // Cập nhật hình ảnh 3D ngay lập tức
        }); // Đóng sự kiện click Hotspot
    }); // Đóng vòng lặp Hotspot

    // Dừng vòng lặp hoạt ảnh khi đóng Modal để tiết kiệm CPU/GPU
    modalEl.addEventListener('hidden.bs.modal', function () { // Khi người dùng đóng cửa sổ 360 độ
        if (animFrameId) { // Nếu vòng lặp đang chạy
            cancelAnimationFrame(animFrameId); // Hủy vòng lặp hoạt ảnh
            animFrameId = null; // Đặt lại biến định danh
        } // Đóng kiểm tra
    }); // Đóng sự kiện hidden.bs.modal

    // Hàm toàn cục mở Modal xoay 360 độ cho bất kỳ sản phẩm nào khi bấm vào ảnh
    window.open360Viewer = function (imgUrl, productName, categoryName, priceStr, detailUrl) { // Khai báo hàm mở trình xem 360 độ
        document.getElementById('v360-title').textContent = productName || 'Xem Xoay 360° Sản Phẩm'; // Gán tên sản phẩm lên tiêu đề Modal
        document.getElementById('v360-category').textContent = categoryName || 'Sản phẩm chính hãng'; // Gán tên danh mục
        document.getElementById('v360-price').textContent = priceStr || ''; // Gán giá tiền

        rawImageUrl = imgUrl || '/images/products/342df03d-388b-422a-960b-7a9460c05b3a.png'; // Lưu đường dẫn ảnh gốc để xử lý tách nền AI
        isBgRemoveEnabled = true; // Luôn bật sẵn chế độ Tách nền AI khi mở Studio 360°
        applyStudioImageSource(); // Tự động xóa nền trắng và căn giữa sản phẩm ngay lập tức
        configureDynamicHotspots(productName, categoryName); // Tự động cập nhật nhãn 4 nút Hotspot theo đúng loại sản phẩm

        const hsBtns = document.querySelectorAll('.v360-hotspot'); // Lấy các nút Hotspot
        hsBtns.forEach((b, idx) => b.classList.toggle('active', idx === 0)); // Đặt lại nút Hotspot số 1 (Toàn cảnh 360°) làm mặc định
        if (detailBox && hsBtns.length > 0) { // Cập nhật lại hộp mô tả về góc toàn cảnh
            detailBox.textContent = hsBtns[0].getAttribute('data-desc') || ''; // Hiển thị mô tả góc 1
        } // Đóng cập nhật detailBox

        const linkEl = document.getElementById('v360-detail-link'); // Nút chuyển tới trang chi tiết sản phẩm
        if (linkEl && detailUrl) linkEl.href = detailUrl; // Cập nhật đường dẫn đặt mua

        currentAngle = 0; // Khởi tạo từ góc 0 độ
        currentZoom = 1; // Khởi tạo ở mức toàn cảnh 1x
        currentOrigin = '50% 50%'; // Tâm chính giữa
        isAutoRotating = true; // Tự động quay vòng 360 độ ngay khi mở lên
        updateAutoButtonUI(); // Cập nhật nút trạng thái
        applyTransform(); // Vẽ trạng thái đầu tiên

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl); // Lấy thực thể Bootstrap Modal
        modal.show(); // Hiển thị Modal xoay 360 độ lên màn hình

        if (!animFrameId) { // Nếu vòng lặp xoay chưa chạy
            animFrameId = requestAnimationFrame(tickAutoRotate); // Bắt đầu quay 360 độ mượt mà
        } // Đóng kiểm tra
    }; // Đóng hàm open360Viewer
} // Đóng hàm init360Viewer
