# Design time and previewers

XAML previewers show localized strings like the running application does: the Avalonia previewer and
the Visual Studio WPF designer evaluate `{l:Localize}` and `{l:LocalizeEnum}` as live bindings. This
page covers how to choose the language a previewer shows, what a previewer needs to resolve search-all
lookups, and what to expect in Rider's WPF previewer.

## Choose the preview language: `DesignTime.Culture`

Set the attached property `DesignTime.Culture` on the root element of a view to preview it in another
language, without changing initialization code or the machine's language:

**Avalonia:**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.Avalonia;assembly=ResXLocalization.Avalonia"
        l:DesignTime.Culture="de">
  <!-- the previewer shows German; at run time the attribute has no effect -->
</Window>
```

**WPF:**

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
        l:DesignTime.Culture="de">
  <!-- the designer shows German; at run time the attribute has no effect -->
</Window>
```

`xmlns:l` is the same namespace import that `{l:Localize}` uses.

- **Process-wide.** Inside a previewer, the property assigns `Localizer.Current.CurrentCulture`. Strings
  that a view model looks up in code, such as `Localizer.Current.Get(AppStringsKeys.Greeting)`, therefore
  agree with the XAML.
- **No run-time effect.** Outside a previewer, setting the property only stores the value; the culture of
  `Localizer.Current` stays as it is. Nothing has to be hidden with `mc:Ignorable`.
- **Re-renders on edit.** Changing the value, for example from `de` to `en`, re-renders the preview in the
  new language.
- **Removing the attribute keeps the language.** When you delete `l:DesignTime.Culture`, nothing assigns
  the culture, so the previewer keeps showing the last language it was set to. To go back just close the preview
  and re-open it.
- **Typing is safe.** An empty value and a name that `CultureInfo` rejects, such as a half-typed one, leave
  the culture unchanged; nothing throws.

## The culture a previewer uses without it

Without `DesignTime.Culture`, a previewer uses the machine's UI culture - the default of
`Localizer.Current.CurrentCulture` is `CultureInfo.CurrentUICulture` - unless initialization that the
previewer executes selects another culture.

- **Avalonia:** the previewer runs `BuildAvaloniaApp`, so a culture assigned there applies in the previewer
  too.
- **WPF:** the designer does not call `App.OnStartup`, so a culture assigned there does not apply at design
  time. A `[ModuleInitializer]` of the application assembly does run in the designer; the WPF sample
  application selects English in its module initializer for this reason (see the next section).

`DesignTime.Culture` overrides a culture selected by such initialization: with
`l:DesignTime.Culture="de"`, the WPF sample's views preview in German although its module initializer
selects English.

## Initialization for the WPF designer: a module initializer

Because the WPF designer does not call `App.OnStartup`, a search-all lookup such as
`{l:Localize Greeting}` renders the failure text there when the resource files are registered only in
`OnStartup`. The same holds for `Localizer.Current.UseMessageFormat()` of the
`ResXLocalization.MessageFormat` package: called only in `OnStartup`, it does not apply in the designer,
which then formats with the default formatter.

Do this initialization in a `[ModuleInitializer]` of the application assembly instead. It runs before the
first use of the assembly - at run time before `Main`, and in the WPF designer before the first view is
rendered:

```csharp
using System.Globalization;
using System.Runtime.CompilerServices;
using RentADeveloper.ResXLocalization;
using RentADeveloper.ResXLocalization.MessageFormat;   // only with the MessageFormat package
using YourApp.Resources;

internal static class LocalizationInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Localizer.Current.CurrentCulture = new CultureInfo("en");
        Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
        Localizer.Current.UseMessageFormat();                 // only with the MessageFormat package
    }
}
```

[Registering resource files](registration.md#wpf) covers registration in a module initializer in more
detail, including views in a class library; [ICU MessageFormat](message-format.md#opting-in) covers
`UseMessageFormat()`.

## Rider's WPF previewer

**The running application, the Visual Studio WPF designer and the Avalonia previewer (in Visual Studio
and in Rider) are not affected by anything in this section.**

Rider's XAML previewer has a number of defects that prevent ResXLocalization from working correctly in
it, and the same defects break many other WPF libraries - this is not a ResXLocalization shortcoming or a
support decision.

Rider's WPF previewer supplies a target property to a markup extension as a CLR property instead of a WPF
dependency property, and WPF then throws for every binding a markup extension returns. In Rider's WPF
previewer only, `{l:Localize}` and `{l:LocalizeEnum}` therefore return a plain string instead of a
binding, outside style setters, so the window renders (style setters are covered below). Which text you
see there depends on how the key is written, because Rider delivers some keys to the markup extension and
not others.

**Rendered: the raw resource value.** A key written as the first value inside the braces, or as the
`Key` attribute of an `<l:Localize>` element, renders its resource value as written, under the default
formatter. The value is looked up without arguments, because the `<l:Argument>` bindings have no data
context yet when the markup extension produces the string:

```xml
<!-- Both render "{0} people invited" in Rider's WPF previewer (PeopleInvited is "{0} people invited"). -->
<TextBlock Text="{l:Localize PeopleInvited}" />

<TextBlock>
  <TextBlock.Text>
    <l:Localize Key="{x:Static res:AppStringsKeys.PeopleInvited}">
      <l:Argument Value="{Binding PeopleCount}" />
    </l:Localize>
  </TextBlock.Text>
</TextBlock>
```

**Rendered empty.** Rider does not deliver a key written with `{x:Static}` inside the braces, a key
assigned with `Key=` inside the braces, or a bound key:

```xml
<!-- All three render an empty text in Rider's WPF previewer. -->
<TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />
<TextBlock Text="{l:Localize Key=Greeting}" />
<TextBlock Text="{l:Localize {Binding StatusKey}}" />
```

**Enum values.** The same rules apply to the enum value of `{l:LocalizeEnum}`, its `Value` property; here
`xmlns:m` is the namespace of the enum type `FileSortOrder`. A fixed enum member written as the `Value`
attribute of an `<l:LocalizeEnum>` element renders its text; a fixed member inside the braces and a bound
enum value render empty:

```xml
<!-- Renders the text of the resource Enum_FileSortOrder_Ascending in Rider's WPF previewer. -->
<TextBlock>
  <TextBlock.Text>
    <l:LocalizeEnum Value="{x:Static m:FileSortOrder.Ascending}" />
  </TextBlock.Text>
</TextBlock>

<!-- Both render an empty text in Rider's WPF previewer. -->
<TextBlock Text="{l:LocalizeEnum Value={x:Static m:FileSortOrder.Ascending}}" />
<TextBlock Text="{l:LocalizeEnum Value={Binding SortOrder}}" />
```

More differences in Rider's WPF previewer:

- **Style setters** keep a live binding, as at run time, when Rider delivers the key. When Rider does not
  deliver it, the setter's text renders empty, and Rider's previewer reports the error
  `{l:Localize} has no Key` for the setter's `Value` property, although the key is written correctly; the
  running application and the Visual Studio designer render the text. This error is a consequence of the
  Rider defect, not of your markup:

  ```xml
  <!-- Renders "Hello and welcome!" in Rider's WPF previewer. -->
  <Setter Property="Text" Value="{l:Localize Greeting}" />

  <!-- Renders an empty text in Rider's WPF previewer, which reports "{l:Localize} has no Key". -->
  <Setter Property="Text" Value="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />
  ```

- **Texts bound to a view model** render empty when Rider cannot create the design-time view model, for
  example one without a parameterless constructor.
- **Errors in the markup** outside style setters, such as a `{l:Localize}` without a key, are not reported
  in Rider's previewer; the running application and the Visual Studio designer report them.
- **`DesignTime.Culture`** changes the culture, but the previewer silently shows the neutral resources:
  Rider loads the project assemblies in a way that prevents .NET from finding the satellite assemblies of
  other languages. The lookup succeeds with the neutral value, so neither the failure text nor
  `Localizer.Current.LookupFailed` reports it.

With ICU MessageFormat installed through `Localizer.Current.UseMessageFormat()`, the previewer's lookup
without arguments is still parsed as ICU: escaping such as `It''s ready` is interpreted, and a value that
needs a variable, such as `Hello {Name}`, renders the failure text and raises
`Localizer.Current.LookupFailed` - see [Every resolved value is ICU](message-format.md#every-resolved-value-is-icu).

The defects are reported to JetBrains:

- [RIDER-143207](https://youtrack.jetbrains.com/issue/RIDER-143207/WPF-XAML-previewer-supplies-RuntimePropertyInfo-instead-of-DependencyProperty-to-a-custom-markup-extension-causing-exceptions):
  the previewer supplies a CLR property instead of a dependency property to a markup extension.
- [RIDER-143052](https://youtrack.jetbrains.com/issue/RIDER-143052/WPF-previewer-fails-to-resolve-ResX-satellite-assemblies-when-loading-project-assemblies-from-bytes):
  the previewer does not resolve the satellite assemblies of other languages.
- [RIDER-143208](https://youtrack.jetbrains.com/issue/RIDER-143208): the previewer reports that it is not
  in design mode for the root `Window` and supplies no target property for `Window.Title`.

Further defects were found that are not on this list. When JetBrains fixes the previewer so that it
supplies dependency properties, `{l:Localize}` and `{l:LocalizeEnum}` return live bindings there too,
without a change to your application or to ResXLocalization.
