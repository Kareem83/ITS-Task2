using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Services;
namespace ITS_Task.Tests;
[Trait("Category", "Unit")]
public class QueryRulesTests
{
    [Theory] [InlineData(0, 10)] [InlineData(-1, 10)] [InlineData(1, 0)] [InlineData(1, 101)] [InlineData(1000001, 10)]
    public void InvalidPaginationIsRejected(int page, int size) => Assert.Throws<ApiException>(() => QueryRules.Validate(new() { Page = page, PageSize = size }, "id"));
    [Fact] public void SortIsCaseInsensitive() => QueryRules.Validate(new() { SortBy = "NAME", SortOrder = "DESC" }, "name");
    [Fact] public void UnknownSortRejected() => Assert.Throws<ApiException>(() => QueryRules.Validate(new() { SortBy = "password" }, "id"));
    [Fact] public void UnknownDirectionRejected() => Assert.Throws<ApiException>(() => QueryRules.Validate(new() { SortOrder = "bad" }, "id"));
    [Fact] public void OversizedSearchRejected() => Assert.Throws<ApiException>(() => QueryRules.Validate(new() { Search = new string('x', 151) }, "id"));
    [Theory] [InlineData(0, 0)] [InlineData(1, 1)] [InlineData(11, 2)]
    public void PageCountRoundsUp(int count, int pages) => Assert.Equal(pages, new PagedResultDto<int> { Page = 1, PageSize = 10, TotalCount = count }.TotalPages);
}
