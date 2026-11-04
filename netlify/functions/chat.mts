import type { Config } from "@netlify/functions";
import { GoogleGenAI } from "@google/genai";
import { and, eq } from "drizzle-orm";
import { db, products, categories } from "../../db/index.js";
import { handle, json, readJson, str, HttpError } from "../lib/http.js";

const vnd = (n: number) => Number(n).toLocaleString("vi-VN") + "đ";
const norm = (s: string) => s.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d");

export default handle(async (req) => {
  if (req.method !== "POST") throw new HttpError(405, "Method not allowed");
  const b = await readJson(req);
  const message = str(b.message, 1000);
  if (!message) throw new HttpError(400, "Bạn hãy nhập câu hỏi nhé.");
  const history = (Array.isArray(b.history) ? b.history : []).slice(-8)
    .filter((t: any) => t && (t.role === "user" || t.role === "bot") && typeof t.text === "string")
    .map((t: any) => ({ role: t.role === "user" ? "user" : "model", parts: [{ text: t.text.slice(0, 1500) }] }));

  const list = await db.select({
    id: products.id, name: products.name, price: products.price, stock: products.stock,
    description: products.description, category: categories.name,
  }).from(products).innerJoin(categories, eq(categories.id, products.categoryId))
    .where(and(eq(products.isActive, true), eq(categories.isActive, true)));

  const catalog = list.map((p) =>
    `#${p.id} | ${p.name} | ${p.category} | ${vnd(p.price)} | ${p.stock > 0 ? `còn ${p.stock}` : "HẾT HÀNG"} | ${p.description ?? ""}`).join("\n");

  const system = `Bạn là AquaBot, trợ lý tư vấn của cửa hàng cá cảnh – thủy sinh Aquarium Store (Việt Nam).
- Luôn trả lời bằng tiếng Việt, thân thiện, ngắn gọn (tối đa khoảng 150 từ), có thể dùng gạch đầu dòng.
- Tư vấn chăm sóc cá, tép, cây thủy sinh, thông số nước, setup bể; khi gợi ý sản phẩm CHỈ dùng sản phẩm trong danh sách dưới đây, ghi đúng tên và giá.
- Khi nhắc tới sản phẩm, viết mã dạng [#id] ngay sau tên để website tạo liên kết.
- Không bịa sản phẩm, giá hay chính sách. Hết hàng thì nói rõ và gợi ý sản phẩm thay thế.
- Thanh toán hỗ trợ: COD và chuyển khoản ngân hàng (mã VietQR). Câu hỏi ngoài chủ đề thì lịch sự đưa về chủ đề cửa hàng.

DANH SÁCH SẢN PHẨM (mã | tên | danh mục | giá | tồn kho | mô tả):
${catalog}`;

  let reply = "";
  try {
    const ai = new GoogleGenAI({});
    const res = await ai.models.generateContent({
      model: "gemini-3-flash-preview",
      contents: [...history, { role: "user", parts: [{ text: message }] }],
      config: { systemInstruction: system, temperature: 0.6, maxOutputTokens: 800 },
    });
    reply = (res.text || "").trim();
  } catch (err) {
    console.error("AquaBot AI error", err);
  }
  if (!reply) reply = fallback(message, list);

  const ids = [...new Set([...reply.matchAll(/\[#(\d+)\]/g)].map((m) => Number(m[1])))];
  const suggestions = list.filter((p) => ids.includes(p.id)).slice(0, 4).map(({ id, name, price, stock }) => ({ id, name, price, stock }));
  return json({ reply, suggestions });
});

// Trả lời dự phòng khi dịch vụ AI tạm thời không khả dụng
function fallback(message: string, list: { id: number; name: string; price: number; stock: number; category: string }[]) {
  const q = norm(message);
  const words = q.split(/\s+/).filter((w) => w.length >= 3);
  const hits = list
    .map((p) => ({ p, score: words.filter((w) => norm(`${p.name} ${p.category}`).includes(w)).length }))
    .filter((x) => x.score > 0).sort((a, b) => b.score - a.score).slice(0, 4).map((x) => x.p);
  if (hits.length)
    return "Mình tìm thấy vài sản phẩm phù hợp:\n" + hits.map((p) => `- ${p.name} [#${p.id}] – ${vnd(p.price)}${p.stock > 0 ? "" : " (tạm hết hàng)"}`).join("\n");
  return "Xin chào! Mình là AquaBot 🐠. Bạn có thể hỏi mình về cá cảnh, tép, cây thủy sinh, thiết bị bể hoặc cách chăm sóc. Ví dụ: \"Bể 60cm nên nuôi cá gì?\"";
}

export const config: Config = { path: "/api/chat" };
