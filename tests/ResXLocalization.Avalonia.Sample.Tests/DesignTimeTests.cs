namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers <see cref="DesignTime" />.Culture at run time. Its design-time effect cannot be tested here, because
/// <c>Design.IsDesignMode</c> has no public setter; the maintainer checks it in the previewer.
/// </summary>
public class DesignTimeTests
{
    [AvaloniaFact]
    public void DesignTime_DoesNothing_AtRunTime()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var textBlock = new TextBlock();

        try
        {
            DesignTime.SetCulture(textBlock, "de");

            Localizer.Current.CurrentCulture.Name.Should().Be("en");
            DesignTime.GetCulture(textBlock).Should().Be("de");
        }
        finally
        {
            Localizer.Current.CurrentCulture = TestSupport.English;
        }
    }
}
