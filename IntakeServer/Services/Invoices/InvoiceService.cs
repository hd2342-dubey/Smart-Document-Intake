using System.Globalization;
using IntakeServer.Exceptions;
using IntakeServer.Repositories.Invoices;
using LibShared.Models.Invoices;

namespace IntakeServer.Services.Invoices;

/// <summary>
/// Business logic for invoice intake: parsing, validation, duplicate detection,
/// persistence, search, dashboard statistics, and document comparison.
/// </summary>
public class InvoiceService(IInvoiceRepository repository, ILogger<InvoiceService> logger) : IInvoiceService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IInvoiceRepository _repository = repository;
    private readonly ILogger<InvoiceService> _logger = logger;

    public async Task<InvoiceResponse> CreateFromDocumentAsync(IFormFile file)
    {
        ValidateFile(file);

        InvoiceRequest request = await InvoiceDocumentParser.ParseAsync(file);
        ValidateRequest(request);

        // Business rule: an invoice number must be unique per supplier.
        Invoice? existing = await _repository.GetByNumberAndSupplierAsync(request.InvoiceNumber, request.Supplier);
        if (existing is not null)
            throw new DuplicateInvoiceException(request.InvoiceNumber, request.Supplier);

        var invoice = new Invoice
        {
            InvoiceNumber = request.InvoiceNumber.Trim(),
            Supplier = request.Supplier.Trim(),
            InvoiceDate = request.InvoiceDate,
            SourceFileName = file.FileName,
            CreatedAtUtc = DateTime.UtcNow,
            Items = request.Items.Select(i => new InvoiceItem
            {
                Description = i.Description.Trim(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = Math.Round(i.Quantity * i.UnitPrice, 2)
            }).ToList()
        };

        // The stored total is always computed from line items so it can never drift.
        invoice.TotalAmount = invoice.Items.Sum(i => i.LineTotal);

        Invoice created = await _repository.AddAsync(invoice);
        _logger.LogInformation("Invoice {InvoiceNumber} from {Supplier} created with id {InvoiceId}",
            created.InvoiceNumber, created.Supplier, created.InvoiceId);

        return MapToResponse(created);
    }

    public async Task<InvoiceResponse> GetByIdAsync(int invoiceId)
    {
        Invoice invoice = await _repository.GetByIdAsync(invoiceId)
            ?? throw new InvoiceNotFoundException(invoiceId);

        return MapToResponse(invoice);
    }

    public async Task<SearchInvoiceResponse> SearchAsync(InvoiceSearchFilter filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

        (List<Invoice> invoices, int totalCount) = await _repository.SearchAsync(filter);

        return new SearchInvoiceResponse
        {
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            Invoices = invoices.Select(MapToResponse).ToList()
        };
    }

    public async Task DeleteAsync(int invoiceId)
    {
        Invoice invoice = await _repository.GetByIdAsync(invoiceId)
            ?? throw new InvoiceNotFoundException(invoiceId);

        await _repository.DeleteAsync(invoice);
        _logger.LogInformation("Invoice {InvoiceId} deleted", invoiceId);
    }

    public Task<DashboardResponse> GetDashboardAsync()
    {
        return _repository.GetDashboardStatsAsync();
    }

    public async Task<CompareInvoiceResponse> CompareAsync(IFormFile file)
    {
        ValidateFile(file);

        InvoiceRequest uploaded = await InvoiceDocumentParser.ParseAsync(file);
        ValidateRequest(uploaded);

        Invoice? stored = await _repository.GetByNumberAsync(uploaded.InvoiceNumber);

        var response = new CompareInvoiceResponse
        {
            InvoiceNumber = uploaded.InvoiceNumber,
            MatchFound = stored is not null
        };

        if (stored is null)
            return response;

        decimal uploadedTotal = uploaded.Items.Sum(i => Math.Round(i.Quantity * i.UnitPrice, 2));

        AddDifferenceIfChanged(response, "Supplier", stored.Supplier, uploaded.Supplier.Trim());
        AddDifferenceIfChanged(response, "InvoiceDate",
            stored.InvoiceDate.ToString("yyyy-MM-dd"), uploaded.InvoiceDate.ToString("yyyy-MM-dd"));
        AddDifferenceIfChanged(response, "TotalAmount",
            stored.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
            uploadedTotal.ToString("0.00", CultureInfo.InvariantCulture));
        AddDifferenceIfChanged(response, "ItemCount",
            stored.Items.Count.ToString(), uploaded.Items.Count.ToString());

        response.IsIdentical = response.Differences.Count == 0;
        return response;
    }

    private static void ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new InvoiceValidationException("A non-empty document file is required.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvoiceValidationException("The document exceeds the maximum allowed size of 5 MB.");
    }

    private static void ValidateRequest(InvoiceRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            errors.Add("Invoice number is required.");

        if (string.IsNullOrWhiteSpace(request.Supplier))
            errors.Add("Supplier is required.");

        if (request.InvoiceDate == default)
            errors.Add("Invoice date is required.");

        if (request.Items.Count == 0)
            errors.Add("At least one line item is required.");

        foreach (InvoiceItemRequest item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
                errors.Add("Every line item requires a description.");

            if (item.Quantity <= 0)
                errors.Add($"Line item '{item.Description}' must have a quantity greater than zero.");

            if (item.UnitPrice < 0)
                errors.Add($"Line item '{item.Description}' cannot have a negative unit price.");
        }

        if (errors.Count > 0)
            throw new InvoiceValidationException(string.Join(" ", errors));
    }

    private static void AddDifferenceIfChanged(
        CompareInvoiceResponse response, string fieldName, string? storedValue, string? uploadedValue)
    {
        if (!string.Equals(storedValue, uploadedValue, StringComparison.OrdinalIgnoreCase))
        {
            response.Differences.Add(new InvoiceFieldDifference
            {
                FieldName = fieldName,
                StoredValue = storedValue,
                UploadedValue = uploadedValue
            });
        }
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            Supplier = invoice.Supplier,
            InvoiceDate = invoice.InvoiceDate,
            TotalAmount = invoice.TotalAmount,
            SourceFileName = invoice.SourceFileName,
            CreatedAtUtc = invoice.CreatedAtUtc,
            Items = invoice.Items.Select(i => new InvoiceItemResponse
            {
                InvoiceItemId = i.InvoiceItemId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}
