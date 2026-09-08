using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Controls;

public sealed class SyntaxTextBlock : TextBlock
{
    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code),
        typeof(string),
        typeof(SyntaxTextBlock),
        new(string.Empty, OnCodeChanged)
    );

    private static readonly Brush KeywordBrush = ParseBrush("#0000FF");
    private static readonly Brush MemberBrush = ParseBrush("#660E7A");

    private static readonly Brush PunctuationBrush = ParseBrush("#808080");
    private static readonly Brush ResourceBrush = ParseBrush("#2B91AF");
    private static readonly Brush StringBrush = ParseBrush("#008000");
    private static readonly Brush TextBrush = ParseBrush("#000000");

    public string Code
    {
        get => (string)this.GetValue(CodeProperty);
        set => this.SetValue(CodeProperty, value);
    }

    private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value is '_' or ':' or '.';

    private static bool IsPunctuation(char value) =>
        value is '<' or '>' or '/' or '{' or '}' or '(' or ')' or '[' or ']' or ',' or '=';

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SyntaxTextBlock)d).Highlight();

    private static Brush ParseBrush(string hex) =>
        (SolidColorBrush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.Black);

    private static Brush SelectIdentifierBrush(string text) =>
        text switch
        {
            _ when text.Contains(':', StringComparison.Ordinal) => KeywordBrush,
            _ when text.Contains('.', StringComparison.Ordinal) => ResourceBrush,
            "StaticResource" => KeywordBrush,
            "Localizer" or "Get" or "ResourceManager" or "Converter" or "Key" or "KeyPrefix" or "Code" => MemberBrush,
            _ => TextBrush,
        };

    private void AppendRun(string text, Brush foreground) =>
        this.Inlines.Add(new Run(text) { Foreground = foreground });

    private void Highlight()
    {
        this.Inlines.Clear();

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
