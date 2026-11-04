import type { Config } from "@netlify/functions";
import { getStore } from "@netlify/blobs";
import crypto from "node:crypto";
import { handle, json, requireUser, HttpError, STAFF } from "../lib/http.js";

const TYPES: Record<string, string> = { "image/jpeg": "jpg", "image/png": "png", "image/webp": "webp", "image/gif": "gif" };
const MAX = 4 * 1024 * 1024;

export default handle(async (req, ctx) => {
  const store = getStore({ name: "product-images", consistency: "strong" });

  const key = decodeURIComponent(new URL(req.url).pathname.replace(/^\/api\/images\/?/, ""));
  if (req.method === "GET" && key) {
    if (!/^[a-f0-9-]{36}\.(jpg|png|webp|gif)$/.test(key)) return new Response("Not found", { status: 404 });
    const res = await store.getWithMetadata(key, { type: "arrayBuffer" });
    if (!res) return new Response("Not found", { status: 404 });
    return new Response(res.data, {
      headers: { "content-type": String(res.metadata?.contentType || "image/jpeg"), "cache-control": "public, max-age=31536000, immutable" },
    });
  }

  if (req.method === "POST") {
    await requireUser(req, STAFF);
    const form = await req.formData().catch(() => null);
    const file = form?.get("file");
    if (!file || typeof file === "string") throw new HttpError(400, "Vui lòng chọn ảnh để tải lên.");
    const ext = TYPES[file.type];
    if (!ext) throw new HttpError(400, "Chỉ hỗ trợ ảnh JPG, PNG, WEBP hoặc GIF.");
    if (file.size > MAX) throw new HttpError(400, "Ảnh tối đa 4MB.");
    const key = `${crypto.randomUUID()}.${ext}`;
    await store.set(key, await file.arrayBuffer(), { metadata: { contentType: file.type } });
    return json({ url: `/api/images/${key}` }, 201);
  }

  throw new HttpError(404, "Không tìm thấy.");
});

export const config: Config = { path: ["/api/images", "/api/images/:key"] };
