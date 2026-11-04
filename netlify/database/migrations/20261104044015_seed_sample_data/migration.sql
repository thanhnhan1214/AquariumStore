INSERT INTO roles (id, name) VALUES (1,'Admin'),(2,'Staff'),(3,'Customer');
--> statement-breakpoint
INSERT INTO users (id, role_id, username, password_hash, full_name, email, phone, address, created_at, is_active) VALUES
(1, 1, 'admin',      'scrypt$3977dd861f873b78acdc592b7a199f07$60bd800d830b5b3afce63e026580547fe65832d82787a140a7f35faa721eea5fca8b09685a529c698db9255994c1956a6840e54fbe981ae7b4b70684eff08d1b', 'Quản trị viên hệ thống',       'admin@aquariumstore.vn',    '0901234567', '123 Nguyễn Huệ, Q.1, TP.HCM',               '2024-01-01T00:00:00+07', true),
(2, 2, 'staff',      'scrypt$127dfaf826aa5e6a82480d1b7a783598$0bd7acbdfbbd7fce693b2870e5827b6ed18b8fe317cf58641c2f50d56d75690c0dde9d452c47ecea14bcc17cb39bb4dc7de163dc18eb1f7e7f5e2d262f085123', 'Nguyễn Hoàng Nam (Nhân viên)', 'staff@aquariumstore.vn',    '0902345678', '45 Lê Lợi, Quận 1, TP.HCM',                 (now() + interval '-4 months'), true),
(3, 3, 'customer',   'scrypt$a030d016c7d018b95cf5304cef7f801d$358beeeb938744657074a4cee78eaaa8a96d7701ba918a213831bfc303701d0e54f92f6a8d7e462018925df86b191617459d66b65415aaeac63f685d5589798b', 'Trần Minh Anh',                'minhanh@gmail.com',         '0918223344', '88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM',   (now() + interval '-3 months'), true),
(4, 3, 'khachhang2', 'scrypt$a030d016c7d018b95cf5304cef7f801d$358beeeb938744657074a4cee78eaaa8a96d7701ba918a213831bfc303701d0e54f92f6a8d7e462018925df86b191617459d66b65415aaeac63f685d5589798b', 'Lê Quốc Bảo',                  'quocbao.aqua@gmail.com',    '0987654321', '210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', (now() + interval '-2 months'), true),
(5, 3, 'khachhang3', 'scrypt$a030d016c7d018b95cf5304cef7f801d$358beeeb938744657074a4cee78eaaa8a96d7701ba918a213831bfc303701d0e54f92f6a8d7e462018925df86b191617459d66b65415aaeac63f685d5589798b', 'Phạm Thu Hà',                  'thuha.aquascape@gmail.com', '0933445566', '15 Phan Xích Long, Quận Phú Nhuận, TP.HCM',  (now() + interval '-1 months'), true);
--> statement-breakpoint
INSERT INTO categories (id, name, description, is_active) VALUES
(1, 'Cá cảnh nước ngọt',  'Các loại cá cảnh nước ngọt: Betta, Neon, Guppy, Discus, ...', true),
(2, 'Cá cảnh nước mặn',   'Các loại cá cảnh biển: Nemo, Tang, Angel, ...', true),
(3, 'Cây thủy sinh',      'Các loại cây thủy sinh trang trí bể cá', true),
(4, 'Thiết bị bể cá',     'Bể kính, máy lọc, đèn LED, máy sục khí, ...', true),
(5, 'Thức ăn & Thuốc',    'Thức ăn cá, thuốc xử lý nước, vitamin, ...', true),
(6, 'Phụ kiện trang trí', 'Sỏi, đá, gỗ lũa, san hô giả, ...', true),
(7, 'Tép cảnh & Cầu rêu', 'Các dòng tép màu Neocaridina, tép ong Caridina cao cấp và cầu rêu Marimo Nhật Bản', true),
(8, 'Cần câu & Dụng cụ',  'Cần câu tôm tép giải trí Carbon, cần câu Lure UL, vợt bắt tép 3D Inox và dụng cụ chăm sóc bể', true);
--> statement-breakpoint
INSERT INTO products (id, category_id, name, description, price, stock, image_url, created_at, is_active) VALUES
(1,  1, 'Cá Betta Halfmoon',                         'Cá Betta Halfmoon đuôi tròn, nhiều màu sắc rực rỡ. Dễ nuôi, phù hợp bể nhỏ.',                                                                                                    85000.00,  50, '/images/products/8f0d79f2-b08a-46ab-aa96-953998cb8ef2.jpg',  '2024-01-01T00:00:00+07', true),
(2,  1, 'Cá Neon Tetra',                             'Cá Neon sọc xanh đỏ, nuôi theo đàn rất đẹp. Kích thước 2-3cm.',                                                                                                                   5000.00, 200, '/images/products/86a23d4d-45f7-4f27-a9ee-26020fb2ef6b.jpg',  '2024-01-01T00:00:00+07', true),
(3,  1, 'Cá Guppy Full Red',                         'Cá Guppy đỏ toàn thân, đuôi rộng. Dễ sinh sản.',                                                                                                                                 15000.00, 150, '/images/products/e2de0ebc-c429-4dbb-bbd9-79140928e5f3.jpg',  '2024-01-01T00:00:00+07', true),
(4,  2, 'Cá Nemo (Clownfish)',                       'Cá hề Nemo cam trắng, biểu tượng cá cảnh biển. Cần bể nước mặn.',                                                                                                               120000.00,  30, '/images/products/c4f0b6cc-ce44-4968-83f3-ced8155daca0.jpg',  '2024-01-01T00:00:00+07', true),
(5,  3, 'Cây Trân Châu Ngọc Trai',                   'Cây thủy sinh lá tròn nhỏ, trải thảm đẹp. Cần CO2 và ánh sáng mạnh.',                                                                                                            35000.00,  80, '/images/products/d01fddbb-6561-4efc-9bf7-0c52e2742238.png',  '2024-01-01T00:00:00+07', true),
(6,  3, 'Cây Rong Đuôi Chồn',                        'Cây thủy sinh dạng thân, lá mềm mại tạo cảnh quan tự nhiên.',                                                                                                                    20000.00, 100, '/images/products/c0128e7d-215d-4c35-862b-18bd41112678.png',  '2024-01-01T00:00:00+07', true),
(7,  4, 'Bể cá kính 60cm',                           'Bể kính siêu trong 60x30x36cm, kính dày 6mm. Kèm nắp đậy.',                                                                                                                     450000.00,  20, '/images/products/5f869c8e-aba8-40f7-ba6d-e8181dec6d65.jpeg', '2024-01-01T00:00:00+07', true),
(8,  4, 'Máy lọc Sunsun HW-603B',                    'Máy lọc thùng ngoài cho bể 60-80cm. Công suất 600L/h.',                                                                                                                         380000.00,  15, '/images/products/b25193ff-dc9f-41ce-a169-22bf756af14b.png',  '2024-01-01T00:00:00+07', true),
(9,  5, 'Thức ăn Tetra Min Flakes',                  'Thức ăn dạng vảy cho cá cảnh nhiệt đới. Hộp 52g.',                                                                                                                               65000.00, 100, '/images/products/c9ad1a96-8c24-4bd6-baee-ee17e027dbbf.jpg',  '2024-01-01T00:00:00+07', true),
(10, 6, 'Gỗ lũa Redmoor',                            'Gỗ lũa tự nhiên tạo cảnh quan bể thủy sinh. Kích thước 20-30cm.',                                                                                                               150000.00,  10, '/images/products/e3276c08-cce8-43a0-81bc-7abdc7326005.jpg',  '2024-01-01T00:00:00+07', true),
(11, 1, 'Cá Đĩa (Discus) Lam Tự Nhiên',              'Vua của các loài cá cảnh nước ngọt. Dáng tròn chuẩn, vây căng, màu xanh lam ánh kim nổi bật trong bể thủy sinh.',                                                               350000.00,  18, '/images/products/0a29b17d-1db2-4d16-a637-bbcbe7d3b710.jpg',  (now() + interval '-20 days'), true),
(12, 2, 'Cá Blue Tang (Dory Biển)',                  'Cá cảnh biển màu xanh coban rực rỡ, tính tình hiền lành, bơi năng động. Phù hợp bể cá biển từ 100L trở lên.',                                                                   420000.00,  12, '/images/products/b38b0b67-b08d-4c43-8bc3-dc478f517fd4.jpg',  (now() + interval '-15 days'), true),
(13, 3, 'Cây Ráy Nana Petite Gắn Lũa',               'Ráy thủy sinh lá nhỏ gắn sẵn trên giá thể lũa tự nhiên. Không cần CO2 cao, cực kỳ bền khỏe và dễ chăm sóc.',                                                                     95000.00,  25, '/images/products/fe590f58-f6db-4451-9745-4217b52b5e28.jpg',  (now() + interval '-10 days'), true),
(14, 4, 'Đèn LED Thủy Sinh WRGB Chihiros 60cm',       'Đèn LED quang phổ WRGB giúp cây thủy sinh lên màu đỏ căng và cá cảnh rực rỡ. Tích hợp điều khiển tăng giảm độ sáng.',                                                           890000.00,  10, '/images/products/4c7df9cb-a31d-48c8-b153-eabd2c5888eb.jpg',  (now() + interval '-5 days'), true),
(15, 7, 'Tép Đỏ Red Cherry (Fire Red Neocaridina)',  'Dòng tép màu Neocaridina đỏ rực toàn thân, sức sống cực khỏe, chăm dọn rêu hại trên lá cây thủy sinh và sinh sản nhanh. Thông số: pH 6.5-7.5, TDS 120-250, nhiệt độ 22-28°C.',   18000.00, 120, '/images/products/9334c666-78c8-4f0f-98a2-67a3a8f45865.png',  (now() + interval '-4 days'), true),
(16, 7, 'Tép Xanh Blue Dream Sapphire',              'Tép màu xanh ngọc bích đậm sâu nổi bật trên nền cát trắng hoặc rêu xanh. Dễ nuôi không cần Chiller, thích hợp cho cả bể nano để bàn.',                                           25000.00,  90, '/images/products/874c7573-624a-4bcc-95fc-46907955cfbd.JPG',  (now() + interval '-3 days'), true),
(17, 7, 'Tép Ong Đỏ Red Bee PRL (Grade SS)',         'Dòng tép ong Caridina Pure Red Line thuần chủng với các khoang trắng sứ dày xen kẽ đỏ tươi sắc nét. Phù hợp bể nền công nghiệp, pH 5.8-6.5, TDS 100-140, nhiệt độ 22-25°C.',   120000.00,  35, '/images/products/d68d454c-b7b8-4a12-96b4-79beae404318.jpg',  (now() + interval '-3 days'), true),
(18, 7, 'Tép Galaxy Pinto Black & Red Cao Cấp',      'Siêu phẩm tép cảnh dòng Caridina cao cấp với chấm sao Galaxy hai bên má và sọc lưng Zebra sắc sảo. Tuyển chọn từng cá thể khỏe mạnh.',                                          250000.00,  18, '/images/products/ad4606ff-f34e-4a96-8845-08137cb84942.jpg',  (now() + interval '-2 days'), true),
(19, 7, 'Cầu Rêu Marimo Nhật Bản (Ổ Vi Sinh Bể Tép)', 'Quả cầu rêu nhung xanh tròn tự nhiên đường kính 4-5cm. Giúp hấp thụ Nitrate, tạo nơi trú ẩn và nguồn thức ăn vi sinh tự nhiên cho tép cảnh.',                                    45000.00,  65, '/images/products/c28623f8-536c-44c9-b573-6f3fcbdc1e8b.jpg',  (now() + interval '-2 days'), true),
(20, 8, 'Cần Câu Tôm Tép Mini Carbon Siêu Nhẹ 1.5m',  'Cần câu tôm/tép giải trí chất liệu sợi Carbon cao cấp siêu nhẹ (chỉ 38g), đọt dịu cảm giác cực nhạy khi tôm tép nhấp mồi.',                                                     195000.00,  25, '/images/products/342df03d-388b-422a-960b-7a9460c05b3a.png',  (now() + interval '-1 days'), true),
(21, 8, 'Cần Câu Lure Carbon UL 2 Khúc 1.8m',        'Cần câu cá Lure Ultra-Light (UL) sợi Carbon xoắn X chịu tải cao, dàn khoen sứ chống xoắn dây, tay cầm bần êm ái.',                                                              460000.00,  15, '/images/products/7599a23e-022b-4f2f-84f9-fc8af2c51f24.png',  (now() + interval '-1 days'), true),
(22, 8, 'Vợt Bắt Tép Cảnh 3D Cán Rút Inox Không Gỉ',  'Vợt lưới 3D giữ form cứng cáp dưới nước, mắt lưới siêu mịn không làm gãy râu hay tổn thương chân tép cảnh. Cán Inox 304 kéo dài 55cm.',                                          35000.00,  50, '/images/products/6034ce93-d457-4bfb-b1aa-cbfb766d3237.png',  (now() + interval '-1 days'), true);
--> statement-breakpoint
INSERT INTO orders (id, user_id, order_date, total_amount, status, shipping_address, recipient_phone, payment_method) VALUES
(1, 3, (now() + interval '-4 months' + interval '-3 days'),  935000.00, 'Hoàn thành', '88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM',    '0918223344', 'Chuyển khoản'),
(2, 4, (now() + interval '-3 months' + interval '-5 days'),  570000.00, 'Hoàn thành', '210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM',  '0987654321', 'COD'),
(3, 5, (now() + interval '-2 months' + interval '-2 days'), 1110000.00, 'Hoàn thành', '15 Phan Xích Long, Quận Phú Nhuận, TP.HCM',   '0933445566', 'Chuyển khoản'),
(4, 3, (now() + interval '-1 months' + interval '-6 days'), 1035000.00, 'Hoàn thành', '88 Nguyễn Thị Minh Khai, Quận 3, TP.HCM',    '0918223344', 'Chuyển khoản'),
(5, 4, (now() + interval '-3 days'),                      385000.00, 'Đang giao',  '210 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM',  '0987654321', 'Chuyển khoản'),
(6, 5, (now() + interval '-6 hours'),                     310000.00, 'Chờ xử lý',  '15 Phan Xích Long, Quận Phú Nhuận, TP.HCM',   '0933445566', 'COD');
--> statement-breakpoint
INSERT INTO order_details (id, order_id, product_id, quantity, unit_price) VALUES
(1,  1, 7,  1, 450000.00), 
(2,  1, 8,  1, 380000.00), 
(3,  1, 5,  3,  35000.00), 
(4,  2, 1,  4,  85000.00), 
(5,  2, 2, 20,   5000.00), 
(6,  2, 9,  2,  65000.00), 
(7,  3, 7,  1, 450000.00), 
(8,  3, 4,  3, 120000.00), 
(9,  3, 10, 2, 150000.00), 
(10, 4, 8,  2, 380000.00), 
(11, 4, 5,  5,  35000.00), 
(12, 4, 6,  5,  20000.00), 
(13, 5, 1,  2,  85000.00), 
(14, 5, 3, 10,  15000.00), 
(15, 5, 9,  1,  65000.00), 
(16, 6, 4,  2, 120000.00), 
(17, 6, 5,  2,  35000.00); 
--> statement-breakpoint
INSERT INTO inventory_logs (id, product_id, user_id, quantity_change, reason, created_at) VALUES
(1,  7,  3, -1,  'Xuất kho - Đơn hàng #1', (now() + interval '-4 months' + interval '-3 days')),
(2,  8,  3, -1,  'Xuất kho - Đơn hàng #1', (now() + interval '-4 months' + interval '-3 days')),
(3,  5,  3, -3,  'Xuất kho - Đơn hàng #1', (now() + interval '-4 months' + interval '-3 days')),
(4,  1,  4, -4,  'Xuất kho - Đơn hàng #2', (now() + interval '-3 months' + interval '-5 days')),
(5,  2,  4, -20, 'Xuất kho - Đơn hàng #2', (now() + interval '-3 months' + interval '-5 days')),
(6,  9,  4, -2,  'Xuất kho - Đơn hàng #2', (now() + interval '-3 months' + interval '-5 days')),
(7,  7,  5, -1,  'Xuất kho - Đơn hàng #3', (now() + interval '-2 months' + interval '-2 days')),
(8,  4,  5, -3,  'Xuất kho - Đơn hàng #3', (now() + interval '-2 months' + interval '-2 days')),
(9,  10, 5, -2,  'Xuất kho - Đơn hàng #3', (now() + interval '-2 months' + interval '-2 days')),
(10, 8,  3, -2,  'Xuất kho - Đơn hàng #4', (now() + interval '-1 months' + interval '-6 days')),
(11, 5,  3, -5,  'Xuất kho - Đơn hàng #4', (now() + interval '-1 months' + interval '-6 days')),
(12, 6,  3, -5,  'Xuất kho - Đơn hàng #4', (now() + interval '-1 months' + interval '-6 days')),
(13, 1,  4, -2,  'Xuất kho - Đơn hàng #5', (now() + interval '-3 days')),
(14, 3,  4, -10, 'Xuất kho - Đơn hàng #5', (now() + interval '-3 days')),
(15, 9,  4, -1,  'Xuất kho - Đơn hàng #5', (now() + interval '-3 days')),
(16, 4,  5, -2,  'Xuất kho - Đơn hàng #6', (now() + interval '-6 hours')),
(17, 5,  5, -2,  'Xuất kho - Đơn hàng #6', (now() + interval '-6 hours'));
--> statement-breakpoint
UPDATE orders o SET recipient_name = u.full_name FROM users u WHERE u.id = o.user_id;
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('roles', 'id'), (SELECT MAX(id) FROM roles));
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('users', 'id'), (SELECT MAX(id) FROM users));
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('categories', 'id'), (SELECT MAX(id) FROM categories));
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('products', 'id'), (SELECT MAX(id) FROM products));
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('orders', 'id'), (SELECT MAX(id) FROM orders));
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('order_details', 'id'), (SELECT MAX(id) FROM order_details));
--> statement-breakpoint
SELECT setval(pg_get_serial_sequence('inventory_logs', 'id'), (SELECT MAX(id) FROM inventory_logs));
