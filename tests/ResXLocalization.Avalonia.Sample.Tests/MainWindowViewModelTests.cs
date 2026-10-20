namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Verifies the <see cref="MainWindowViewModel" />: it drives the culture from the language selection, and
/// its code-behind read-out properties recompute and raise change notifications when the language or the
/// selected sort order changes.
/// </summary>
public class MainWindowViewModelTests
{
    [AvaloniaFact]
    public void Construction_ExposesEnglishValues()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        using var viewModel = new MainWindowViewModel(Localizer.Current);

        viewModel.GreetingViaSearchAll.Should().Be("Hello and welcome!");
        viewModel.GreetingViaIndexer.Should().Be("Hello and welcome!");
        viewModel.GreetingViaTypedKey.Should().Be("Hello and welcome!");
        viewModel.GreetingViaScopedManager.Should().Be("Hello and welcome!");
        viewModel.PoweredByViaTypedKey.Should().Be("Powered by rent-a-developer");
        viewModel.PoweredByViaScopedInternalManager.Should().Be("Powered by rent-a-developer");
        viewModel.MissingKeyExample.Should().Be("!ThisKeyDoesNotExist!");
        viewModel.SortOrderViaSearchAllDefaultPrefix.Should().Be("Ascending");
        viewModel.SortOrderViaSearchAllCustomPrefix.Should().Be("A to Z");
        viewModel.SortOrderViaScopedDefaultPrefix.Should().Be("Ascending");
        viewModel.SortOrderViaScopedCustomPrefix.Should().Be("A to Z");
    }

    [AvaloniaFact]
    public void SelectingLanguage_RecomputesAndNotifies_ReadOutProperties()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var viewModel = new MainWindowViewModel(Localizer.Current);

        using var monitor = viewModel.Monitor();

        viewModel.SelectedLanguage = TestSupport.German;

        viewModel.GreetingViaSearchAll.Should().Be("Hallo und willkommen!");
        viewModel.SortOrderViaSearchAllDefaultPrefix.Should().Be("Aufsteigend");
        viewModel.PoweredByViaTypedKey.Should().Be("Bereitgestellt von rent-a-developer");

        monitor.Should().RaisePropertyChangeFor(model => model.GreetingViaSearchAll);
        monitor.Should().RaisePropertyChangeFor(model => model.SortOrderViaSearchAllDefaultPrefix);
        monitor.Should().RaisePropertyChangeFor(model => model.PoweredByViaTypedKey);
    }

    [AvaloniaFact]
    public void SelectingLanguage_SwitchesTheAmbientCulture()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        using var viewModel = new MainWindowViewModel(Localizer.Current);
        viewModel.SelectedLanguage = TestSupport.German;

        Localizer.Current.CurrentCulture.Name.Should().Be("de");
    }

    [AvaloniaFact]
    public void SelectingSortOrder_RecomputesAndNotifies_EnumReadOuts()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var viewModel = new MainWindowViewModel(Localizer.Current);

        using var monitor = viewModel.Monitor();

        viewModel.SelectedFileSortOrder = FileSortOrder.Descending;

        viewModel.SortOrderViaSearchAllDefaultPrefix.Should().Be("Descending");
        viewModel.SortOrderViaSearchAllCustomPrefix.Should().Be("Z to A");
        monitor.Should().RaisePropertyChangeFor(model => model.SortOrderViaSearchAllDefaultPrefix);
        monitor.Should().RaisePropertyChangeFor(model => model.SortOrderViaScopedCustomPrefix);
    }

    [AvaloniaFact]
    public void SupportedLanguages_AndSortOrders_AreExposedForTheComboBoxes()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var viewModel = new MainWindowViewModel(Localizer.Current);

        viewModel.SupportedLanguages.Select(culture => culture.Name).Should().Equal("en", "de");
        viewModel.FileSortOrders.Should().Equal(Enum.GetValues<FileSortOrder>());
    }
}
