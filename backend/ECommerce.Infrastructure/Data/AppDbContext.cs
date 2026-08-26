using System;
using System.Collections.Generic;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ECommerce.Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatusLog> OrderStatusLogs { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ReviewImage> ReviewImages { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WishlistItem> WishlistItems { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        // === FIX: MySQL/Pomelo trả về DateTime với Kind=Unspecified, khiến khi
        // serialize sang JSON chuỗi thời gian không có hậu tố 'Z' (UTC marker).
        // Angular DatePipe khi đó hiểu nhầm đây là giờ local của trình duyệt
        // và hiển thị nguyên văn -> lệch 7 tiếng so với giờ VN thực tế (vì toàn
        // bộ code backend đang lưu bằng DateTime.UtcNow).
        // Đoạn dưới đây ép mọi cột DateTime/DateTime? khi đọc từ DB lên đều
        // được gắn nhãn Kind=Utc, áp dụng tự động cho TẤT CẢ entity/property
        // mà không cần khai báo lặp lại ở từng bảng.
        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var utcNullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcNullableDateTimeConverter);
                }
            }
        }

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Address");

            entity.HasIndex(e => e.UserId, "idx_address_user");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.DetailAddress).HasMaxLength(255);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.RecipientName).HasMaxLength(150);
            entity.Property(e => e.Ward).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_address_user");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Brand");

            entity.HasIndex(e => e.Name, "Name").IsUnique();

            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Cart");

            entity.HasIndex(e => e.UserId, "UserId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .HasConstraintName("fk_cart_user");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("CartItem");

            entity.HasIndex(e => e.ProductVariantId, "fk_cartitem_variant");

            entity.HasIndex(e => new { e.CartId, e.ProductVariantId }, "uq_cartitem_cart_variant").IsUnique();

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("fk_cartitem_cart");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cartitem_variant");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Category");

            entity.HasIndex(e => e.ParentId, "fk_category_parent");

            entity.HasIndex(e => new { e.Name, e.ParentId }, "uq_category_name_parent").IsUnique();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("fk_category_parent");
        });
        modelBuilder.Entity<EmailVerificationToken>(entity =>
{
    entity.HasKey(e => e.Id).HasName("PRIMARY");

    entity.ToTable("EmailVerificationToken");

    // OTP: không còn unique toàn hệ thống, chỉ unique/tra cứu theo (UserId, Token)
    // vì mã OTP 6 số có thể trùng giữa nhiều user khác nhau.
    entity.HasIndex(e => new { e.UserId, e.Token }, "idx_emailverify_user_token");

    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        .HasColumnType("datetime");
    entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
    entity.Property(e => e.Token).HasMaxLength(10);

    entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
        .HasForeignKey(d => d.UserId)
        .HasConstraintName("fk_emailverify_user");
});


        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Order");

            entity.HasIndex(e => e.AddressId, "fk_order_address");

            entity.HasIndex(e => e.CreatedAt, "idx_order_createdat");

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_order_user_status");

            entity.Property(e => e.CancelReason).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ShippingMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Standard'");
            // BUG FIX: cột mới lưu phí vận chuyển thực tế được backend tính (không tin
            // số tiền FE gửi lên) — xem OrderService.ResolveShippingFee.
            entity.Property(e => e.ShippingFee)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("'0.00'");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Confirmed','Processing','Shipping','Delivered','Cancelled')");
            entity.Property(e => e.TotalAmount).HasPrecision(15, 2);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            // FIX (đơn hàng bị đổi theo địa chỉ mới): mapping cho các cột snapshot
            // địa chỉ mới thêm vào Order. Các cột này được ghi 1 lần duy nhất khi tạo
            // đơn (OrderService.CreateOrderAsync) và không phụ thuộc vào bảng Address
            // nữa khi hiển thị chi tiết đơn hàng, nên nếu khách sửa lại Address gốc
            // sau này, đơn hàng cũ vẫn hiển thị đúng địa chỉ đã dùng để giao hàng.
            entity.Property(e => e.RecipientName).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Ward).HasMaxLength(100);
            entity.Property(e => e.DetailAddress).HasMaxLength(255);

            entity.HasOne(d => d.Address).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_address");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_user");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.OrderId, "fk_orderitem_order");

            entity.HasIndex(e => e.ProductVariantId, "idx_orderitem_variant");

            entity.Property(e => e.UnitPrice).HasPrecision(15, 2);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_orderitem_order");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orderitem_variant");
        });

        modelBuilder.Entity<OrderStatusLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("OrderStatusLog");

            entity.HasIndex(e => e.OrderId, "fk_statuslog_order");

            entity.HasIndex(e => e.ChangedBy, "fk_statuslog_user");

            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.OrderStatusLogs)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_statuslog_user");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusLogs)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_statuslog_order");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("PasswordResetToken");

            entity.HasIndex(e => e.Token, "Token").IsUnique();

            entity.HasIndex(e => e.UserId, "fk_pwdreset_user");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_pwdreset_user");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.OrderId, "OrderId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Method).HasColumnType("enum('COD','VNPay')");
            entity.Property(e => e.PaidAt).HasColumnType("datetime");
            // BUG FIX: thêm trạng thái 'Refunded' — khi Admin hủy đơn đã thanh toán VNPay
            // thành công, hệ thống đánh dấu cần hoàn tiền thay vì im lặng bỏ qua.
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Success','Failed','Refunded')");
            entity.Property(e => e.TransactionCode).HasMaxLength(100);
             
             entity.Property(e => e.TransactionDate).HasMaxLength(14);
             entity.Property(e => e.RefundResponseId).HasMaxLength(50);

            entity.HasOne(d => d.Order).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.OrderId)
                .HasConstraintName("fk_payment_order");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Product");

            entity.HasIndex(e => e.BrandId, "fk_product_brand");

            entity.HasIndex(e => e.CategoryId, "fk_product_category");

            entity.HasIndex(e => e.Name, "ftx_product_name").HasAnnotation("MySql:FullTextIndex", true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Inactive')");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_brand");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_category");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ProductImage");

            entity.HasIndex(e => e.ProductId, "fk_image_product");

            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_image_product");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ProductVariant");

            entity.HasIndex(e => e.Sku, "SKU").IsUnique();

            entity.HasIndex(e => e.ProductId, "fk_variant_product");

            entity.HasIndex(e => e.Price, "idx_variant_price");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Price).HasPrecision(15, 2);
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .HasColumnName("SKU");
            entity.Property(e => e.VariantName).HasMaxLength(255);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_variant_product");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("RefreshToken");

            entity.HasIndex(e => e.UserId, "fk_refreshtoken_user");

            entity.HasIndex(e => e.Token, "idx_refreshtoken_token").HasAnnotation("MySql:IndexPrefixLength", new[] { 191 });

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_refreshtoken_user");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Review");

            entity.HasIndex(e => e.OrderItemId, "OrderItemId").IsUnique();

            entity.HasIndex(e => e.UserId, "fk_review_user");

            entity.HasIndex(e => new { e.ProductId, e.Status }, "idx_review_product_status");

            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.RejectReason).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Approved','Rejected')");

            // SOFT DELETE: cho phép Admin "xóa" đánh giá (làm mờ khỏi trang chi tiết
            // sản phẩm) mà vẫn khôi phục được sau này, giống pattern Brand/Category/
            // Product/User. Global Query Filter tự động ẩn review đã xóa khỏi mọi
            // truy vấn mặc định (kể cả GetAverageRatingAsync, GetByProductIdAsync...),
            // trang quản trị dùng IgnoreQueryFilters() để vẫn thấy được nhằm khôi phục.
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(d => d.OrderItem).WithOne(p => p.Review)
                .HasForeignKey<Review>(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_orderitem");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_product");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_user");
        });

        modelBuilder.Entity<ReviewImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ReviewImage");

            entity.HasIndex(e => e.ReviewId, "fk_reviewimage_review");

            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewImages)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("fk_reviewimage_review");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Role");

            entity.HasIndex(e => e.Name, "Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.RoleId, "fk_user_role");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.EmailVerified)
                .IsRequired()
                .HasDefaultValueSql("'0'");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            // SOFT DELETE: không dùng HasDefaultValueSql — EF Core có thể không ghi
            // IsDeleted=true khi update nếu cấu hình sentinel/default sai.
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            // Global query filter: mọi truy vấn User (kể cả qua navigation như
            // Order.User, Review.User...) tự động loại user đã xóa mềm, không cần
            // sửa từng chỗ gọi GetByIdAsync/GetAllAsync. Muốn lấy cả user đã xóa
            // (vd. audit) thì gọi .IgnoreQueryFilters() ở nơi cần.
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("WishlistItem");

            entity.HasIndex(e => e.ProductId, "fk_wishlist_product");

            entity.HasIndex(e => new { e.UserId, e.ProductId }, "uq_wishlist_user_product").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_wishlist_product");

            entity.HasOne(d => d.User).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_wishlist_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
