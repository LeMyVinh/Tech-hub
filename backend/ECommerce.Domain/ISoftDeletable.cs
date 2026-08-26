using System;

namespace ECommerce.Domain;

// Đánh dấu entity hỗ trợ soft delete. Khi 1 entity implement interface này:
//   - IsDeleted=true nghĩa là đã "xóa mềm" (vẫn còn trong DB để khôi phục/audit)
//   - DeletedAt lưu thời điểm xóa (UTC) để truy vết
//
// EF Core tự lọc các bản ghi IsDeleted=true khỏi mọi query mặc định
// thông qua Global Query Filter cấu hình trong AppDbContext.
// Khi cần truy cập bản ghi đã xóa, dùng IgnoreQueryFilters().
//
// Các bảng KHÔNG nên implement:
//   - Order, OrderItem, OrderStatusLog, Payment (lịch sử giao dịch - cần xóa cứng
//     hoặc giữ nguyên để toàn vẹn dữ liệu kế toán),
//   - Cart, CartItem, WishlistItem (dữ liệu tạm),
//   - RefreshToken, EmailVerificationToken, PasswordResetToken (đã có cơ chế tự
//     cleanup theo ExpiredAt).
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }
}
