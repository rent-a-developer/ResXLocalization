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

## In item templates: `{l:LocalizeEnum}`

Inside a `ComboBox` or `ListBox` item template each item *is* the enum value — it is the
`DataContext` — so the markup extension localizes it directly:

```xml
<ComboBox ItemsSource="{Binding FileSortOrders}"
          SelectedItem="{Binding SelectedFileSortOrder}">
  <ComboBox.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{l:LocalizeEnum}" />
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

## As a bound value: `LocalizeEnumConverter`

When the enum is a bound property rather than the `DataContext`, use `LocalizeEnumConverter` in a
`MultiBinding`. The second binding — to the current culture — is what re-triggers the conversion on
a language switch:

```xml
<!-- xmlns:core="clr-namespace:RentADeveloper.ResXLocalization;assembly=ResXLocalization.Core" -->
<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{x:Static l:LocalizeEnumConverter.Default}">
      <Binding Path="SelectedFileSortOrder" />
      <Binding Path="CurrentCulture" Source="{x:Static core:Localizer.Current}" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

## In code

```csharp
Localizer.Current.Get(FileSortOrder.Ascending);   // "Ascending (A-Z)" / "Aufsteigend (A-Z)"
```

An enum label can have placeholders and take [format arguments](format-arguments.md), like any other
lookup. With a `Count_FileSortOrder_Ascending` entry of `{0} files, ascending`:

```csharp
Localizer.Current.Get(FileSortOrder.Ascending, MessageArguments.FromValues(count), keyPrefix: "Count_");   // "3 files, ascending"
Localizer.Current.Get(FileSortOrder.Ascending, MessageArguments.FromValues(count), SortingStrings.ResourceManager, "Count_");
```

The optional parameters after the value are `arguments`, `resourceManager` and `keyPrefix`; name the
ones you pass when you skip an earlier one.

## Custom prefix and scoping

The markup extension, the converter and the code API all accept a **`KeyPrefix`** (by default
`null`, which means `Enum_` - see [Changing the key convention](#changing-the-key-convention)) and an
optional **`ResourceManager`** that scopes the lookup to one `.resx` file. That is what lets
the same enum carry different label sets, or keeps enum labels in a file of their own:

```xml
<TextBlock Text="{l:LocalizeEnum KeyPrefix=Display_,
                                 ResourceManager={x:Static res:SortingStrings.ResourceManager}}" />
```

```csharp
Localizer.Current.Get(FileSortOrder.Ascending, resourceManager: SortingStrings.ResourceManager, keyPrefix: "Display_");
```

`LocalizeEnumConverter.Default` is read-only, so a converter with a custom prefix is declared as its
own instance in resources:

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
