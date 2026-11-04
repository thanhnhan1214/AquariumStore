import crypto from "node:crypto";
import { and, eq, gt } from "drizzle-orm";
import { db, sessions, users, roles } from "../../db/index.js";

export class HttpError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

export const json = (data: unknown, status = 200, headers: Record<string, string> = {}) =>
  Response.json(data, { status, headers: { "cache-control": "no-store", ...headers } });

/** Bọc handler: bắt mọi lỗi và trả về JSON thân thiện thay vì làm sập function. */
export const handle =
  (fn: (req: Request, ctx: any) => Promise<Response>) =>
  async (req: Request, ctx: any) => {
    try {
      return await fn(req, ctx);
    } catch (err) {
      if (err instanceof HttpError) return json({ error: err.message }, err.status);
      console.error(err);
      return json({ error: "Đã có lỗi máy chủ, vui lòng thử lại sau." }, 500);
    }
  };

export async function readJson<T = any>(req: Request): Promise<T> {
  try {
    return (await req.json()) as T;
  } catch {
    throw new HttpError(400, "Dữ liệu gửi lên không hợp lệ.");
  }
}

export const str = (v: unknown, max = 500) => (typeof v === "string" ? v.trim().slice(0, max) : "");
export const int = (v: unknown) => {
  const n = Number(v);
  return Number.isInteger(n) ? n : NaN;
};

// ---------- Mật khẩu ----------
export function hashPassword(password: string) {
  const salt = crypto.randomBytes(16).toString("hex");
  const hash = crypto.scryptSync(password, salt, 64).toString("hex");
  return `scrypt$${salt}$${hash}`;
}

export function verifyPassword(password: string, stored: string) {
  const [algo, salt, hash] = stored.split("$");
  if (algo !== "scrypt" || !salt || !hash) return false;
  const test = crypto.scryptSync(password, salt, 64);
  const expected = Buffer.from(hash, "hex");
  return expected.length === test.length && crypto.timingSafeEqual(expected, test);
}

// ---------- Phiên đăng nhập ----------
const COOKIE = "aq_session";
const SESSION_DAYS = 7;

export async function createSession(userId: number) {
  const token = crypto.randomBytes(32).toString("hex");
  const expiresAt = new Date(Date.now() + SESSION_DAYS * 86400_000);
  await db.insert(sessions).values({ token, userId, expiresAt });
  return `${COOKIE}=${token}; Path=/; HttpOnly; Secure; SameSite=Lax; Max-Age=${SESSION_DAYS * 86400}`;
}

export const clearCookie = `${COOKIE}=; Path=/; HttpOnly; Secure; SameSite=Lax; Max-Age=0`;

function getToken(req: Request) {
  const cookie = req.headers.get("cookie") || "";
  const m = cookie.match(new RegExp(`(?:^|;\\s*)${COOKIE}=([a-f0-9]{64})`));
  return m?.[1] ?? null;
}

export type SessionUser = {
  id: number;
  username: string;
  fullName: string;
  email: string;
  phone: string | null;
  address: string | null;
  role: string;
};

export async function currentUser(req: Request): Promise<SessionUser | null> {
  const token = getToken(req);
  if (!token) return null;
  const rows = await db
    .select({
      id: users.id,
      username: users.username,
      fullName: users.fullName,
      email: users.email,
      phone: users.phone,
      address: users.address,
      role: roles.name,
      isActive: users.isActive,
    })
    .from(sessions)
    .innerJoin(users, eq(users.id, sessions.userId))
    .innerJoin(roles, eq(roles.id, users.roleId))
    .where(and(eq(sessions.token, token), gt(sessions.expiresAt, new Date())))
    .limit(1);
  const u = rows[0];
  if (!u || !u.isActive) return null;
  const { isActive, ...rest } = u;
  return rest;
}

export async function destroySession(req: Request) {
  const token = getToken(req);
  if (token) await db.delete(sessions).where(eq(sessions.token, token));
}

export async function requireUser(req: Request, allowed?: string[]) {
  const user = await currentUser(req);
  if (!user) throw new HttpError(401, "Vui lòng đăng nhập để tiếp tục.");
  if (allowed && !allowed.includes(user.role)) throw new HttpError(403, "Bạn không có quyền thực hiện thao tác này.");
  return user;
}

export const STAFF = ["Admin", "Staff"];
export const ORDER_STATUSES = ["Chờ xử lý", "Đã xác nhận", "Đang giao", "Hoàn thành", "Đã hủy"];
