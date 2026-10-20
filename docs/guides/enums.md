# Localizing enum values

Enum members are localized **by naming convention**: add one string entry per member to any of your
`.resx` files, named `Enum_<EnumTypeName>_<MemberName>`. No attributes on the enum, no extra code.

```csharp
public enum FileSortOrder { Unsorted, Ascending, Descending }
```

| Key | `AppStrings.resx` (English) | `AppStrings.de.resx` (German) |
| --- | --- | --- |
| `Enum_FileSortOrder_Unsorted` | `Unsorted` | `Unsortiert` |
| `Enum_FileSortOrder_Ascending` | `Ascending (A-Z)` | `Aufsteigend (A-Z)` |
| `Enum_FileSortOrder_Descending` | `Descending (Z-A)` | `Absteigend (Z-A)` |

Like every other lookup, enum labels update live when the culture changes, and the same three usages
work identically in Avalonia and WPF.

## In XAML: `{l:LocalizeEnum}`

`{l:LocalizeEnum}` localizes the enum value you give it in `Value` - a binding or a fixed member:

```xml
<TextBlock Text="{l:LocalizeEnum Value={Binding SortOrder}}" />
<TextBlock Text="{l:LocalizeEnum {Binding SortOrder}}" />                           <!-- the same, shorter -->
<TextBlock Text="{l:LocalizeEnum Value={x:Static m:FileSortOrder.Ascending}}" />    <!-- a fixed member -->
```

The text follows the bound value: when `SortOrder` changes, so does the text. A bound value that is
not an enum - for example a `null` property while the view loads - renders an empty string.

A fixed member is written with `{x:Static}`. `Value=Ascending` is the **string** `"Ascending"`, not the
enum member, and `{l:LocalizeEnum}` throws `InvalidOperationException` for it, as it does when `Value`
is missing.

`Value` accepts the same bindings as an `<l:Argument>` `Value`: in Avalonia any binding, in WPF a plain
`{Binding}` only - a `MultiBinding` or a `PriorityBinding` throws `InvalidOperationException`. See
[Dynamic format arguments](format-arguments.md#worth-knowing).

### In item templates

Inside a `ComboBox` or `ListBox` item template each item *is* the enum value - it is the
`DataContext` - so `Value={Binding}`, a binding without a path, localizes it:

```xml
<ComboBox ItemsSource="{Binding FileSortOrders}"
          SelectedItem="{Binding SelectedFileSortOrder}">
  <ComboBox.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{l:LocalizeEnum Value={Binding}}" />
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

### With format arguments

An enum label can have placeholders and take [format arguments](format-arguments.md), as
`<l:Argument>` children - the same as for `{l:Localize}`. With a `Count_FileSortOrder_Ascending` entry
of `{0} files, ascending`:

```xml
<TextBlock>
  <TextBlock.Text>
    <l:LocalizeEnum Value="{Binding SortOrder}" KeyPrefix="Count_">
      <l:Argument Value="{Binding FileCount}" />
    </l:LocalizeEnum>
  </TextBlock.Text>
</TextBlock>
<!-- "3 files, ascending" -->
```

The enum value itself is not an argument. A text that needs it, for example under a name, binds it
explicitly:

```xml
<l:LocalizeEnum Value="{Binding SortOrder}">
  <l:Argument Name="Kind" Value="{Binding SortOrder}" />
</l:LocalizeEnum>
```

Under the default formatter, a text that needs a plural - `1 file` and `3 files` - needs one enum
member per plural case: the default formatter is `string.Format` and does not choose between forms.
With the ICU MessageFormat formatter of the `ResXLocalization.MessageFormat` package, one member and a
`plural` pattern suffice, for example `{0, plural, one {# file} other {# files}}`; see
[ICU MessageFormat](message-format.md).

## Where a markup extension is not accepted: `LocalizeEnumConverter`

A markup extension does not work everywhere a binding is expected - for example in a property of type
`Binding` or `BindingBase`, such as `DataGridTextColumn.Binding`. A hand-written `MultiBinding` with
`LocalizeEnumConverter` does. Its children are, in this order:

1. The refresh trigger: `RefreshTriggeringRevision` of `Localizer.Current`, which re-runs the
   conversion whenever a lookup's result can change, such as on a language switch.
2. The enum value.
3. From index 2 on, the format arguments, in order.

```xml
<!-- xmlns:core="clr-namespace:RentADeveloper.ResXLocalization;assembly=ResXLocalization.Core" -->
<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{x:Static l:LocalizeEnumConverter.Default}">
      <Binding Path="RefreshTriggeringRevision" Source="{x:Static core:Localizer.Current}" />
      <Binding Path="SelectedFileSortOrder" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

A `MultiBinding` that puts the enum value first renders an empty string, and nothing reports it; check
the order whenever a converter shows nothing.

### With format arguments

Every child from index 2 on is one positional argument: child 2 fills `{0}`, child 3 fills `{1}`, and so
on. A converter with a key prefix is declared as a resource; with a `Count_FileSortOrder_Ascending` entry
of `{0} files, ascending`:

```xml
<Window.Resources>
  <l:LocalizeEnumConverter x:Key="CountConverter" KeyPrefix="Count_" />
</Window.Resources>

<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{StaticResource CountConverter}">
      <Binding Path="RefreshTriggeringRevision" Source="{x:Static core:Localizer.Current}" />
      <Binding Path="SortOrder" />
      <Binding Path="FileCount" />                 <!-- {0} -->
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
<!-- "3 files, ascending" -->
```

Two placeholders take two argument children, in placeholder order. With a `Range_FileSortOrder_Ascending`
entry of `{0} of {1} files, ascending`:

```xml
<Window.Resources>
  <l:LocalizeEnumConverter x:Key="RangeConverter" KeyPrefix="Range_" />
</Window.Resources>

<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{StaticResource RangeConverter}">
      <Binding Path="RefreshTriggeringRevision" Source="{x:Static core:Localizer.Current}" />
      <Binding Path="SortOrder" />
      <Binding Path="ShownCount" />                <!-- {0} -->
      <Binding Path="FileCount" />                 <!-- {1} -->
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
<!-- "3 of 10 files, ascending" -->
```

### Named arguments: `ArgumentNames`

A `MultiBinding` child has no name. A formatter that understands named arguments, such as ICU
MessageFormat, gets the names from `ArgumentNames` on the converter: one comma-separated string whose
entry `i` names the argument at child `2 + i`.

With such a formatter assigned to `ILocalizer.MessageFormatter`, the entry can use names. With a
`Range_FileSortOrder_Ascending` entry of `{Shown} of {Total} files, ascending`:

```xml
<Window.Resources>
  <l:LocalizeEnumConverter x:Key="RangeConverter" KeyPrefix="Range_" ArgumentNames="Shown,Total" />
</Window.Resources>

<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{StaticResource RangeConverter}">
      <Binding Path="RefreshTriggeringRevision" Source="{x:Static core:Localizer.Current}" />
      <Binding Path="SortOrder" />
      <Binding Path="ShownCount" />                <!-- {Shown}, also {0} -->
      <Binding Path="FileCount" />                 <!-- {Total}, also {1} -->
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
<!-- "3 of 10 files, ascending" -->
```

The default formatter, `string.Format`, ignores names and knows only positions: under it the entry must
use `{0}` and `{1}`, as in the previous example. `{Shown}` is not a valid composite format item, so it
renders the failure text and raises `LookupFailed`.

Entries are trimmed, and an empty entry leaves that argument unnamed: `",Total"` names only the second.
Two entries with the same name throw `ArgumentException`. Extra names are ignored, and arguments without
an entry are unnamed. Every argument keeps its position whether it is named or not, and the default
formatter ignores names.

## In code

```csharp
Localizer.Current.Get(FileSortOrder.Ascending);   // "Ascending (A-Z)" / "Aufsteigend (A-Z)"
```

With format arguments, and with a resource manager and a key prefix:

```csharp
Localizer.Current.Get(FileSortOrder.Ascending, MessageArguments.FromValues(count), keyPrefix: "Count_");   // "3 files, ascending"
Localizer.Current.Get(FileSortOrder.Ascending, MessageArguments.FromValues(count), SortingStringsKeys.ResourceManager, "Count_");
```

The optional parameters after the value are `arguments`, `resourceManager` and `keyPrefix`; name the
ones you pass when you skip an earlier one.

## Custom prefix and scoping

The markup extension, the converter and the code API all accept a **`KeyPrefix`** (by default
`null`, which means `Enum_` - see [Changing the key convention](#changing-the-key-convention)) and an
optional **`ResourceManager`** that scopes the lookup to one `.resx` file. That is what lets
the same enum carry different label sets, or keeps enum labels in a file of their own:

```xml
<TextBlock Text="{l:LocalizeEnum Value={Binding SortOrder},
                                 KeyPrefix=Display_,
                                 ResourceManager={x:Static res:SortingStringsKeys.ResourceManager}}" />
```

```csharp
Localizer.Current.Get(FileSortOrder.Ascending, resourceManager: SortingStringsKeys.ResourceManager, keyPrefix: "Display_");
```

`LocalizeEnumConverter.Default` is read-only, so a converter with a custom prefix, a resource manager or
argument names is declared as its own instance in resources:

```xml
<l:LocalizeEnumConverter x:Key="DisplayEnumConverter" KeyPrefix="Display_" />
```

## Changing the key convention

`ILocalizer.EnumKeyBuilder` is the function that turns an enum value and an optional key prefix into
the resource key. Set it once at startup, and it applies to every enum lookup - `Get(Enum, …)`,
`{l:LocalizeEnum}` and `LocalizeEnumConverter`:

```csharp
Localizer.Current.EnumKeyBuilder = static (value, keyPrefix) =>
    (keyPrefix ?? "Text_") + value.GetType().Name + "." + value;

Localizer.Current.Get(FileSortOrder.Ascending);   // looks up "Text_FileSortOrder.Ascending"
```

The function receives two parameters:

1. The enum value of the lookup; never `null`.
2. The key prefix the lookup passed - `keyPrefix` in code, `KeyPrefix` in XAML - or `null` when it
   passed none. `null` means "use your own default"; a non-null value is an explicit override.

The default is the convention above, `{keyPrefix ?? "Enum_"}{EnumTypeName}_{MemberName}`, for example
`Enum_FileSortOrder_Ascending`. `keyPrefix` and `KeyPrefix` therefore override only the prefix: they
are passed to the builder, which decides what to do with them. An application that passes the same
prefix on every enum lookup can make it the builder's default instead.

Assigning a different function later re-resolves bound text, like a language switch; assigning the
current one again does nothing. The builder must not throw or return `null` or an empty string: an
exception it throws propagates out of the lookup, and a `null` or empty result makes the lookup throw
`InvalidOperationException`. Assigning `null` throws `ArgumentNullException`.

## When a member has no entry

An enum member with no matching resource entry resolves like any other missing key: the
`!Enum_FileSortOrder_Ascending!` failure text, and a `LookupFailed` event. See
[Lookup modes and culture fallback](lookup-and-fallback.md).
