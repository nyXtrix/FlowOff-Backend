namespace LMS.Application.Common.DTOs;

public record QueryRequest(int Page = 1, int PageSize = 10, string? SearchTerm = null, Dictionary<string, string>? Filters = null);
public record PaginatedResult<T>(List<T> Items, int TotalCount);