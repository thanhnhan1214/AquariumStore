import type { Config } from "@netlify/functions";
import { and, asc, desc, eq, sql } from "drizzle-orm";
import { db, products, categories, orderDetails, orders } from "../../db/index.js";
import { handle, json, HttpError, int } from "../lib/http.js";

const productCols = {
  id: products.id, name: products.name, description: products.description, price: products.price,
  stock: products.stock, imageUrl: products.imageUrl, categoryId: products.categoryId,
  categoryName: categories.name, createdAt: products.createdAt,
};

export default handle(async (req, ctx) => {
  const id = ctx.params.id ? int(ctx.params.id) : null;

  if (id !== null) {
    if (Number.isNaN(id)) throw new HttpError(404, "Không tìm thấy sản phẩm.");
    const [p] = await db.select(productCols).from(products)
      .innerJoin(categories, eq(categories.id, products.categoryId))
      .where(and(eq(products.id, id), eq(products.isActive, true), eq(categories.isActive, true)));
    if (!p) throw new HttpError(404, "Không tìm thấy sản phẩm hoặc sản phẩm đã ngừng bán.");
    const related = await db.select(productCols).from(products)
      .innerJoin(categories, eq(categories.id, products.categoryId))
      .where(and(eq(products.categoryId, p.categoryId), eq(products.isActive, true), sql`${products.id} <> ${id}`))
      .orderBy(desc(products.createdAt)).limit(4);
    return json({ product: p, related });
  }

  const cats = await db.select({ id: categories.id, name: categories.name, description: categories.description })
    .from(categories).where(eq(categories.isActive, true)).orderBy(asc(categories.id));
  const list = await db.select({
    ...productCols,
    sold: sql<number>`coalesce((select sum(od.quantity) from ${orderDetails} od join ${orders} o on o.id = od.order_id where od.product_id = ${products.id} and o.status <> 'Đã hủy'), 0)::int`,
  }).from(products)
    .innerJoin(categories, eq(categories.id, products.categoryId))
    .where(and(eq(products.isActive, true), eq(categories.isActive, true)))
    .orderBy(desc(products.createdAt), desc(products.id));
  return json({ categories: cats, products: list });
});

export const config: Config = { path: ["/api/catalog", "/api/catalog/:id"], method: "GET" };
