using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KeysVisorOverlay.Services;

namespace KeysVisorOverlay;

public partial class MainWindow : Window
{
    private static readonly Brush TileIdleBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
    private static readonly Brush TileActiveBrush = new SolidColorBrush(Color.FromRgb(125, 226, 209));
    private static readonly Brush TileIdleBorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
    private static readonly Brush TileActiveBorderBrush = new SolidColorBrush(Color.FromRgb(255, 139, 167));
    private static readonly Brush TileTextBrush = new SolidColorBrush(Color.FromRgb(246, 247, 251));
    private static readonly Brush TileActiveTextBrush = new SolidColorBrush(Color.FromRgb(9, 12, 18));

    private readonly InputTracker tracker = new();
    private readonly NativeInputHook inputHook = new();
    private readonly DispatcherTimer renderTimer;
    private readonly Dictionary<int, TileView> tiles = new();

    public MainWindow()
    {
        InitializeComponent();

        BuildTiles();

        inputHook.KeyDown += tracker.HandleKeyDown;
        inputHook.KeyUp += tracker.HandleKeyUp;
        inputHook.MouseDown += tracker.HandleMouseDown;
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
            HookStatusText.Text = "Live";
        }
        catch (Exception ex)
        {
            HookStatusText.Text = "Input failed";
            MessageBox.Show(
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
        foreach (WatchedInput input in tracker.WatchedInputs)
        {
            Border shell = new()
            {
                CornerRadius = new CornerRadius(6),
                Background = TileIdleBrush,
                BorderBrush = TileIdleBorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(7, 5, 7, 5),
                MinHeight = 34
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
                HorizontalAlignment = HorizontalAlignment.Right,
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

    private void RenderSnapshot(InputSnapshot snapshot)
    {
        KpsText.Text = snapshot.CurrentKps.ToString();
        MaxText.Text = snapshot.MaxKps.ToString();
        AvgText.Text = snapshot.AverageKps.ToString("0.0");
        TotalText.Text = snapshot.TotalPresses.ToString("N0");

        foreach (InputTileSnapshot tile in snapshot.Tiles)
        {
            if (!tiles.TryGetValue(tile.Code, out TileView? view))
            {
                continue;
            }

            view.Count.Text = tile.Count.ToString("N0");
            view.Shell.Background = tile.IsDown ? TileActiveBrush : TileIdleBrush;
            view.Shell.BorderBrush = tile.IsDown ? TileActiveBorderBrush : TileIdleBorderBrush;
            view.Label.Foreground = tile.IsDown ? TileActiveTextBrush : TileTextBrush;
            view.Count.Foreground = tile.IsDown ? TileActiveTextBrush : TileTextBrush;
        }
    }

    private sealed record TileView(Border Shell, TextBlock Label, TextBlock Count);
}
