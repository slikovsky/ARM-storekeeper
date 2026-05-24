-- ============================================
-- Расширенная база данных для АРМ Кладовщика
-- ============================================

-- Очистка
DROP TABLE IF EXISTS warehouse_operations CASCADE;
DROP TABLE IF EXISTS products CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS suppliers CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TYPE IF EXISTS operation_type CASCADE;

-- ============================================
-- Таблица пользователей
-- ============================================
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    "role" VARCHAR(20) NOT NULL DEFAULT 'storekeeper' CHECK ("role" IN ('admin', 'storekeeper')),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP
);

-- ============================================
-- Таблица категорий товаров
-- ============================================
CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT
);

-- ============================================
-- Таблица поставщиков
-- ============================================
CREATE TABLE suppliers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    contact_person VARCHAR(100),
    phone VARCHAR(20),
    email VARCHAR(100),
    address TEXT
);

-- ============================================
-- Таблица товаров
-- ============================================
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    article VARCHAR(50),
    barcode VARCHAR(50),
    category_id INTEGER REFERENCES categories(id) ON DELETE SET NULL,
    supplier_id INTEGER REFERENCES suppliers(id) ON DELETE SET NULL,
    price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    quantity INTEGER NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    min_quantity INTEGER NOT NULL DEFAULT 10 CHECK (min_quantity >= 0),
    unit VARCHAR(20) NOT NULL DEFAULT 'шт',
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- Таблица складских операций (INTEGER вместо ENUM)
-- ============================================
CREATE TABLE warehouse_operations (
    id SERIAL PRIMARY KEY,
    operation_type INTEGER NOT NULL CHECK (operation_type BETWEEN 1 AND 4),
    product_id INTEGER NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    reason VARCHAR(255),
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    operation_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    notes TEXT
);

-- ============================================
-- Индексы
-- ============================================
CREATE INDEX idx_products_name ON products(name);
CREATE INDEX idx_products_article ON products(article);
CREATE INDEX idx_products_barcode ON products(barcode);
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_operations_date ON warehouse_operations(operation_date);
CREATE INDEX idx_operations_product ON warehouse_operations(product_id);
CREATE INDEX idx_operations_type ON warehouse_operations(operation_type);

-- ============================================
-- Триггер для обновления updated_at
-- ============================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_products_updated_at
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- Триггер для проверки остатков
-- ============================================
CREATE OR REPLACE FUNCTION check_stock_before_outgoing()
RETURNS TRIGGER AS $$
DECLARE
    current_stock INTEGER;
BEGIN
    IF NEW.operation_type IN (2, 4) THEN
        SELECT quantity INTO current_stock FROM products WHERE id = NEW.product_id;
        IF current_stock < NEW.quantity THEN
            RAISE EXCEPTION 'Недостаточно товара на складе. Доступно: %, Требуется: %', current_stock, NEW.quantity;
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_stock
    BEFORE INSERT ON warehouse_operations
    FOR EACH ROW
    EXECUTE FUNCTION check_stock_before_outgoing();

-- ============================================
-- КАТЕГОРИИ (15 штук)
-- ============================================
INSERT INTO categories (name, description) VALUES
('Электроника', 'Компьютеры, ноутбуки, комплектующие'),
('Офисные товары', 'Канцелярия, бумага, расходники'),
('Мебель', 'Офисная, складская, мягкая мебель'),
('Инструменты', 'Ручной и электроинструмент'),
('Расходные материалы', 'Картриджи, тонеры, расходники для оргтехники'),
('Сетевое оборудование', 'Роутеры, свитчи, кабели, Wi-Fi'),
('Периферия', 'Мыши, клавиатуры, мониторы, колонки'),
('Хранение данных', 'SSD, HDD, флешки, карты памяти'),
('Безопасность', 'Камеры, сигнализации, домофоны'),
('Электрика', 'Удлинители, стабилизаторы, ИБП'),
('Сантехника', 'Краны, шланги, фитинги'),
('Строительные материалы', 'Краски, клей, герметики'),
('Освещение', 'Лампы, светильники, прожекторы'),
('Крепёж', 'Саморезы, дюбели, анкера, болты'),
('Упаковка', 'Коробки, плёнка, скотч, пакеты');

-- ============================================
-- ПОСТАВЩИКИ (10 штук)
-- ============================================
INSERT INTO suppliers (name, contact_person, phone, email, address) VALUES
('ООО ТехноПоставка', 'Иванов Иван Иванович', '+7(999)123-45-67', 'info@techno.ru', 'г. Москва, ул. Ленина, д. 1, офис 305'),
('ИП Сидоров А.В.', 'Сидоров Алексей Викторович', '+7(999)765-43-21', 'sidorov@mail.ru', 'г. Санкт-Петербург, пр. Невский, д. 10'),
('ОфисОпт', 'Петрова Мария Сергеевна', '+7(999)111-22-33', 'zakaz@officeopt.ru', 'г. Казань, ул. Баумана, д. 5'),
('ЭлектроМир', 'Козлов Дмитрий Петрович', '+7(999)222-33-44', 'sales@electromir.ru', 'г. Новосибирск, ул. Красная, д. 25'),
('МебельПлюс', 'Соколова Анна Владимировна', '+7(999)333-44-55', 'mebel@mebelplus.ru', 'г. Екатеринбург, ул. Малышева, д. 15'),
('СтройТорг', 'Николаев Сергей Алексеевич', '+7(999)444-55-66', 'opt@stroytorg.ru', 'г. Челябинск, пр. Ленина, д. 32'),
('ИнструментПро', 'Волков Андрей Игоревич', '+7(999)555-66-77', 'zakaz@instrumentpro.ru', 'г. Нижний Новгород, ул. Родионова, д. 8'),
('СветТехника', 'Попова Елена Дмитриевна', '+7(999)666-77-88', 'info@svettek.ru', 'г. Самара, ул. Московское шоссе, д. 50'),
('УпаковкаОпт', 'Морозов Павел Сергеевич', '+7(999)777-88-99', 'sales@upakovkaopt.ru', 'г. Омск, ул. Лермонтова, д. 12'),
('КрепёжЦентр', 'Васильев Игорь Николаевич', '+7(999)888-99-00', 'zakaz@krepezhcentr.ru', 'г. Ростов-на-Дону, пр. Ворошиловский, д. 18');

-- ============================================
-- ТОВАРЫ (50 штук)
-- ============================================
INSERT INTO products (name, article, barcode, category_id, supplier_id, price, quantity, min_quantity, unit, description) VALUES
-- Электроника
('Ноутбук Dell Latitude 5520', 'NB-DL-001', '4601234567890', 1, 1, 85000.00, 12, 3, 'шт', '15.6" FHD, Intel Core i5-1145G7, 16GB RAM, 512GB SSD, Windows 11 Pro'),
('Ноутбук HP ProBook 450 G8', 'NB-HP-002', '4601234567891', 1, 1, 72000.00, 8, 2, 'шт', '15.6" FHD, Intel Core i7-1165G7, 16GB RAM, 512GB SSD'),
('Системный блок Acer Veriton', 'PC-AC-003', '4601234567892', 1, 1, 45000.00, 15, 5, 'шт', 'Intel Core i5-10400, 8GB RAM, 256GB SSD, Windows 10 Pro'),
('Монитор 24" Dell P2419H', 'MN-DL-004', '4601234567893', 1, 1, 18500.00, 20, 5, 'шт', '23.8" IPS, 1920x1080, 60Hz, HDMI, DisplayPort, USB-hub'),
('Монитор 27" Samsung Odyssey G5', 'MN-SM-005', '4601234567894', 1, 1, 32000.00, 6, 2, 'шт', '27" VA, 2560x1440, 144Hz, 1ms, изогнутый'),
('Планшет Samsung Galaxy Tab S8', 'TB-SM-006', '4601234567895', 1, 1, 55000.00, 10, 3, 'шт', '11" LTPS, Snapdragon 8 Gen 1, 8GB RAM, 128GB'),
('Проектор Epson EB-E01', 'PR-EP-007', '4601234567896', 1, 1, 28000.00, 5, 2, 'шт', 'XGA 1024x768, 3300 люмен, LCD, HDMI'),

-- Периферия
('Мышь Logitech MX Master 3', 'MS-LG-008', '4601234567897', 7, 1, 8500.00, 30, 10, 'шт', 'Беспроводная, 4000 DPI, USB-C, магнитное колесо прокрутки'),
('Клавиатура Logitech MX Keys', 'KB-LG-009', '4601234567898', 7, 1, 9500.00, 25, 8, 'шт', 'Беспроводная, подсветка, USB-C, мульти-девайс'),
('Веб-камера Logitech C920 HD Pro', 'WC-LG-010', '4601234567899', 7, 1, 6500.00, 18, 5, 'шт', 'Full HD 1080p, автофокус, стереомикрофон'),
('Наушники JBL Tune 760NC', 'HP-JB-011', '4601234567900', 7, 1, 7500.00, 22, 6, 'шт', 'Беспроводные, ANC, 35ч работы, Bluetooth 5.0'),
('Коврик для мыши SteelSeries QcK Large', 'MP-SS-012', '4601234567901', 7, 2, 1200.00, 50, 15, 'шт', '450x400x2мм, ткань + резина, игровой'),

-- Сетевое оборудование
('Роутер TP-Link Archer AX73', 'RT-TP-013', '4601234567902', 6, 4, 9500.00, 15, 5, 'шт', 'Wi-Fi 6, AX5400, 5 портов Gigabit, USB 3.0'),
('Коммутатор TP-Link TL-SG108', 'SW-TP-014', '4601234567903', 6, 4, 2500.00, 40, 12, 'шт', '8 портов Gigabit, металлический корпус'),
('Точка доступа Ubiquiti UniFi 6 Lite', 'AP-UB-015', '4601234567904', 6, 4, 8500.00, 12, 4, 'шт', 'Wi-Fi 6, AX1500, PoE, потолочное крепление'),
('Сетевой кабель UTP Cat.6 305м', 'CB-TP-016', '4601234567905', 6, 4, 8500.00, 8, 3, 'бух', 'Медный, 23 AWG, для внутренней прокладки'),
('Патч-корд UTP Cat.6 2м (синий)', 'PC-TP-017', '4601234567906', 6, 4, 120.00, 200, 50, 'шт', 'Медный, экранированный разъём, LSZH'),

-- Хранение данных
('SSD Samsung 980 Pro 1TB', 'SD-SM-018', '4601234567907', 8, 1, 12500.00, 20, 6, 'шт', 'M.2 NVMe, PCIe 4.0, 7000/5000 МБ/с'),
('SSD Kingston NV2 500GB', 'SD-KG-019', '4601234567908', 8, 1, 3500.00, 35, 10, 'шт', 'M.2 NVMe, PCIe 4.0, 3500/2100 МБ/с'),
('Внешний HDD Seagate Expansion 2TB', 'HD-SG-020', '4601234567909', 8, 1, 5500.00, 18, 5, 'шт', '2.5", USB 3.0, 5400 об/мин'),
('Флешка SanDisk Ultra 128GB', 'FD-SD-021', '4601234567910', 8, 1, 850.00, 100, 30, 'шт', 'USB 3.0, 100 МБ/с, выдвижной разъём'),
('Карта памяти microSD SanDisk 64GB', 'MC-SD-022', '4601234567911', 8, 1, 450.00, 80, 25, 'шт', 'Class 10, UHS-I, 100 МБ/с, с адаптером SD'),

-- Расходные материалы
('Картридж HP 305A Black (CE410A)', 'CR-HP-023', '4601234567912', 5, 3, 3200.00, 25, 8, 'шт', 'Для HP LaserJet Pro 300/400, 2200 страниц'),
('Картридж HP 305A Cyan (CE411A)', 'CR-HP-024', '4601234567913', 5, 3, 3200.00, 15, 5, 'шт', 'Для HP LaserJet Pro 300/400, 2600 страниц'),
('Тонер-картридж Brother TN-2375', 'CR-BR-025', '4601234567914', 5, 3, 1800.00, 30, 10, 'шт', 'Для Brother HL-L2300/2340/2360/2365, 2600 стр.'),
('Бумага A4 Svetocopy 500 л.', 'PP-SV-026', '4601234567915', 2, 3, 280.00, 500, 100, 'пач', 'Класс C, 80 г/м², белизна 146% CIE'),
('Бумага A4 Double A 500 л.', 'PP-DA-027', '4601234567916', 2, 3, 450.00, 200, 50, 'пач', 'Премиум, 80 г/м², белизна 165% CIE'),
('Бумага A3 Svetocopy 500 л.', 'PP-SV-028', '4601234567917', 2, 3, 520.00, 80, 20, 'пач', 'Класс C, 80 г/м², для широкоформатной печати'),

-- Офисные товары
('Степлер №10 BRAUBERG', 'ST-BR-029', '4601234567918', 2, 3, 120.00, 60, 20, 'шт', 'Металлический, до 15 листов, антистеплер'),
('Дырокол BRAUBERG 30 л.', 'PU-BR-030', '4601234567919', 2, 3, 280.00, 40, 12, 'шт', 'Металлический, 2 отверстия, линейка'),
('Клей-карандаш PVP 21г', 'GL-UN-031', '4601234567920', 2, 3, 35.00, 150, 50, 'шт', 'Универсальный, прозрачный, не токсичен'),
('Скотч канцелярский 19мм×33м', 'SC-UN-032', '4601234567921', 2, 3, 25.00, 200, 60, 'шт', 'Прозрачный, акриловый, для бумаги'),
('Нож канцелярский 9мм', 'KN-UN-033', '4601234567922', 2, 3, 45.00, 80, 25, 'шт', 'Металлический корпус, автофиксация, 2 лезвия'),
('Лоток для бумаг горизонтальный', 'TR-UN-034', '4601234567923', 2, 3, 150.00, 45, 15, 'шт', 'Пластик, серый, 3 секции, сборный'),

-- Мебель
('Стул офисный BRABIX Energy', 'CH-BX-035', '4601234567924', 3, 5, 5500.00, 20, 6, 'шт', 'Сетка, эргономичная спинка, регулировка высоты'),
('Кресло руководителя BRABIX Status', 'CH-BX-036', '4601234567925', 3, 5, 18500.00, 8, 2, 'шт', 'Кожа, хром, подголовник, реклайнер'),
('Стол письменный 140×70', 'TB-UN-037', '4601234567926', 3, 5, 8500.00, 12, 4, 'шт', 'ЛДСП, дуб сонома, 2 ящика, кабель-канал'),
('Шкаф для документов 4 полки', 'CB-UN-038', '4601234567927', 3, 5, 12000.00, 6, 2, 'шт', 'Металл, 1850×900×400мм, замок'),
('Тумба подкатная 3 ящика', 'DR-UN-039', '4601234567928', 3, 5, 4500.00, 15, 5, 'шт', 'ЛДСП, дуб сонома, центральный замок'),

-- Инструменты
('Дрель-шуруповёрт Makita DF333D', 'DR-MK-040', '4601234567929', 4, 7, 8900.00, 12, 4, 'шт', '12V, 2×1.5Ah Li-Ion, 30 Н·м, кейс'),
('Перфоратор Bosch GBH 2-28 F', 'PR-BS-041', '4601234567930', 4, 7, 18500.00, 5, 2, 'шт', '880W, 3.2 Дж, SDS-plus, 3 режима'),
('Шлифмашина угловая Makita GA5030', 'AG-MK-042', '4601234567931', 4, 7, 6500.00, 8, 3, 'шт', '720W, 125мм, 11000 об/мин, М14'),
('Набор отвёрток 6 шт.', 'SD-UN-043', '4601234567932', 4, 7, 450.00, 40, 12, 'шт', 'Cr-V, магнитный наконечник, двухкомпонентная рукоятка'),
('Набор ключей рожковых 8 шт.', 'WK-UN-044', '4601234567933', 4, 7, 850.00, 30, 10, 'шт', 'Cr-V, 6-22мм, хромированное покрытие'),
('Рулетка 5м×25мм', 'TM-UN-045', '4601234567934', 4, 7, 180.00, 60, 20, 'шт', 'Обрезиненный корпус, фиксатор, двусторонняя шкала'),

-- Электрика
('ИБП APC Back-UPS 650VA', 'UP-AP-046', '4601234567935', 10, 4, 8500.00, 10, 3, 'шт', '400W, 4 розетки, USB, защита телефонной линии'),
('Стабилизатор напряжения 1000VA', 'VS-UN-047', '4601234567936', 10, 4, 6500.00, 8, 3, 'шт', 'Релейный, 140-260V, 4 розетки, LED-дисплей'),
('Удлинитель 5м 4 розетки с заземлением', 'EX-UN-048', '4601234567937', 10, 4, 450.00, 50, 15, 'шт', '16A, ПВС 3×1.5, выключатель, защита от детей'),
('Светодиодная лампа E27 10W 4000K', 'LB-UN-049', '4601234567938', 13, 8, 85.00, 200, 60, 'шт', '900 лм, Ra>80, 25000ч, алюминиевый радиатор'),
('Прожектор LED 50W 6500K IP65', 'FL-UN-050', '4601234567939', 13, 8, 1200.00, 25, 8, 'шт', '4500 лм, SMD2835, алюминий + стекло, уличный');

-- ============================================
-- ПОЛЬЗОВАТЕЛИ
-- ============================================
INSERT INTO users (username, password_hash, full_name, "role", is_active) VALUES
('admin', '$2a$11$vBzJ4Yg6HQh8XjJ3kL5mN.8sP9qR2tU4wX6yZ8aB0cD2eF4gH6iJ8k', 'Администратор Системы', 'admin', true),
('storekeeper', '$2a$11$mN.8sP9qR2tU4wX6yZ8aB0cD2eF4gH6iJ8kL0mN2oP4qR6sT8uV0w', 'Иванов Петр Сергеевич', 'storekeeper', true),
('smirnov', '$2a$11$XyZ9AbC0dE1fG2hI3jK4lM5nO6pQ7rS8tU9vW0xY1zA2bC3dE4fG5h', 'Смирнов Алексей Дмитриевич', 'storekeeper', true);

-- ============================================
-- ОПЕРАЦИИ (30 записей)
-- ============================================
-- 1 = incoming (Приход), 2 = outgoing (Расход), 3 = transfer (Перемещение), 4 = writeoff (Списание)

INSERT INTO warehouse_operations (operation_type, product_id, quantity, unit_price, user_id, notes) VALUES
-- Приходы
(1, 1, 10, 75000.00, 1, 'Поступление ноутбуков Dell от ООО ТехноПоставка, счёт №452 от 15.01.2024'),
(1, 2, 8, 65000.00, 1, 'Поступление ноутбуков HP, счёт №453'),
(1, 4, 15, 15000.00, 2, 'Мониторы Dell для отдела разработки'),
(1, 8, 30, 6500.00, 2, 'Мыши Logitech для всех сотрудников'),
(1, 9, 25, 7500.00, 2, 'Клавиатуры Logitech MX Keys'),
(1, 13, 15, 8500.00, 1, 'Роутеры Wi-Fi 6 для нового офиса'),
(1, 18, 20, 11000.00, 1, 'SSD Samsung 980 Pro для апгрейда рабочих станций'),
(1, 23, 25, 2800.00, 2, 'Картриджи HP 305A Black, регулярная поставка'),
(1, 26, 200, 250.00, 2, 'Бумага A4 Svetocopy, оптовая закупка на квартал'),
(1, 35, 20, 4800.00, 2, 'Офисные стулья BRABIX Energy'),
(1, 40, 12, 7800.00, 2, 'Дрели-шуруповёрты Makita для склада'),
(1, 46, 10, 7800.00, 1, 'ИБП APC для серверной комнаты'),
(1, 49, 200, 75.00, 2, 'LED-лампы для замены освещения в коридорах'),

-- Расходы
(2, 1, 3, 85000.00, 2, 'Выдача ноутбуков Dell новым сотрудникам: Иванов, Петров, Сидоров'),
(2, 4, 5, 18500.00, 2, 'Мониторы для отдела бухгалтерии'),
(2, 8, 10, 8500.00, 2, 'Мыши Logitech для отдела маркетинга'),
(2, 18, 5, 12500.00, 2, 'SSD для апгрейда 5 рабочих станций бухгалтерии'),
(2, 23, 8, 3200.00, 2, 'Картриджи HP для принтера отдела кадров'),
(2, 26, 50, 280.00, 2, 'Бумага A4 для ежедневной печати отчётов'),
(2, 35, 8, 5500.00, 2, 'Стулья для новых сотрудников отдела продаж'),
(2, 40, 3, 8900.00, 2, 'Дрели для ремонтной бригады'),
(2, 46, 2, 8500.00, 2, 'ИБП для рабочих станций бухгалтерии'),

-- Перемещения
(3, 1, 2, 85000.00, 3, 'Перемещение 2 ноутбуков Dell на склад филиала в Санкт-Петербурге'),
(3, 13, 3, 9500.00, 3, 'Перемещение роутеров в конференц-зал на 3 этаже'),
(3, 26, 30, 280.00, 3, 'Перемещение бумаги на склад печати'),

-- Списания
(4, 4, 2, 18500.00, 1, 'Списание мониторов Dell: один разбит при транспортировке, второй — неисправен после грозы'),
(4, 26, 5, 280.00, 2, 'Списание бумаги A4: повреждена водой при затоплении склада'),
(4, 35, 1, 5500.00, 2, 'Списание стула: сломан механизм регулировки высоты'),
(4, 40, 1, 8900.00, 2, 'Списание дрели: сгорел двигатель при перегрузке'),
(4, 49, 10, 85.00, 2, 'Списание LED-ламп: брак производства, мерцают');

-- ============================================
-- Представления
-- ============================================
CREATE OR REPLACE VIEW v_stock_report AS
SELECT 
    p.id, p.name, p.article, c.name as category,
    p.quantity, p.min_quantity, p.unit,
    CASE 
        WHEN p.quantity <= p.min_quantity THEN 'Критический'
        WHEN p.quantity <= p.min_quantity * 1.5 THEN 'Низкий'
        ELSE 'Норма'
    END as stock_status,
    p.price, p.quantity * p.price as total_value
FROM products p
LEFT JOIN categories c ON p.category_id = c.id;

CREATE OR REPLACE VIEW v_product_movement AS
SELECT 
    p.name as product_name, p.article,
    SUM(CASE WHEN wo.operation_type = 1 THEN wo.quantity ELSE 0 END) as total_incoming,
    SUM(CASE WHEN wo.operation_type = 2 THEN wo.quantity ELSE 0 END) as total_outgoing,
    SUM(CASE WHEN wo.operation_type = 4 THEN wo.quantity ELSE 0 END) as total_writeoff,
    p.quantity as current_stock
FROM products p
LEFT JOIN warehouse_operations wo ON p.id = wo.product_id
GROUP BY p.id, p.name, p.article, p.quantity;

-- ============================================
-- Функции
-- ============================================
CREATE OR REPLACE FUNCTION get_current_stock(product_id_param INTEGER)
RETURNS INTEGER AS $$
DECLARE stock INTEGER;
BEGIN
    SELECT quantity INTO stock FROM products WHERE id = product_id_param;
    RETURN COALESCE(stock, 0);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_turnover_report(from_date DATE, to_date DATE)
RETURNS TABLE (product_name VARCHAR, category_name VARCHAR, incoming_qty BIGINT, outgoing_qty BIGINT, avg_price NUMERIC) AS $$
BEGIN
    RETURN QUERY
    SELECT p.name, c.name,
        COALESCE(SUM(CASE WHEN wo.operation_type = 1 THEN wo.quantity END), 0),
        COALESCE(SUM(CASE WHEN wo.operation_type = 2 THEN wo.quantity END), 0),
        COALESCE(AVG(wo.unit_price), 0)
    FROM products p
    LEFT JOIN categories c ON p.category_id = c.id
    LEFT JOIN warehouse_operations wo ON p.id = wo.product_id AND wo.operation_date BETWEEN from_date AND to_date
    GROUP BY p.id, p.name, c.name;
END;
$$ LANGUAGE plpgsql;

COMMIT;
