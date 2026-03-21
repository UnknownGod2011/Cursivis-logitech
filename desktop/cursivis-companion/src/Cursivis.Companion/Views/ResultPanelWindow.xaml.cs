using Cursivis.Companion.Infrastructure;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace Cursivis.Companion.Views;

public partial class ResultPanelWindow : Window
{
    private static readonly Regex HeadingRegex = new(@"^\s{0,3}(#{1,6})\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex BulletRegex = new(@"^\s*[-*]\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex NumberedRegex = new(@"^\s*\d+\.\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex MathLineRegex = new(@"(\\[A-Za-z]+|\$|[\^_]|=|≤|≥|≠|√)", RegexOptions.Compiled);
    private static readonly Regex FractionRegex = new(@"\\frac\{([^{}]+)\}\{([^{}]+)\}", RegexOptions.Compiled);
    private static readonly Regex SqrtRegex = new(@"\\sqrt\{([^{}]+)\}", RegexOptions.Compiled);
    private static readonly Regex TextRegex = new(@"\\text\{([^{}]+)\}", RegexOptions.Compiled);
    private static readonly Regex InlineMathRegex = new(@"\$\$(.+?)\$\$|\$(.+?)\$|\\\((.+?)\\\)|\\\[(.+?)\\\]", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex SuperscriptRegex = new(@"(?<base>[A-Za-z0-9\)\]])\^(?<exp>\{[^{}]+\}|[A-Za-z0-9+\-=().]+)", RegexOptions.Compiled);
    private static readonly Regex SubscriptRegex = new(@"(?<base>[A-Za-z0-9\)\]])_(?<sub>\{[^{}]+\}|[A-Za-z0-9+\-=().]+)", RegexOptions.Compiled);
    private static readonly Regex BoldRegex = new(@"\*\*(.+?)\*\*|__(.+?)__", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> LatexTokenMap = new(StringComparer.Ordinal)
    {
        ["\\alpha"] = "alpha",
        ["\\beta"] = "beta",
        ["\\gamma"] = "gamma",
        ["\\delta"] = "delta",
        ["\\theta"] = "theta",
        ["\\lambda"] = "lambda",
        ["\\mu"] = "mu",
        ["\\pi"] = "pi",
        ["\\sigma"] = "sigma",
        ["\\phi"] = "phi",
        ["\\omega"] = "omega",
        ["\\times"] = "\u00D7",
        ["\\cdot"] = "\u00B7",
        ["\\leq"] = "\u2264",
        ["\\geq"] = "\u2265",
        ["\\neq"] = "\u2260",
        ["\\approx"] = "\u2248",
        ["\\pm"] = "\u00B1",
        ["\\sum"] = "\u2211",
        ["\\prod"] = "\u220F",
        ["\\infty"] = "\u221E",
        ["\\rightarrow"] = "\u2192",
        ["\\Rightarrow"] = "\u21D2"
    };

    private static readonly Dictionary<char, char> SuperscriptMap = new()
    {
        ['0'] = '\u2070',
        ['1'] = '\u00B9',
        ['2'] = '\u00B2',
        ['3'] = '\u00B3',
        ['4'] = '\u2074',
        ['5'] = '\u2075',
        ['6'] = '\u2076',
        ['7'] = '\u2077',
        ['8'] = '\u2078',
        ['9'] = '\u2079',
        ['+'] = '\u207A',
        ['-'] = '\u207B',
        ['='] = '\u207C',
        ['('] = '\u207D',
        [')'] = '\u207E',
        ['n'] = '\u207F',
        ['i'] = '\u2071'
    };

    private static readonly Dictionary<char, char> SubscriptMap = new()
    {
        ['0'] = '\u2080',
        ['1'] = '\u2081',
        ['2'] = '\u2082',
        ['3'] = '\u2083',
        ['4'] = '\u2084',
        ['5'] = '\u2085',
        ['6'] = '\u2086',
        ['7'] = '\u2087',
        ['8'] = '\u2088',
        ['9'] = '\u2089',
        ['+'] = '\u208A',
        ['-'] = '\u208B',
        ['='] = '\u208C',
        ['('] = '\u208D',
        [')'] = '\u208E',
        ['a'] = '\u2090',
        ['e'] = '\u2091',
        ['h'] = '\u2095',
        ['i'] = '\u1D62',
        ['j'] = '\u2C7C',
        ['k'] = '\u2096',
        ['l'] = '\u2097',
        ['m'] = '\u2098',
        ['n'] = '\u2099',
        ['o'] = '\u2092',
        ['p'] = '\u209A',
        ['r'] = '\u1D63',
        ['s'] = '\u209B',
        ['t'] = '\u209C',
        ['u'] = '\u1D64',
        ['v'] = '\u1D65',
        ['x'] = '\u2093'
    };

    private bool _isUserPositioned;
    private bool _hasInitialPlacement;
    private CancellationTokenSource? _revealCts;

    public ResultPanelWindow()
    {
        InitializeComponent();
        UiPresentation.ApplyShinyText(ActionText, ColorFromHex("#E4B4FF"), ColorFromHex("#FFFFFF"), 2.6);
        Deactivated += (_, _) =>
        {
            if (IsVisible)
            {
                Hide();
            }
        };
    }

    public event EventHandler? InsertRequested;

    public event EventHandler? MoreOptionsRequested;

    public event EventHandler? TakeActionRequested;

    public event EventHandler? UndoRequested;

    public string LastResult { get; private set; } = string.Empty;

    public void ShowResult(string action, string output, Point cursor)
    {
        LastResult = output;
        TitleText.Text = "Cursivis";
        ActionText.Text = $"Action: {action}";
        TakeActionButton.IsEnabled = true;
        TakeActionButton.Visibility = Visibility.Visible;

        PositionPanel(cursor);
        EnsureShown();
        StartPresentation(output);
    }

    public void SetUndoAvailable(bool isAvailable)
    {
        UndoButton.IsEnabled = isAvailable;
    }

    public void ShowInfo(string text, Point cursor, bool allowTakeAction = false)
    {
        LastResult = text;
        TitleText.Text = "Cursivis";
        ActionText.Text = allowTakeAction ? "Action: Take Action Status" : "Action: System Status";
        TakeActionButton.IsEnabled = allowTakeAction;
        TakeActionButton.Visibility = allowTakeAction ? Visibility.Visible : Visibility.Collapsed;

        PositionPanel(cursor);
        EnsureShown();
        StartPresentation(text);
    }

    public void HidePanel()
    {
        if (IsVisible)
        {
            Hide();
        }
    }

    private void InsertButton_OnClick(object sender, RoutedEventArgs e)
    {
        InsertRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MoreOptionsButton_OnClick(object sender, RoutedEventArgs e)
    {
        MoreOptionsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TakeActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        TakeActionRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UndoButton_OnClick(object sender, RoutedEventArgs e)
    {
        UndoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PositionPanel(Point cursor)
    {
        if (_isUserPositioned)
        {
            return;
        }

        if (!_hasInitialPlacement)
        {
            _hasInitialPlacement = true;
            Left = cursor.X + 55;
            Top = cursor.Y + 65;
        }

        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left + 8, Math.Min(Left, workArea.Right - Width - 8));
        Top = Math.Max(workArea.Top + 8, Math.Min(Top, workArea.Bottom - Height - 8));
    }

    private void DragHeader_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        _isUserPositioned = true;
        try
        {
            DragMove();
        }
        catch
        {
            // Ignore drag interruption.
        }
    }

    private void EnsureShown()
    {
        if (!IsVisible)
        {
            Show();
        }
    }

    private void StartPresentation(string body)
    {
        UiPresentation.AnimateEntrance(RootCard, PanelTranslateTransform, fromY: 18, durationMs: 280);
        _ = PresentBodyAsync(body);
    }

    private async Task PresentBodyAsync(string body)
    {
        _revealCts?.Cancel();
        _revealCts?.Dispose();
        _revealCts = new CancellationTokenSource();
        var cancellationToken = _revealCts.Token;

        try
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ResultDocumentBox.Document = BuildDocument(body);
                ResultDocumentBox.CaretPosition = ResultDocumentBox.Document.ContentStart;
                ResultDocumentBox.ScrollToHome();
            });
        }
        catch (OperationCanceledException)
        {
            // No-op.
        }
        finally
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static FlowDocument BuildDocument(string body)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(ColorFromHex("#FFF7FBFF")),
            FontFamily = new FontFamily("Bahnschrift"),
            FontSize = 13,
            LineHeight = 21,
            TextAlignment = TextAlignment.Left
        };

        var normalized = NormalizeNewlines(body);
        var lines = normalized.Split('\n');
        var pendingSpacing = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                pendingSpacing = true;
                continue;
            }

            var paragraph = BuildParagraph(line);
            if (pendingSpacing && document.Blocks.Count > 0)
            {
                paragraph.Margin = new Thickness(0, 8, 0, 0);
            }

            document.Blocks.Add(paragraph);
            pendingSpacing = false;
        }

        if (document.Blocks.Count == 0)
        {
            document.Blocks.Add(new Paragraph(new Run(string.Empty)));
        }

        return document;
    }

    private static Paragraph BuildParagraph(string line)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 6)
        };

        var headingMatch = HeadingRegex.Match(line);
        if (headingMatch.Success)
        {
            var level = headingMatch.Groups[1].Value.Length;
            var headingText = CleanInlineMarkdown(headingMatch.Groups[2].Value);
            paragraph.Inlines.Add(new Bold(new Run(headingText)));
            paragraph.FontSize = Math.Max(14, 18 - level);
            paragraph.Foreground = new SolidColorBrush(ColorFromHex("#FFF6D6FF"));
            return paragraph;
        }

        var bulletMatch = BulletRegex.Match(line);
        if (bulletMatch.Success)
        {
            paragraph.Inlines.Add(new Run("\u2022 ")
            {
                Foreground = new SolidColorBrush(ColorFromHex("#FFE4B7FF")),
                FontWeight = FontWeights.SemiBold
            });
            AppendFormattedInlines(paragraph.Inlines, bulletMatch.Groups[1].Value);
            return paragraph;
        }

        var numberedMatch = NumberedRegex.Match(line);
        if (numberedMatch.Success)
        {
            var prefix = line[..(line.IndexOf('.', StringComparison.Ordinal) + 1)] + " ";
            paragraph.Inlines.Add(new Run(prefix)
            {
                Foreground = new SolidColorBrush(ColorFromHex("#FFE4B7FF")),
                FontWeight = FontWeights.SemiBold
            });
            AppendFormattedInlines(paragraph.Inlines, numberedMatch.Groups[1].Value);
            return paragraph;
        }

        if (LooksLikeMathLine(line))
        {
            paragraph.Background = new SolidColorBrush(Color.FromArgb(52, 64, 116, 142));
            paragraph.Padding = new Thickness(8, 4, 8, 4);
            paragraph.Margin = new Thickness(0, 2, 0, 8);
            AppendMathInline(paragraph.Inlines, line);
            return paragraph;
        }

        AppendFormattedInlines(paragraph.Inlines, line);
        return paragraph;
    }

    private static void AppendFormattedInlines(InlineCollection inlines, string text)
    {
        var index = 0;
        while (index < text.Length)
        {
            var nextToken = FindNextToken(text, index);
            if (nextToken.Index > index)
            {
                inlines.Add(new Run(NormalizeLatexPlain(text[index..nextToken.Index])));
            }

            if (nextToken.Index < 0)
            {
                inlines.Add(new Run(NormalizeLatexPlain(text[index..])));
                break;
            }

            switch (nextToken.Kind)
            {
                case TokenKind.Bold:
                    var boldContent = text.Substring(nextToken.ContentStart, nextToken.ContentLength);
                    inlines.Add(new Bold(new Run(NormalizeLatexPlain(CleanInlineMarkdown(boldContent)))));
                    index = nextToken.NextIndex;
                    break;
                case TokenKind.Code:
                    var codeContent = text.Substring(nextToken.ContentStart, nextToken.ContentLength);
                    var codeSpan = new Span(new Run(codeContent))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromArgb(70, 29, 40, 52)),
                        Foreground = new SolidColorBrush(ColorFromHex("#FFD9F2FF"))
                    };
                    inlines.Add(codeSpan);
                    index = nextToken.NextIndex;
                    break;
                case TokenKind.Math:
                    var mathContent = text.Substring(nextToken.ContentStart, nextToken.ContentLength);
                    AppendMathInline(inlines, mathContent);
                    index = nextToken.NextIndex;
                    break;
                default:
                    index = text.Length;
                    break;
            }
        }
    }

    private static void AppendMathInline(InlineCollection inlines, string mathContent)
    {
        var mathSpan = new Span(new Run(NormalizeLatexPlain(mathContent)))
        {
            FontFamily = new FontFamily("Cambria Math"),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ColorFromHex("#FFF3D7FF"))
        };
        inlines.Add(mathSpan);
    }

    private static bool LooksLikeMathLine(string line)
    {
        return MathLineRegex.IsMatch(line);
    }

    private static string NormalizeLatexPlain(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var formatted = value;
        formatted = formatted.Replace("\r", string.Empty, StringComparison.Ordinal);
        formatted = formatted.Replace("\\(", string.Empty, StringComparison.Ordinal);
        formatted = formatted.Replace("\\)", string.Empty, StringComparison.Ordinal);
        formatted = formatted.Replace("\\[", string.Empty, StringComparison.Ordinal);
        formatted = formatted.Replace("\\]", string.Empty, StringComparison.Ordinal);
        formatted = formatted.Replace("$$", string.Empty, StringComparison.Ordinal);

        string previous;
        do
        {
            previous = formatted;
            formatted = FractionRegex.Replace(formatted, match =>
            {
                var numerator = NormalizeLatexPlain(match.Groups[1].Value);
                var denominator = NormalizeLatexPlain(match.Groups[2].Value);
                return $"({numerator})/({denominator})";
            });
            formatted = SqrtRegex.Replace(formatted, match => $"sqrt({NormalizeLatexPlain(match.Groups[1].Value)})");
            formatted = TextRegex.Replace(formatted, match => NormalizeLatexPlain(match.Groups[1].Value));
        }
        while (!string.Equals(previous, formatted, StringComparison.Ordinal));

        foreach (var token in LatexTokenMap)
        {
            formatted = formatted.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        formatted = SuperscriptRegex.Replace(formatted, match =>
        {
            var baseValue = match.Groups["base"].Value;
            var exponent = TrimLatexBraces(match.Groups["exp"].Value);
            return baseValue + ConvertScript(exponent, SuperscriptMap);
        });

        formatted = SubscriptRegex.Replace(formatted, match =>
        {
            var baseValue = match.Groups["base"].Value;
            var subscript = TrimLatexBraces(match.Groups["sub"].Value);
            return baseValue + ConvertScript(subscript, SubscriptMap);
        });

        formatted = Regex.Replace(formatted, @"\{|\}", string.Empty);
        formatted = Regex.Replace(formatted, @"\s+", " ");
        return formatted.Trim();
    }

    private static string ConvertScript(string value, IReadOnlyDictionary<char, char> map)
    {
        var output = new List<char>(value.Length);
        foreach (var character in value)
        {
            if (map.TryGetValue(character, out var converted))
            {
                output.Add(converted);
            }
            else
            {
                output.Add(character);
            }
        }

        return new string(output.ToArray());
    }

    private static string TrimLatexBraces(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[^1] == '}')
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    private static string CleanInlineMarkdown(string value)
    {
        var cleaned = value;
        cleaned = cleaned.Replace("**", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("__", string.Empty, StringComparison.Ordinal);
        return cleaned.Trim();
    }

    private static string NormalizeNewlines(string body)
    {
        var normalized = body ?? string.Empty;
        normalized = normalized.Replace("\r\n", "\n", StringComparison.Ordinal);
        normalized = normalized.Replace('\r', '\n');
        return normalized.Trim();
    }

    private static TokenMatch FindNextToken(string text, int startIndex)
    {
        TokenMatch best = TokenMatch.None;

        var boldMatch = BoldRegex.Match(text, startIndex);
        if (boldMatch.Success)
        {
            best = TokenMatch.From(TokenKind.Bold, boldMatch.Index, boldMatch.Length, boldMatch.Groups[1].Success ? boldMatch.Groups[1] : boldMatch.Groups[2]);
        }

        var codeMatch = InlineCodeRegex.Match(text, startIndex);
        if (codeMatch.Success && (!best.Found || codeMatch.Index < best.Index))
        {
            best = TokenMatch.From(TokenKind.Code, codeMatch.Index, codeMatch.Length, codeMatch.Groups[1]);
        }

        var mathMatch = InlineMathRegex.Match(text, startIndex);
        if (mathMatch.Success && (!best.Found || mathMatch.Index < best.Index))
        {
            var group = mathMatch.Groups.Cast<Group>().Skip(1).First(g => g.Success);
            best = TokenMatch.From(TokenKind.Math, mathMatch.Index, mathMatch.Length, group);
        }

        return best;
    }

    private static Color ColorFromHex(string value)
    {
        return (Color)ColorConverter.ConvertFromString(value);
    }

    private void ResizeThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }

    private enum TokenKind
    {
        None,
        Bold,
        Code,
        Math
    }

    private readonly record struct TokenMatch(bool Found, TokenKind Kind, int Index, int ContentStart, int ContentLength, int NextIndex)
    {
        public static TokenMatch None => new(false, TokenKind.None, -1, -1, 0, -1);

        public static TokenMatch From(TokenKind kind, int index, int length, Group contentGroup)
        {
            return new TokenMatch(true, kind, index, contentGroup.Index, contentGroup.Length, index + length);
        }
    }
}
