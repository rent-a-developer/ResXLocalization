namespace RentADeveloper.ResXLocalization.MessageFormat;

/// <summary>
/// Installs ICU MessageFormat as the formatter of an <see cref="ILocalizer" />, for messages whose shape
/// depends on their arguments, such as <c>{Count, plural, one {# file deleted} other {# files deleted}}</c>.
/// </summary>
public static class MessageFormatLocalizerExtensions
{
    /// <summary>
    /// Replaces the <see cref="ILocalizer.MessageFormatter" /> of <paramref name="localizer" /> with the ICU
    /// MessageFormat formatter, for every lookup in code and in XAML. Call it once, at startup:
    /// <c>Localizer.Current.UseMessageFormat();</c>
    /// </summary>
    /// <remarks>
    /// <para>
    /// After the call, every resolved resource value is parsed as an ICU message, also for a lookup without
    /// arguments: <c>Localizer.Current.Get("Key")</c>, the <see cref="ILocalizer" /> indexer, and
    /// <c>{l:Localize Key}</c> of the ResXLocalization.Avalonia and ResXLocalization.WPF packages. Every
    /// resource value must therefore follow ICU syntax, including texts without arguments: <c>It''s ready</c>
    /// renders <c>It's ready</c>, <c>'{literal}'</c> renders <c>{literal}</c>, and a value that references a
    /// variable the lookup does not supply returns the failure text and raises
    /// <see cref="ILocalizer.LookupFailed" />.
    /// </para>
    /// <para>
    /// Named arguments are built with <see cref="MessageArguments" />, for example
    /// <c>Localizer.Current.Get(AppStringsKeys.FilesDeleted, MessageArguments.FromTuples(("Count", 5)))</c>, and
    /// declared in XAML as <c>&lt;l:Argument Name="Count" Value="{Binding DeletedCount}" /&gt;</c>. Every
    /// argument is also reachable under its position: <c>{0}</c>, <c>{1}</c> and so on.
    /// </para>
    /// <para>
    /// The method works with any <see cref="ILocalizer" /> implementation, because it only assigns
    /// <see cref="ILocalizer.MessageFormatter" />. An exception thrown by the setter of a custom implementation,
    /// or by a <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged" /> handler during the
    /// assignment, propagates.
    /// </para>
    /// <para>
    /// See <see href="https://rent-a-developer.github.io/ResXLocalization/guides/message-format.html">ICU MessageFormat</see>.
    /// </para>
    /// </remarks>
    /// <param name="localizer">
    /// The localizer whose formatter to replace, usually <see cref="Localizer.Current" />.
    /// </param>
    /// <param name="messageFormatter">
    /// The <see cref="Jeffijoe.MessageFormat.MessageFormatter" /> of the MessageFormat library that formats every
    /// message, or <see langword="null" /> (the default) for a new one with the library's defaults. A non-null
    /// instance is used as given, with its custom formatters, pluralizers and <c>CustomValueFormatter</c>, and
    /// is shared by every culture and every thread: configure it completely before the call, and do not modify
    /// it afterwards. Its <c>Culture</c> property has no effect, because every lookup passes its own culture.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="localizer" /> is <see langword="null" />.</exception>
    public static void UseMessageFormat(this ILocalizer localizer, JeffijoeMessageFormatter? messageFormatter = null)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        localizer.MessageFormatter = new IcuMessageFormatter(messageFormatter ?? new JeffijoeMessageFormatter());
    }
}
