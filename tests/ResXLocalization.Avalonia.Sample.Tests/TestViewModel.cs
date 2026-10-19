using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>The data context of the argument tests: one property per bound argument value.</summary>
public sealed class TestViewModel : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets a value that is always <see langword="null" />.</summary>
    public string? Nothing { get; }

    /// <summary>Gets or sets the first value of <c>FormatPair</c>.</summary>
    public int A
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the second value of <c>FormatPair</c>.</summary>
    public int B
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the people count of <c>PeopleInvited</c>.</summary>
    public int Count
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the drive number of <c>PartitionDisplay</c>.</summary>
    public int Drive
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the bound key of the bound-key tests: a key, or any other value.</summary>
    public object? Key
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the volume label of <c>PartitionDisplay</c>.</summary>
    public string? Label
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the drive letter of <c>PartitionDisplay</c>.</summary>
    public string? Letter
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the partition number of <c>PartitionDisplay</c>.</summary>
    public int Partition
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Gets or sets the enumeration value of the enum tests.</summary>
    public FileSortOrder? SortOrder
    {
        get;
        set => this.Set(ref field, value);
    }

    /// <summary>Assigns a property's backing field and raises <see cref="PropertyChanged" />.</summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="field">The backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property name, supplied by the compiler.</param>
    private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        field = value;
        this.PropertyChanged?.Invoke(this, new(propertyName));
    }
}
