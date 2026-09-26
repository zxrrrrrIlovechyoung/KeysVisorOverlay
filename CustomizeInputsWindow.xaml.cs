using System.Collections.ObjectModel;
using System.Windows;
using KeysVisorOverlay.Services;

namespace KeysVisorOverlay;

public partial class CustomizeInputsWindow : Window
{
    private readonly ObservableCollection<InputOption> inputs;
    private volatile bool waitingForInput;

    public CustomizeInputsWindow(IEnumerable<WatchedInput> currentInputs)
    {
        InitializeComponent();

        inputs = new ObservableCollection<InputOption>(
            SettingsStore.Normalize(currentInputs)
                .Select(input => new InputOption(input.Code, input.Label)));

        InputList.ItemsSource = inputs;
    }

    public IReadOnlyList<WatchedInput> SelectedInputs =>
        inputs.Select(input => new WatchedInput(input.Code, input.Label)).ToArray();

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

    public sealed record InputOption(int Code, string Label);
}
