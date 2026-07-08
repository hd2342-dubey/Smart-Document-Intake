using Bunit;
using IntakeWeb.Layout;
using MudBlazor.Services;
using Xunit;

namespace IntakeTests.IntakeWebTests.Layout;

public class NavMenuTests : BunitContext
{
    public NavMenuTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void RendersApplicationBrandAndInvoicesLink()
    {
        IRenderedComponent<NavMenu> cut = Render<NavMenu>();

        Assert.Contains("Smart Document Intake", cut.Markup);
        Assert.Contains("Invoices", cut.Markup);
    }
}