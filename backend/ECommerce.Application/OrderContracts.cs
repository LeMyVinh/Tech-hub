namespace ECommerce.Application;

public sealed record CreateOrderRequest(
    int AddressId,
    string ShippingMethod,
    string PaymentMethod,
    string? Note
);

public sealed record UpdateOrderStatusRequest(
    string Status,
    string? Note
);

public sealed record OrderResponse(
    int Id,
    string OrderCode,
    decimal TotalAmount,
    // BUG FIX: expose phí vận chuyển thực tế đã được backend tính và cộng vào TotalAmount,
    // để FE/Admin thấy rõ khoản này thay vì chỉ có một con số tổng gộp.
    decimal ShippingFee,
    string Status,
    string ShippingMethod,
    string? CancelReason,
    List<OrderItemResponse> Items,
    DateTime CreatedAt,
    string? PaymentUrl = null,
    string? CustomerName = null
);

public sealed record OrderItemResponse(
    int Id,
    int VariantId,
    int ProductId,
    string ProductName,
    string VariantName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    // FIX: trước đây frontend tự "nhớ" item nào đã đánh giá trong 1 signal cục bộ
    // (order-detail.component.ts), nên chỉ đúng trong phiên hiện tại; F5 lại trang là
    // sai trạng thái. Giờ backend trả thẳng trạng thái thật dựa trên bảng Review.
    bool HasReviewed = false
);

public sealed record OrderListResponse(
    List<OrderResponse> Orders,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record OrderDetailResponse(
    int Id,
    string OrderCode,
    decimal TotalAmount,
    decimal ShippingFee,
    string Status,
    string ShippingMethod,
    string? CancelReason,
    List<OrderItemResponse> Items,
    AddressResponse Address,
    PaymentResponse? Payment,
    List<OrderStatusLogResponse> StatusHistory,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CustomerName = null
);

public sealed record AddressResponse(
    int Id,
    string RecipientName,
    string Phone,
    string DetailAddress,
    string Ward,
    string District,
    string Province,
    bool IsDefault = false,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
);
public sealed record PaymentResponse(
    int Id,
    string Method,
    decimal Amount,
    string Status,
    string? TransactionCode,
    DateTime? PaidAt,
    string? PaymentUrl = null
);

public sealed record OrderStatusLogResponse(
    string Status,
    string? Note,
    DateTime ChangedAt
);

public sealed class OrderException : Exception
{
    public OrderException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
