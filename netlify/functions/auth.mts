import type { Config } from "@netlify/functions";
import { eq, or, and, ne } from "drizzle-orm";
import { db, users, roles } from "../../db/index.js";
import {
  handle, json, readJson, str, HttpError, hashPassword, verifyPassword,
  createSession, clearCookie, currentUser, destroySession, requireUser,
} from "../lib/http.js";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PHONE_RE = /^0\d{9,10}$/;

export default handle(async (req, ctx) => {
  const action = ctx.params.action as string;
  const method = req.method;

  if (action === "me" && method === "GET") {
    return json({ user: await currentUser(req) });
  }

  if (action === "login" && method === "POST") {
    const body = await readJson(req);
    const login = str(body.username, 200).toLowerCase();
    const password = typeof body.password === "string" ? body.password : "";
    if (!login || !password) throw new HttpError(400, "Vui lòng nhập tên đăng nhập và mật khẩu.");
    const [u] = await db.select().from(users)
      .where(or(eq(users.username, login), eq(users.email, login))).limit(1);
    if (!u || !verifyPassword(password, u.passwordHash))
      throw new HttpError(401, "Tên đăng nhập hoặc mật khẩu không đúng.");
    if (!u.isActive) throw new HttpError(403, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ cửa hàng.");
    const cookie = await createSession(u.id);
    return json({ user: await userView(u.id) }, 200, { "set-cookie": cookie });
  }

  if (action === "register" && method === "POST") {
    const b = await readJson(req);
    const username = str(b.username, 100).toLowerCase();
    const email = str(b.email, 200).toLowerCase();
    const fullName = str(b.fullName, 150);
    const phone = str(b.phone, 20);
    const address = str(b.address, 500);
    const password = typeof b.password === "string" ? b.password : "";
    if (!/^[a-z0-9_.]{3,50}$/.test(username))
      throw new HttpError(400, "Tên đăng nhập 3-50 ký tự, chỉ gồm chữ thường không dấu, số, dấu _ hoặc .");
    if (!fullName) throw new HttpError(400, "Vui lòng nhập họ tên.");
    if (!EMAIL_RE.test(email)) throw new HttpError(400, "Email không hợp lệ.");
    if (phone && !PHONE_RE.test(phone)) throw new HttpError(400, "Số điện thoại không hợp lệ.");
    if (password.length < 6) throw new HttpError(400, "Mật khẩu phải có ít nhất 6 ký tự.");
    if (password !== b.confirmPassword) throw new HttpError(400, "Mật khẩu xác nhận không khớp.");
    const exists = await db.select({ id: users.id, username: users.username }).from(users)
      .where(or(eq(users.username, username), eq(users.email, email))).limit(1);
    if (exists.length) throw new HttpError(409, exists[0].username === username ? "Tên đăng nhập đã tồn tại." : "Email đã được sử dụng.");
    const [role] = await db.select().from(roles).where(eq(roles.name, "Customer")).limit(1);
    const [u] = await db.insert(users).values({
      roleId: role.id, username, email, fullName, phone: phone || null, address: address || null,
      passwordHash: hashPassword(password),
    }).returning({ id: users.id });
    const cookie = await createSession(u.id);
    return json({ user: await userView(u.id) }, 201, { "set-cookie": cookie });
  }

  if (action === "logout" && method === "POST") {
    await destroySession(req);
    return json({ ok: true }, 200, { "set-cookie": clearCookie });
  }

  if (action === "profile" && method === "PUT") {
    const me = await requireUser(req);
    const b = await readJson(req);
    const fullName = str(b.fullName, 150);
    const email = str(b.email, 200).toLowerCase();
    const phone = str(b.phone, 20);
    const address = str(b.address, 500);
    if (!fullName) throw new HttpError(400, "Vui lòng nhập họ tên.");
    if (!EMAIL_RE.test(email)) throw new HttpError(400, "Email không hợp lệ.");
    if (phone && !PHONE_RE.test(phone)) throw new HttpError(400, "Số điện thoại không hợp lệ.");
    const dup = await db.select({ id: users.id }).from(users)
      .where(and(eq(users.email, email), ne(users.id, me.id))).limit(1);
    if (dup.length) throw new HttpError(409, "Email đã được tài khoản khác sử dụng.");
    const update: Record<string, unknown> = { fullName, email, phone: phone || null, address: address || null };
    if (b.newPassword) {
      const [u] = await db.select().from(users).where(eq(users.id, me.id));
      if (!verifyPassword(String(b.currentPassword || ""), u.passwordHash))
        throw new HttpError(400, "Mật khẩu hiện tại không đúng.");
      if (String(b.newPassword).length < 6) throw new HttpError(400, "Mật khẩu mới phải có ít nhất 6 ký tự.");
      update.passwordHash = hashPassword(String(b.newPassword));
    }
    await db.update(users).set(update).where(eq(users.id, me.id));
    return json({ user: await userView(me.id) });
  }

  throw new HttpError(404, "Không tìm thấy.");
});

async function userView(id: number) {
  const [u] = await db.select({
    id: users.id, username: users.username, fullName: users.fullName, email: users.email,
    phone: users.phone, address: users.address, role: roles.name,
  }).from(users).innerJoin(roles, eq(roles.id, users.roleId)).where(eq(users.id, id));
  return u;
}

export const config: Config = { path: "/api/auth/:action" };
