namespace NewsAPI.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
