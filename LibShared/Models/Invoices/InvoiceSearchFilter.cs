namespace LibShared.Models.Invoices;

/// <summary>
/// Optional filters for searching invoices. All properties are optional.
/// </summary>
public class InvoiceSearchFilter
{
    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
