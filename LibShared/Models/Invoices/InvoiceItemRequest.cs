using System.ComponentModel.DataAnnotations;

namespace LibShared.Models.Invoices;

public class InvoiceItemRequest
{
    [Required(ErrorMessage = "Item description is required.")]
    [MaxLength(300, ErrorMessage = "Item description cannot exceed 300 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue, ErrorMessage = "Item quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Item unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }
}
