using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Controls;

public sealed class SyntaxTextBlock : TextBlock
{
    public static readonly StyledProperty<string> CodeProperty = AvaloniaProperty.Register<SyntaxTextBlock, string>(
        nameof(Code),
        string.Empty
    );

    private static readonly IBrush KeywordBrush = SolidColorBrush.Parse("#0000FF");
    private static readonly IBrush MemberBrush = SolidColorBrush.Parse("#660E7A");

    private static readonly IBrush PunctuationBrush = SolidColorBrush.Parse("#808080");
    private static readonly IBrush ResourceBrush = SolidColorBrush.Parse("#2B91AF");
    private static readonly IBrush StringBrush = SolidColorBrush.Parse("#008000");
    private static readonly IBrush TextBrush = SolidColorBrush.Parse("#000000");

    public string Code
    {
        get => this.GetValue(CodeProperty);
        set => this.SetValue(CodeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CodeProperty)
        {
            this.Highlight();
        }
    }

    private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value is '_' or ':' or '.';

    private static bool IsPunctuation(char value) =>
        value is '<' or '>' or '/' or '{' or '}' or '(' or ')' or '[' or ']' or ',' or '=';

    private static IBrush SelectIdentifierBrush(string text) =>
        text switch
        {
            _ when text.Contains(':', StringComparison.Ordinal) => KeywordBrush,
            _ when text.Contains('.', StringComparison.Ordinal) => ResourceBrush,
            "StaticResource" => KeywordBrush,
            "Localizer" or "Get" or "ResourceManager" or "Converter" or "Key" or "KeyPrefix" or "Code" => MemberBrush,
            _ => TextBrush,
        };

    private void AppendRun(string text, IBrush foreground) =>
        this.Inlines?.Add(new Run(text) { Foreground = foreground });

    private void Highlight()
    {
        this.Inlines?.Clear();

        if (string.IsNullOrEmpty(this.Code))
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

    private bool TryReadIdentifier(ref int index)
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

    private bool TryReadQuotedString(ref int index)
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
}
