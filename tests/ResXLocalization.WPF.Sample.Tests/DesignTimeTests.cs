using System.Reflection;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers <see cref="DesignTime" />.Culture and the design-time fallback of <see cref="LocalizeExtension" /> and
/// <see cref="LocalizeEnumExtension" />: inside a XAML designer that supplies the target property as a CLR
/// property, as Rider's WPF previewer does, the extensions return a string instead of a binding.
/// </summary>
public class DesignTimeTests
{
    /// <summary><c>Setter.Value</c>, a CLR property in every host.</summary>
    private static readonly PropertyInfo SetterValue = typeof(Setter).GetProperty(nameof(Setter.Value))!;

    /// <summary><c>TextBlock.Text</c> as a CLR property, the way Rider's WPF previewer supplies it.</summary>
    private static readonly PropertyInfo TextBlockTextProperty = typeof(TextBlock).GetProperty(nameof(TextBlock.Text))!;

    [Fact]
    public void DesignTime_DoesNothing_AtRunTime() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(false);
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
        });

    [Fact]
    public void DesignTime_EmptyOrInvalidName_LeavesTheCultureUnchanged() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);
            var textBlock = new TextBlock();

            try
            {
                DesignTime.SetCulture(textBlock, "invalid!culture");
                Localizer.Current.CurrentCulture.Name.Should().Be("en");

                DesignTime.SetCulture(textBlock, "");
                Localizer.Current.CurrentCulture.Name.Should().Be("en");
            }
            finally
            {
                Localizer.Current.CurrentCulture = TestSupport.English;
            }
        });

    [Fact]
    public void DesignTime_SetsTheCulture_InDesignMode() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            try
            {
                DesignTime.SetCulture(new TextBlock(), "de");

                Localizer.Current.CurrentCulture.Name.Should().Be("de");
            }
            finally
            {
                Localizer.Current.CurrentCulture = TestSupport.English;
            }
        });

    [Fact]
    public void Fallback_BoundKey_ReturnsAnEmptyString() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            var value = new LocalizeExtension(new Binding("Key")).ProvideValue(ClrTextTarget());

            value.Should().Be("");
        });

    [Fact]
    public void Fallback_Enum_ReturnsTheConstantMemberText_OrAnEmptyStringForABinding() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            var bound = new LocalizeEnumExtension { Value = new Binding(nameof(TestViewModel.SortOrder)) }.ProvideValue(
                ClrTextTarget()
            );
            var constant = new LocalizeEnumExtension { Value = FileSortOrder.Ascending }.ProvideValue(ClrTextTarget());

            bound.Should().Be("");
            constant.Should().Be("Ascending");
        });

    [Fact]
    public void Fallback_MissingKey_ReturnsTheFailureText() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            var value = new LocalizeExtension("ThisKeyDoesNotExist").ProvideValue(ClrTextTarget());

            value.Should().Be("!ThisKeyDoesNotExist!");
        });

    [Fact]
    public void Fallback_PrecedesValidation() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            var withoutKey = () => new LocalizeExtension().ProvideValue(ClrTextTarget());
            var withUnsetArgument = () =>
                new LocalizeExtension("Greeting") { Arguments = { new Argument() } }.ProvideValue(ClrTextTarget());

            withoutKey.Should().NotThrow().Which.Should().Be("");
            withUnsetArgument.Should().NotThrow().Which.Should().Be("Hello and welcome!");
        });

    [Fact]
    public void Fallback_TypedKey_ReturnsTheRawPattern_ForAClrTargetProperty() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            var value = PeopleInvitedWithArgument().ProvideValue(ClrTextTarget());

            value.Should().Be("{0} people invited");
        });

    [Fact]
    public void NoFallback_ForADependencyPropertyTarget_InDesignMode() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(true);

            var value = PeopleInvitedWithArgument().ProvideValue(Target(new TextBlock(), TextBlock.TextProperty));

            value.Should().BeOfType<MultiBindingExpression>();
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoFallback_ForASetter(bool isInDesignMode) =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var scope = new TestSupport.DesignModeScope(isInDesignMode);
            var setter = new Setter();

            var value = PeopleInvitedWithArgument().ProvideValue(Target(setter, SetterValue));

            // WPF's Setter handles markup extensions itself: the MultiBinding is returned, or assigned to
            // Setter.Value.
            (value is MultiBinding || setter.Value is MultiBinding)
                .Should()
                .BeTrue();
        });

    private static IServiceProvider ClrTextTarget() => Target(new TextBlock(), TextBlockTextProperty);

    private static LocalizeExtension PeopleInvitedWithArgument() =>
        new(ApplicationStringsKeys.PeopleInvited)
        {
            Arguments = { new Argument { Value = new Binding(nameof(TestViewModel.Count)) } },
        };

    private static IServiceProvider Target(object targetObject, object targetProperty) =>
        new TestSupport.SimpleServiceProvider(new TestSupport.SimpleProvideValueTarget(targetObject, targetProperty));
}
