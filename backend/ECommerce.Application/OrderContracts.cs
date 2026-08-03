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
    string Status,
    string ShippingMethod,
    string? CancelReason,
    List<OrderItemResponse> Items,
    DateTime CreatedAt,
    string? PaymentUrl = null
);

public sealed record OrderItemResponse(
    int Id,
    int VariantId,
    string ProductName,
    string VariantName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
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
    string Status,
    string ShippingMethod,
    string? CancelReason,
    List<OrderItemResponse> Items,
    AddressResponse Address,
    PaymentResponse? Payment,
    List<OrderStatusLogResponse> StatusHistory,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record AddressResponse(
    int Id,
    string RecipientName,
    string Phone,
    string DetailAddress,
    string Ward,
    string District,
    string Province,
    bool IsDefault = false
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
