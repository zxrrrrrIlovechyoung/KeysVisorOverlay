using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using KeysVisorOverlay.Services;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace KeysVisorOverlay;

public partial class CustomizeInputsWindow : Window
{
    private readonly ObservableCollection<InputOption> inputs;
    private string selectedPressedColor = OverlaySettings.DefaultPressedColor;
    private volatile bool waitingForInput;

    public CustomizeInputsWindow(OverlaySettings currentSettings)
    {
        InitializeComponent();

        OverlaySettings settings = SettingsStore.Normalize(currentSettings);
        inputs = new ObservableCollection<InputOption>(
            settings.Inputs
                .Select(input => new InputOption(input.Code, input.Label)));

        InputList.ItemsSource = inputs;
        StatsPlacementBox.SelectedIndex = settings.StatsPlacement == StatsPlacement.Bottom ? 1 : 0;
        TrailsCheckBox.IsChecked = settings.TrailsEnabled;
        TransparentBackgroundCheckBox.IsChecked = settings.TransparentBackground;
        BlurBackgroundCheckBox.IsChecked = settings.BlurBackground;
        selectedPressedColor = settings.PressedColor;
        ScaleSlider.Value = settings.OverlayScale;
        KeyWidthSlider.Value = settings.KeyWidth;
        KeyHeightSlider.Value = settings.KeyHeight;
        TrailHeightSlider.Value = settings.TrailHeight;
        UpdatePressedColorPreview();
        UpdateSliderValues();
    }

    public OverlaySettings SelectedSettings => new()
    {
        Inputs = inputs.Select(input => new WatchedInput(input.Code, input.Label)).ToList(),
        StatsPlacement = StatsPlacementBox.SelectedIndex == 1 ? StatsPlacement.Bottom : StatsPlacement.Top,
        TrailsEnabled = TrailsCheckBox.IsChecked == true,
        TransparentBackground = TransparentBackgroundCheckBox.IsChecked == true,
        BlurBackground = BlurBackgroundCheckBox.IsChecked == true,
        PressedColor = selectedPressedColor,
        OverlayScale = ScaleSlider.Value,
        KeyWidth = KeyWidthSlider.Value,
        KeyHeight = KeyHeightSlider.Value,
        TrailHeight = TrailHeightSlider.Value
    };

    public bool TryCaptureInput(int code)
    {
        if (!waitingForInput)
        {
            return false;
        }

        Dispatcher.BeginInvoke(() => CaptureInput(code));
        return true;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        waitingForInput = true;
        CaptureStatusText.Text = "Press a key or mouse button...";
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (InputList.SelectedItem is InputOption selected)
        {
            inputs.Remove(selected);
            CaptureStatusText.Text = "Removed";
        }
    }

    private void Defaults_Click(object sender, RoutedEventArgs e)
    {
        waitingForInput = false;
        inputs.Clear();
        foreach (WatchedInput input in SettingsStore.DefaultInputs)
        {
            inputs.Add(new InputOption(input.Code, input.Label));
        }

        CaptureStatusText.Text = "osu!mania defaults restored";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderValues();
    }

    private void KeyWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderValues();
    }

    private void KeyHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderValues();
    }

    private void TrailHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderValues();
    }

    private void ChoosePressedColor_Click(object sender, RoutedEventArgs e)
    {
        MediaColor currentColor = (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(selectedPressedColor);
        using Forms.ColorDialog dialog = new()
        {
            AllowFullOpen = true,
            AnyColor = true,
            FullOpen = true,
            Color = DrawingColor.FromArgb(currentColor.R, currentColor.G, currentColor.B)
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        selectedPressedColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        UpdatePressedColorPreview();
    }

    private void CaptureInput(int code)
    {
        if (!waitingForInput)
        {
            return;
        }

        waitingForInput = false;

        if (inputs.Any(input => input.Code == code))
        {
            CaptureStatusText.Text = $"{InputCodes.GetDisplayName(code)} is already added";
            return;
        }

        InputOption option = new(code, InputCodes.GetDisplayName(code));
        inputs.Add(option);
        InputList.SelectedItem = option;
        InputList.ScrollIntoView(option);
        CaptureStatusText.Text = $"Added {option.Label}";
    }

    private void UpdateSliderValues()
    {
        if (ScaleValueText != null)
        {
            ScaleValueText.Text = $"{ScaleSlider.Value * 100.0D:0}%";
        }

        if (KeyWidthValueText != null)
        {
            KeyWidthValueText.Text = $"{KeyWidthSlider.Value:0}";
        }

        if (KeyHeightValueText != null)
        {
            KeyHeightValueText.Text = $"{KeyHeightSlider.Value:0}";
        }

        if (TrailHeightValueText != null)
        {
            TrailHeightValueText.Text = $"{TrailHeightSlider.Value:0}";
        }
    }

    private void UpdatePressedColorPreview()
    {
        if (PressedColorPreview == null || PressedColorButton == null)
        {
            return;
        }

        string color = SettingsStore.NormalizeColor(selectedPressedColor);
        selectedPressedColor = color;
        PressedColorButton.Content = color;
        PressedColorPreview.Background = new SolidColorBrush((MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    public sealed record InputOption(int Code, string Label);
}
