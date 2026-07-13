namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Verifies the <see cref="MainWindowViewModel" />: it drives the culture from the language selection, and
/// its code-behind read-out properties recompute and raise change notifications when the language or the
/// selected sort order changes.
/// </summary>
public class MainWindowViewModelTests
{
    [Fact]
    public void Construction_ExposesEnglishValues() => WpfThread.Invoke(() =>
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
    );

    [Fact]
    public void SelectingLanguage_RecomputesAndNotifies_ReadOutProperties() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var viewModel = new MainWindowViewModel(Localizer.Current);

            var changed = new List<String>();
            viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName ?? String.Empty);

            viewModel.SelectedLanguage = TestSupport.German;

            viewModel.GreetingViaSearchAll.Should().Be("Hallo und willkommen!");
            viewModel.SortOrderViaSearchAllDefaultPrefix.Should().Be("Aufsteigend");
            viewModel.PoweredByViaTypedKey.Should().Be("Bereitgestellt von rent-a-developer");

            changed.Should().Contain(nameof(MainWindowViewModel.GreetingViaSearchAll));
            changed.Should().Contain(nameof(MainWindowViewModel.SortOrderViaSearchAllDefaultPrefix));
            changed.Should().Contain(nameof(MainWindowViewModel.PoweredByViaTypedKey));
        }
    );

    [Fact]
    public void SelectingLanguage_SwitchesTheAmbientCulture() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            using var viewModel = new MainWindowViewModel(Localizer.Current);
            viewModel.SelectedLanguage = TestSupport.German;

            Localizer.Current.CurrentCulture.Name.Should().Be("de");
        }
    );

    [Fact]
    public void SelectingSortOrder_RecomputesAndNotifies_EnumReadOuts() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var viewModel = new MainWindowViewModel(Localizer.Current);

            var changed = new List<String>();
            viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName ?? String.Empty);

            viewModel.SelectedFileSortOrder = FileSortOrder.Descending;

            viewModel.SortOrderViaSearchAllDefaultPrefix.Should().Be("Descending");
            viewModel.SortOrderViaSearchAllCustomPrefix.Should().Be("Z to A");
            changed.Should().Contain(nameof(MainWindowViewModel.SortOrderViaSearchAllDefaultPrefix));
            changed.Should().Contain(nameof(MainWindowViewModel.SortOrderViaScopedCustomPrefix));
        }
    );

    [Fact]
    public void SupportedLanguages_AndSortOrders_AreExposedForTheComboBoxes() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var viewModel = new MainWindowViewModel(Localizer.Current);

            viewModel.SupportedLanguages.Select(culture => culture.Name).Should().Equal("en", "de");
            viewModel.FileSortOrders.Should().Equal(Enum.GetValues<FileSortOrder>());
        }
    );
}
