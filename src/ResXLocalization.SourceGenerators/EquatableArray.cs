using System.Collections;
using System.Collections.Immutable;

namespace RentADeveloper.ResXLocalization.SourceGenerators;

/// <summary>
/// An <see cref="ImmutableArray{T}" />-backed value type with structural (sequence) equality and an
/// aggregated hash code. Incremental generator models must have value equality so Roslyn can cache
/// pipeline steps; a raw array or <see cref="ImmutableArray{T}" /> field would compare by reference
/// and defeat the cache, re-running the generator on every keystroke in the IDE.
/// </summary>
/// <typeparam name="T">The element type, itself compared with the default equality comparer.</typeparam>
/// <param name="array">The backing array whose elements this instance wraps.</param>
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>Determines whether two arrays are sequence-equal.</summary>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true" /> when the arrays contain equal elements in the same order.</returns>
    public static Boolean operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>Determines whether two arrays are not sequence-equal.</summary>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true" /> when the arrays differ in length or in any element.</returns>
    public static Boolean operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    /// <summary>
    /// Determines whether this array and <paramref name="other" /> contain equal elements in the
    /// same order. Two default (uninitialized) instances are equal; a default instance never equals
    /// an initialized one, not even an empty one.
    /// </summary>
    /// <param name="other">The array to compare with this one.</param>
    /// <returns><see langword="true" /> when the arrays are sequence-equal.</returns>
    public Boolean Equals(EquatableArray<T> other)
    {
        var left = this.Items;
        var right = other.Items;

        if (left.IsDefault || right.IsDefault)
        {
            return left.IsDefault == right.IsDefault;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="obj" /> is an <see cref="EquatableArray{T}" /> that is
    /// sequence-equal to this one.
    /// </summary>
    /// <param name="obj">The object to compare with this array.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is a sequence-equal array.</returns>
    public override Boolean Equals(Object? obj) => obj is EquatableArray<T> other && this.Equals(other);

    /// <summary>Computes a hash code aggregated over all elements, so sequence-equal arrays hash alike.</summary>
    /// <returns>The aggregated hash code; <c>0</c> for a default (uninitialized) instance.</returns>
    public override Int32 GetHashCode()
    {
        if (array.IsDefault)
        {
            return 0;
        }

        var hash = 17;

        foreach (var item in array)
        {
            hash = unchecked((hash * 31) + (item?.GetHashCode() ?? 0));
        }

        return hash;
    }

    /// <summary>Returns an enumerator over the elements; a default (uninitialized) instance enumerates as empty.</summary>
    /// <returns>The element enumerator.</returns>
    public IEnumerator<T> GetEnumerator() =>
        (array.IsDefault ? ImmutableArray<T>.Empty : array).AsEnumerable().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Gets the backing array. Exists because a primary-constructor parameter is only accessible on
    /// the current instance - <see cref="Equals(EquatableArray{T})" /> needs to read
    /// <c>other</c>'s array too.
    /// </summary>
    private ImmutableArray<T> Items => array;
}
