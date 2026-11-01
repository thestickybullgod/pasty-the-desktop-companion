using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Pasty.Desktop.Character;

/// <summary>
/// The speech bubble: typewriter text, thinking dots and the ask box. It's its own small window
/// that sits above Pasty, so no large invisible window ever covers the desktop.
/// </summary>
public partial class BubbleWindow : Window
{
    private const int CharsPerTick = 2;

    private readonly DispatcherTimer _typewriter = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private readonly DispatcherTimer _autoHide = new();
    private readonly DispatcherTimer _dots = new() { Interval = TimeSpan.FromMilliseconds(160) };
    private string _fullText = "";
    private int _shown;
    private int _dotPhase;

    public BubbleWindow()
    {
        InitializeComponent();

        _typewriter.Tick += (_, _) => TypeNext();
        _autoHide.Tick += (_, _) =>
        {
            _autoHide.Stop();
            if (!IsAsking) HideBubble();
        };
        _dots.Tick += (_, _) => AnimateDots();

        CloseButton.Click += (_, _) => Dismiss();
        SendButton.Click += (_, _) => Submit();
        AskBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { Submit(); e.Handled = true; }
            else if (e.Key == Key.Escape) { Dismiss(); e.Handled = true; }
        };
        SizeChanged += (_, _) => Reposition();
    }

    /// <summary>Supplies the point (physical pixels) the tail should touch: just above Pasty's head.</summary>
    public Func<(PixelPoint Anchor, PixelRect WorkArea)?>? AnchorProvider { get; set; }

    /// <summary>
    /// Halloween Edition: a black, candlelit bubble with orange trim instead of tooltip yellow.
    /// Colours stay high-contrast and nothing flickers.
    /// </summary>
    public bool Spooky
    {
        set
        {
            var (fill, stroke, text, accent) = value
                ? ("#1C1410", "#FF8A1C", "#FFE8CC", "#FF9F1C")
                : ("#FFFDE7", "#B9A867", "#2B2B2B", "#8A7D4A");
            var fillBrush = new SolidColorBrush(Color.Parse(fill));
            var strokeBrush = new SolidColorBrush(Color.Parse(stroke));
            Card.Background = fillBrush;
            Card.BorderBrush = strokeBrush;
            Card.BoxShadow = BoxShadows.Parse(value ? "0 0 14 1 #66FF8A1C" : "0 2 12 0 #40000000");
            Tail.Fill = fillBrush;
            Tail.Stroke = strokeBrush;
            Message.Foreground = new SolidColorBrush(Color.Parse(text));
            CloseGlyph.Stroke = new SolidColorBrush(Color.Parse(accent));
            foreach (var dot in new[] { Dot1, Dot2, Dot3 })
                dot.Fill = new SolidColorBrush(Color.Parse(accent));
            SendButton.Background = new SolidColorBrush(Color.Parse(value ? "#FF7A1A" : "#14A99A"));
        }
    }

    public event EventHandler? FinishedTyping;

    public event EventHandler<string>? Asked;

    public event EventHandler? Dismissed;

    public bool IsAsking => AskPanel.IsVisible && IsVisible;

    public bool IsShowing => IsVisible && Root.Opacity > 0;

    public void ShowMessage(string text)
    {
        StopDots();
        _autoHide.Stop();
        AskPanel.IsVisible = false;
        Message.IsVisible = true;
        _fullText = text;
        _shown = 0;
        Message.Text = "";
        ShowBubble();
        _typewriter.Start();
    }

    public void ShowThinking()
    {
        _typewriter.Stop();
        _autoHide.Stop();
        AskPanel.IsVisible = false;
        Message.IsVisible = false;
        Dots.IsVisible = true;
        _dots.Start();
        ShowBubble();
    }

    public void ShowAsk(string prompt)
    {
        StopDots();
        _typewriter.Stop();
        _autoHide.Stop();
        Message.Text = prompt;
        Message.IsVisible = true;
        AskPanel.IsVisible = true;
        AskBox.Text = "";
        ShowBubble();
        Activate();
        Dispatcher.UIThread.Post(() => AskBox.Focus(), DispatcherPriority.Input);
    }

    public void HideBubble()
    {
        _typewriter.Stop();
        _autoHide.Stop();
        StopDots();
        if (!IsVisible) return;
        Root.Opacity = 0;
        DispatcherTimer.RunOnce(() =>
        {
            // Only hide if nothing re-showed the bubble during the fade.
            if (Root.Opacity == 0) Hide();
        }, TimeSpan.FromMilliseconds(170));
    }

    /// <summary>Move to sit above Pasty. Called on show, on resize, and whenever Pasty moves.</summary>
    public void Reposition()
    {
        if (AnchorProvider?.Invoke() is not var (anchor, work)) return;

        var scaling = RenderScaling;
        var width = (int)Math.Ceiling(Bounds.Width * scaling);
        var height = (int)Math.Ceiling(Bounds.Height * scaling);
        if (width == 0 || height == 0) return;

        // The tail is drawn slightly right of centre; line its tip up with the anchor.
        var x = anchor.X - width / 2 - (int)(4 * scaling);
        var y = anchor.Y - height;
        x = Math.Clamp(x, work.X, Math.Max(work.X, work.Right - width));
        y = Math.Max(y, work.Y);
        Position = new PixelPoint(x, y);
    }

    private void ShowBubble()
    {
        if (!IsVisible) Show();
        Reposition();
        Root.Opacity = 1;
    }

    private void TypeNext()
    {
        _shown = Math.Min(_fullText.Length, _shown + CharsPerTick);
        if (_shown < _fullText.Length && char.IsHighSurrogate(_fullText[_shown - 1]))
            _shown++; // never split an emoji in half
        Message.Text = _fullText[.._shown];
        if (_shown < _fullText.Length) return;

        _typewriter.Stop();
        // Reading time: generous, so a long reply doesn't vanish mid-sentence.
        _autoHide.Interval = TimeSpan.FromMilliseconds(Math.Clamp(2500 + _fullText.Length * 55, 4000, 20000));
        _autoHide.Start();
        FinishedTyping?.Invoke(this, EventArgs.Empty);
    }

    private void AnimateDots()
    {
        _dotPhase = (_dotPhase + 1) % 6;
        Dot1.Opacity = _dotPhase is 0 or 5 ? 1 : 0.3;
        Dot2.Opacity = _dotPhase is 1 or 4 ? 1 : 0.3;
        Dot3.Opacity = _dotPhase is 2 or 3 ? 1 : 0.3;
    }

    private void StopDots()
    {
        _dots.Stop();
        Dots.IsVisible = false;
    }

    private void Submit()
    {
        var text = AskBox.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        Asked?.Invoke(this, text);
    }

    private void Dismiss()
    {
        HideBubble();
        Dismissed?.Invoke(this, EventArgs.Empty);
    }
}
