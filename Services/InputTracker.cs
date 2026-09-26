using System.Diagnostics;

namespace KeysVisorOverlay.Services;

public sealed class InputTracker
{
    private readonly object gate = new();
    private readonly Dictionary<int, long> counts = new();
    private readonly HashSet<int> downInputs = new();
    private readonly Queue<long> recentPresses = new();
    private readonly long oneSecondTicks = Stopwatch.Frequency;
    private long totalPresses;
    private long? firstPressTick;
    private int maxKps;

    public event Action<OverlayCommand>? CommandRequested;

    public IReadOnlyList<WatchedInput> WatchedInputs { get; } =
    [
        new(InputCodes.VkZ, "Z"),
        new(InputCodes.VkX, "X"),
        new(InputCodes.MouseLeft, "M1"),
        new(InputCodes.MouseRight, "M2"),
        new(InputCodes.VkSpace, "SPACE")
    ];

    public void HandleKeyDown(int code)
    {
        if (HandleDown(code))
        {
            TryRunCommand(code);
        }
    }

    public void HandleKeyUp(int code)
    {
        HandleUp(code);
    }

    public void HandleMouseDown(int code)
    {
        HandleDown(code);
    }

    public void HandleMouseUp(int code)
    {
        HandleUp(code);
    }

    public InputSnapshot GetSnapshot()
    {
        lock (gate)
        {
            long now = Stopwatch.GetTimestamp();
            TrimRecentPresses(now);

            double average = 0;
            if (firstPressTick is long startedAt)
            {
                double elapsedSeconds = Math.Max((now - startedAt) / (double)Stopwatch.Frequency, 1);
                average = totalPresses / elapsedSeconds;
            }

            InputTileSnapshot[] tiles = WatchedInputs
                .Select(input => new InputTileSnapshot(
                    input.Code,
                    input.Label,
                    counts.GetValueOrDefault(input.Code),
                    downInputs.Contains(input.Code)))
                .ToArray();

            return new InputSnapshot(recentPresses.Count, totalPresses, maxKps, average, tiles);
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            counts.Clear();
            recentPresses.Clear();
            totalPresses = 0;
            firstPressTick = null;
            maxKps = 0;
        }
    }

    private bool HandleDown(int code)
    {
        lock (gate)
        {
            if (!downInputs.Add(code))
            {
                return false;
            }

            RegisterPress(code, Stopwatch.GetTimestamp());
            return true;
        }
    }

    private void HandleUp(int code)
    {
        lock (gate)
        {
            downInputs.Remove(code);
        }
    }

    private void RegisterPress(int code, long now)
    {
        firstPressTick ??= now;
        totalPresses++;
        counts[code] = counts.GetValueOrDefault(code) + 1;

        recentPresses.Enqueue(now);
        TrimRecentPresses(now);
        maxKps = Math.Max(maxKps, recentPresses.Count);
    }

    private void TrimRecentPresses(long now)
    {
        while (recentPresses.Count > 0 && now - recentPresses.Peek() > oneSecondTicks)
        {
            recentPresses.Dequeue();
        }
    }

    private void TryRunCommand(int code)
    {
        bool controlDown;
        bool shiftDown;

        lock (gate)
        {
            controlDown = downInputs.Any(InputCodes.IsControl);
            shiftDown = downInputs.Any(InputCodes.IsShift);
        }

        if (!controlDown || !shiftDown)
        {
            return;
        }

        switch (code)
        {
            case InputCodes.VkH:
                CommandRequested?.Invoke(OverlayCommand.ToggleVisibility);
                break;
            case InputCodes.VkR:
                CommandRequested?.Invoke(OverlayCommand.Reset);
                break;
            case InputCodes.VkQ:
                CommandRequested?.Invoke(OverlayCommand.Exit);
                break;
        }
    }
}
