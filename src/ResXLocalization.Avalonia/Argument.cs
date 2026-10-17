namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// One format argument of <see cref="LocalizeExtension" />, written as an <c>&lt;l:Argument&gt;</c> child
/// element:
/// <c>&lt;l:Localize Key="{x:Static res:StringsKeys.PeopleInvited}"&gt;&lt;l:Argument Value="{Binding PeopleCount}" /&gt;&lt;/l:Localize&gt;</c>.
/// </summary>
/// <remarks>
/// <see cref="Argument" /> declares an argument. At each conversion it becomes a
/// <see cref="RentADeveloper.ResXLocalization.MessageArgument" /> whose position is its index among the
/// <c>&lt;l:Argument&gt;</c> children, with its <see cref="Name" /> and the resolved <see cref="Value" /> -
/// what the formatter receives. The code counterpart is <c>MessageArguments.FromTuples</c>, with a
/// <see langword="null" /> name for an unnamed argument (or <c>MessageArguments.FromValues</c> without
/// names).
/// </remarks>
public sealed class Argument
{
    /// <summary>
    /// Gets or sets the name of the argument. Unset, the argument is positional only. Set, the argument is
    /// also available under this name to a formatter that understands names, such as ICU MessageFormat; the
    /// default formatter ignores names and uses the position.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the argument: a constant or a binding. Constants are a literal
    /// (<c>Value="Text"</c>, which is the string <c>"Text"</c>), <c>{x:Static}</c>, <c>{StaticResource}</c>,
    /// <c>{x:Null}</c> or a typed property element such as <c>&lt;x:Int32&gt;</c>. A binding is any Avalonia
    /// binding, for example <c>{Binding PeopleCount}</c>, <c>{CompiledBinding}</c> or
    /// <c>{DynamicResource}</c>. A value that is not resolved yet is <see langword="null" />.
    /// </summary>
    [AssignBinding]
    public object? Value
    {
        get;
        set
        {
            field = value;
            this.IsValueSet = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="Value" /> was assigned, so that a forgotten
    /// <see cref="Value" /> differs from <c>Value="{x:Null}"</c>.
    /// </summary>
    internal bool IsValueSet { get; private set; }
}
