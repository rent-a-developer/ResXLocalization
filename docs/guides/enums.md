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

## Custom prefix and scoping

The markup extension, the converter and the code API all accept a **`KeyPrefix`** (default `Enum_`)
and an optional **`ResourceManager`** that scopes the lookup to one `.resx` file. That is what lets
the same enum carry different label sets, or keeps enum labels in a file of their own:

```xml
<TextBlock Text="{l:LocalizeEnum KeyPrefix=Display_,
                                 ResourceManager={x:Static res:SortingStrings.ResourceManager}}" />
```

```csharp
Localizer.Current.Get(FileSortOrder.Ascending, SortingStrings.ResourceManager, "Display_");
```

`LocalizeEnumConverter.Default` is read-only, so a converter with a custom prefix is declared as its
own instance in resources:

```xml
<l:LocalizeEnumConverter x:Key="DisplayEnumConverter" KeyPrefix="Display_" />
```

## When a member has no entry

An enum member with no matching resource entry resolves like any other missing key: the
`!Enum_FileSortOrder_Ascending!` sentinel, and a `TranslationNotFound` event. See
[Lookup modes and culture fallback](lookup-and-fallback.md).
