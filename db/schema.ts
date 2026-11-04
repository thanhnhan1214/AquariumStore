import { pgTable, serial, text, integer, boolean, timestamp, numeric, index } from "drizzle-orm/pg-core";

export const roles = pgTable("roles", {
  id: serial().primaryKey(),
  name: text().notNull().unique(),
});

export const users = pgTable("users", {
  id: serial().primaryKey(),
  roleId: integer("role_id").notNull().references(() => roles.id),
  username: text().notNull().unique(),
  passwordHash: text("password_hash").notNull(),
  fullName: text("full_name").notNull(),
  email: text().notNull().unique(),
  phone: text(),
  address: text(),
  isActive: boolean("is_active").notNull().default(true),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const sessions = pgTable("sessions", {
  token: text().primaryKey(),
  userId: integer("user_id").notNull().references(() => users.id, { onDelete: "cascade" }),
  expiresAt: timestamp("expires_at", { withTimezone: true }).notNull(),
});

export const categories = pgTable("categories", {
  id: serial().primaryKey(),
  name: text().notNull().unique(),
  description: text(),
  isActive: boolean("is_active").notNull().default(true),
});

export const products = pgTable("products", {
  id: serial().primaryKey(),
  categoryId: integer("category_id").notNull().references(() => categories.id),
  name: text().notNull(),
  description: text(),
  price: numeric({ precision: 18, scale: 2, mode: "number" }).notNull(),
  stock: integer().notNull().default(0),
  imageUrl: text("image_url"),
  isActive: boolean("is_active").notNull().default(true),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
}, (t) => [index("products_category_idx").on(t.categoryId)]);

export const orders = pgTable("orders", {
  id: serial().primaryKey(),
  userId: integer("user_id").notNull().references(() => users.id),
  orderDate: timestamp("order_date", { withTimezone: true }).notNull().defaultNow(),
  totalAmount: numeric("total_amount", { precision: 18, scale: 2, mode: "number" }).notNull(),
  status: text().notNull().default("Chờ xử lý"),
  recipientName: text("recipient_name"),
  shippingAddress: text("shipping_address").notNull(),
  recipientPhone: text("recipient_phone").notNull(),
  paymentMethod: text("payment_method").notNull(),
  note: text(),
}, (t) => [index("orders_user_idx").on(t.userId)]);

export const orderDetails = pgTable("order_details", {
  id: serial().primaryKey(),
  orderId: integer("order_id").notNull().references(() => orders.id, { onDelete: "cascade" }),
  productId: integer("product_id").notNull().references(() => products.id),
  quantity: integer().notNull(),
  unitPrice: numeric("unit_price", { precision: 18, scale: 2, mode: "number" }).notNull(),
}, (t) => [index("order_details_order_idx").on(t.orderId)]);

export const inventoryLogs = pgTable("inventory_logs", {
  id: serial().primaryKey(),
  productId: integer("product_id").notNull().references(() => products.id),
  userId: integer("user_id").notNull().references(() => users.id),
  quantityChange: integer("quantity_change").notNull(),
  reason: text().notNull(),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});
