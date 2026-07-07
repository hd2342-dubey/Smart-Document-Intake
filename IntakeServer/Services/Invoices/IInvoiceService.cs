using LibShared.Models.Invoices;

namespace IntakeServer.Services.Invoices;

public interface IInvoiceService
{
    Task<InvoiceResponse> CreateFromDocumentAsync(IFormFile file);
    Task<InvoiceResponse> GetByIdAsync(int invoiceId);
    Task<SearchInvoiceResponse> SearchAsync(InvoiceSearchFilter filter);
    Task DeleteAsync(int invoiceId);
    Task<DashboardResponse> GetDashboardAsync();
    Task<CompareInvoiceResponse> CompareAsync(IFormFile file);
}
