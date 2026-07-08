using IntakeServer.Controllers;
using IntakeServer.Services.Invoices;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace IntakeTests.IntakeServerTests.Controllers;

public class InvoicesControllerTests
{
    private readonly IInvoiceService _invoiceService = Substitute.For<IInvoiceService>();
    private readonly InvoicesController _controller;

    public InvoicesControllerTests()
    {
        _controller = new InvoicesController(_invoiceService);
    }

    [Fact]
    public async Task GetInvoice_ReturnsInvoice()
    {
        InvoiceResponse expected = new()
        {
            InvoiceId = 7,
            InvoiceNumber = "INV-007",
            Supplier = "Adventure Works"
        };

        _invoiceService.GetByIdAsync(7).Returns(expected);

        ActionResult<InvoiceResponse> actionResult = await _controller.GetInvoice(7);

        InvoiceResponse result = Assert.IsType<InvoiceResponse>(actionResult.Value);
        Assert.Equal(expected.InvoiceId, result.InvoiceId);
        Assert.Equal(expected.InvoiceNumber, result.InvoiceNumber);
        Assert.Equal(expected.Supplier, result.Supplier);
    }

    [Fact]
    public async Task SearchInvoices_ReturnsSearchResults()
    {
        InvoiceSearchFilter filter = new()
        {
            Supplier = "Contoso",
            Page = 2,
            PageSize = 25
        };

        SearchInvoiceResponse expected = new()
        {
            TotalCount = 1,
            Page = 2,
            PageSize = 25,
            Invoices =
            [
                new InvoiceResponse
                {
                    InvoiceId = 11,
                    InvoiceNumber = "INV-011",
                    Supplier = "Contoso"
                }
            ]
        };

        _invoiceService.SearchAsync(filter).Returns(expected);

        ActionResult<SearchInvoiceResponse> actionResult = await _controller.SearchInvoices(filter);

        SearchInvoiceResponse result = Assert.IsType<SearchInvoiceResponse>(actionResult.Value);
        Assert.Equal(expected.TotalCount, result.TotalCount);
        Assert.Equal(expected.Page, result.Page);
        Assert.Equal(expected.PageSize, result.PageSize);
        Assert.Single(result.Invoices);
        Assert.Equal("INV-011", result.Invoices[0].InvoiceNumber);
    }

    [Fact]
    public async Task GetDashboard_ReturnsDashboardSummary()
    {
        DashboardResponse expected = new()
        {
            TotalInvoices = 4,
            TotalAmount = 123.45m,
            AverageInvoiceAmount = 30.86m,
            SupplierCount = 2
        };

        _invoiceService.GetDashboardAsync().Returns(expected);

        ActionResult<DashboardResponse> actionResult = await _controller.GetDashboard();

        DashboardResponse result = Assert.IsType<DashboardResponse>(actionResult.Value);
        Assert.Equal(expected.TotalInvoices, result.TotalInvoices);
        Assert.Equal(expected.TotalAmount, result.TotalAmount);
        Assert.Equal(expected.AverageInvoiceAmount, result.AverageInvoiceAmount);
        Assert.Equal(expected.SupplierCount, result.SupplierCount);
    }

    [Fact]
    public async Task DeleteInvoice_DelegatesToService()
    {
        await _controller.DeleteInvoice(55);

        await _invoiceService.Received(1).DeleteAsync(55);
    }

    [Fact]
    public async Task CompareInvoice_ReturnsCompareResult()
    {
        IFormFile file = CreateFile();
        CompareInvoiceResponse expected = new()
        {
            InvoiceNumber = "INV-099",
            MatchFound = true,
            IsIdentical = false
        };

        _invoiceService.CompareAsync(file).Returns(expected);

        ActionResult<CompareInvoiceResponse> actionResult = await _controller.CompareInvoice(file);

        CompareInvoiceResponse result = Assert.IsType<CompareInvoiceResponse>(actionResult.Value);
        Assert.Equal(expected.InvoiceNumber, result.InvoiceNumber);
        Assert.Equal(expected.MatchFound, result.MatchFound);
        Assert.Equal(expected.IsIdentical, result.IsIdentical);
    }

    [Fact]
    public async Task UploadInvoice_ReturnsCreatedAtActionResult()
    {
        IFormFile file = CreateFile();
        InvoiceResponse expected = new()
        {
            InvoiceId = 42,
            InvoiceNumber = "INV-042",
            Supplier = "Northwind"
        };

        _invoiceService.CreateFromDocumentAsync(file).Returns(expected);

        ActionResult<InvoiceResponse> actionResult = await _controller.UploadInvoice(file);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(nameof(InvoicesController.GetInvoice), createdResult.ActionName);
        Assert.Equal(42, createdResult.RouteValues!["id"]);

        InvoiceResponse response = Assert.IsType<InvoiceResponse>(createdResult.Value);
        Assert.Equal(expected.InvoiceId, response.InvoiceId);
        Assert.Equal(expected.InvoiceNumber, response.InvoiceNumber);
    }

    private static IFormFile CreateFile()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("{}");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "invoice.json")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };
    }
}