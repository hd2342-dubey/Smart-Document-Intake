namespace LibShared.Models.Invoices;

/// <summary>
/// Result of comparing an uploaded invoice document against the stored invoice
/// with the same invoice number.
/// </summary>
public class CompareInvoiceResponse
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public bool MatchFound { get; set; }
    public bool IsIdentical { get; set; }
    public List<InvoiceFieldDifference> Differences { get; set; } = [];
}

public class InvoiceFieldDifference
{
    public string FieldName { get; set; } = string.Empty;
    public string? StoredValue { get; set; }
    public string? UploadedValue { get; set; }
}
