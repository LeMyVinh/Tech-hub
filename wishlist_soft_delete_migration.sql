-- Chạy một lần trên database MySQL hiện tại.
ALTER TABLE `WishlistItem`
    ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `CreatedAt`,
    ADD COLUMN `DeletedAt` DATETIME NULL AFTER `IsDeleted`;

CREATE INDEX `idx_wishlist_active` ON `WishlistItem` (`UserId`, `IsDeleted`);
