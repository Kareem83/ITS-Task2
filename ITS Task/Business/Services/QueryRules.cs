using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
namespace ITS_Task.Business.Services;
public static class QueryRules
{
    public static void Validate(ListQueryDto query, params string[] sorts)
    {
        if (query.Page < 1 || query.Page > 1000000 || query.PageSize < 1 || query.PageSize > 100)
            throw new ApiException(400, "Page must be between 1 and 1000000 and pageSize between 1 and 100");
        if (!new[] { "asc", "desc" }.Contains(query.SortOrder, StringComparer.OrdinalIgnoreCase))
            throw new ApiException(400, "Invalid sortOrder");
        if (!sorts.Contains(query.SortBy, StringComparer.OrdinalIgnoreCase))
            throw new ApiException(400, "Invalid sortBy");
        if (query.Search?.Length > 150) throw new ApiException(400, "Search is too long");
    }
}
