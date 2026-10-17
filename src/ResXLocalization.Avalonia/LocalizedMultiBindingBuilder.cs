namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// Builds the <see cref="MultiBinding" /> behind <see cref="LocalizeExtension" /> and validates its
/// <see cref="Argument" /> entries. The children of the multi-binding are: 0, the refresh trigger, bound to
/// <see cref="ILocalizer.RefreshTriggeringRevision" /> of <see cref="Localizer.Current" />; 1, the key; 2 and
/// up, the argument values in declaration order. Every child is a binding: a constant is wrapped in a
/// binding whose source is the constant, so <see cref="LocalizedMessageConverter" /> reads every value the
/// same way.
/// </summary>
internal static class LocalizedMultiBindingBuilder
{
    /// <summary>Builds the multi-binding that yields the localized, formatted text.</summary>
    /// <param name="key">The key: a <see cref="string" /> or a <see cref="ResourceKey" />.</param>
    /// <param name="arguments">The arguments, already validated by <see cref="ValidateArguments" />.</param>
    /// <param name="resourceManager">
    /// The resource manager that scopes a <see cref="string" /> key, or <see langword="null" /> to search all
    /// registered resource managers.
    /// </param>
    /// <returns>The multi-binding.</returns>
    internal static MultiBinding Build(object key, Collection<Argument> arguments, ResourceManager? resourceManager)
    {
        var multiBinding = new MultiBinding
        {
            Converter = new LocalizedMessageConverter(
                [.. arguments.Select(argument => argument.Name)],
                resourceManager
            ),
            Mode = BindingMode.OneWay,
        };

        // 0 = refresh trigger, 1 = key, 2.. = argument values
        multiBinding.Bindings.Add(
            CompiledBinding.Create<ILocalizer, int>(localizer => localizer.RefreshTriggeringRevision, Localizer.Current)
        );
        multiBinding.Bindings.Add(CreateBinding(key));

        foreach (var argument in arguments)
        {
            multiBinding.Bindings.Add(CreateBinding(argument.Value));
        }

        return multiBinding;
    }

    /// <summary>
    /// Checks the arguments: every entry has a <see cref="Argument.Value" />, no
    /// <see cref="Argument.Name" /> is an empty string, and no two entries have the same name (compared
    /// ordinally).
    /// </summary>
    /// <param name="arguments">The arguments to check.</param>
    /// <exception cref="InvalidOperationException">An argument breaks one of the rules.</exception>
    internal static void ValidateArguments(Collection<Argument> arguments)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        for (var position = 0; position < arguments.Count; position++)
        {
            var argument = arguments[position];

            if (!argument.IsValueSet)
            {
                var name = argument.Name is null ? string.Empty : $" (Name '{argument.Name}')";
                throw new InvalidOperationException(
                    $"The <l:Argument> at position {position}{name} has no Value. Set Value to a constant or a "
                        + "binding; to pass null, write Value=\"{x:Null}\"."
                );
            }

            if (argument.Name is null)
            {
                continue;
            }

            if (argument.Name.Length == 0)
            {
                throw new InvalidOperationException(
                    $"The <l:Argument> at position {position} has an empty Name. An empty name is not allowed; "
                        + "leave Name unset for a positional argument."
                );
            }

            if (!names.Add(argument.Name))
            {
                throw new InvalidOperationException(
                    $"Two <l:Argument> entries have the Name '{argument.Name}'. Argument names must be unique."
                );
            }
        }
    }

    /// <summary>Returns a binding unchanged and wraps a constant in a binding whose source is the constant.</summary>
    /// <param name="value">A binding or a constant.</param>
    /// <returns>The binding.</returns>
    private static BindingBase CreateBinding(object? value) =>
        value switch
        {
            BindingBase binding => binding,

            // A constant null cannot be the source of a binding: a binding whose source is null has no source
            // and reads the DataContext instead. So this binding reads the DataContext like any other, and
            // ToNullConverter turns whatever it reads into the constant null.
            null => CompiledBinding.Create<object?, object?>(source => source, converter: ToNullConverter.Instance),
            _ => CompiledBinding.Create<object?, object?>(source => source, value),
        };

    /// <summary>
    /// Turns every value into <see langword="null" />. <see cref="CreateBinding" /> uses it for an argument
    /// whose constant value is <see langword="null" />: that argument's binding reads the <c>DataContext</c>,
    /// because a binding with a <see langword="null" /> source has no source, and this converter replaces
    /// what it reads with <see langword="null" />.
    /// </summary>
    private sealed class ToNullConverter : IValueConverter
    {
        /// <summary>The shared instance.</summary>
        internal static readonly ToNullConverter Instance = new();

        /// <summary>Returns <see langword="null" />.</summary>
        /// <param name="value">The bound value; ignored.</param>
        /// <param name="targetType">The type of the binding target property; ignored.</param>
        /// <param name="parameter">An optional converter parameter; ignored.</param>
        /// <param name="culture">The culture supplied by the binding; ignored.</param>
        /// <returns><see langword="null" />.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

        /// <summary>Not supported; the converter is one-way.</summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetType">The type to convert back to.</param>
        /// <param name="parameter">An optional converter parameter; not used.</param>
        /// <param name="culture">The culture supplied by the binding; not used.</param>
        /// <returns>Never returns; always throws.</returns>
        /// <exception cref="NotSupportedException">Always thrown; the converter is one-way.</exception>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
