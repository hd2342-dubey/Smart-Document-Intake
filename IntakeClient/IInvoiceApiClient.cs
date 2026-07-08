using LibShared.Models.Invoices;

namespace IntakeClient;

public interface IInvoiceApiClient
{
    Task<SearchInvoiceResponse?> SearchAsync(string? invoiceNumber, string? supplier, DateOnly? dateFrom, DateOnly? dateTo, int page, int pageSize);
    Task<InvoiceResponse?> GetByIdAsync(int id);
    Task<DashboardResponse?> GetDashboardAsync();
    Task<(InvoiceResponse? Invoice, string? Error)> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<(CompareInvoiceResponse? Result, string? Error)> CompareAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAsync(int id);
}