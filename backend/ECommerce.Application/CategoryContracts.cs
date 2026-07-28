namespace ECommerce.Application;

public sealed record CreateCategoryRequest(string? Name, int? ParentId);

public sealed record UpdateCategoryRequest(string? Name, int? ParentId);

public sealed record CategoryResponse(int Id, string Name, int? ParentId, string? ParentName, bool IsActive);

public class CatalogException : Exception
{
    public CatalogException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
