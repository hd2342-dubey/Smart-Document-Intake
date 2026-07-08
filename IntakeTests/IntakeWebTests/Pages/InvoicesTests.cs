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

    [Fact]
    public async Task ClickSearch_ReloadsInvoicesFromFirstPage()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();
        IDialogService dialogService = Substitute.For<IDialogService>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10)
            .Returns(Task.FromResult<SearchInvoiceResponse?>(new SearchInvoiceResponse()));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(dialogService);

        var cut = ctx.Render(BuildRenderTree);

        cut.WaitForAssertion(() => Assert.Contains("Search", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Search").Click();

        cut.WaitForAssertion(() =>
        {
            _ = invoiceApi.Received(2).SearchAsync(null, null, null, null, 1, 10);
            _ = invoiceApi.Received(2).GetDashboardAsync();
        });
    }

    [Fact]
    public async Task Pagination_WhenTotalExceedsPageSize_LoadsSelectedPage()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();
        IDialogService dialogService = Substitute.For<IDialogService>();

        invoiceApi.SearchAsync(null, null, null, null, Arg.Any<int>(), 10)
            .Returns(Task.FromResult<SearchInvoiceResponse?>(new SearchInvoiceResponse
            {
                TotalCount = 25,
                Page = 1,
                PageSize = 10,
                Invoices = []
            }));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);
        ctx.Services.AddSingleton(dialogService);

        var cut = ctx.Render(BuildRenderTree);

        // 25 results with page size 10 => pagination with 3 pages is rendered.
        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "2"));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "2").Click();

        cut.WaitForAssertion(() =>
            _ = invoiceApi.Received(1).SearchAsync(null, null, null, null, 2, 10));
    }

    [Fact]
    public async Task ViewInvoice_OpensDetailsDialogWithInvoiceData()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10)
            .Returns(Task.FromResult<SearchInvoiceResponse?>(SingleInvoiceResult()));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));

        // Use the real dialog service so the dialog actually renders.
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTreeWithDialogProvider);

        cut.WaitForAssertion(() => Assert.Contains("INV-001", cut.Markup));

        // First icon button in the row is View, second is Delete.
        cut.FindAll("td button.mud-icon-button")[0].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Invoice INV-001", cut.Markup);
            Assert.Contains("Line Items", cut.Markup);
            Assert.Contains("Paper", cut.Markup);
        });
    }

    [Fact]
    public async Task DeleteInvoice_WhenConfirmed_DeletesAndReloads()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10)
            .Returns(Task.FromResult<SearchInvoiceResponse?>(SingleInvoiceResult()));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));
        invoiceApi.DeleteAsync(1).Returns(Task.FromResult(true));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTreeWithDialogProvider);

        cut.WaitForAssertion(() => Assert.Contains("INV-001", cut.Markup));

        // Second icon button in the row is Delete.
        cut.FindAll("td button.mud-icon-button")[1].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Are you sure you want to delete this expense?", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Yes").Click();

        cut.WaitForAssertion(() =>
        {
            _ = invoiceApi.Received(1).DeleteAsync(1);
            snackbar.Received(1).Add("Invoice Deleted", Severity.Warning);
            _ = invoiceApi.Received(2).SearchAsync(null, null, null, null, 1, 10);
        });
    }

    [Fact]
    public async Task DeleteInvoice_WhenDeclined_DoesNotDelete()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10)
            .Returns(Task.FromResult<SearchInvoiceResponse?>(SingleInvoiceResult()));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTreeWithDialogProvider);

        cut.WaitForAssertion(() => Assert.Contains("INV-001", cut.Markup));

        cut.FindAll("td button.mud-icon-button")[1].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Are you sure you want to delete this expense?", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "No").Click();

        cut.WaitForAssertion(() =>
        {
            _ = invoiceApi.DidNotReceiveWithAnyArgs().DeleteAsync(default);
            _ = invoiceApi.Received(1).SearchAsync(null, null, null, null, 1, 10);
        });
    }

    [Fact]
    public async Task DeleteInvoice_WhenApiFails_ShowsErrorSnackbar()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();
        ISnackbar snackbar = Substitute.For<ISnackbar>();

        invoiceApi.SearchAsync(null, null, null, null, 1, 10)
            .Returns(Task.FromResult<SearchInvoiceResponse?>(SingleInvoiceResult()));
        invoiceApi.GetDashboardAsync().Returns(Task.FromResult<DashboardResponse?>(new DashboardResponse()));
        invoiceApi.DeleteAsync(1).Returns(Task.FromResult(false));

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render(BuildRenderTreeWithDialogProvider);

        cut.WaitForAssertion(() => Assert.Contains("INV-001", cut.Markup));

        cut.FindAll("td button.mud-icon-button")[1].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Are you sure you want to delete this expense?", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Yes").Click();

        cut.WaitForAssertion(() =>
            snackbar.Received(1).Add("Failed to delete invoice", Severity.Error));
    }

    private static SearchInvoiceResponse SingleInvoiceResult() => new()
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
    };

    private static RenderFragment BuildRenderTree => builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();

        builder.OpenComponent<Invoices>(1);
        builder.CloseComponent();
    };

    private static RenderFragment BuildRenderTreeWithDialogProvider => builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();

        builder.OpenComponent<MudDialogProvider>(1);
        builder.CloseComponent();

        builder.OpenComponent<Invoices>(2);
        builder.CloseComponent();
    };
}
