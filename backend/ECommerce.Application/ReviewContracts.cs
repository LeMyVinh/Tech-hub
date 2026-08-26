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
    DateTime CreatedAt,
    // SOFT DELETE: cho FE biết đánh giá này đã bị Admin xóa mềm hay chưa, để
    // hiển thị làm mờ + nút Khôi phục thay vì Duyệt/Từ chối.
    bool IsDeleted = false,
    DateTime? DeletedAt = null
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