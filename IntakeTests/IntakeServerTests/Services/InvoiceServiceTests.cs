using IntakeServer.Exceptions;
using IntakeServer.Repositories.Invoices;
using IntakeServer.Services.Invoices;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace IntakeTests.IntakeServerTests.Services;

public class InvoiceServiceTests
{
    private readonly IInvoiceRepository _repository = Substitute.For<IInvoiceRepository>();
    private readonly ILogger<InvoiceService> _logger = Substitute.For<ILogger<InvoiceService>>();
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _service = new InvoiceService(_repository, _logger);
    }

    [Fact]
    public async Task CreateFromDocumentAsync_FutureInvoiceDate_ThrowsValidationException()
    {
        DateOnly futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        IFormFile file = CreateJsonFile(futureDate);

        InvoiceValidationException exception =
            await Assert.ThrowsAsync<InvoiceValidationException>(() => _service.CreateFromDocumentAsync(file));

        Assert.Contains("Invoice date cannot be in the future.", exception.Message);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Invoice>());
    }

    [Fact]
    public async Task CreateFromDocumentAsync_TodayInvoiceDate_CreatesInvoice()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        IFormFile file = CreateJsonFile(today);

        _repository.GetByNumberAndSupplierAsync("INV-100", "Contoso")
            .Returns(Task.FromResult<Invoice?>(null));
        _repository.AddAsync(Arg.Any<Invoice>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Invoice>()));

        InvoiceResponse response = await _service.CreateFromDocumentAsync(file);

        Assert.Equal("INV-100", response.InvoiceNumber);
        Assert.Equal(today, response.InvoiceDate);
        await _repository.Received(1).AddAsync(Arg.Any<Invoice>());
    }

    [Fact]
    public async Task CreateFromDocumentAsync_PastInvoiceDate_CreatesInvoice()
    {
        DateOnly pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
        IFormFile file = CreateJsonFile(pastDate);

        _repository.GetByNumberAndSupplierAsync("INV-100", "Contoso")
            .Returns(Task.FromResult<Invoice?>(null));
        _repository.AddAsync(Arg.Any<Invoice>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Invoice>()));

        InvoiceResponse response = await _service.CreateFromDocumentAsync(file);

        Assert.Equal(pastDate, response.InvoiceDate);
        await _repository.Received(1).AddAsync(Arg.Any<Invoice>());
    }

    private static IFormFile CreateJsonFile(DateOnly invoiceDate)
    {
        string json = $$"""
        {
            "invoiceNumber": "INV-100",
            "supplier": "Contoso",
            "invoiceDate": "{{invoiceDate:yyyy-MM-dd}}",
            "items": [
                { "description": "Widget", "quantity": 2, "unitPrice": 10.50 }
            ]
        }
        """;

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "invoice.json")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };
    }
}
