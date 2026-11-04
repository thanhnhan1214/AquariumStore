import type { Config } from "@netlify/functions";
import { and, desc, eq, inArray, sql } from "drizzle-orm";
import { db, orders, orderDetails, products, inventoryLogs } from "../../db/index.js";
import { handle, json, readJson, requireUser, str, int, HttpError, STAFF } from "../lib/http.js";
import { restockOrder } from "../lib/stock.js";

const PAYMENTS = ["COD", "Chuyển khoản"];

export default handle(async (req, ctx) => {
  const me = await requireUser(req);
  const [rawId, action] = new URL(req.url).pathname.replace(/^\/api\/orders\/?/, "").split("/").filter(Boolean);
  const id = rawId ? int(rawId) : null;

  // Danh sách đơn của tôi
  if (id === null && req.method === "GET") {
    const list = await db.select({
      id: orders.id, orderDate: orders.orderDate, totalAmount: orders.totalAmount, status: orders.status,
      paymentMethod: orders.paymentMethod,
      itemCount: sql<number>`(select coalesce(sum(od.quantity),0) from order_details od where od.order_id = "orders"."id")::int`,
    }).from(orders).where(eq(orders.userId, me.id)).orderBy(desc(orders.orderDate));
    return json({ orders: list });
  }

  // Đặt hàng
  if (id === null && req.method === "POST") {
    const b = await readJson(req);
    const recipientName = str(b.recipientName, 150);
    const shippingAddress = str(b.shippingAddress, 500);
    const recipientPhone = str(b.recipientPhone, 20);
    const paymentMethod = str(b.paymentMethod, 50);
    const note = str(b.note, 500);
    if (!recipientName) throw new HttpError(400, "Vui lòng nhập tên người nhận.");
    if (shippingAddress.length < 5) throw new HttpError(400, "Vui lòng nhập địa chỉ giao hàng đầy đủ.");
    if (!/^0\d{9,10}$/.test(recipientPhone)) throw new HttpError(400, "Số điện thoại người nhận không hợp lệ.");
    if (!PAYMENTS.includes(paymentMethod)) throw new HttpError(400, "Phương thức thanh toán không hợp lệ.");

    // Gộp các dòng trùng sản phẩm, kiểm tra số lượng
    const qty = new Map<number, number>();
    for (const it of Array.isArray(b.items) ? b.items : []) {
      const pid = int(it?.productId), q = int(it?.quantity);
      if (Number.isNaN(pid) || Number.isNaN(q) || q < 1 || q > 999) throw new HttpError(400, "Giỏ hàng có sản phẩm không hợp lệ.");
      qty.set(pid, (qty.get(pid) ?? 0) + q);
    }
    if (!qty.size) throw new HttpError(400, "Giỏ hàng đang trống.");

    const orderId = await db.transaction(async (tx) => {
      const rows = await tx.select().from(products).where(inArray(products.id, [...qty.keys()]));
      let total = 0;
      for (const [pid, q] of qty) {
        const p = rows.find((r) => r.id === pid);
        if (!p || !p.isActive) throw new HttpError(400, "Có sản phẩm trong giỏ đã ngừng bán. Vui lòng cập nhật giỏ hàng.");
        if (p.stock < q) throw new HttpError(409, `"${p.name}" chỉ còn ${p.stock} sản phẩm trong kho.`);
        total += Number(p.price) * q;
      }
      const [o] = await tx.insert(orders).values({
        userId: me.id, totalAmount: total, recipientName, shippingAddress, recipientPhone, paymentMethod, note: note || null,
      }).returning({ id: orders.id });
      for (const [pid, q] of qty) {
        const p = rows.find((r) => r.id === pid)!;
        // Trừ kho có điều kiện để chống bán vượt tồn khi nhiều người đặt cùng lúc
        const upd = await tx.update(products).set({ stock: sql`${products.stock} - ${q}` })
          .where(and(eq(products.id, pid), sql`${products.stock} >= ${q}`)).returning({ id: products.id });
        if (!upd.length) throw new HttpError(409, `"${p.name}" vừa hết hàng, vui lòng giảm số lượng.`);
        await tx.insert(orderDetails).values({ orderId: o.id, productId: pid, quantity: q, unitPrice: Number(p.price) });
        await tx.insert(inventoryLogs).values({ productId: pid, userId: me.id, quantityChange: -q, reason: `Xuất kho - Đơn hàng #${o.id}` });
      }
      return o.id;
    });
    return json({ orderId }, 201);
  }

  if (id === null || Number.isNaN(id)) throw new HttpError(404, "Không tìm thấy đơn hàng.");
  const [order] = await db.select().from(orders).where(eq(orders.id, id));
  if (!order || (order.userId !== me.id && !STAFF.includes(me.role))) throw new HttpError(404, "Không tìm thấy đơn hàng.");

  if (!action && req.method === "GET") {
    const items = await db.select({
      productId: orderDetails.productId, quantity: orderDetails.quantity, unitPrice: orderDetails.unitPrice,
      name: products.name, imageUrl: products.imageUrl,
    }).from(orderDetails).innerJoin(products, eq(products.id, orderDetails.productId))
      .where(eq(orderDetails.orderId, id));
    return json({ order, items });
  }

  if (action === "cancel" && req.method === "POST") {
    if (order.userId !== me.id) throw new HttpError(403, "Bạn chỉ có thể hủy đơn hàng của mình.");
    await db.transaction(async (tx) => {
      const upd = await tx.update(orders).set({ status: "Đã hủy" })
        .where(and(eq(orders.id, id), eq(orders.status, "Chờ xử lý"))).returning({ id: orders.id });
      if (!upd.length) throw new HttpError(409, "Chỉ có thể hủy đơn hàng đang ở trạng thái Chờ xử lý.");
      await restockOrder(tx, id, me.id);
    });
    return json({ ok: true });
  }

  throw new HttpError(404, "Không tìm thấy.");
});

export const config: Config = { path: ["/api/orders", "/api/orders/:id", "/api/orders/:id/:action"] };
