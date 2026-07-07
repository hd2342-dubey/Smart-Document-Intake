namespace LibShared.Models.Invoices;

public class InvoiceResponse
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<InvoiceItemResponse> Items { get; set; } = [];
}
