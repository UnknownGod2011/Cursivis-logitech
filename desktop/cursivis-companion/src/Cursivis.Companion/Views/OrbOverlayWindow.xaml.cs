using Cursivis.Companion.Infrastructure;
using Cursivis.Companion.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Cursivis.Companion.Views;

public partial class OrbOverlayWindow : Window
{
    private readonly Border[] _actionChips;
    private readonly TextBlock[] _actionTexts;
    private readonly Ellipse[] _magicRings;
    private readonly string[] _idleCommands = ["Trigger", "Talk", "Snip-it", "Action"];
    private readonly DispatcherTimer _actionRingHideTimer;
    private Storyboard? _pulseStoryboard;
    private Storyboard? _rotationStoryboard;
    private Storyboard? _completionStoryboard;
    private OrbState _currentState = OrbState.Idle;
    private bool _isUserPositioned;
    private bool _hasPosition;
    private bool _isActionRingVisible;
    private bool _isMenuMode;
    private string _modeDisplay = "Smart";
    private int _idleCommandIndex;
    private List<string> _menuOptions = [];
    private int _selectedMenuIndex;

    public OrbOverlayWindow()
    {
        InitializeComponent();

        _actionChips =
        [
            ActionChipTop,
            ActionChipUpperRight,
            ActionChipLowerRight,
            ActionChipLowerLeft,
            ActionChipUpperLeft
        ];

        _actionTexts =
        [
            ActionTopText,
            ActionUpperRightText,
            ActionLowerRightText,
            ActionLowerLeftText,
            ActionUpperLeftText
        ];

        _magicRings =
        [
            MagicRing1,
            MagicRing2,
            MagicRing3,
            MagicRing4
        ];

        foreach (var ring in _magicRings)
        {
            ring.RenderTransformOrigin = new Point(0.5, 0.5);
            ring.RenderTransform = new ScaleTransform(0.72, 0.72);
        }

        foreach (var chip in _actionChips)
        {
            chip.Visibility = Visibility.Collapsed;
            chip.Opacity = 0;
            chip.IsHitTestVisible = false;
        }

        _actionRingHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1250)
        };
        _actionRingHideTimer.Tick += (_, _) => HideActionRing();

        UiPresentation.ApplyShinyText(StatusText, ColorFromHex("#AFC6DA"), Colors.White, 2.4);
        ResetListeningLevelVisual();
        ApplyPalette(OrbState.Idle);
        UpdateIdleTexts();
        UpdatePresentationMode();
    }

    public event EventHandler<string>? MenuOptionSelected;

    public event EventHandler<string>? IdleCommandInvoked;

    public event EventHandler<int>? ModeStepRequested;

    public event EventHandler? ListeningStopRequested;

    public bool IsMenuVisible => _isMenuMode && _menuOptions.Count > 0;

    public string CurrentIdleCommand => _idleCommands[_idleCommandIndex];

    public void MoveNearCursor(Point cursor)
    {
        if (!_hasPosition)
        {
            MoveToTopRight();
        }
    }

    public void MoveToTopRight(bool force = false)
    {
        if (_isUserPositioned && !force)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Top + 20;
        _hasPosition = true;
    }

    public void SetModeDisplay(string modeDisplay)
    {
        _modeDisplay = string.IsNullOrWhiteSpace(modeDisplay) ? "Smart" : modeDisplay;
        UpdateIdleTexts();
        if (_currentState == OrbState.Idle && !_isMenuMode)
        {
            StateText.Text = _modeDisplay;
            StatusText.Text = $"Ready ({_modeDisplay})";
        }
    }

    public void SetState(OrbState state, string status)
    {
        _currentState = state;
        StateText.Text = state == OrbState.Idle ? _modeDisplay : state.ToString();
        StatusText.Text = status;
        ListeningStopButton.Visibility = state == OrbState.Listening ? Visibility.Visible : Visibility.Collapsed;
        ListeningStopButton.IsHitTestVisible = state == OrbState.Listening;
        ApplyPalette(state);

        switch (state)
        {
            case OrbState.Processing:
                StartPulse(isListening: false);
                AnimateBaseScale(1.0);
                break;
            case OrbState.Listening:
                StartPulse(isListening: true);
                AnimateBaseScale(1.0);
                break;
            case OrbState.Completed:
                StopPulse();
                ResetListeningLevelVisual();
                PlayCompletionBurst();
                AnimateBaseScale(0.98);
                break;
            default:
                StopPulse();
                ResetListeningLevelVisual();
                AnimateBaseScale(_isMenuMode ? 1.0 : 0.88);
                break;
        }

        UpdatePresentationMode();
    }

    public void SetListeningLevel(double level)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => SetListeningLevel(level));
            return;
        }

        if (_currentState != OrbState.Listening)
        {
            ResetListeningLevelVisual();
            return;
        }

        var clamped = Math.Clamp(level, 0, 1);
        VoiceGlowHalo.Opacity = 0.18 + (clamped * 0.28);
        VoiceGlowScaleTransform.ScaleX = 0.98 + (clamped * 0.16);
        VoiceGlowScaleTransform.ScaleY = 0.98 + (clamped * 0.16);
    }

    public void UpdateActionRing(IReadOnlyList<string> actions, int selectedIndex)
    {
        var visibleEntries = BuildVisibleEntries(actions, selectedIndex);

        for (var i = 0; i < _actionChips.Length; i++)
        {
            if (i >= visibleEntries.Count)
            {
                _actionTexts[i].Text = string.Empty;
                _actionChips[i].Tag = null;
                continue;
            }

            var entry = visibleEntries[i];
            _actionTexts[i].Text = entry.Label;
            _actionChips[i].Tag = entry.ActualIndex;

            var isSelected = entry.ActualIndex == selectedIndex;
            _actionChips[i].Background = isSelected
                ? CreateChipBrush(ColorFromHex("#7F0D2039"), ColorFromHex("#D03A1A5B"), ColorFromHex("#D01D4E64"))
                : CreateChipBrush(ColorFromHex("#8A0D1620"), ColorFromHex("#7A111F2D"), ColorFromHex("#7A0E1822"));
            _actionChips[i].BorderBrush = isSelected
                ? new SolidColorBrush(ColorFromHex("#FFD4EAFF"))
                : new SolidColorBrush(Color.FromArgb(72, 255, 255, 255));
            _actionTexts[i].Foreground = isSelected
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(ColorFromHex("#E4F3FF"));
        }
    }

    public void SetActionRingVisible(bool isVisible)
    {
        foreach (var chip in _actionChips)
        {
            AnimateActionChip(chip, isVisible);
            chip.IsHitTestVisible = isVisible;
        }

        _isActionRingVisible = isVisible;
        UpdatePresentationMode();
    }

    public void ShowActionRingTemporarily()
    {
        _isMenuMode = false;
        SetActionRingVisible(true);
        _actionRingHideTimer.Stop();
        _actionRingHideTimer.Start();
    }

    public void HideActionRing()
    {
        _actionRingHideTimer.Stop();
        if (_isActionRingVisible)
        {
            SetActionRingVisible(false);
        }
    }

    public void ShowOptionMenu(IReadOnlyList<string> options, int selectedIndex = 0)
    {
        _isMenuMode = true;
        _menuOptions = options
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _selectedMenuIndex = _menuOptions.Count == 0
            ? 0
            : Math.Clamp(selectedIndex, 0, _menuOptions.Count - 1);

        StateText.Text = "Guided";
        StatusText.Text = "Choose an action";
        AnimateBaseScale(1.0);
        UpdateActionRing(_menuOptions, _selectedMenuIndex);
        SetActionRingVisible(_menuOptions.Count > 0);
    }

    public void UpdateOptionMenu(IReadOnlyList<string> options, int? selectedIndex = null)
    {
        if (!_isMenuMode)
        {
            ShowOptionMenu(options, selectedIndex ?? 0);
            return;
        }

        var selectedLabel = selectedIndex is null && _selectedMenuIndex >= 0 && _selectedMenuIndex < _menuOptions.Count
            ? _menuOptions[_selectedMenuIndex]
            : null;

        _menuOptions = options
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_menuOptions.Count == 0)
        {
            HideOptionMenu();
            return;
        }

        if (selectedIndex.HasValue)
        {
            _selectedMenuIndex = Math.Clamp(selectedIndex.Value, 0, _menuOptions.Count - 1);
        }
        else if (!string.IsNullOrWhiteSpace(selectedLabel))
        {
            var preservedIndex = _menuOptions.FindIndex(option => string.Equals(option, selectedLabel, StringComparison.OrdinalIgnoreCase));
            _selectedMenuIndex = preservedIndex >= 0 ? preservedIndex : 0;
        }
        else
        {
            _selectedMenuIndex = 0;
        }

        UpdateActionRing(_menuOptions, _selectedMenuIndex);
        SetActionRingVisible(true);
    }

    public void HideOptionMenu()
    {
        _isMenuMode = false;
        _menuOptions.Clear();
        _selectedMenuIndex = 0;
        HideActionRing();
        UpdatePresentationMode();
    }

    public void NavigateOptionMenu(int delta)
    {
        if (!_isMenuMode || _menuOptions.Count == 0 || delta == 0)
        {
            return;
        }

        _selectedMenuIndex = (_selectedMenuIndex + Math.Sign(delta) + _menuOptions.Count) % _menuOptions.Count;
        UpdateActionRing(_menuOptions, _selectedMenuIndex);
        SetActionRingVisible(true);
    }

    public bool TryConfirmMenuSelection()
    {
        if (!_isMenuMode || _menuOptions.Count == 0)
        {
            return false;
        }

        MenuOptionSelected?.Invoke(this, _menuOptions[_selectedMenuIndex]);
        return true;
    }

    public void NavigateIdleCommand(int delta)
    {
        if (_isMenuMode || delta == 0)
        {
            return;
        }

        _idleCommandIndex = (_idleCommandIndex + Math.Sign(delta) + _idleCommands.Length) % _idleCommands.Length;
        UpdateIdleTexts();
    }

    public void CollapseToIdleShell()
    {
        HideOptionMenu();
        SetState(OrbState.Idle, $"Ready ({_modeDisplay})");
    }

    private void StartPulse(bool isListening)
    {
        _pulseStoryboard?.Stop();

        var baseScale = Math.Max(OrbScaleTransform.ScaleX, 0.96);
        var xAnim = new DoubleAnimation
        {
            From = baseScale,
            To = baseScale + 0.07,
            Duration = TimeSpan.FromMilliseconds(620),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        var yAnim = xAnim.Clone();
        Storyboard.SetTarget(xAnim, OrbScaleTransform);
        Storyboard.SetTargetProperty(xAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
        Storyboard.SetTarget(yAnim, OrbScaleTransform);
        Storyboard.SetTargetProperty(yAnim, new PropertyPath(ScaleTransform.ScaleYProperty));

        var glowX = new DoubleAnimation
        {
            From = 1.0,
            To = isListening ? 1.06 : 1.14,
            Duration = TimeSpan.FromMilliseconds(760),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        var glowY = glowX.Clone();
        var glowOpacity = new DoubleAnimation
        {
            From = isListening ? 0.42 : 0.68,
            To = isListening ? 0.62 : 0.98,
            Duration = TimeSpan.FromMilliseconds(760),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        Storyboard.SetTarget(glowX, GlowScaleTransform);
        Storyboard.SetTargetProperty(glowX, new PropertyPath(ScaleTransform.ScaleXProperty));
        Storyboard.SetTarget(glowY, GlowScaleTransform);
        Storyboard.SetTargetProperty(glowY, new PropertyPath(ScaleTransform.ScaleYProperty));
        Storyboard.SetTarget(glowOpacity, GlowHalo);
        Storyboard.SetTargetProperty(glowOpacity, new PropertyPath(UIElement.OpacityProperty));

        _pulseStoryboard = new Storyboard();
        _pulseStoryboard.Children.Add(xAnim);
        _pulseStoryboard.Children.Add(yAnim);
        _pulseStoryboard.Children.Add(glowX);
        _pulseStoryboard.Children.Add(glowY);
        _pulseStoryboard.Children.Add(glowOpacity);

        UiPresentation.ApplyShinyText(
            StatusText,
            isListening ? ColorFromHex("#F0C0D9") : ColorFromHex("#AFC6DA"),
            Colors.White,
            isListening ? 1.6 : 2.2);
        _pulseStoryboard.Begin();
        StartOrbitRotation(isListening ? 4.0 : 7.5);
    }

    private void StopPulse()
    {
        _pulseStoryboard?.Stop();
        _rotationStoryboard?.Stop();
        GlowScaleTransform.ScaleX = 1;
        GlowScaleTransform.ScaleY = 1;
        GlowHalo.Opacity = 0.84;
        UiPresentation.SetFlatText(StatusText, Colors.White);
    }

    private void ResetListeningLevelVisual()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(ResetListeningLevelVisual);
            return;
        }

        VoiceGlowHalo.Opacity = 0;
        VoiceGlowScaleTransform.ScaleX = 0.98;
        VoiceGlowScaleTransform.ScaleY = 0.98;
    }

    private void UpdatePresentationMode()
    {
        var showCompact = _currentState == OrbState.Idle && !_isActionRingVisible && !_isMenuMode;
        CompactIdlePanel.Visibility = showCompact ? Visibility.Visible : Visibility.Collapsed;
        ExpandedStatePanel.Visibility = showCompact ? Visibility.Collapsed : Visibility.Visible;
        IdleGlowHalo.Visibility = showCompact ? Visibility.Visible : Visibility.Collapsed;
        GlowHalo.Visibility = showCompact ? Visibility.Collapsed : Visibility.Visible;
        OrbitRingCanvas.Visibility = showCompact ? Visibility.Collapsed : Visibility.Visible;
        AnimateBaseScale(showCompact ? 0.88 : 1.0);
    }

    private void UpdateIdleTexts()
    {
        ModeText.Text = _modeDisplay;
        IdleRunButton.Content = _idleCommands[_idleCommandIndex];
    }

    private static IReadOnlyList<(int ActualIndex, string Label)> BuildVisibleEntries(IReadOnlyList<string> options, int selectedIndex)
    {
        if (options.Count == 0)
        {
            return [];
        }

        var clampedSelected = Math.Clamp(selectedIndex, 0, Math.Max(options.Count - 1, 0));
        var startIndex = Math.Max(0, Math.Min(clampedSelected - 2, Math.Max(0, options.Count - 5)));
        var entries = new List<(int ActualIndex, string Label)>();
        for (var index = startIndex; index < Math.Min(startIndex + 5, options.Count); index++)
        {
            entries.Add((index, CompactLabel(options[index])));
        }

        return entries;
    }

    private static string CompactLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.StartsWith("__deferred__", StringComparison.Ordinal))
        {
            return "...";
        }

        var compact = value.Trim()
            .Replace("Custom Voice Command", "Custom Task", StringComparison.OrdinalIgnoreCase)
            .Replace("Extract Insights", "Insights", StringComparison.OrdinalIgnoreCase)
            .Replace("Bullet Points", "Bullet Pts", StringComparison.OrdinalIgnoreCase)
            .Replace("Answer Question", "Answer", StringComparison.OrdinalIgnoreCase)
            .Replace("Rewrite Structured", "Rewrite", StringComparison.OrdinalIgnoreCase)
            .Replace("Generate Captions", "Captions", StringComparison.OrdinalIgnoreCase)
            .Replace("Extract Dominant Colors", "Colors", StringComparison.OrdinalIgnoreCase);

        return compact.Length <= 18 ? compact : $"{compact[..15]}...";
    }

    private static void AnimateActionChip(Border chip, bool show)
    {
        if (show)
        {
            chip.Visibility = Visibility.Visible;
        }

        var animation = new DoubleAnimation
        {
            To = show ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(show ? 180 : 220),
            EasingFunction = new QuadraticEase
            {
                EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn
            }
        };

        if (!show)
        {
            animation.Completed += (_, _) => chip.Visibility = Visibility.Collapsed;
        }

        chip.BeginAnimation(UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        MoveToTopRight();
    }

    private void OrbCore_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) is not null)
        {
            return;
        }

        _isUserPositioned = true;
        _hasPosition = true;
        try
        {
            DragMove();
        }
        catch
        {
            // Ignore drag interruptions.
        }
    }

    private void ActionChip_OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_isMenuMode || sender is not Border chip || chip.Tag is not int actualIndex || actualIndex < 0 || actualIndex >= _menuOptions.Count)
        {
            return;
        }

        _selectedMenuIndex = actualIndex;
        UpdateActionRing(_menuOptions, _selectedMenuIndex);
        MenuOptionSelected?.Invoke(this, _menuOptions[_selectedMenuIndex]);
        e.Handled = true;
    }

    private void ModePrevButton_OnClick(object sender, RoutedEventArgs e)
    {
        ModeStepRequested?.Invoke(this, -1);
    }

    private void ModeNextButton_OnClick(object sender, RoutedEventArgs e)
    {
        ModeStepRequested?.Invoke(this, 1);
    }

    private void CommandPrevButton_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateIdleCommand(-1);
    }

    private void CommandNextButton_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateIdleCommand(1);
    }

    private void IdleRunButton_OnClick(object sender, RoutedEventArgs e)
    {
        IdleCommandInvoked?.Invoke(this, _idleCommands[_idleCommandIndex]);
    }

    private void ListeningStopButton_OnClick(object sender, RoutedEventArgs e)
    {
        ListeningStopRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void AnimateBaseScale(double targetScale)
    {
        if (_pulseStoryboard is not null && _currentState is OrbState.Processing or OrbState.Listening)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            To = targetScale,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        OrbScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation, HandoffBehavior.SnapshotAndReplace);

        var animationY = animation.Clone();
        OrbScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animationY, HandoffBehavior.SnapshotAndReplace);
    }

    private void ApplyPalette(OrbState state)
    {
        switch (state)
        {
            case OrbState.Processing:
                OrbCore.Background = CreateOrbBrush(ColorFromHex("#16283A"), ColorFromHex("#122133"), ColorFromHex("#0C1621"));
                GlowHalo.Fill = CreateGlowBrush(ColorFromHex("#C255E5FF"), ColorFromHex("#48F562E7"), ColorFromHex("#0854A8FF"));
                StateText.Foreground = new SolidColorBrush(ColorFromHex("#D6E9FF"));
                break;
            case OrbState.Listening:
                OrbCore.Background = CreateOrbBrush(ColorFromHex("#331A38"), ColorFromHex("#251629"), ColorFromHex("#140E1A"));
                GlowHalo.Fill = CreateGlowBrush(ColorFromHex("#5DAAD5F3"), ColorFromHex("#2A78B4D8"), ColorFromHex("#04243A4A"));
                StateText.Foreground = new SolidColorBrush(ColorFromHex("#FFD6F8"));
                break;
            case OrbState.Completed:
                OrbCore.Background = CreateOrbBrush(ColorFromHex("#173A29"), ColorFromHex("#113021"), ColorFromHex("#0B1812"));
                GlowHalo.Fill = CreateGlowBrush(ColorFromHex("#E4FFD36A"), ColorFromHex("#685EEBFF"), ColorFromHex("#083B5A64"));
                StateText.Foreground = new SolidColorBrush(ColorFromHex("#FFF5D5"));
                break;
            default:
                OrbCore.Background = CreateOrbBrush(ColorFromHex("#172636"), ColorFromHex("#101B28"), ColorFromHex("#0B131E"));
                GlowHalo.Fill = CreateGlowBrush(ColorFromHex("#AAF562E7"), ColorFromHex("#885EEBFF"), ColorFromHex("#1065A7FF"));
                StateText.Foreground = new SolidColorBrush(ColorFromHex("#F0F7FF"));
                break;
        }
    }

    private void StartOrbitRotation(double secondsPerRotation)
    {
        _rotationStoryboard ??= new Storyboard();
        _rotationStoryboard.Stop();
        _rotationStoryboard.Children.Clear();

        var angleAnimation = new DoubleAnimation
        {
            From = OrbitRotateTransform.Angle,
            To = OrbitRotateTransform.Angle + 360,
            Duration = TimeSpan.FromSeconds(secondsPerRotation),
            RepeatBehavior = RepeatBehavior.Forever
        };

        Storyboard.SetTarget(angleAnimation, OrbitRotateTransform);
        Storyboard.SetTargetProperty(angleAnimation, new PropertyPath(RotateTransform.AngleProperty));
        _rotationStoryboard.Children.Add(angleAnimation);
        _rotationStoryboard.Begin();
    }

    private void PlayCompletionBurst()
    {
        _completionStoryboard ??= new Storyboard();
        _completionStoryboard.Stop();
        _completionStoryboard.Children.Clear();

        for (var i = 0; i < _magicRings.Length; i++)
        {
            var ring = _magicRings[i];
            ring.Opacity = 0;
            ring.Stroke = new SolidColorBrush(i % 2 == 0 ? ColorFromHex("#FFF562E7") : ColorFromHex("#FF5EEBFF"));

            if (ring.RenderTransform is not ScaleTransform scaleTransform)
            {
                scaleTransform = new ScaleTransform(0.72, 0.72);
                ring.RenderTransform = scaleTransform;
            }

            scaleTransform.ScaleX = 0.72;
            scaleTransform.ScaleY = 0.72;

            var beginTime = TimeSpan.FromMilliseconds(i * 80);
            var opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = beginTime
            };
            opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(820))));

            var scaleAnimation = new DoubleAnimation
            {
                BeginTime = beginTime,
                From = 0.72,
                To = 1.24 + (i * 0.05),
                Duration = TimeSpan.FromMilliseconds(860),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(opacityAnimation, ring);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(scaleAnimation, scaleTransform);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));

            var scaleYAnimation = scaleAnimation.Clone();
            Storyboard.SetTarget(scaleYAnimation, scaleTransform);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath(ScaleTransform.ScaleYProperty));

            _completionStoryboard.Children.Add(opacityAnimation);
            _completionStoryboard.Children.Add(scaleAnimation);
            _completionStoryboard.Children.Add(scaleYAnimation);
        }

        _completionStoryboard.Begin();
    }

    private static T? FindParent<T>(DependencyObject? source) where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static Brush CreateOrbBrush(Color inner, Color mid, Color outer)
    {
        return new RadialGradientBrush
        {
            GradientOrigin = new Point(0.3, 0.25),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.7,
            RadiusY = 0.7,
            GradientStops =
            {
                new GradientStop(inner, 0),
                new GradientStop(mid, 0.58),
                new GradientStop(outer, 1)
            }
        };
    }

    private static Brush CreateGlowBrush(Color center, Color mid, Color outer)
    {
        return new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            GradientStops =
            {
                new GradientStop(ScaleAlpha(center, 0.96), 0),
                new GradientStop(ScaleAlpha(mid, 0.78), 0.34),
                new GradientStop(ScaleAlpha(mid, 0.36), 0.62),
                new GradientStop(ScaleAlpha(outer, 0.46), 0.88),
                new GradientStop(ScaleAlpha(outer, 0), 1)
            }
        };
    }

    private static Color ScaleAlpha(Color color, double factor)
    {
        var alpha = (byte)Math.Clamp((int)Math.Round(color.A * factor), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Brush CreateChipBrush(Color left, Color center, Color right)
    {
        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new(left, 0),
                new(center, 0.5),
                new(right, 1)
            },
            new Point(0, 0),
            new Point(1, 1));
    }

    private static Color ColorFromHex(string value)
    {
        return (Color)ColorConverter.ConvertFromString(value);
    }
}
