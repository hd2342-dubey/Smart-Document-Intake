using System.ComponentModel.DataAnnotations;

namespace LibShared.Models.Invoices;

/// <summary>
/// Structured invoice data extracted from an uploaded document.
/// </summary>
public class InvoiceRequest
{
    [Required(ErrorMessage = "Invoice number is required.")]
    [MaxLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters.")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Supplier is required.")]
    [MaxLength(200, ErrorMessage = "Supplier cannot exceed 200 characters.")]
    public string Supplier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Invoice date is required.")]
    public DateOnly InvoiceDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Total cannot be negative.")]
    public decimal TotalAmount { get; set; }

    public List<InvoiceItemRequest> Items { get; set; } = [];
}
