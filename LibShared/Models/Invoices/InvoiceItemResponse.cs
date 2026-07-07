namespace LibShared.Models.Invoices;

public class InvoiceItemResponse
{
    public int InvoiceItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
