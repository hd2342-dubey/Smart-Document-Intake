namespace LibShared.Models.Invoices;

/// <summary>
/// Invoice entity. Table mapping is configured with Fluent API
/// (see InvoiceConfiguration in the API project).
/// </summary>
public class Invoice
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string Supplier { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? SourceFileName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<InvoiceItem> Items { get; set; } = [];
}
