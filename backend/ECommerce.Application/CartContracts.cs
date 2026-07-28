namespace ECommerce.Application;

public sealed record AddToCartRequest(int VariantId, int Quantity);
public sealed record UpdateCartItemRequest(int Quantity);

public sealed record CartResponse(
    int Id,
    List<CartItemResponse> Items,
    decimal TotalAmount
);

public sealed record CartItemResponse(
    int Id,
    int VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    decimal Price,
    int Quantity,
    int StockQuantity,
    string? ImageUrl,
    decimal Subtotal
);

public sealed class CartException : Exception
{
    public CartException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
