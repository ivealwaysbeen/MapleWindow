using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;

namespace MapleWindow.App.Views;

public partial class SpeechBubbleControl : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SpeechBubbleControl),
            new PropertyMetadata(string.Empty, OnContentChanged));

    public static readonly DependencyProperty EmphasisTermsProperty =
        DependencyProperty.Register(nameof(EmphasisTerms), typeof(IReadOnlyList<string>), typeof(SpeechBubbleControl),
            new PropertyMetadata(Array.Empty<string>(), OnContentChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Content name / boss difficulty substrings to render in the Bold weight; everything else stays Light.</summary>
    public IReadOnlyList<string> EmphasisTerms
    {
        get => (IReadOnlyList<string>)GetValue(EmphasisTermsProperty);
        set => SetValue(EmphasisTermsProperty, value);
    }

    public SpeechBubbleControl()
    {
        InitializeComponent();
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SpeechBubbleControl)d).Rebuild();

    private void Rebuild()
    {
        var terms = EmphasisTerms?.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList() ?? [];

        BubbleTextBlock.Inlines.Clear();
        if (string.IsNullOrEmpty(Text)) return;

        var text = KeepWordsTogether(Text);

        if (terms.Count == 0)
        {
            BubbleTextBlock.Inlines.Add(new Run(text));
            return;
        }

        var pattern = string.Join('|', terms.Select(t => Regex.Escape(KeepWordsTogether(t))));
        var lastIndex = 0;
        foreach (Match match in Regex.Matches(text, pattern))
        {
            if (match.Index > lastIndex) BubbleTextBlock.Inlines.Add(new Run(text[lastIndex..match.Index]));
            BubbleTextBlock.Inlines.Add(new Run(match.Value) { FontWeight = FontWeights.Bold });
            lastIndex = match.Index + match.Length;
        }
        if (lastIndex < text.Length) BubbleTextBlock.Inlines.Add(new Run(text[lastIndex..]));
    }

    /// <summary>Inserts a zero-width word joiner (U+2060) between adjacent non-whitespace characters so
    /// WPF's line breaker — which otherwise treats every Hangul syllable as its own break opportunity —
    /// only wraps at the real spaces between words, never inside one.</summary>
    private static string KeepWordsTogether(string text)
    {
        var sb = new StringBuilder(text.Length * 2);
        for (var i = 0; i < text.Length; i++)
        {
            sb.Append(text[i]);
            if (i < text.Length - 1 && !char.IsWhiteSpace(text[i]) && !char.IsWhiteSpace(text[i + 1]))
                sb.Append('⁠');
        }
        return sb.ToString();
    }
}
