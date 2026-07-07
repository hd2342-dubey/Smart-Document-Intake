namespace LibShared.Models.Invoices;

/// <summary>
/// Paginated invoice search result.
/// </summary>
public class SearchInvoiceResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<InvoiceResponse> Invoices { get; set; } = [];
}
