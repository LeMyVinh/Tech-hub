-- =============================================================
-- SOFT DELETE MIGRATION — TechHub (MySQL 8.x)
-- Áp dụng cho 8 bảng: User, Address, Brand, Category, Product,
-- ProductVariant, ProductImage, Review
--
-- Chạy trong MySQL Workbench / DBeaver / mysql CLI:
--   mysql -u root -p techhub_db < soft_delete_migration.sql
-- hoặc paste từngng dòng vào tab Query.
--
-- An toàn: script chỉ thêm cột + index, KHÔNG xóa/sửa dữ liệu cũ.
-- Mọi bản ghi hiện có sẽ tự được gán IsDeleted=0 (active).
-- =============================================================

USE techhub_db;

-- -------------------------------------------------------------
-- 1. User
-- -------------------------------------------------------------
-- Đã bỏ cờ IsActive trên entity User: chỉ dùng IsDeleted để quản lý trạng thái
-- (active = IsDeleted=false, soft delete = IsDeleted=true, restore = IsDeleted=false).
ALTER TABLE `User`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `EmailVerified`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 2. Address
-- -------------------------------------------------------------
ALTER TABLE `Address`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `CreatedAt`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 3. Brand
-- -------------------------------------------------------------
ALTER TABLE `Brand`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `IsActive`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 4. Category
-- -------------------------------------------------------------
ALTER TABLE `Category`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `IsActive`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 5. Product
-- -------------------------------------------------------------
ALTER TABLE `Product`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `Status`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 6. ProductVariant
-- -------------------------------------------------------------
ALTER TABLE `ProductVariant`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `StockQuantity`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 7. ProductImage
-- -------------------------------------------------------------
ALTER TABLE `ProductImage`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `IsPrimary`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- -------------------------------------------------------------
-- 8. Review
-- -------------------------------------------------------------
ALTER TABLE `Review`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `Status`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

-- =============================================================
-- INDEX phụ (tùy chọn) — tăng tốc truy vấn "chỉ lấy active"
-- EF Core Global Query Filter sẽ sinh câu SQL có
--   WHERE IsDeleted = 0
-- nên thêm index hỗn hợp giúp sản phẩm/brand/category load nhanh hơn.
-- =============================================================

ALTER TABLE `Product`     ADD INDEX `idx_product_active`     (`IsDeleted`, `Status`);
ALTER TABLE `Brand`       ADD INDEX `idx_brand_active`       (`IsDeleted`, `IsActive`);
ALTER TABLE `Category`    ADD INDEX `idx_category_active`    (`IsDeleted`, `IsActive`);
-- User: bỏ cờ IsActive, chỉ còn IsDeleted -> index chỉ phụ thuộc IsDeleted.
ALTER TABLE `User`        ADD INDEX `idx_user_active`        (`IsDeleted`);
ALTER TABLE `Review`      ADD INDEX `idx_review_active`      (`IsDeleted`, `Status`);

-- -------------------------------------------------------------
-- 9. (Tùy chọn) Drop cột IsActive cũ trên bảng User
-- Bỏ comment đoạn dưới và chạy SAU KHI đã chạy đoạn ALTER TABLE phía trên,
-- SAU KHI deploy code mới (code mới không còn tham chiếu IsActive nữa).
-- Nếu chưa chắc chắn code đã sẵn sàng, giữ cột IsActive thêm một thời gian
-- để rollback an toàn.
-- -------------------------------------------------------------
-- ALTER TABLE `User` DROP COLUMN `IsActive`;

-- =============================================================
-- KIỂM TRA SAU KHI CHẠY
-- =============================================================
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'techhub_db'
  AND COLUMN_NAME IN ('IsDeleted', 'DeletedAt')
ORDER BY TABLE_NAME, COLUMN_NAME;
