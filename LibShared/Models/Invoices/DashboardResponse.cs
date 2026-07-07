namespace LibShared.Models.Invoices;

/// <summary>
/// Invoice dashboard statistics.
/// </summary>
public class DashboardResponse
{
    public int TotalInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageInvoiceAmount { get; set; }
    public int SupplierCount { get; set; }
    public List<SupplierTotal> TopSuppliers { get; set; } = [];
    public List<InvoiceResponse> RecentInvoices { get; set; } = [];
}

public class SupplierTotal
{
    public string Supplier { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
}
