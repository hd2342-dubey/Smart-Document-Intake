using Bunit;
using Bunit.JSInterop;
using IntakeWeb.Layout;
using MudBlazor.Services;
using Xunit;

namespace IntakeTests.IntakeWebTests.Layout;

public class MainLayoutTests
{
    [Fact]
    public async Task RendersShellBrandTheme()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        IRenderedComponent<MainLayout> cut = ctx.Render<MainLayout>();

        Assert.Contains("Smart Document Intake", cut.Markup);
        Assert.Contains("Theme", cut.Markup);
        Assert.Contains("Invoices", cut.Markup);
    }
}