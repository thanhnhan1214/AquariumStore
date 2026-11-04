import type { Config } from "@netlify/functions";
import { and, asc, desc, eq, ilike, ne, sql } from "drizzle-orm";
import { db, products, categories, orders, orderDetails, users, roles, inventoryLogs, sessions } from "../../db/index.js";
import { handle, json, readJson, requireUser, str, int, HttpError, STAFF, ORDER_STATUSES } from "../lib/http.js";
import { restockOrder } from "../lib/stock.js";

export default handle(async (req, ctx) => {
  const me = await requireUser(req, STAFF);
  const url = new URL(req.url);
  const [resource, rawId, action] = url.pathname.replace(/^\/api\/admin\/?/, "").split("/").filter(Boolean);
  const id = rawId ? int(rawId) : null;
  if (id !== null && Number.isNaN(id)) throw new HttpError(404, "Không tìm thấy.");
  const m = req.method;

  switch (resource) {
    // ================= DASHBOARD =================
    case "stats": {
      const [revenue] = await db.select({
        total: sql<number>`coalesce(sum(${orders.totalAmount}) filter (where ${orders.status} = 'Hoàn thành'), 0)::float`,
        orderCount: sql<number>`count(*)::int`,
        pending: sql<number>`(count(*) filter (where ${orders.status} = 'Chờ xử lý'))::int`,
      }).from(orders);
      const [{ customers }] = await db.select({ customers: sql<number>`count(*)::int` })
        .from(users).innerJoin(roles, eq(roles.id, users.roleId)).where(eq(roles.name, "Customer"));
      const [{ productCount }] = await db.select({ productCount: sql<number>`count(*)::int` }).from(products).where(eq(products.isActive, true));
      const monthly = await db.execute(sql`
        select to_char(m, 'MM/YYYY') as label,
          coalesce((select sum(total_amount) from orders where status = 'Hoàn thành'
            and date_trunc('month', order_date) = m), 0)::float as revenue,
          (select count(*) from orders where status <> 'Đã hủy' and date_trunc('month', order_date) = m)::int as orders
        from generate_series(date_trunc('month', now()) - interval '5 months', date_trunc('month', now()), interval '1 month') m
        order by m`);
      const byStatus = await db.select({ status: orders.status, count: sql<number>`count(*)::int` }).from(orders).groupBy(orders.status);
      const top = await db.select({
        id: products.id, name: products.name, imageUrl: products.imageUrl,
        sold: sql<number>`sum(${orderDetails.quantity})::int`,
        revenue: sql<number>`sum(${orderDetails.quantity} * ${orderDetails.unitPrice})::float`,
      }).from(orderDetails).innerJoin(orders, eq(orders.id, orderDetails.orderId))
        .innerJoin(products, eq(products.id, orderDetails.productId))
        .where(ne(orders.status, "Đã hủy")).groupBy(products.id).orderBy(desc(sql`sum(${orderDetails.quantity})`)).limit(5);
      const lowStock = await db.select({ id: products.id, name: products.name, stock: products.stock })
        .from(products).where(and(eq(products.isActive, true), sql`${products.stock} <= 10`)).orderBy(asc(products.stock));
      const recent = await db.select({ id: orders.id, orderDate: orders.orderDate, totalAmount: orders.totalAmount, status: orders.status, customer: users.fullName })
        .from(orders).innerJoin(users, eq(users.id, orders.userId)).orderBy(desc(orders.orderDate)).limit(6);
      const rows = (monthly as any).rows ?? monthly;
      return json({ revenue: revenue.total, orderCount: revenue.orderCount, pending: revenue.pending, customers, productCount, monthly: rows, byStatus, top, lowStock, recent });
    }

    // ================= CATEGORIES =================
    case "categories": {
      if (m === "GET") {
        const list = await db.select({
          id: categories.id, name: categories.name, description: categories.description, isActive: categories.isActive,
          productCount: sql<number>`(select count(*) from products p where p.category_id = "categories"."id")::int`,
        }).from(categories).orderBy(asc(categories.id));
        return json({ categories: list });
      }
      if (m === "POST" || m === "PUT") {
        const b = await readJson(req);
        const name = str(b.name, 200), description = str(b.description, 500);
        if (!name) throw new HttpError(400, "Tên danh mục không được để trống.");
        const dup = await db.select({ id: categories.id }).from(categories)
          .where(and(sql`lower(${categories.name}) = lower(${name})`, id ? ne(categories.id, id) : sql`true`)).limit(1);
        if (dup.length) throw new HttpError(409, "Tên danh mục đã tồn tại.");
        const values = { name, description: description || null, isActive: b.isActive !== false };
        if (m === "POST") {
          const [c] = await db.insert(categories).values(values).returning();
          return json({ category: c }, 201);
        }
        if (!id) throw new HttpError(400, "Thiếu mã danh mục.");
        const [c] = await db.update(categories).set(values).where(eq(categories.id, id)).returning();
        if (!c) throw new HttpError(404, "Không tìm thấy danh mục.");
        return json({ category: c });
      }
      if (m === "DELETE" && id) {
        const [{ n }] = await db.select({ n: sql<number>`count(*)::int` }).from(products).where(eq(products.categoryId, id));
        if (n > 0) throw new HttpError(409, `Không thể xóa: danh mục đang có ${n} sản phẩm. Hãy chuyển sản phẩm sang danh mục khác hoặc tạm ẩn danh mục.`);
        await db.delete(categories).where(eq(categories.id, id));
        return json({ ok: true });
      }
      break;
    }

    // ================= PRODUCTS =================
    case "products": {
      if (m === "GET") {
        const list = await db.select({
          id: products.id, name: products.name, description: products.description, price: products.price, stock: products.stock,
          imageUrl: products.imageUrl, isActive: products.isActive, categoryId: products.categoryId, categoryName: categories.name,
          createdAt: products.createdAt,
        }).from(products).innerJoin(categories, eq(categories.id, products.categoryId)).orderBy(desc(products.id));
        return json({ products: list });
      }
      if (m === "POST" || m === "PUT") {
        const b = await readJson(req);
        const name = str(b.name, 300), description = str(b.description, 2000), imageUrl = str(b.imageUrl, 500);
        const price = Number(b.price), categoryId = int(b.categoryId);
        if (!name) throw new HttpError(400, "Tên sản phẩm không được để trống.");
        if (!Number.isFinite(price) || price < 0 || price > 1e12) throw new HttpError(400, "Giá bán không hợp lệ.");
        const [cat] = Number.isNaN(categoryId) ? [] : await db.select().from(categories).where(eq(categories.id, categoryId));
        if (!cat) throw new HttpError(400, "Vui lòng chọn danh mục hợp lệ.");
        const values: any = { name, description: description || null, price: Math.round(price), categoryId, imageUrl: imageUrl || null, isActive: b.isActive !== false };
        if (m === "POST") {
          const stock = int(b.stock ?? 0);
          if (Number.isNaN(stock) || stock < 0) throw new HttpError(400, "Số lượng tồn kho ban đầu không hợp lệ.");
          const p = await db.transaction(async (tx) => {
            const [p] = await tx.insert(products).values({ ...values, stock }).returning();
            if (stock > 0) await tx.insert(inventoryLogs).values({ productId: p.id, userId: me.id, quantityChange: stock, reason: "Nhập kho ban đầu khi tạo sản phẩm" });
            return p;
          });
          return json({ product: p }, 201);
        }
        if (!id) throw new HttpError(400, "Thiếu mã sản phẩm.");
        const [p] = await db.update(products).set(values).where(eq(products.id, id)).returning();
        if (!p) throw new HttpError(404, "Không tìm thấy sản phẩm.");
        return json({ product: p });
      }
      if (m === "DELETE" && id) {
        // Sản phẩm đã có đơn hàng / nhật ký kho → ngừng bán (xóa mềm) để giữ lịch sử
        const [{ n }] = await db.select({ n: sql<number>`count(*)::int` }).from(orderDetails).where(eq(orderDetails.productId, id));
        const [{ l }] = await db.select({ l: sql<number>`count(*)::int` }).from(inventoryLogs).where(eq(inventoryLogs.productId, id));
        if (n > 0 || l > 0) {
          await db.update(products).set({ isActive: false }).where(eq(products.id, id));
          return json({ ok: true, softDeleted: true });
        }
        await db.delete(products).where(eq(products.id, id));
        return json({ ok: true });
      }
      break;
    }

    // ================= ORDERS =================
    case "orders": {
      if (m === "GET" && !id) {
        const status = url.searchParams.get("status");
        const list = await db.select({
          id: orders.id, orderDate: orders.orderDate, totalAmount: orders.totalAmount, status: orders.status,
          paymentMethod: orders.paymentMethod, customer: users.fullName, recipientPhone: orders.recipientPhone,
        }).from(orders).innerJoin(users, eq(users.id, orders.userId))
          .where(status && ORDER_STATUSES.includes(status) ? eq(orders.status, status) : undefined)
          .orderBy(desc(orders.orderDate));
        return json({ orders: list, statuses: ORDER_STATUSES });
      }
      if (m === "GET" && id) {
        const [order] = await db.select({
          id: orders.id, orderDate: orders.orderDate, totalAmount: orders.totalAmount, status: orders.status,
          paymentMethod: orders.paymentMethod, recipientName: orders.recipientName, recipientPhone: orders.recipientPhone,
          shippingAddress: orders.shippingAddress, note: orders.note, customer: users.fullName, email: users.email,
        }).from(orders).innerJoin(users, eq(users.id, orders.userId)).where(eq(orders.id, id));
        if (!order) throw new HttpError(404, "Không tìm thấy đơn hàng.");
        const items = await db.select({
          productId: orderDetails.productId, quantity: orderDetails.quantity, unitPrice: orderDetails.unitPrice,
          name: products.name, imageUrl: products.imageUrl,
        }).from(orderDetails).innerJoin(products, eq(products.id, orderDetails.productId)).where(eq(orderDetails.orderId, id));
        return json({ order, items, statuses: ORDER_STATUSES });
      }
      if (m === "PUT" && id && action === "status") {
        const b = await readJson(req);
        const status = str(b.status, 50);
        if (!ORDER_STATUSES.includes(status)) throw new HttpError(400, "Trạng thái không hợp lệ.");
        await db.transaction(async (tx) => {
          const [o] = await tx.select().from(orders).where(eq(orders.id, id)).for("update");
          if (!o) throw new HttpError(404, "Không tìm thấy đơn hàng.");
          if (o.status === status) return;
          if (o.status === "Đã hủy") throw new HttpError(409, "Đơn hàng đã hủy không thể đổi trạng thái.");
          if (o.status === "Hoàn thành") throw new HttpError(409, "Đơn hàng đã hoàn thành không thể đổi trạng thái.");
          await tx.update(orders).set({ status }).where(eq(orders.id, id));
          if (status === "Đã hủy") await restockOrder(tx, id, me.id);
        });
        return json({ ok: true });
      }
      break;
    }

    // ================= INVENTORY =================
    case "inventory": {
      if (m === "GET") {
        const logs = await db.select({
          id: inventoryLogs.id, quantityChange: inventoryLogs.quantityChange, reason: inventoryLogs.reason,
          createdAt: inventoryLogs.createdAt, productName: products.name, userName: users.fullName,
        }).from(inventoryLogs).innerJoin(products, eq(products.id, inventoryLogs.productId))
          .innerJoin(users, eq(users.id, inventoryLogs.userId)).orderBy(desc(inventoryLogs.createdAt), desc(inventoryLogs.id)).limit(300);
        return json({ logs });
      }
      if (m === "POST") {
        const b = await readJson(req);
        const productId = int(b.productId), quantity = int(b.quantity);
        const reason = str(b.reason, 400) || "Nhập hàng từ nhà cung cấp";
        if (Number.isNaN(productId)) throw new HttpError(400, "Vui lòng chọn sản phẩm.");
        if (Number.isNaN(quantity) || quantity < 1 || quantity > 100000) throw new HttpError(400, "Số lượng nhập phải từ 1 đến 100.000.");
        const p = await db.transaction(async (tx) => {
          const [p] = await tx.update(products).set({ stock: sql`${products.stock} + ${quantity}` }).where(eq(products.id, productId)).returning();
          if (!p) throw new HttpError(404, "Không tìm thấy sản phẩm.");
          await tx.insert(inventoryLogs).values({ productId, userId: me.id, quantityChange: quantity, reason: `Nhập kho - ${reason}` });
          return p;
        });
        return json({ product: p }, 201);
      }
      break;
    }

    // ================= USERS (chỉ Admin) =================
    case "users": {
      if (me.role !== "Admin") throw new HttpError(403, "Chỉ Quản trị viên mới được quản lý người dùng.");
      if (m === "GET") {
        const q = str(url.searchParams.get("q"), 100);
        const list = await db.select({
          id: users.id, username: users.username, fullName: users.fullName, email: users.email, phone: users.phone,
          role: roles.name, isActive: users.isActive, createdAt: users.createdAt,
          orderCount: sql<number>`(select count(*) from orders o where o.user_id = "users"."id")::int`,
        }).from(users).innerJoin(roles, eq(roles.id, users.roleId))
          .where(q ? sql`(${ilike(users.fullName, `%${q}%`)} or ${ilike(users.username, `%${q}%`)} or ${ilike(users.email, `%${q}%`)})` : undefined)
          .orderBy(asc(users.id));
        return json({ users: list, roles: ["Admin", "Staff", "Customer"] });
      }
      if (m === "PUT" && id) {
        if (id === me.id) throw new HttpError(400, "Bạn không thể tự thay đổi quyền hoặc khóa tài khoản của chính mình.");
        const b = await readJson(req);
        const update: any = {};
        if (typeof b.isActive === "boolean") update.isActive = b.isActive;
        if (b.role) {
          const [r] = await db.select().from(roles).where(eq(roles.name, str(b.role, 50)));
          if (!r) throw new HttpError(400, "Vai trò không hợp lệ.");
          update.roleId = r.id;
        }
        if (!Object.keys(update).length) throw new HttpError(400, "Không có thay đổi.");
        const [u] = await db.update(users).set(update).where(eq(users.id, id)).returning({ id: users.id });
        if (!u) throw new HttpError(404, "Không tìm thấy người dùng.");
        if (update.isActive === false) await db.delete(sessions).where(eq(sessions.userId, id));
        return json({ ok: true });
      }
      break;
    }
  }
  throw new HttpError(404, "Không tìm thấy.");
});

export const config: Config = { path: "/api/admin/*" };
