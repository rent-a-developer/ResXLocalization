namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Builds the <see cref="MultiBinding" /> behind <see cref="LocalizeExtension" /> and
/// <see cref="LocalizeEnumExtension" /> and validates its <see cref="Argument" /> entries. The children of the
/// multi-binding are: 0, the refresh trigger, bound to <see cref="ILocalizer.RefreshTriggeringRevision" /> of
/// <see cref="Localizer.Current" />; 1, the key or the enumeration value; 2 and up, the argument values in
/// declaration order. Every child is a binding: a constant is wrapped in a
/// binding whose source is the constant, so <see cref="LocalizedMessageConverter" /> reads every value the
/// same way.
/// </summary>
internal static class LocalizedMultiBindingBuilder
{
    /// <summary>Builds the multi-binding that yields the localized, formatted text.</summary>
    /// <param name="key">
    /// The key: a <see cref="string" /> or a <see cref="ResourceKey" />; with <paramref name="isEnumKey" />, the
    /// enumeration value. A binding or a constant.
    /// </param>
    /// <param name="arguments">The arguments, already validated by <see cref="ValidateArguments" />.</param>
    /// <param name="resourceManager">
    /// The resource manager that scopes a <see cref="string" /> key, or <see langword="null" /> to search all
    /// registered resource managers.
    /// </param>
    /// <param name="isEnumKey">
    /// <see langword="true" /> when <paramref name="key" /> is an enumeration value whose key
    /// <see cref="ILocalizer.EnumKeyBuilder" /> builds.
    /// </param>
    /// <param name="enumKeyPrefix">
    /// The key prefix passed to <see cref="ILocalizer.EnumKeyBuilder" />, or <see langword="null" /> to let the
    /// builder decide. Used only with <paramref name="isEnumKey" />.
    /// </param>
    /// <returns>The multi-binding.</returns>
    internal static MultiBinding Build(
        object key,
        Collection<Argument> arguments,
        ResourceManager? resourceManager,
        bool isEnumKey = false,
        string? enumKeyPrefix = null
    )
    {
        var multiBinding = new MultiBinding
        {
            Converter = new LocalizedMessageConverter(
                [.. arguments.Select(argument => argument.Name)],
                resourceManager,
                isEnumKey,
                enumKeyPrefix
            ),
            Mode = BindingMode.OneWay,
        };

        // 0 = refresh trigger, 1 = key or enumeration value, 2.. = argument values. The trigger's source is the process-wide
        // localizer, an INotifyPropertyChanged, which WPF observes through the weak event listener PropertyChangedEventManager.
        multiBinding.Bindings.Add(
            new Binding(nameof(ILocalizer.RefreshTriggeringRevision)) { Source = Localizer.Current }
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
    /// <see cref="Argument.Name" /> is an empty string, no two entries have the same name (compared
    /// ordinally), and a binding is a plain <see cref="Binding" />.
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

            // WPF's MultiBinding accepts only plain Binding children.
            if (argument.Value is BindingBase and not Binding)
            {
                throw new InvalidOperationException(
                    $"The <l:Argument> at position {position} has a Value of type "
                        + $"{argument.Value.GetType().FullName}. WPF supports only a plain {{Binding}} here."
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
    /// <param name="value">A binding or a constant; a <see langword="null" /> source yields <see langword="null" />.</param>
    /// <returns>The binding.</returns>
    private static BindingBase CreateBinding(object? value) =>
        value switch
        {
            BindingBase binding => binding,
            _ => new Binding { Source = value },
        };
}
