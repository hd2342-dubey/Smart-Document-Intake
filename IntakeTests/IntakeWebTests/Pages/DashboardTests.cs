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

public class DashboardTests
{
    [Fact]
    public async Task InitialLoad_RendersStatCardsAndCharts()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(FullDashboardResult()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTree);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Total Invoices", cut.Markup);
            Assert.Contains("Total Amount", cut.Markup);
            Assert.Contains("Average Invoice", cut.Markup);
            Assert.Contains("Suppliers", cut.Markup);
            Assert.Contains("125.00", cut.Markup);
            Assert.Contains("Amount by Supplier", cut.Markup);
            Assert.Contains("Invoices per Supplier", cut.Markup);
            Assert.Contains("Contoso", cut.Markup);
        });

        await invoiceApi.Received(1).GetDashboardAsync();
    }

    [Fact]
    public async Task InitialLoad_RendersRecentInvoices()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(FullDashboardResult()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTree);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Recent Invoices", cut.Markup);
            Assert.Contains("INV-001", cut.Markup);
            Assert.Contains("01-Jul-2026", cut.Markup);
        });
    }

    [Fact]
    public async Task InitialLoad_WhenNoSupplierData_ShowsEmptyChartMessages()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTree);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No supplier data available.", cut.Markup);
            Assert.Contains("No recent invoices.", cut.Markup);
        });
    }

    [Fact]
    public async Task InitialLoad_WhenDashboardIsNull_ShowsNoDataMessage()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(null));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTree);

        cut.WaitForAssertion(() =>
            Assert.Contains("No dashboard data available.", cut.Markup));
    }

    [Fact]
    public async Task InitialLoad_WhenApiFails_ShowsSnackbarError()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.GetDashboardAsync()
            .Returns(Task.FromException<DashboardResponse?>(new InvalidOperationException("Network down")));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        ctx.Render(BuildRenderTree);

        await Task.Delay(1);

        snackbar.Received(1).Add("Failed to load dashboard: Network down", Severity.Error);
    }

    private static DashboardResponse FullDashboardResult() => new()
    {
        TotalInvoices = 3,
        TotalAmount = 125m,
        AverageInvoiceAmount = 41.67m,
        SupplierCount = 2,
        TopSuppliers =
        [
            new SupplierTotal { Supplier = "Contoso", InvoiceCount = 2, TotalAmount = 100m },
            new SupplierTotal { Supplier = "Fabrikam", InvoiceCount = 1, TotalAmount = 25m }
        ],
        RecentInvoices =
        [
            new InvoiceResponse
            {
                InvoiceId = 1,
                InvoiceNumber = "INV-001",
                Supplier = "Contoso",
                InvoiceDate = new DateOnly(2026, 7, 1),
                TotalAmount = 25m
            }
        ]
    };

    private static RenderFragment BuildRenderTree => builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();

        builder.OpenComponent<Dashboard>(1);
        builder.CloseComponent();
    };
}
