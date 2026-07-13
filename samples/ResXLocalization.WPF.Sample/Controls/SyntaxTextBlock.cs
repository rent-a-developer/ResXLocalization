using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Controls;

public sealed class SyntaxTextBlock : TextBlock
{
    public String Code
    {
        get => (String)this.GetValue(CodeProperty);
        set => this.SetValue(CodeProperty, value);
    }

    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register(
            nameof(Code),
            typeof(String),
            typeof(SyntaxTextBlock),
            new(String.Empty, OnCodeChanged)
        );

    private void AppendRun(String text, Brush foreground) =>
        this.Inlines.Add(new Run(text) { Foreground = foreground });

    private void Highlight()
    {
        this.Inlines.Clear();

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

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SyntaxTextBlock)d).Highlight();

    private static Brush ParseBrush(String hex) =>
        (SolidColorBrush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.Black);

    private static Brush SelectIdentifierBrush(String text) =>
        text switch
        {
            _ when text.Contains(':', StringComparison.Ordinal) => KeywordBrush,
            _ when text.Contains('.', StringComparison.Ordinal) => ResourceBrush,
            "StaticResource" => KeywordBrush,
            "Localizer" or "Get" or "ResourceManager" or "Converter" or "Key" or "KeyPrefix" or "Code" =>
                MemberBrush,
            _ => TextBrush
        };

    private static readonly Brush KeywordBrush = ParseBrush("#0000FF");
    private static readonly Brush MemberBrush = ParseBrush("#660E7A");

    private static readonly Brush PunctuationBrush = ParseBrush("#808080");
    private static readonly Brush ResourceBrush = ParseBrush("#2B91AF");
    private static readonly Brush StringBrush = ParseBrush("#008000");
    private static readonly Brush TextBrush = ParseBrush("#000000");
}
