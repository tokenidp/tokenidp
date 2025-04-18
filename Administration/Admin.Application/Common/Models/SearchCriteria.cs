using Identity.Domain.Enums;

namespace Identity.Application.Common.Models;

public class SearchCriteria
{
    public string ColumnName { get; set; }
    public string Value { get; set; }
    public SearchColumnType ColumnType { get; set; }
}
