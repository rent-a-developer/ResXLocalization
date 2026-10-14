namespace RentADeveloper.ResXLocalization;

/// <summary>
/// One argument of a lookup: its position, an optional name and its value. The arguments of one lookup
/// are a <see cref="MessageArguments" />; no arguments is <see cref="MessageArguments.Empty" />.
/// </summary>
/// <remarks>
/// <see cref="MessageArgument" /> is what an <see cref="IMessageFormatter" /> receives - a resolved
/// value. In XAML an argument is declared as <c>&lt;l:Argument&gt;</c>, which becomes a
/// <see cref="MessageArgument" /> at each conversion, with its index among the children as
/// <see cref="Position" /> and its <c>Name</c>.
/// </remarks>
/// <param name="Position">
/// The argument's position: the argument at position 0 fills placeholder <c>{0}</c>, and so on, also when it
/// has a name. This is the only source of an argument's position: an argument's index in the enumeration of
/// a <see cref="MessageArguments" /> is not guaranteed to be the same as its position.
/// </param>
/// <param name="Name">
/// <see langword="null" /> for a positional-only argument; otherwise the name under which a formatter
/// that understands names (ICU MessageFormat) also finds the argument. The default
/// (<see cref="string.Format(IFormatProvider, string, object[])" />) formatter uses positions only and
/// ignores names.
/// </param>
/// <param name="Value">The argument's value; may be <see langword="null" />.</param>
public readonly record struct MessageArgument(int Position, string? Name, object? Value);
