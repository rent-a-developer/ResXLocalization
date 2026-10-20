using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RentADeveloper.ResXLocalization;

/// <summary>
/// The arguments for a translation lookup: positional arguments from values, named arguments from
/// name/value tuples, a dictionary or an object's properties. Build them with the static methods and pass
/// them to <c>ILocalizer.Get</c>, for example
/// <c>Localizer.Current.Get("FilePosition", MessageArguments.FromValues(3, 10))</c>.
/// </summary>
/// <remarks>
/// <para>
/// An argument's position is its <see cref="MessageArgument.Position" />, and only that: an argument's index
/// in the enumeration is not guaranteed to be the same as its <see cref="MessageArgument.Position" />. The
/// positions are 0 to <see cref="Count" /> minus 1, each exactly once; no name is an empty string, and no
/// name occurs twice. The static methods check this when
/// they build the arguments, and an instance cannot be changed afterwards.
/// </para>
/// <para>
/// The default formatter (<see cref="StringFormatMessageFormatter" />) ignores names and uses positions; a
/// formatter that understands names (ICU MessageFormat) finds a named argument under both.
/// </para>
/// </remarks>
public sealed class MessageArguments : IEnumerable<MessageArgument>
{
    /// <summary>The arguments.</summary>
    private readonly MessageArgument[] arguments;

    /// <summary>Initializes a new instance over arguments that have already been checked.</summary>
    /// <param name="arguments">The arguments.</param>
    private MessageArguments(MessageArgument[] arguments) => this.arguments = arguments;

    /// <summary>Gets an instance without arguments.</summary>
    public static MessageArguments Empty { get; } = new([]);

    /// <summary>Gets the number of arguments.</summary>
    public int Count => this.arguments.Length;

    /// <summary>
    /// Builds named arguments from an existing dictionary:
    /// <c>MessageArguments.FromDictionary(new Dictionary&lt;string, object?&gt; { ["Count"] = 5 })</c>.
    /// The positions follow the dictionary's enumeration order: the first entry has position 0, the next 1,
    /// and so on.
    /// </summary>
    /// <param name="arguments">The arguments, keyed by name.</param>
    /// <returns>One named argument per entry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A key is an empty string.</exception>
    public static MessageArguments FromDictionary(IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return Create(
            [.. arguments.Select((entry, index) => new MessageArgument(index, entry.Key, entry.Value))],
            nameof(arguments)
        );
    }

    /// <summary>
    /// Builds named arguments from the public instance properties of an object, for example an anonymous
    /// one: <c>MessageArguments.FromObject(new { Count = 5, Total = 10 })</c>. Meant for named arguments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The properties are those of the static type <typeparamref name="T" />, without indexers, in
    /// metadata order - the declaration order for a class or an anonymous type; the first property has
    /// position 0, the next 1, and so on. A value typed
    /// <see cref="object" /> yields no arguments. An exception thrown by a property getter propagates.
    /// </para>
    /// <para>
    /// Trim and Native AOT safe: the properties of <typeparamref name="T" /> are preserved. A generic method
    /// of your own that forwards its type parameter to <c>MessageArguments.FromObject</c> must annotate that
    /// parameter with the same <c>[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]</c>,
    /// or the trimmer reports IL2091 at that call.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type whose public instance properties become the arguments.</typeparam>
    /// <param name="source">The object to read the property values from.</param>
    /// <returns>One named argument per property.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Two properties have the same name, for example a base property hidden with <see langword="new" /> and
    /// a different property type.
    /// </exception>
    public static MessageArguments FromObject<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T
    >(T source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Create(
            [
                .. typeof(T)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.GetIndexParameters().Length == 0)
                    .Select((property, index) => new MessageArgument(index, property.Name, property.GetValue(source))),
            ],
            nameof(source)
        );
    }

    /// <summary>
    /// Builds arguments from name/value tuples, in declaration order:
    /// <c>MessageArguments.FromTuples(("Count", 5), ("Total", 10))</c>. A <see langword="null" /> name makes
    /// that argument positional only, so named and unnamed arguments can be mixed:
    /// <c>MessageArguments.FromTuples((null, file), ("Total", 10))</c>.
    /// </summary>
    /// <param name="arguments">The name/value pairs; the first has position 0, the next 1, and so on.</param>
    /// <returns>One argument per tuple.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// A name is an empty string, or two tuples have the same name (compared ordinally, so <c>A</c> and
    /// <c>a</c> differ).
    /// </exception>
    public static MessageArguments FromTuples(params (string? Name, object? Value)[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return Create(
            [.. arguments.Select((argument, index) => new MessageArgument(index, argument.Name, argument.Value))],
            nameof(arguments)
        );
    }

    /// <summary>
    /// Builds positional arguments from values: <c>MessageArguments.FromValues(3, 10)</c> for
    /// <c>File {0} of {1}</c>.
    /// </summary>
    /// <remarks>
    /// Because of <see langword="params" />, <c>MessageArguments.FromValues(dictionary)</c> is one positional
    /// argument whose value is the dictionary itself; use <see cref="FromDictionary" /> for named arguments.
    /// One <see langword="null" /> value is <c>MessageArguments.FromValues((object?)null)</c>.
    /// </remarks>
    /// <param name="values">The argument values; the first has position 0, the next 1, and so on.</param>
    /// <returns>One unnamed argument per value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    public static MessageArguments FromValues(params object?[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new([.. values.Select((value, index) => new MessageArgument(index, null, value))]);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Returns an enumerator over the arguments. An argument's index in the enumeration is not guaranteed to
    /// be the same as its <see cref="MessageArgument.Position" />; read the position from
    /// <see cref="MessageArgument.Position" />.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<MessageArgument> GetEnumerator() =>
        ((IEnumerable<MessageArgument>)this.arguments).GetEnumerator();

    /// <summary>
    /// Builds arguments from <see cref="MessageArgument" /> values whose positions are given explicitly, in
    /// any order. For tests that need arguments whose index in the enumeration differs from their
    /// <see cref="MessageArgument.Position" />.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The checked arguments.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// An argument position is negative, not less than the number of arguments, or occurs more than once; or
    /// an argument name is an empty string or occurs more than once.
    /// </exception>
    internal static MessageArguments FromArguments(params MessageArgument[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return Create([.. arguments], nameof(arguments));
    }

    /// <summary>
    /// Checks the arguments and wraps them: the argument positions must be 0 to the number of arguments
    /// minus 1, each exactly once, no argument name may be an empty string, and no argument name may occur
    /// twice. Names are compared ordinally, as XAML compares names and as
    /// ICU variable names are matched.
    /// </summary>
    /// <param name="arguments">The arguments to check; the new instance takes ownership of the array.</param>
    /// <param name="parameterName">
    /// The name of the calling method's parameter that supplied the arguments; an
    /// <see cref="ArgumentException" /> thrown for an invalid position or name reports it as its
    /// <see cref="ArgumentException.ParamName" />.
    /// </param>
    /// <returns>The checked arguments.</returns>
    /// <exception cref="ArgumentException">
    /// An argument position is negative, not less than the number of arguments, or occurs more than once; or
    /// an argument name is an empty string or occurs more than once.
    /// </exception>
    private static MessageArguments Create(MessageArgument[] arguments, string parameterName)
    {
        var positions = new HashSet<int>();

        // The first position outside 0 to Count - 1, or that the set already holds, is invalid. With every
        // position in range and none repeated, the positions are exactly 0 to Count - 1.
        var invalidPosition = arguments
            .Select(argument => (int?)argument.Position)
            .FirstOrDefault(position =>
                position < 0 || position >= arguments.Length || !positions.Add(position!.Value)
            );

        if (invalidPosition is not null)
        {
            throw new ArgumentException(
                $"The argument position {invalidPosition} is negative, not less than the number of arguments "
                    + $"({arguments.Length}), or occurs more than once; the positions must be 0 to "
                    + $"{arguments.Length - 1}, each exactly once.",
                parameterName
            );
        }

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in arguments.Select(argument => argument.Name).OfType<string>())
        {
            if (name.Length == 0)
            {
                throw new ArgumentException(
                    "An empty string is not an argument name. Use null for a positional-only argument.",
                    parameterName
                );
            }

            if (!names.Add(name))
            {
                throw new ArgumentException(
                    $"The argument name '{name}' occurs more than once; every name must be unique.",
                    parameterName
                );
            }
        }

        return new(arguments);
    }
}
