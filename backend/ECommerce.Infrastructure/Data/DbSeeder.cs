using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Database.CanConnectAsync())
        {
            // Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var laptopCat = new Category { Name = "Laptop", IsActive = true };
                var componentCat = new Category { Name = "Linh kiện máy tính", IsActive = true };
                var peripheralCat = new Category { Name = "Thiết bị ngoại vi", IsActive = true };
                var accessoryCat = new Category { Name = "Phụ kiện máy tính", IsActive = true };

                await context.Categories.AddRangeAsync(laptopCat, componentCat, peripheralCat, accessoryCat);
                await context.SaveChangesAsync();

                var cpuCat = new Category { Name = "CPU", ParentId = componentCat.Id, IsActive = true };
                var ramCat = new Category { Name = "RAM", ParentId = componentCat.Id, IsActive = true };
                var vgaCat = new Category { Name = "VGA - Card màn hình", ParentId = componentCat.Id, IsActive = true };
                var monitorCat = new Category { Name = "Màn hình", ParentId = peripheralCat.Id, IsActive = true };
                var keyboardCat = new Category { Name = "Bàn phím", ParentId = peripheralCat.Id, IsActive = true };
                var mouseCat = new Category { Name = "Chuột", ParentId = peripheralCat.Id, IsActive = true };

                await context.Categories.AddRangeAsync(cpuCat, ramCat, vgaCat, monitorCat, keyboardCat, mouseCat);
                await context.SaveChangesAsync();
            }

            // Seed Brands
            if (!await context.Brands.AnyAsync())
            {
                var brands = new List<Brand>
                {
                    new Brand { Name = "Dell", LogoUrl = "https://cdn.techhub.vn/brands/dell.png", IsActive = true },
                    new Brand { Name = "ASUS", LogoUrl = "https://cdn.techhub.vn/brands/asus.png", IsActive = true },
                    new Brand { Name = "MSI", LogoUrl = "https://cdn.techhub.vn/brands/msi.png", IsActive = true },
                    new Brand { Name = "Logitech", LogoUrl = "https://cdn.techhub.vn/brands/logitech.png", IsActive = true },
                    new Brand { Name = "Kingston", LogoUrl = "https://cdn.techhub.vn/brands/kingston.png", IsActive = true }
                };

                await context.Brands.AddRangeAsync(brands);
                await context.SaveChangesAsync();
            }

            // Seed Products
            if (!await context.Products.AnyAsync())
            {
                var dellBrand = await context.Brands.FirstAsync(b => b.Name == "Dell");
                var asusBrand = await context.Brands.FirstAsync(b => b.Name == "ASUS");
                var logiBrand = await context.Brands.FirstAsync(b => b.Name == "Logitech");

                var laptopCategory = await context.Categories.FirstAsync(c => c.Name == "Laptop");
                var keyboardCategory = await context.Categories.FirstAsync(c => c.Name == "Bàn phím");

                var p1 = new Product
                {
                    Name = "Laptop Dell XPS 13",
                    Description = "Laptop mỏng nhẹ cao cấp Dell XPS 13 màn hình OLED sắc nét.",
                    CategoryId = laptopCategory.Id,
                    BrandId = dellBrand.Id,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    ProductVariants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            VariantName = "16GB RAM - 512GB SSD - Bạc",
                            Sku = "DELL-XPS13-16-512-SLV",
                            Price = 32990000,
                            StockQuantity = 10,
                            CreatedAt = DateTime.UtcNow
                        },
                        new ProductVariant
                        {
                            VariantName = "32GB RAM - 1TB SSD - Đen",
                            Sku = "DELL-XPS13-32-1TB-BLK",
                            Price = 39990000,
                            StockQuantity = 5,
                            CreatedAt = DateTime.UtcNow
                        }
                    },
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://cdn.techhub.vn/products/dell-xps-13-1.jpg", IsPrimary = true },
                        new ProductImage { ImageUrl = "https://cdn.techhub.vn/products/dell-xps-13-2.jpg", IsPrimary = false }
                    }
                };

                var p2 = new Product
                {
                    Name = "Laptop ASUS ROG Strix G16",
                    Description = "Laptop gaming hiệu năng cực khủng với Intel Core i9 và RTX 4080.",
                    CategoryId = laptopCategory.Id,
                    BrandId = asusBrand.Id,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    ProductVariants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            VariantName = "32GB RAM - 1TB SSD - RTX 4080",
                            Sku = "ASUS-ROG-G16-32-1TB-4080",
                            Price = 52990000,
                            StockQuantity = 8,
                            CreatedAt = DateTime.UtcNow
                        }
                    },
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://cdn.techhub.vn/products/asus-rog-g16-1.jpg", IsPrimary = true }
                    }
                };

                var p3 = new Product
                {
                    Name = "Bàn phím cơ Logitech G Pro X",
                    Description = "Bàn phím cơ chuyên nghiệp dành cho gamer eSports.",
                    CategoryId = keyboardCategory.Id,
                    BrandId = logiBrand.Id,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    ProductVariants = new List<ProductVariant>
                    {
                        new ProductVariant
                        {
                            VariantName = "GX Blue Clicky Switch",
                            Sku = "LOGI-GPROX-BLUE",
                            Price = 2890000,
                            StockQuantity = 20,
                            CreatedAt = DateTime.UtcNow
                        },
                        new ProductVariant
                        {
                            VariantName = "GX Red Linear Switch",
                            Sku = "LOGI-GPROX-RED",
                            Price = 2890000,
                            StockQuantity = 15,
                            CreatedAt = DateTime.UtcNow
                        }
                    },
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://cdn.techhub.vn/products/logi-gprox-1.jpg", IsPrimary = true }
                    }
                };

                await context.Products.AddRangeAsync(p1, p2, p3);
                await context.SaveChangesAsync();
            }
        }
    }
}
