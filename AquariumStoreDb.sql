/* =========================================================================================
   BÁO CÁO ĐỒ ÁN 2: HỆ THỐNG CỬA HÀNG CÁ CẢNH & THỦY SINH (AQUARIUM STORE)
   Tập tin kịch bản Cơ sở dữ liệu: AquariumStoreDb.sql
   Hệ quản trị CSDL: Microsoft SQL Server (Tương thích SQL Server 2019/2022 & SQL LocalDB)
   Kiến trúc ORM: ASP.NET Core 10.0 MVC - Entity Framework Core 10.0.13
   Mô tả: Kịch bản khởi tạo toàn bộ 7 bảng thực thể, khóa chính, khóa ngoại, chỉ mục (Index),
          dữ liệu mẫu (Seed Data) và các câu truy vấn kiểm tra phục vụ chấm điểm Đồ Án 2.
========================================================================================= */

USE [master];
GO

-- 1. KHỞI TẠO CƠ SỞ DỮ LIỆU [AquariumStoreDb] NẾU CHƯA TỒN TẠI
IF DB_ID(N'AquariumStoreDb') IS NULL
BEGIN
    CREATE DATABASE [AquariumStoreDb];
END
GO

USE [AquariumStoreDb];
GO

-- =========================================================================================
-- 2. XÓA CÁC BẢNG CŨ THEO THỨ TỰ KHÓA NGOẠI (ĐỂ CÓ THỂ CHẠY LẠI SCRIPT NHIỀU LẦN AN TOÀN)
-- =========================================================================================
IF OBJECT_ID(N'[dbo].[InventoryLogs]', N'U') IS NOT NULL DROP TABLE [dbo].[InventoryLogs];
IF OBJECT_ID(N'[dbo].[OrderDetails]', N'U') IS NOT NULL DROP TABLE [dbo].[OrderDetails];
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL DROP TABLE [dbo].[Orders];
IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL DROP TABLE [dbo].[Products];
IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL DROP TABLE [dbo].[Users];
IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NOT NULL DROP TABLE [dbo].[Categories];
IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NOT NULL DROP TABLE [dbo].[Roles];
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL DROP TABLE [dbo].[__EFMigrationsHistory];
GO

-- =========================================================================================
-- 3. BẢNG LỊCH SỬ MIGRATION CỦA ENTITY FRAMEWORK CORE
-- =========================================================================================
CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId]    NVARCHAR(150) NOT NULL, -- Mã định danh phiên bản Migration
    [ProductVersion] NVARCHAR(32)  NOT NULL, -- Phiên bản Entity Framework Core
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
GO

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261029014118_InitialCreate', N'10.0.13');
GO

-- =========================================================================================
-- 4. TẠO CẤU TRÚC 7 BẢNG CHÍNH CỦA HỆ THỐNG (DDL)
-- =========================================================================================

-- BẢNG 1: Roles (Vai trò phân quyền: Admin, Staff, Customer)
CREATE TABLE [dbo].[Roles] (
    [RoleId]   INT           IDENTITY(1,1) NOT NULL, -- Khóa chính tự tăng
    [RoleName] NVARCHAR(50)  NOT NULL,               -- Tên vai trò (Admin / Staff / Customer)
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
);
GO

-- BẢNG 2: Categories (Danh mục sản phẩm cá cảnh, thủy sinh, thiết bị)
CREATE TABLE [dbo].[Categories] (
    [CategoryId]   INT            IDENTITY(1,1) NOT NULL, -- Khóa chính tự tăng
    [CategoryName] NVARCHAR(200)  NOT NULL,               -- Tên danh mục sản phẩm (Unique)
    [Description]  NVARCHAR(500)  NULL,                   -- Mô tả chi tiết danh mục
    [IsActive]     BIT            NOT NULL DEFAULT (1),   -- Trạng thái hoạt động (1 = Hiển thị, 0 = Ẩn)
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([CategoryId] ASC)
);
GO

-- BẢNG 3: Users (Tài khoản người dùng hệ thống)
CREATE TABLE [dbo].[Users] (
    [UserId]       INT            IDENTITY(1,1) NOT NULL, -- Khóa chính tự tăng
    [RoleId]       INT            NOT NULL,               -- Khóa ngoại liên kết bảng Roles
    [Username]     NVARCHAR(100)  NOT NULL,               -- Tên đăng nhập (Unique)
    [PasswordHash] NVARCHAR(256)  NOT NULL,               -- Mật khẩu đã băm SHA256 Base64
    [FullName]     NVARCHAR(150)  NOT NULL,               -- Họ và tên đầy đủ
    [Email]        NVARCHAR(200)  NOT NULL,               -- Địa chỉ Email (Unique)
    [PhoneNumber]  NVARCHAR(20)   NULL,                   -- Số điện thoại liên hệ
    [Address]      NVARCHAR(500)  NULL,                   -- Địa chỉ giao hàng mặc định
    [CreatedAt]    DATETIME2(7)   NOT NULL,               -- Thời điểm đăng ký tài khoản
    [IsActive]     BIT            NOT NULL DEFAULT (1),   -- Trạng thái tài khoản (1 = Hoạt động, 0 = Khóa)
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE NO ACTION -- Chặn xóa Role khi còn User
);
GO

-- BẢNG 4: Products (Sản phẩm cá cảnh, cây thủy sinh, thiết bị, thức ăn, phụ kiện)
CREATE TABLE [dbo].[Products] (
    [ProductId]     INT             IDENTITY(1,1) NOT NULL, -- Khóa chính tự tăng
    [CategoryId]    INT             NOT NULL,               -- Khóa ngoại liên kết bảng Categories
    [ProductName]   NVARCHAR(300)   NOT NULL,               -- Tên sản phẩm
    [Description]   NVARCHAR(2000)  NULL,                   -- Mô tả kỹ thuật chi tiết
    [Price]         DECIMAL(18,2)   NOT NULL,               -- Giá bán lẻ (VNĐ)
    [StockQuantity] INT             NOT NULL,               -- Số lượng tồn kho thực tế
    [ImageUrl]      NVARCHAR(500)   NULL,                   -- Đường dẫn ảnh sản phẩm trong wwwroot/images/products/
    [CreatedAt]     DATETIME2(7)    NOT NULL,               -- Ngày tạo sản phẩm
    [IsActive]      BIT             NOT NULL DEFAULT (1),   -- Trạng thái kinh doanh (1 = Đang bán, 0 = Ngừng bán)
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([ProductId] ASC),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId])
        REFERENCES [dbo].[Categories] ([CategoryId]) ON DELETE NO ACTION -- Chặn xóa Category khi còn Product
);
GO

-- BẢNG 5: Orders (Đơn đặt hàng của khách)
CREATE TABLE [dbo].[Orders] (
    [OrderId]         INT            IDENTITY(1,1) NOT NULL, -- Mã đơn hàng tự tăng
    [UserId]          INT            NOT NULL,               -- Khóa ngoại liên kết người đặt hàng (Users)
    [OrderDate]       DATETIME2(7)   NOT NULL,               -- Ngày giờ đặt hàng
    [TotalAmount]     DECIMAL(18,2)  NOT NULL,               -- Tổng thành tiền của đơn hàng (VNĐ)
    [Status]          NVARCHAR(50)   NOT NULL,               -- Trạng thái: Chờ xử lý / Đã xác nhận / Đang giao / Hoàn thành / Đã hủy
    [ShippingAddress] NVARCHAR(500)  NOT NULL,               -- Địa chỉ giao hàng
    [RecipientPhone]  NVARCHAR(20)   NOT NULL,               -- Số điện thoại người nhận
    [PaymentMethod]   NVARCHAR(50)   NOT NULL,               -- Phương thức thanh toán (COD / Chuyển khoản)
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([OrderId] ASC),
    CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION -- Giữ lịch sử đơn hàng, tránh multiple cascade paths
);
GO

-- BẢNG 6: OrderDetails (Chi tiết từng mặt hàng trong đơn hàng)
CREATE TABLE [dbo].[OrderDetails] (
    [OrderDetailId] INT            IDENTITY(1,1) NOT NULL, -- Khóa chính chi tiết đơn hàng
    [OrderId]       INT            NOT NULL,               -- Khóa ngoại liên kết bảng Orders
    [ProductId]     INT            NOT NULL,               -- Khóa ngoại liên kết bảng Products
    [Quantity]      INT            NOT NULL,               -- Số lượng mua
    [UnitPrice]     DECIMAL(18,2)  NOT NULL,               -- Đơn giá cố định tại thời điểm đặt mua
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED ([OrderDetailId] ASC),
    CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId])
        REFERENCES [dbo].[Orders] ([OrderId]) ON DELETE CASCADE, -- Xóa Order sẽ tự động xóa OrderDetails đi kèm
    CONSTRAINT [FK_OrderDetails_Products_ProductId] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products] ([ProductId]) ON DELETE NO ACTION -- Chặn xóa cứng sản phẩm đã phát sinh đơn hàng
);
GO

-- BẢNG 7: InventoryLogs (Nhật ký biến động nhập kho / xuất bán / hoàn kho)
CREATE TABLE [dbo].[InventoryLogs] (
    [LogId]          INT            IDENTITY(1,1) NOT NULL, -- Mã nhật ký kho tự tăng
    [ProductId]      INT            NOT NULL,               -- Khóa ngoại liên kết sản phẩm biến động kho
    [UserId]         INT            NOT NULL,               -- Khóa ngoại liên kết người thực hiện giao dịch
    [QuantityChange] INT            NOT NULL,               -- Số lượng thay đổi (+ Nhập/Hoàn kho, - Xuất bán)
    [Reason]         NVARCHAR(500)  NOT NULL,               -- Lý do biến động kho
    [CreatedAt]      DATETIME2(7)   NOT NULL,               -- Thời điểm ghi nhận nhật ký
    CONSTRAINT [PK_InventoryLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
    CONSTRAINT [FK_InventoryLogs_Products_ProductId] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products] ([ProductId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryLogs_Users_UserId] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
);
GO

-- =========================================================================================
-- 5. TẠO CÁC CHỈ MỤC (INDEXES & UNIQUE CONSTRAINTS) TỐI ƯU TRUY VẤN
-- =========================================================================================
CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_CategoryName] ON [dbo].[Categories] ([CategoryName] ASC);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username]          ON [dbo].[Users] ([Username] ASC);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email]             ON [dbo].[Users] ([Email] ASC);

CREATE NONCLUSTERED INDEX [IX_Users_RoleId]            ON [dbo].[Users] ([RoleId] ASC);
CREATE NONCLUSTERED INDEX [IX_Products_CategoryId]     ON [dbo].[Products] ([CategoryId] ASC);
CREATE NONCLUSTERED INDEX [IX_Orders_UserId]           ON [dbo].[Orders] ([UserId] ASC);
CREATE NONCLUSTERED INDEX [IX_OrderDetails_OrderId]    ON [dbo].[OrderDetails] ([OrderId] ASC);
CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductId]  ON [dbo].[OrderDetails] ([ProductId] ASC);
CREATE NONCLUSTERED INDEX [IX_InventoryLogs_ProductId] ON [dbo].[InventoryLogs] ([ProductId] ASC);
CREATE NONCLUSTERED INDEX [IX_InventoryLogs_UserId]    ON [dbo].[InventoryLogs] ([UserId] ASC);
GO

-- =========================================================================================
-- 6. NẠP DỮ LIỆU MẪU (DATA SEEDING)
-- =========================================================================================

-- 6.1. Dữ liệu bảng Roles (3 vai trò)
SET IDENTITY_INSERT [dbo].[Roles] ON;
INSERT INTO [dbo].[Roles] ([RoleId], [RoleName]) VALUES
(1, N'Admin'),    -- Quản trị viên toàn quyền
(2, N'Staff'),    -- Nhân viên quản lý sản phẩm, đơn hàng, kho
(3, N'Customer'); -- Khách hàng mua sắm
SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO

-- 6.2. Dữ liệu bảng Users (5 tài khoản mẫu)
-- Ghi chú mật khẩu gốc trước khi băm SHA256 Base64:
--   admin       -> Admin@123    (Hash: 6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=)
--   staff       -> Staff@123    (Hash: 39SPNjOKo2Io67niBLumtOGNsLYj4lxFiQHtyDH7GOk=)
--   customer    -> Customer@123 (Hash: mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=)
--   khachhang2  -> Customer@123 (Hash: mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=)
--   khachhang3  -> Customer@123 (Hash: mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=)
SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] ([UserId], [RoleId], [Username], [PasswordHash], [FullName], [Email], [PhoneNumber], [Address], [CreatedAt], [IsActive]) VALUES
(1, 1, N'admin',      N'6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', N'Quản trị viên hệ thống',       N'admin@aquariumstore.vn',    N'0901234567', N'123 Nguyễn Huệ, Q.1, TP.HCM',               '2024-01-01T00:00:00', 1),
(2, 2, N'staff',      N'39SPNjOKo2Io67niBLumtOGNsLYj4lxFiQHtyDH7GOk=', N'Nguyễn Hoàng Nam (Nhân viên)', N'staff@aquariumstore.vn',    N'0902345678', N'45 Lê Lợi, Quận 1, TP.HCM',                 DATEADD(MONTH, -4, GETDATE()), 1),
(3, 3, N'customer',   N'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', N'Trần Minh Anh',                N'minhanh@gmail.com',         N'0918223344', N'88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM',   DATEADD(MONTH, -3, GETDATE()), 1),
(4, 3, N'khachhang2', N'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', N'Lê Quốc Bảo',                  N'quocbao.aqua@gmail.com',    N'0987654321', N'210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', DATEADD(MONTH, -2, GETDATE()), 1),
(5, 3, N'khachhang3', N'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', N'Phạm Thu Hà',                  N'thuha.aquascape@gmail.com', N'0933445566', N'15 Phan Xích Long, Quận Phú Nhuận, TP.HCM',  DATEADD(MONTH, -1, GETDATE()), 1);
SET IDENTITY_INSERT [dbo].[Users] OFF;
GO

-- 6.3. Dữ liệu bảng Categories (8 danh mục)
SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([CategoryId], [CategoryName], [Description], [IsActive]) VALUES
(1, N'Cá cảnh nước ngọt',  N'Các loại cá cảnh nước ngọt: Betta, Neon, Guppy, Discus, ...',                                    1),
(2, N'Cá cảnh nước mặn',   N'Các loại cá cảnh biển: Nemo, Tang, Angel, ...',                                                  1),
(3, N'Cây thủy sinh',      N'Các loại cây thủy sinh trang trí bể cá',                                                         1),
(4, N'Thiết bị bể cá',     N'Bể kính, máy lọc, đèn LED, máy sục khí, ...',                                                    1),
(5, N'Thức ăn & Thuốc',    N'Thức ăn cá, thuốc xử lý nước, vitamin, ...',                                                     1),
(6, N'Phụ kiện trang trí', N'Sỏi, đá, gỗ lũa, san hô giả, ...',                                                               1),
(7, N'Tép cảnh & Cầu rêu', N'Các dòng tép màu Neocaridina, tép ong Caridina cao cấp và cầu rêu Marimo Nhật Bản',              1),
(8, N'Cần câu & Dụng cụ',  N'Cần câu tôm tép giải trí Carbon, cần câu Lure UL, vợt bắt tép 3D Inox và dụng cụ chăm sóc bể',   1);
SET IDENTITY_INSERT [dbo].[Categories] OFF;
GO

-- 6.4. Dữ liệu bảng Products (22 sản phẩm)
SET IDENTITY_INSERT [dbo].[Products] ON;
INSERT INTO [dbo].[Products] ([ProductId], [CategoryId], [ProductName], [Description], [Price], [StockQuantity], [ImageUrl], [CreatedAt], [IsActive]) VALUES
(1,  1, N'Cá Betta Halfmoon',                         N'Cá Betta Halfmoon đuôi tròn, nhiều màu sắc rực rỡ. Dễ nuôi, phù hợp bể nhỏ.',                                                                                                    85000.00,  50, N'/images/products/8f0d79f2-b08a-46ab-aa96-953998cb8ef2.jpg',  '2024-01-01T00:00:00', 1),
(2,  1, N'Cá Neon Tetra',                             N'Cá Neon sọc xanh đỏ, nuôi theo đàn rất đẹp. Kích thước 2-3cm.',                                                                                                                   5000.00, 200, N'/images/products/86a23d4d-45f7-4f27-a9ee-26020fb2ef6b.jpg',  '2024-01-01T00:00:00', 1),
(3,  1, N'Cá Guppy Full Red',                         N'Cá Guppy đỏ toàn thân, đuôi rộng. Dễ sinh sản.',                                                                                                                                 15000.00, 150, N'/images/products/e2de0ebc-c429-4dbb-bbd9-79140928e5f3.jpg',  '2024-01-01T00:00:00', 1),
(4,  2, N'Cá Nemo (Clownfish)',                       N'Cá hề Nemo cam trắng, biểu tượng cá cảnh biển. Cần bể nước mặn.',                                                                                                               120000.00,  30, N'/images/products/c4f0b6cc-ce44-4968-83f3-ced8155daca0.jpg',  '2024-01-01T00:00:00', 1),
(5,  3, N'Cây Trân Châu Ngọc Trai',                   N'Cây thủy sinh lá tròn nhỏ, trải thảm đẹp. Cần CO2 và ánh sáng mạnh.',                                                                                                            35000.00,  80, N'/images/products/5aa87c4e-0ff5-4be3-b86a-8d5b41746963.png',  '2024-01-01T00:00:00', 1),
(6,  3, N'Cây Rong Đuôi Chồn',                        N'Cây thủy sinh dạng thân, lá mềm mại tạo cảnh quan tự nhiên.',                                                                                                                    20000.00, 100, N'/images/products/c0128e7d-215d-4c35-862b-18bd41112678.png',  '2024-01-01T00:00:00', 1),
(7,  4, N'Bể cá kính 60cm',                           N'Bể kính siêu trong 60x30x36cm, kính dày 6mm. Kèm nắp đậy.',                                                                                                                     450000.00,  20, N'/images/products/5f869c8e-aba8-40f7-ba6d-e8181dec6d65.jpeg', '2024-01-01T00:00:00', 1),
(8,  4, N'Máy lọc Sunsun HW-603B',                    N'Máy lọc thùng ngoài cho bể 60-80cm. Công suất 600L/h.',                                                                                                                         380000.00,  15, N'/images/products/b25193ff-dc9f-41ce-a169-22bf756af14b.png',  '2024-01-01T00:00:00', 1),
(9,  5, N'Thức ăn Tetra Min Flakes',                  N'Thức ăn dạng vảy cho cá cảnh nhiệt đới. Hộp 52g.',                                                                                                                               65000.00, 100, N'/images/products/c9ad1a96-8c24-4bd6-baee-ee17e027dbbf.jpg',  '2024-01-01T00:00:00', 1),
(10, 6, N'Gỗ lũa Redmoor',                            N'Gỗ lũa tự nhiên tạo cảnh quan bể thủy sinh. Kích thước 20-30cm.',                                                                                                               150000.00,  10, N'/images/products/e3276c08-cce8-43a0-81bc-7abdc7326005.jpg',  '2024-01-01T00:00:00', 1),
(11, 1, N'Cá Đĩa (Discus) Lam Tự Nhiên',              N'Vua của các loài cá cảnh nước ngọt. Dáng tròn chuẩn, vây căng, màu xanh lam ánh kim nổi bật trong bể thủy sinh.',                                                               350000.00,  18, N'/images/products/0a29b17d-1db2-4d16-a637-bbcbe7d3b710.jpg',  DATEADD(DAY, -20, GETDATE()), 1),
(12, 2, N'Cá Blue Tang (Dory Biển)',                  N'Cá cảnh biển màu xanh coban rực rỡ, tính tình hiền lành, bơi năng động. Phù hợp bể cá biển từ 100L trở lên.',                                                                   420000.00,  12, N'/images/products/b38b0b67-b08d-4c43-8bc3-dc478f517fd4.jpg',  DATEADD(DAY, -15, GETDATE()), 1),
(13, 3, N'Cây Ráy Nana Petite Gắn Lũa',               N'Ráy thủy sinh lá nhỏ gắn sẵn trên giá thể lũa tự nhiên. Không cần CO2 cao, cực kỳ bền khỏe và dễ chăm sóc.',                                                                     95000.00,  25, N'/images/products/fe590f58-f6db-4451-9745-4217b52b5e28.jpg',  DATEADD(DAY, -10, GETDATE()), 1),
(14, 4, N'Đèn LED Thủy Sinh WRGB Chihiros 60cm',       N'Đèn LED quang phổ WRGB giúp cây thủy sinh lên màu đỏ căng và cá cảnh rực rỡ. Tích hợp điều khiển tăng giảm độ sáng.',                                                           890000.00,  10, N'/images/products/4c7df9cb-a31d-48c8-b153-eabd2c5888eb.jpg',  DATEADD(DAY, -5, GETDATE()),  1),
(15, 7, N'Tép Đỏ Red Cherry (Fire Red Neocaridina)',  N'Dòng tép màu Neocaridina đỏ rực toàn thân, sức sống cực khỏe, chăm dọn rêu hại trên lá cây thủy sinh và sinh sản nhanh. Thông số: pH 6.5-7.5, TDS 120-250, nhiệt độ 22-28°C.',   18000.00, 120, N'/images/products/9334c666-78c8-4f0f-98a2-67a3a8f45865.png',  DATEADD(DAY, -4, GETDATE()),  1),
(16, 7, N'Tép Xanh Blue Dream Sapphire',              N'Tép màu xanh ngọc bích đậm sâu nổi bật trên nền cát trắng hoặc rêu xanh. Dễ nuôi không cần Chiller, thích hợp cho cả bể nano để bàn.',                                           25000.00,  90, N'/images/products/874c7573-624a-4bcc-95fc-46907955cfbd.JPG',  DATEADD(DAY, -3, GETDATE()),  1),
(17, 7, N'Tép Ong Đỏ Red Bee PRL (Grade SS)',         N'Dòng tép ong Caridina Pure Red Line thuần chủng với các khoang trắng sứ dày xen kẽ đỏ tươi sắc nét. Phù hợp bể nền công nghiệp, pH 5.8-6.5, TDS 100-140, nhiệt độ 22-25°C.',   120000.00,  35, N'/images/products/d68d454c-b7b8-4a12-96b4-79beae404318.jpg',  DATEADD(DAY, -3, GETDATE()),  1),
(18, 7, N'Tép Galaxy Pinto Black & Red Cao Cấp',      N'Siêu phẩm tép cảnh dòng Caridina cao cấp với chấm sao Galaxy hai bên má và sọc lưng Zebra sắc sảo. Tuyển chọn từng cá thể khỏe mạnh.',                                          250000.00,  18, N'/images/products/ad4606ff-f34e-4a96-8845-08137cb84942.jpg',  DATEADD(DAY, -2, GETDATE()),  1),
(19, 7, N'Cầu Rêu Marimo Nhật Bản (Ổ Vi Sinh Bể Tép)', N'Quả cầu rêu nhung xanh tròn tự nhiên đường kính 4-5cm. Giúp hấp thụ Nitrate, tạo nơi trú ẩn và nguồn thức ăn vi sinh tự nhiên cho tép cảnh.',                                    45000.00,  65, N'/images/products/c28623f8-536c-44c9-b573-6f3fcbdc1e8b.jpg',  DATEADD(DAY, -2, GETDATE()),  1),
(20, 8, N'Cần Câu Tôm Tép Mini Carbon Siêu Nhẹ 1.5m',  N'Cần câu tôm/tép giải trí chất liệu sợi Carbon cao cấp siêu nhẹ (chỉ 38g), đọt dịu cảm giác cực nhạy khi tôm tép nhấp mồi.',                                                     195000.00,  25, N'/images/products/342df03d-388b-422a-960b-7a9460c05b3a.png',  DATEADD(DAY, -1, GETDATE()),  1),
(21, 8, N'Cần Câu Lure Carbon UL 2 Khúc 1.8m',        N'Cần câu cá Lure Ultra-Light (UL) sợi Carbon xoắn X chịu tải cao, dàn khoen sứ chống xoắn dây, tay cầm bần êm ái.',                                                              460000.00,  15, N'/images/products/68b261bc-f23f-4fe2-aacc-e9df6f218194.png',  DATEADD(DAY, -1, GETDATE()),  1),
(22, 8, N'Vợt Bắt Tép Cảnh 3D Cán Rút Inox Không Gỉ',  N'Vợt lưới 3D giữ form cứng cáp dưới nước, mắt lưới siêu mịn không làm gãy râu hay tổn thương chân tép cảnh. Cán Inox 304 kéo dài 55cm.',                                          35000.00,  50, N'/images/products/8a606044-b4f4-4f53-a208-0b3b9389f674.png',  DATEADD(DAY, -1, GETDATE()),  1);
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

-- 6.5. Dữ liệu bảng Orders (6 đơn hàng mẫu theo các tháng phục vụ biểu đồ doanh thu Dashboard)
SET IDENTITY_INSERT [dbo].[Orders] ON;
INSERT INTO [dbo].[Orders] ([OrderId], [UserId], [OrderDate], [TotalAmount], [Status], [ShippingAddress], [RecipientPhone], [PaymentMethod]) VALUES
(1, 3, DATEADD(DAY, -3, DATEADD(MONTH, -4, GETDATE())),  935000.00, N'Hoàn thành', N'88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM',    N'0918223344', N'Chuyển khoản'),
(2, 4, DATEADD(DAY, -5, DATEADD(MONTH, -3, GETDATE())),  570000.00, N'Hoàn thành', N'210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM',  N'0987654321', N'COD'),
(3, 5, DATEADD(DAY, -2, DATEADD(MONTH, -2, GETDATE())), 1110000.00, N'Hoàn thành', N'15 Phan Xích Long, Quận Phú Nhuận, TP.HCM',   N'0933445566', N'Chuyển khoản'),
(4, 3, DATEADD(DAY, -6, DATEADD(MONTH, -1, GETDATE())), 1035000.00, N'Hoàn thành', N'88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM',    N'0918223344', N'Chuyển khoản'),
(5, 4, DATEADD(DAY, -3, GETDATE()),                      385000.00, N'Đang giao',  N'210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM',  N'0987654321', N'Chuyển khoản'),
(6, 5, DATEADD(HOUR, -6, GETDATE()),                     310000.00, N'Chờ xử lý',  N'15 Phan Xích Long, Quận Phú Nhuận, TP.HCM',   N'0933445566', N'COD');
SET IDENTITY_INSERT [dbo].[Orders] OFF;
GO

-- 6.6. Dữ liệu bảng OrderDetails (Chi tiết từng đơn hàng)
SET IDENTITY_INSERT [dbo].[OrderDetails] ON;
INSERT INTO [dbo].[OrderDetails] ([OrderDetailId], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES
-- Đơn #1 (935,000đ)
(1,  1, 7,  1, 450000.00), -- Bể cá kính 60cm x1
(2,  1, 8,  1, 380000.00), -- Máy lọc Sunsun HW-603B x1
(3,  1, 5,  3,  35000.00), -- Cây Trân Châu Ngọc Trai x3
-- Đơn #2 (570,000đ)
(4,  2, 1,  4,  85000.00), -- Cá Betta Halfmoon x4
(5,  2, 2, 20,   5000.00), -- Cá Neon Tetra x20
(6,  2, 9,  2,  65000.00), -- Thức ăn Tetra Min Flakes x2
-- Đơn #3 (1,110,000đ)
(7,  3, 7,  1, 450000.00), -- Bể cá kính 60cm x1
(8,  3, 4,  3, 120000.00), -- Cá Nemo (Clownfish) x3
(9,  3, 10, 2, 150000.00), -- Gỗ lũa Redmoor x2
-- Đơn #4 (1,035,000đ)
(10, 4, 8,  2, 380000.00), -- Máy lọc Sunsun HW-603B x2
(11, 4, 5,  5,  35000.00), -- Cây Trân Châu Ngọc Trai x5
(12, 4, 6,  5,  20000.00), -- Cây Rong Đuôi Chồn x5
-- Đơn #5 (385,000đ)
(13, 5, 1,  2,  85000.00), -- Cá Betta Halfmoon x2
(14, 5, 3, 10,  15000.00), -- Cá Guppy Full Red x10
(15, 5, 9,  1,  65000.00), -- Thức ăn Tetra Min Flakes x1
-- Đơn #6 (310,000đ)
(16, 6, 4,  2, 120000.00), -- Cá Nemo (Clownfish) x2
(17, 6, 5,  2,  35000.00); -- Cây Trân Châu Ngọc Trai x2
SET IDENTITY_INSERT [dbo].[OrderDetails] OFF;
GO

-- 6.7. Dữ liệu bảng InventoryLogs (Nhật ký xuất kho tương ứng với các đơn hàng)
SET IDENTITY_INSERT [dbo].[InventoryLogs] ON;
INSERT INTO [dbo].[InventoryLogs] ([LogId], [ProductId], [UserId], [QuantityChange], [Reason], [CreatedAt]) VALUES
(1,  7,  3, -1,  N'Xuất kho - Đơn hàng #1', DATEADD(DAY, -3, DATEADD(MONTH, -4, GETDATE()))),
(2,  8,  3, -1,  N'Xuất kho - Đơn hàng #1', DATEADD(DAY, -3, DATEADD(MONTH, -4, GETDATE()))),
(3,  5,  3, -3,  N'Xuất kho - Đơn hàng #1', DATEADD(DAY, -3, DATEADD(MONTH, -4, GETDATE()))),
(4,  1,  4, -4,  N'Xuất kho - Đơn hàng #2', DATEADD(DAY, -5, DATEADD(MONTH, -3, GETDATE()))),
(5,  2,  4, -20, N'Xuất kho - Đơn hàng #2', DATEADD(DAY, -5, DATEADD(MONTH, -3, GETDATE()))),
(6,  9,  4, -2,  N'Xuất kho - Đơn hàng #2', DATEADD(DAY, -5, DATEADD(MONTH, -3, GETDATE()))),
(7,  7,  5, -1,  N'Xuất kho - Đơn hàng #3', DATEADD(DAY, -2, DATEADD(MONTH, -2, GETDATE()))),
(8,  4,  5, -3,  N'Xuất kho - Đơn hàng #3', DATEADD(DAY, -2, DATEADD(MONTH, -2, GETDATE()))),
(9,  10, 5, -2,  N'Xuất kho - Đơn hàng #3', DATEADD(DAY, -2, DATEADD(MONTH, -2, GETDATE()))),
(10, 8,  3, -2,  N'Xuất kho - Đơn hàng #4', DATEADD(DAY, -6, DATEADD(MONTH, -1, GETDATE()))),
(11, 5,  3, -5,  N'Xuất kho - Đơn hàng #4', DATEADD(DAY, -6, DATEADD(MONTH, -1, GETDATE()))),
(12, 6,  3, -5,  N'Xuất kho - Đơn hàng #4', DATEADD(DAY, -6, DATEADD(MONTH, -1, GETDATE()))),
(13, 1,  4, -2,  N'Xuất kho - Đơn hàng #5', DATEADD(DAY, -3, GETDATE())),
(14, 3,  4, -10, N'Xuất kho - Đơn hàng #5', DATEADD(DAY, -3, GETDATE())),
(15, 9,  4, -1,  N'Xuất kho - Đơn hàng #5', DATEADD(DAY, -3, GETDATE())),
(16, 4,  5, -2,  N'Xuất kho - Đơn hàng #6', DATEADD(HOUR, -6, GETDATE())),
(17, 5,  5, -2,  N'Xuất kho - Đơn hàng #6', DATEADD(HOUR, -6, GETDATE()));
SET IDENTITY_INSERT [dbo].[InventoryLogs] OFF;
GO

-- =========================================================================================
-- 7. CÁC CÂU TRUY VẤN KIỂM TRA DỮ LIỆU (DÀNH CHO GIẢNG VIÊN CHẤM ĐIỂM TRÊN SSMS)
-- =========================================================================================
SELECT N'1. Bảng Roles' AS [Bảng], COUNT(*) AS [Số bản ghi] FROM [dbo].[Roles]
UNION ALL
SELECT N'2. Bảng Users', COUNT(*) FROM [dbo].[Users]
UNION ALL
SELECT N'3. Bảng Categories', COUNT(*) FROM [dbo].[Categories]
UNION ALL
SELECT N'4. Bảng Products', COUNT(*) FROM [dbo].[Products]
UNION ALL
SELECT N'5. Bảng Orders', COUNT(*) FROM [dbo].[Orders]
UNION ALL
SELECT N'6. Bảng OrderDetails', COUNT(*) FROM [dbo].[OrderDetails]
UNION ALL
SELECT N'7. Bảng InventoryLogs', COUNT(*) FROM [dbo].[InventoryLogs];
GO

