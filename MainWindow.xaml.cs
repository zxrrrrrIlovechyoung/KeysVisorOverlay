using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using KeysVisorOverlay.Services;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;

namespace KeysVisorOverlay;

public partial class MainWindow : Window
{
    private const double MinNoteHeight = 18.0D;
    private const double NotePixelsPerSecond = 155.0D;
    private const int MaxTrailNotes = 420;

    private static readonly MediaBrush TileIdleBrush = new SolidColorBrush(MediaColor.FromArgb(38, 255, 255, 255));
    private static readonly MediaBrush TileIdleBorderBrush = new SolidColorBrush(MediaColor.FromArgb(60, 255, 255, 255));
    private static readonly MediaBrush TileTextBrush = new SolidColorBrush(MediaColor.FromRgb(246, 247, 251));
    private static readonly MediaBrush OverlayBackgroundBrush = new SolidColorBrush(MediaColor.FromArgb(220, 9, 12, 18));
    private static readonly MediaBrush BlurBackgroundBrush = new SolidColorBrush(MediaColor.FromArgb(156, 9, 12, 18));
    private static readonly MediaBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);

    private OverlaySettings settings = SettingsStore.Load();
    private MediaBrush tileActiveBrush = new SolidColorBrush(MediaColor.FromRgb(125, 226, 209));
    private MediaBrush tileActiveBorderBrush = new SolidColorBrush(MediaColor.FromRgb(125, 226, 209));
    private MediaBrush tileActiveTextBrush = new SolidColorBrush(MediaColor.FromRgb(9, 12, 18));
    private MediaBrush trailBrush = new SolidColorBrush(MediaColor.FromRgb(125, 226, 209));
    private readonly InputTracker tracker;
    private readonly NativeInputHook inputHook = new();
    private readonly DispatcherTimer renderTimer;
    private readonly Dictionary<int, TileView> tiles = new();
    private readonly Dictionary<long, NoteView> activeNotes = new();
    private CustomizeInputsWindow? customizeWindow;

    public MainWindow()
    {
        InitializeComponent();

        tracker = new InputTracker(settings.Inputs);
        ApplyVisualSettings();
        BuildTiles();

        inputHook.KeyDown += OnGlobalKeyDown;
        inputHook.KeyUp += tracker.HandleKeyUp;
        inputHook.MouseDown += OnGlobalMouseDown;
        inputHook.MouseUp += tracker.HandleMouseUp;
        tracker.CommandRequested += OnCommandRequested;

        renderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        renderTimer.Tick += (_, _) => RenderSnapshot(tracker.GetSnapshot());
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            inputHook.Start();
            renderTimer.Start();
            ApplyBlurSetting();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"KeysVisorOverlay could not start input capture.\n\n{ex.Message}",
                "KeysVisorOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        renderTimer.Stop();
        inputHook.Dispose();
    }

    private void OnGlobalKeyDown(int code)
    {
        if (customizeWindow != null)
        {
            customizeWindow.TryCaptureInput(code);
            return;
        }

        tracker.HandleKeyDown(code);
    }

    private void OnGlobalMouseDown(int code)
    {
        if (customizeWindow != null)
        {
            customizeWindow.TryCaptureInput(code);
            return;
        }

        tracker.HandleMouseDown(code);
    }

    private void DragSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        tracker.Reset();
    }

    private void Customize_Click(object sender, RoutedEventArgs e)
    {
        if (customizeWindow != null)
        {
            customizeWindow.Activate();
            return;
        }

        CustomizeInputsWindow window = new(settings)
        {
            Owner = this
        };

        customizeWindow = window;
        window.Closed += (_, _) => customizeWindow = null;
        bool? result = window.ShowDialog();

        if (result == true)
        {
            settings = window.SelectedSettings;
            SettingsStore.Save(settings);
            tracker.UpdateWatchedInputs(settings.Inputs);
            ApplyVisualSettings();
            BuildTiles();
        }
    }

    private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
    {
        ToggleVisibility();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCommandRequested(OverlayCommand command)
    {
        Dispatcher.BeginInvoke(() =>
        {
            switch (command)
            {
                case OverlayCommand.ToggleVisibility:
                    ToggleVisibility();
                    break;
                case OverlayCommand.Reset:
                    tracker.Reset();
                    break;
                case OverlayCommand.Exit:
                    Close();
                    break;
            }
        });
    }

    private void ToggleVisibility()
    {
        Visibility = Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
    }

    private void BuildTiles()
    {
        IReadOnlyList<WatchedInput> watchedInputs = tracker.WatchedInputs;

        tiles.Clear();
        TilePanel.Children.Clear();
        ClearTrailNotes();
        ResizeForTiles(watchedInputs);

        foreach (WatchedInput input in watchedInputs)
        {
            double tileWidth = GetTileWidth(input.Code);
            Border shell = new()
            {
                CornerRadius = new CornerRadius(6),
                Background = TileIdleBrush,
                BorderBrush = TileIdleBorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(7, 5, 7, 5),
                MinWidth = tileWidth,
                Width = tileWidth,
                MinHeight = settings.KeyHeight,
                Height = settings.KeyHeight
            };

            Grid layout = new();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock label = new()
            {
                Text = input.Label,
                Foreground = TileTextBrush,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock count = new()
            {
                Text = "0",
                Foreground = TileTextBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(label, 0);
            Grid.SetColumn(count, 1);
            layout.Children.Add(label);
            layout.Children.Add(count);
            shell.Child = layout;

            TilePanel.Children.Add(shell);
            tiles[input.Code] = new TileView(shell, label, count);
        }
    }

    private void ApplyVisualSettings()
    {
        DragSurface.LayoutTransform = new ScaleTransform(settings.OverlayScale, settings.OverlayScale);
        UpdateAccentBrushes();
        DragSurface.Background = settings.TransparentBackground
            ? TransparentBrush
            : settings.BlurBackground
                ? BlurBackgroundBrush
                : OverlayBackgroundBrush;
        TrailRow.MinHeight = settings.TrailHeight;
        TrailViewport.MinHeight = settings.TrailHeight;
        TrailCanvas.MinHeight = settings.TrailHeight;
        ApplyBlurSetting();

        if (settings.StatsPlacement == StatsPlacement.Top)
        {
            Grid.SetRow(StatsPanel, 1);
            Grid.SetRow(TrailViewport, 2);
            Grid.SetRow(TilePanel, 3);
            StatsPanel.Margin = new Thickness(0, 10, 0, 9);
            TrailViewport.Margin = new Thickness(0, 0, 0, 6);
            TilePanel.Margin = new Thickness(0);
        }
        else
        {
            Grid.SetRow(TrailViewport, 2);
            Grid.SetRow(TilePanel, 3);
            Grid.SetRow(StatsPanel, 4);
            TrailViewport.Margin = new Thickness(0, 10, 0, 6);
            TilePanel.Margin = new Thickness(0, 0, 0, 9);
            StatsPanel.Margin = new Thickness(0);
        }
    }

    private void ResizeForTiles(IReadOnlyList<WatchedInput> watchedInputs)
    {
        double totalTileWidth = Math.Max(1, watchedInputs.Count) * 6.0D;
        foreach (WatchedInput input in watchedInputs)
        {
            totalTileWidth += GetTileWidth(input.Code);
        }

        double baseWidth = Math.Max(280, 150 + totalTileWidth);
        double baseHeight = 108 + settings.TrailHeight + settings.KeyHeight + 8;

        Width = baseWidth * settings.OverlayScale;
        Height = baseHeight * settings.OverlayScale;
    }

    private void RenderSnapshot(InputSnapshot snapshot)
    {
        KpsText.Text = snapshot.CurrentKps.ToString();
        MaxText.Text = snapshot.MaxKps.ToString();
        AvgText.Text = snapshot.AverageKps.ToString("0.0");
        TotalText.Text = snapshot.TotalPresses.ToString("N0");
        ProcessVisualEvents();
        UpdateHeldNotes();

        foreach (InputTileSnapshot tile in snapshot.Tiles)
        {
            if (!tiles.TryGetValue(tile.Code, out TileView? view))
            {
                continue;
            }

            view.Count.Text = tile.Count.ToString("N0");
            view.Shell.Background = tile.IsDown ? tileActiveBrush : TileIdleBrush;
            view.Shell.BorderBrush = tile.IsDown ? tileActiveBorderBrush : TileIdleBorderBrush;
            view.Label.Foreground = tile.IsDown ? tileActiveTextBrush : TileTextBrush;
            view.Count.Foreground = tile.IsDown ? tileActiveTextBrush : TileTextBrush;
        }
    }

    private void ProcessVisualEvents()
    {
        foreach (InputVisualEvent visualEvent in tracker.DrainVisualEvents())
        {
            switch (visualEvent.Type)
            {
                case InputVisualEventType.Started:
                    StartNote(visualEvent);
                    break;
                case InputVisualEventType.Ended:
                    ReleaseNote(visualEvent);
                    break;
                case InputVisualEventType.Reset:
                    ClearTrailNotes();
                    break;
            }
        }
    }

    private void StartNote(InputVisualEvent visualEvent)
    {
        if (!settings.TrailsEnabled || !tiles.TryGetValue(visualEvent.Code, out TileView? view))
        {
            return;
        }

        TrimTrailNotes();

        double laneCenterX = GetLaneCenterX(visualEvent.Code, view);
        double laneWidth = GetLaneWidth(view);

        double noteWidth = Math.Min(46.0D, Math.Max(28.0D, laneWidth * 0.58D));
        Border note = new()
        {
            Width = noteWidth,
            Height = MinNoteHeight,
            CornerRadius = new CornerRadius(5),
            Background = trailBrush,
            BorderBrush = tileActiveBorderBrush,
            BorderThickness = new Thickness(1),
            Opacity = 0.86D,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = visualEvent.Label,
                Foreground = tileActiveTextBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            }
        };

        TrailCanvas.Children.Add(note);
        NoteView noteView = new(visualEvent.Code, visualEvent.Label, note, visualEvent.StartedAt, laneCenterX, noteWidth, false);
        activeNotes[visualEvent.Id] = noteView;
        SetNoteGeometry(noteView, MinNoteHeight);
    }

    private void UpdateHeldNotes()
    {
        long now = Stopwatch.GetTimestamp();
        foreach (NoteView note in activeNotes.Values)
        {
            if (!note.IsReleased)
            {
                SetNoteGeometry(note, GetNoteHeight(note.StartedAt, now));
            }
        }
    }

    private void ReleaseNote(InputVisualEvent visualEvent)
    {
        if (!activeNotes.Remove(visualEvent.Id, out NoteView? note))
        {
            return;
        }

        NoteView releasedNote = note with { IsReleased = true };
        double finalHeight = GetNoteHeight(visualEvent.StartedAt, visualEvent.EndedAt);
        SetNoteGeometry(releasedNote, finalHeight);

        double startTop = Canvas.GetTop(releasedNote.Element);
        double exitTop = -finalHeight - 18.0D;
        int durationMs = (int)Math.Clamp((startTop - exitTop) / 0.10D, 1050.0D, 2600.0D);

        Duration duration = TimeSpan.FromMilliseconds(durationMs);
        DoubleAnimation rise = new(startTop, exitTop, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation fade = new(0.86D, 0, TimeSpan.FromMilliseconds(durationMs * 0.55D))
        {
            BeginTime = TimeSpan.FromMilliseconds(durationMs * 0.45D)
        };

        rise.Completed += (_, _) => TrailCanvas.Children.Remove(releasedNote.Element);
        releasedNote.Element.BeginAnimation(Canvas.TopProperty, rise);
        releasedNote.Element.BeginAnimation(OpacityProperty, fade);
    }

    private double GetNoteHeight(long startedAt, long currentAt)
    {
        double elapsedSeconds = Math.Max(0, (currentAt - startedAt) / (double)Stopwatch.Frequency);
        return Math.Clamp(MinNoteHeight + elapsedSeconds * NotePixelsPerSecond, MinNoteHeight, GetTrailHeight() + settings.KeyHeight + 120.0D);
    }

    private void SetNoteGeometry(NoteView note, double height)
    {
        double canvasHeight = GetTrailHeight();

        note.Element.Height = height;
        Canvas.SetLeft(note.Element, note.CenterX - note.Width / 2.0D);
        Canvas.SetTop(note.Element, canvasHeight - height - 2.0D);
    }

    private double GetLaneCenterX(int code, TileView view)
    {
        try
        {
            if (view.Shell.ActualWidth > 0 && TrailCanvas.ActualWidth > 0)
            {
                System.Windows.Point lanePoint = view.Shell
                    .TransformToVisual(TrailCanvas)
                    .Transform(new System.Windows.Point(view.Shell.ActualWidth / 2.0D, 0));

                if (!double.IsNaN(lanePoint.X) && lanePoint.X > 0)
                {
                    return lanePoint.X;
                }
            }
        }
        catch (InvalidOperationException)
        {
        }

        int index = Math.Max(0, tiles.Keys.ToList().IndexOf(code));
        double x = 0;

        foreach (int tileCode in tiles.Keys.Take(index))
        {
            x += GetTileWidth(tileCode) + 6.0D;
        }

        return x + GetTileWidth(code) / 2.0D;
    }

    private double GetLaneWidth(TileView view)
    {
        if (view.Shell.ActualWidth > 0)
        {
            return view.Shell.ActualWidth;
        }

        return settings.KeyWidth;
    }

    private double GetTrailHeight()
    {
        return TrailCanvas.ActualHeight > 1.0D ? TrailCanvas.ActualHeight : settings.TrailHeight;
    }

    private double GetTrailWidth()
    {
        double width = TrailCanvas.ActualWidth > 1.0D ? TrailCanvas.ActualWidth : TrailViewport.ActualWidth;
        return width > 1.0D ? width : Math.Max(280, 150 + GetTotalTileWidth());
    }

    private double GetTotalTileWidth()
    {
        if (tiles.Count == 0)
        {
            return settings.KeyWidth + 6.0D;
        }

        double totalWidth = 0;
        foreach (int code in tiles.Keys)
        {
            totalWidth += GetTileWidth(code) + 6.0D;
        }

        return totalWidth;
    }

    private double GetTileWidth(int code)
    {
        return code == InputCodes.VkSpace ? settings.KeyWidth * 1.55D : settings.KeyWidth;
    }

    private void UpdateAccentBrushes()
    {
        string colorText = SettingsStore.NormalizeColor(settings.PressedColor);
        MediaColor color = (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(colorText);
        MediaColor textColor = GetReadableTextColor(color);

        tileActiveBrush = new SolidColorBrush(color);
        tileActiveBorderBrush = new SolidColorBrush(color);
        tileActiveTextBrush = new SolidColorBrush(textColor);
        trailBrush = new SolidColorBrush(color);
    }

    private static MediaColor GetReadableTextColor(MediaColor background)
    {
        double luminance = (0.299D * background.R + 0.587D * background.G + 0.114D * background.B) / 255.0D;
        return luminance > 0.56D ? MediaColor.FromRgb(9, 12, 18) : MediaColor.FromRgb(246, 247, 251);
    }

    private void ApplyBlurSetting()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetBlur(handle, settings.BlurBackground && !settings.TransparentBackground);
    }

    private static void SetBlur(IntPtr handle, bool enabled)
    {
        AccentPolicy accent = new()
        {
            AccentState = enabled ? AccentState.EnableBlurBehind : AccentState.Disabled,
            AccentFlags = 2,
            GradientColor = unchecked((int)0x99090C12)
        };

        int accentSize = Marshal.SizeOf<AccentPolicy>();
        IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);

        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);
            WindowCompositionAttributeData data = new()
            {
                Attribute = WindowCompositionAttribute.AccentPolicy,
                SizeOfData = accentSize,
                Data = accentPtr
            };

            _ = SetWindowCompositionAttribute(handle, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    private void TrimTrailNotes()
    {
        while (TrailCanvas.Children.Count > MaxTrailNotes)
        {
            UIElement oldest = TrailCanvas.Children[0];
            TrailCanvas.Children.RemoveAt(0);
            long? activeId = activeNotes.FirstOrDefault(pair => ReferenceEquals(pair.Value.Element, oldest)).Key;
            if (activeId is long id && id != 0)
            {
                activeNotes.Remove(id);
            }
        }
    }

    private void ClearTrailNotes()
    {
        activeNotes.Clear();
        TrailCanvas.Children.Clear();
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    private enum WindowCompositionAttribute
    {
        AccentPolicy = 19
    }

    private enum AccentState
    {
        Disabled = 0,
        EnableBlurBehind = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private sealed record TileView(Border Shell, TextBlock Label, TextBlock Count);

    private sealed record NoteView(
        int Code,
        string Label,
        Border Element,
        long StartedAt,
        double CenterX,
        double Width,
        bool IsReleased);
}
