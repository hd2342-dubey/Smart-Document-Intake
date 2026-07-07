using LibShared.Models.Invoices;

namespace IntakeServer.Repositories.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int invoiceId);
    Task<Invoice?> GetByNumberAndSupplierAsync(string invoiceNumber, string supplier);
    Task<Invoice?> GetByNumberAsync(string invoiceNumber);
    Task<(List<Invoice> Invoices, int TotalCount)> SearchAsync(InvoiceSearchFilter filter);
    Task<Invoice> AddAsync(Invoice invoice);
    Task DeleteAsync(Invoice invoice);
    Task<DashboardResponse> GetDashboardStatsAsync();
}
