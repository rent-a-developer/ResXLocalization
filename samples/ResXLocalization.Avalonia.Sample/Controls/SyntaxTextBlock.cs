using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Controls;

public sealed class SyntaxTextBlock : TextBlock
{
    public String Code
    {
        get => this.GetValue(CodeProperty);
        set => this.SetValue(CodeProperty, value);
    }

    public static readonly StyledProperty<String> CodeProperty =
        AvaloniaProperty.Register<SyntaxTextBlock, String>(nameof(Code), String.Empty);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CodeProperty)
        {
            this.Highlight();
        }
    }

    private void AppendRun(String text, IBrush foreground) =>
        this.Inlines?.Add(new Run(text) { Foreground = foreground });

    private void Highlight()
    {
        this.Inlines?.Clear();

        if (String.IsNullOrEmpty(this.Code))
        {
            return;
        }

        var index = 0;
        while (index < this.Code.Length)
        {
            if (this.TryReadQuotedString(ref index))
            {
                continue;
            }

            if (this.TryReadIdentifier(ref index))
            {
                continue;
            }

            var current = this.Code[index];
            var brush = IsPunctuation(current) ? PunctuationBrush : TextBrush;
            this.AppendRun(current.ToString(), brush);
            index++;
        }
    }

    private Boolean TryReadIdentifier(ref Int32 index)
    {
        if (!IsIdentifierPart(this.Code[index]))
        {
            return false;
        }

        var start = index;
        while (index < this.Code.Length && IsIdentifierPart(this.Code[index]))
        {
            index++;
        }

        var text = this.Code[start..index];
        this.AppendRun(text, SelectIdentifierBrush(text));
        return true;
    }

    private Boolean TryReadQuotedString(ref Int32 index)
    {
        if (this.Code[index] != '"')
        {
            return false;
        }

        var start = index++;
        while (index < this.Code.Length)
        {
            var current = this.Code[index++];
            if (current == '"')
            {
                break;
            }
        }

        this.AppendRun(this.Code[start..index], StringBrush);
        return true;
    }

    private static Boolean IsIdentifierPart(Char value) =>
        Char.IsLetterOrDigit(value) || value is '_' or ':' or '.';

    private static Boolean IsPunctuation(Char value) =>
        value is '<' or '>' or '/' or '{' or '}' or '(' or ')' or '[' or ']' or ',' or '=';

    private static IBrush SelectIdentifierBrush(String text) =>
        text switch
        {
            _ when text.Contains(':', StringComparison.Ordinal) => KeywordBrush,
            _ when text.Contains('.', StringComparison.Ordinal) => ResourceBrush,
            "StaticResource" => KeywordBrush,
            "Localizer" or "Get" or "ResourceManager" or "Converter" or "Key" or "KeyPrefix" or "Code" =>
                MemberBrush,
            _ => TextBrush
        };

    private static readonly IBrush KeywordBrush = SolidColorBrush.Parse("#0000FF");
    private static readonly IBrush MemberBrush = SolidColorBrush.Parse("#660E7A");

    private static readonly IBrush PunctuationBrush = SolidColorBrush.Parse("#808080");
    private static readonly IBrush ResourceBrush = SolidColorBrush.Parse("#2B91AF");
    private static readonly IBrush StringBrush = SolidColorBrush.Parse("#008000");
    private static readonly IBrush TextBrush = SolidColorBrush.Parse("#000000");
}
