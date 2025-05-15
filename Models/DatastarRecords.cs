
namespace dotnet_html_sortable_table.Models;

public record PaginationRecord(int size);
public record PaginationData(int offset, int forwardoffset, int backwardoffset, int count);
