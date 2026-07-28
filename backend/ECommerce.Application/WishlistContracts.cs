namespace ECommerce.Application;

public sealed record WishlistResponse(
    int Id,
    List<WishlistItemResponse> Items
);

public sealed record WishlistItemResponse(
    int Id,
    int ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    decimal MinPrice,
    decimal MaxPrice,
    DateTime CreatedAt
);

public sealed class WishlistException : Exception
{
    public WishlistException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
