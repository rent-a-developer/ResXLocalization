namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// End-to-end tests that load the real <see cref="MainWindow" /> and assert that the rendered text switches
/// live when the language changes - the headline feature of the library.
/// </summary>
public class MainWindowLiveSwitchTests
{
    [Fact]
    public void PlainStrings_SwitchLive() => WpfThread.Invoke(() =>
        {
            var (window, viewModel) = CreateWindow();

            var before = TestSupport.AllVisibleText(window);
            before.Should().Contain("Hello and welcome!");
            before.Should().Contain("Powered by rent-a-developer");

            viewModel.SelectedLanguage = TestSupport.German;
            TestSupport.Flush();

            var after = TestSupport.AllVisibleText(window);
            after.Should().Contain("Hallo und willkommen!");
            after.Should().Contain("Bereitgestellt von rent-a-developer");
            after.Should().NotContain("Hello and welcome!");
        }
    );

    [Fact]
    public void SelectedEnum_SwitchesLive_AndNeverLeaksIntoTheModel() => WpfThread.Invoke(() =>
        {
            var (window, viewModel) = CreateWindow();
            viewModel.SelectedFileSortOrder.Should().Be(FileSortOrder.Ascending);
            TestSupport.AllVisibleText(window).Should().Contain("Ascending");

            viewModel.SelectedLanguage = TestSupport.German;
            TestSupport.Flush();

            var after = TestSupport.AllVisibleText(window);
            after.Should().Contain("Aufsteigend");
            after.Should().NotContain("Ascending");

            // The view model still holds a clean enum value; only the rendered text was localized.
            viewModel.SelectedFileSortOrder.Should().Be(FileSortOrder.Ascending);
        }
    );

    [Fact]
    public void SwitchingBackAndForth_IsStable() => WpfThread.Invoke(() =>
        {
            var (window, viewModel) = CreateWindow();

            viewModel.SelectedLanguage = TestSupport.German;
            TestSupport.Flush();
            TestSupport.AllVisibleText(window).Should().Contain("Hallo und willkommen!");

            viewModel.SelectedLanguage = TestSupport.English;
            TestSupport.Flush();
            TestSupport.AllVisibleText(window).Should().Contain("Hello and welcome!");
        }
    );

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Ownership is transferred to the returned window, whose Closed handler disposes its DataContext."
    )]
    private static (MainWindow Window, MainWindowViewModel ViewModel) CreateWindow()
    {
        // Localizer.Current is a process-lifetime singleton, so reset the culture for test isolation.
        TestSupport.ResetToEnglishWithTestCatalogs();

        var viewModel = new MainWindowViewModel(Localizer.Current);
        var window = new MainWindow { DataContext = viewModel };

        // Realize the tree and flush binding transfers without requiring an interactive desktop session.
        window.Measure(new(900, 820));
        window.Arrange(new(0, 0, 900, 820));
        window.UpdateLayout();
        TestSupport.Flush();

        return (window, viewModel);
    }
}
