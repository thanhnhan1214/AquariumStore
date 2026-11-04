import { $, renderLayout } from "../core.js";
await renderLayout();
$("#app").innerHTML = `<div class="empty-state"><div style="font-size:4rem">🐡</div><h1 class="h3">Không tìm thấy trang</h1><p>Trang bạn tìm có thể đã bơi đi mất.</p><a href="/" class="btn btn-primary rounded-pill">Về trang chủ</a></div>`;
