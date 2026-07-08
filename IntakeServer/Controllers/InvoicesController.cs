using IntakeServer.Services.Invoices;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace IntakeServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    private readonly IInvoiceService _invoiceService = invoiceService;

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(int id)
    {
        return await _invoiceService.GetByIdAsync(id);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SearchInvoiceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchInvoiceResponse>> SearchInvoices([FromQuery] InvoiceSearchFilter filter)
    {
        return await _invoiceService.SearchAsync(filter);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> GetDashboard()
    {
        return await _invoiceService.GetDashboardAsync();
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceResponse>> UploadInvoice(IFormFile file)
    {
        InvoiceResponse created = await _invoiceService.CreateFromDocumentAsync(file);
        return CreatedAtAction(nameof(GetInvoice), new { id = created.InvoiceId }, created);
    }

    [HttpPost("compare")]
    [ProducesResponseType(typeof(CompareInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompareInvoiceResponse>> CompareInvoice(IFormFile file)
    {
        return await _invoiceService.CompareAsync(file);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return NoContent();
    }
}
