using LMS.Application.Common.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Extension;

public static class QueryableExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(this IQueryable<T> query, QueryRequest request)
    {
        var totalCount = await query.CountAsync();
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

        return new PaginatedResult<T>(items, totalCount);
    }
}