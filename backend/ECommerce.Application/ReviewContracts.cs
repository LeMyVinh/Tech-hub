namespace ECommerce.Application;

public sealed record CreateReviewRequest(
    int OrderItemId,
    int ProductId,
    int Rating,
    string? Comment,
    List<string>? ImageUrls
);

public sealed record ReviewResponse(
    int Id,
    int ProductId,
    string UserName,
    int Rating,
    string? Comment,
    List<string> ImageUrls,
    string Status,
    string? RejectReason,
    DateTime CreatedAt
);

public sealed record ReviewListResponse(
    List<ReviewResponse> Reviews,
    int TotalCount,
    int Page,
    int PageSize,
    double AverageRating
);

public sealed class ReviewException : Exception
{
    public ReviewException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
