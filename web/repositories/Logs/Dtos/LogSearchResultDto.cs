namespace web.Repositories.Logs.Dtos;

public class LogSearchResultDto
{
    public List<LogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
