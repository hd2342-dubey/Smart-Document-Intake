using Bunit;
using IntakeClient;
using IntakeWeb.Pages;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace IntakeTests.IntakeWebTests.Pages;

public class InvoicesTests
{
    [Fact]
    public async Task InitialLoad_RendersInvoicesAndDashboardData()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();
        IDialogService dialogService = Substitute.For<IDialogService>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10).Returns(Task.FromResult<SearchInvoiceResponse?>(new SearchInvoiceResponse
        {
            TotalCount = 1,
            Page = 1,
            PageSize = 10,
            Invoices =
            [
                new InvoiceResponse
                {
                    InvoiceId = 1,
                    InvoiceNumber = "INV-001",
                    Supplier = "Contoso",
                    InvoiceDate = new DateOnly(2026, 7, 1),
                    TotalAmount = 25m,
                    Items =
                    [
                        new InvoiceItemResponse
                        {
                            InvoiceItemId = 10,
                            Description = "Paper",
                            Quantity = 2,
                            UnitPrice = 12.5m,
                            LineTotal = 25m
                        }
                    ]
                }
            ]
        }));

        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse
        {
            TotalInvoices = 1,
            TotalAmount = 25m,
            AverageInvoiceAmount = 25m,
            SupplierCount = 1
        }));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(dialogService);

        var cut = ctx.Render(BuildRenderTree);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("INV-001", cut.Markup);
            Assert.Contains("Contoso", cut.Markup);
            Assert.Contains("25.00", cut.Markup);
            Assert.Contains("Total Invoices", cut.Markup);
            Assert.Contains("1", cut.Markup);
        });

        await invoiceApi.Received(1).SearchAsync(null, null, null, null, 1, 10);
        await invoiceApi.Received(1).GetDashboardAsync();
    }

    [Fact]
    public async Task InitialLoad_WhenSearchFails_ShowsSnackbarError()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();
        IDialogService dialogService = Substitute.For<IDialogService>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10)
            .Returns(Task.FromException<SearchInvoiceResponse?>(new InvalidOperationException("Network down")));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(dialogService);

        ctx.Render(BuildRenderTree);

        await Task.Delay(1);

        snackbar.Received(1).Add("Failed to load invoices: Network down", Severity.Error);
    }

    private static RenderFragment BuildRenderTree => builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();

        builder.OpenComponent<Invoices>(1);
        builder.CloseComponent();
    };
}