using IntakeServer.Services.Invoices;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace IntakeServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    private readonly IInvoiceService _invoiceService = invoiceService;

    /// <summary>
    /// Uploads an invoice document (.json, .csv, or .txt), extracts structured data,
    /// validates it, detects duplicates, and stores the invoice.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceResponse>> UploadInvoice(IFormFile file)
    {
        InvoiceResponse created = await _invoiceService.CreateFromDocumentAsync(file);
        return CreatedAtAction(nameof(GetInvoice), new { id = created.InvoiceId }, created);
    }

    /// <summary>
    /// Gets a single invoice with its line items.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(int id)
    {
        return await _invoiceService.GetByIdAsync(id);
    }

    /// <summary>
    /// Searches invoices with optional filters and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchInvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchInvoiceResponse>> SearchInvoices([FromQuery] InvoiceSearchFilter filter)
    {
        return await _invoiceService.SearchAsync(filter);
    }

    /// <summary>
    /// Deletes an invoice and its line items.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Returns aggregate invoice statistics for the dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> GetDashboard()
    {
        return await _invoiceService.GetDashboardAsync();
    }

    /// <summary>
    /// Compares an uploaded invoice document against the stored invoice
    /// with the same invoice number, without saving anything.
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(CompareInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompareInvoiceResponse>> CompareInvoice(IFormFile file)
    {
        return await _invoiceService.CompareAsync(file);
    }
}
