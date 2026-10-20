using System.ComponentModel;

namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Detects whether the code runs inside a XAML designer/previewer - the Visual Studio WPF designer or Rider's WPF
/// previewer - and decides when the markup extensions return a design-time string instead of a binding.
/// </summary>
internal static class DesignMode
{
    /// <summary>
    /// The detected or forced value that determines whether the code runs inside a XAML designer/previewer;
    /// <see langword="null" /> until the first read.
    /// </summary>
    private static bool? isInDesignMode;

    /// <summary>
    /// Gets or sets a value indicating whether the code runs inside a XAML designer/previewer. Detected on the first
    /// read; tests set it to force design mode on or off.
    /// </summary>
    internal static bool IsInDesignMode
    {
        get => isInDesignMode ??= IsRunningInsideXamlDesigner();
        set => isInDesignMode = value;
    }

    /// <summary>
    /// Determines whether <see cref="LocalizeExtension" /> and <see cref="LocalizeEnumExtension" /> return a
    /// design-time string instead of a binding: inside a XAML designer, when the target property is not a
    /// <see cref="DependencyProperty" /> and the target object is not a <see cref="Setter" />, a
    /// <see cref="DataTrigger" /> or a <see cref="Condition" />.
    /// </summary>
    /// <remarks>
    /// Rider's WPF previewer supplies a dependency property as a CLR <c>PropertyInfo</c> (JetBrains
    /// RIDER-143207), and WPF throws a <c>XamlParseException</c> when a binding is provided for such a
    /// target. Every clause is needed. Outside a designer, a <c>PropertyInfo</c> target is legitimate in
    /// <see cref="Setter.Value" />, <see cref="DataTrigger" /> and <see cref="Condition" />, which handle
    /// markup extensions themselves, and a string there would stop live culture updates inside styles. The
    /// Visual Studio designer supplies those three with a <c>PropertyInfo</c> target too, and a real
    /// <see cref="DependencyProperty" /> everywhere else, so it keeps live bindings.
    /// </remarks>
    /// <param name="serviceProvider">The service provider supplied by the XAML loader.</param>
    /// <returns>
    /// <see langword="true" /> if the markup extension returns a design-time string; otherwise
    /// <see langword="false" />.
    /// </returns>
    internal static bool ShouldReturnDesignTimeText(IServiceProvider? serviceProvider)
    {
        var target = serviceProvider?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        return IsInDesignMode
            && target?.TargetProperty is not DependencyProperty
            && target?.TargetObject is not (Setter or DataTrigger or Condition);
    }

    /// <summary>
    /// Determines whether the current process is one in which an IDE renders XAML for a preview - the process
    /// of the Visual Studio WPF designer or of Rider's WPF previewer - rather than a running application or a
    /// test run. <see cref="IsInDesignMode" /> calls it once, on its first read.
    /// </summary>
    /// <remarks>
    /// Two checks are combined, because each of them recognizes only one of the two IDEs:
    /// <list type="bullet">
    /// <item>
    /// The Visual Studio WPF designer changes the default value of the dependency property
    /// <c>DesignerProperties.IsInDesignModeProperty</c> to <see langword="true" /> for the whole process. Rider
    /// does not.
    /// </item>
    /// <item>
    /// Rider sets the environment variable <c>XAML_PREVIEW_HOST_PROCESS</c> for the process in which its WPF
    /// previewer runs. Visual Studio does not.
    /// </item>
    /// </list>
    /// The per-element method <c>DesignerProperties.GetIsInDesignMode(element)</c> is not used: Rider replaces
    /// a root <see cref="Window" /> with a wrapper object and reports <see langword="false" /> for it
    /// (JetBrains RIDER-143208).
    /// </remarks>
    /// <returns>
    /// <see langword="true" /> in the process of the Visual Studio WPF designer or of Rider's WPF previewer;
    /// otherwise <see langword="false" />.
    /// </returns>
    private static bool IsRunningInsideXamlDesigner() =>
        DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue is true
        || Environment.GetEnvironmentVariable("XAML_PREVIEW_HOST_PROCESS") is not null;
}
