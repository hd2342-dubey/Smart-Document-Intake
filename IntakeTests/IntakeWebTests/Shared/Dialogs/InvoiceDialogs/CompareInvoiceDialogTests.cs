using System.Text;
using Bunit;
using IntakeClient;
using IntakeWeb.Shared.Dialogs.InvoiceDialogs;
using LibShared.Models.Invoices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace IntakeTests.IntakeWebTests.Shared.Dialogs.InvoiceDialogs;

public class CompareInvoiceDialogTests
{
    [Fact]
    public async Task Render_WithoutSelectedFile_DisablesCompareButton()
    {
        await using var ctx = new BunitContext();
        var (provider, _, _) = await ShowDialogAsync(ctx);

        var compareButton = provider.FindAll("button").Single(b => b.TextContent.Trim() == "Compare");

        Assert.True(compareButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Compare_WhenNoMatchFound_ShowsWarning()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi) = await ShowDialogAsync(ctx);

        invoiceApi.CompareAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(CompareInvoiceResponse?, string?)>((new CompareInvoiceResponse
            {
                InvoiceNumber = "INV-404",
                MatchFound = false
            }, null)));

        await SelectFileAsync(provider, CreateFile("invoice.json"));
        ClickCompare(provider);

        provider.WaitForAssertion(() =>
            Assert.Contains("No stored invoice found with number \"INV-404\".", provider.Markup));
    }

    [Fact]
    public async Task Compare_WhenIdentical_ShowsSuccessMessage()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi) = await ShowDialogAsync(ctx);

        invoiceApi.CompareAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(CompareInvoiceResponse?, string?)>((new CompareInvoiceResponse
            {
                InvoiceNumber = "INV-001",
                MatchFound = true,
                IsIdentical = true
            }, null)));

        await SelectFileAsync(provider, CreateFile("invoice.json"));
        ClickCompare(provider);

        provider.WaitForAssertion(() =>
            Assert.Contains("The uploaded document matches the stored invoice exactly.", provider.Markup));
    }

    [Fact]
    public async Task Compare_WhenDifferencesFound_ShowsDifferenceTable()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi) = await ShowDialogAsync(ctx);

        invoiceApi.CompareAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(CompareInvoiceResponse?, string?)>((new CompareInvoiceResponse
            {
                InvoiceNumber = "INV-001",
                MatchFound = true,
                IsIdentical = false,
                Differences =
                [
                    new InvoiceFieldDifference
                    {
                        FieldName = "TotalAmount",
                        StoredValue = "25.00",
                        UploadedValue = "30.00"
                    },
                    new InvoiceFieldDifference
                    {
                        FieldName = "Supplier",
                        StoredValue = "Contoso",
                        UploadedValue = "Fabrikam"
                    }
                ]
            }, null)));

        await SelectFileAsync(provider, CreateFile("invoice.json"));
        ClickCompare(provider);

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("Differences found for invoice \"INV-001\":", provider.Markup);
            Assert.Contains("TotalAmount", provider.Markup);
            Assert.Contains("25.00", provider.Markup);
            Assert.Contains("30.00", provider.Markup);
            Assert.Contains("Supplier", provider.Markup);
            Assert.Contains("Contoso", provider.Markup);
            Assert.Contains("Fabrikam", provider.Markup);
        });
    }

    [Fact]
    public async Task Compare_WhenApiReturnsError_ShowsErrorAlert()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi) = await ShowDialogAsync(ctx);

        invoiceApi.CompareAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(CompareInvoiceResponse?, string?)>((null, "Unsupported file format")));

        await SelectFileAsync(provider, CreateFile("invoice.json"));
        ClickCompare(provider);

        provider.WaitForAssertion(() => Assert.Contains("Unsupported file format", provider.Markup));
    }

    [Fact]
    public async Task Compare_WhenApiThrows_ShowsFailureMessage()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi) = await ShowDialogAsync(ctx);

        invoiceApi.CompareAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromException<(CompareInvoiceResponse?, string?)>(new InvalidOperationException("Network down")));

        await SelectFileAsync(provider, CreateFile("invoice.json"));
        ClickCompare(provider);

        provider.WaitForAssertion(() =>
            Assert.Contains("Comparison failed: Network down", provider.Markup));
    }

    [Fact]
    public async Task SelectingNewFile_ClearsPreviousResult()
    {
        await using var ctx = new BunitContext();
        var (provider, _, invoiceApi) = await ShowDialogAsync(ctx);

        invoiceApi.CompareAsync(Arg.Any<Stream>(), "invoice.json", "application/json")
            .Returns(Task.FromResult<(CompareInvoiceResponse?, string?)>((new CompareInvoiceResponse
            {
                InvoiceNumber = "INV-001",
                MatchFound = true,
                IsIdentical = true
            }, null)));

        await SelectFileAsync(provider, CreateFile("invoice.json"));
        ClickCompare(provider);

        provider.WaitForAssertion(() =>
            Assert.Contains("The uploaded document matches the stored invoice exactly.", provider.Markup));

        await SelectFileAsync(provider, CreateFile("other.json"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("The uploaded document matches the stored invoice exactly.", provider.Markup));
    }

    [Fact]
    public async Task ClickClose_ClosesDialog()
    {
        await using var ctx = new BunitContext();
        var (provider, dialogReference, _) = await ShowDialogAsync(ctx);

        provider.FindAll("button").Single(b => b.TextContent.Trim() == "Close").Click();

        var result = await dialogReference.Result;

        Assert.NotNull(result);
        Assert.False(result.Canceled);
    }

    private static async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference DialogReference, IInvoiceApiClient InvoiceApi)> ShowDialogAsync(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        IInvoiceApiClient invoiceApi = Substitute.For<IInvoiceApiClient>();

        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(invoiceApi);

        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        IDialogReference dialogReference = default!;
        await provider.InvokeAsync(async () =>
            dialogReference = await dialogService.ShowAsync<CompareInvoiceDialog>("Compare Invoice"));

        provider.WaitForAssertion(() => Assert.Contains("Compare Invoice Document", provider.Markup));

        return (provider, dialogReference, invoiceApi);
    }

    private static IBrowserFile CreateFile(string name)
    {
        IBrowserFile file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.Size.Returns(2048L);
        file.ContentType.Returns("application/json");
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes("{}")));
        return file;
    }

    private static async Task SelectFileAsync(IRenderedComponent<MudDialogProvider> provider, IBrowserFile file)
    {
        var fileUpload = provider.FindComponent<MudFileUpload<IBrowserFile>>();
        await provider.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(file));
    }

    private static void ClickCompare(IRenderedComponent<MudDialogProvider> provider)
    {
        provider.FindAll("button").Single(b => b.TextContent.Trim() == "Compare").Click();
    }
}
