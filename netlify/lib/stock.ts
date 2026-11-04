import { eq, sql } from "drizzle-orm";
import { products, orderDetails, inventoryLogs } from "../../db/index.js";

/** Hoàn trả tồn kho cho toàn bộ sản phẩm trong đơn khi đơn bị hủy. */
export async function restockOrder(tx: any, orderId: number, userId: number) {
  const items = await tx.select().from(orderDetails).where(eq(orderDetails.orderId, orderId));
  for (const it of items) {
    await tx.update(products).set({ stock: sql`${products.stock} + ${it.quantity}` }).where(eq(products.id, it.productId));
    await tx.insert(inventoryLogs).values({
      productId: it.productId, userId, quantityChange: it.quantity, reason: `Hoàn kho - Hủy đơn hàng #${orderId}`,
    });
  }
}

