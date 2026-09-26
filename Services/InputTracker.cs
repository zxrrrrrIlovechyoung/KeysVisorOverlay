using System.Diagnostics;

namespace KeysVisorOverlay.Services;

public sealed class InputTracker
{
    private readonly object gate = new();
    private readonly Dictionary<int, long> counts = new();
    private readonly HashSet<int> downInputs = new();
    private readonly Dictionary<int, ActivePress> activePresses = new();
    private readonly Queue<InputVisualEvent> visualEvents = new();
    private readonly Queue<long> recentPresses = new();
    private readonly long oneSecondTicks = Stopwatch.Frequency;
    private List<WatchedInput> watchedInputs;
    private long totalPresses;
    private long? firstPressTick;
    private int maxKps;
    private long nextVisualId;

    public event Action<OverlayCommand>? CommandRequested;

    public InputTracker(IEnumerable<WatchedInput>? inputs = null)
    {
        watchedInputs = SettingsStore.Normalize(inputs ?? SettingsStore.DefaultInputs).ToList();
    }

    public IReadOnlyList<WatchedInput> WatchedInputs
    {
        get
        {
            lock (gate)
            {
                return watchedInputs.ToArray();
            }
        }
    }

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

    public IReadOnlyList<InputVisualEvent> DrainVisualEvents()
    {
        lock (gate)
        {
            if (visualEvents.Count == 0)
            {
                return [];
            }

            InputVisualEvent[] events = visualEvents.ToArray();
            visualEvents.Clear();
            return events;
        }
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

            InputTileSnapshot[] tiles = watchedInputs
                .Select(input => new InputTileSnapshot(
                    input.Code,
                    input.Label,
                    counts.GetValueOrDefault(input.Code),
                    downInputs.Contains(input.Code)))
                .ToArray();

            return new InputSnapshot(recentPresses.Count, totalPresses, maxKps, average, tiles);
        }
    }

    public void UpdateWatchedInputs(IEnumerable<WatchedInput> inputs)
    {
        lock (gate)
        {
            watchedInputs = SettingsStore.Normalize(inputs).ToList();
            ResetUnlocked();
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            ResetUnlocked();
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

            WatchedInput? watchedInput = watchedInputs.FirstOrDefault(input => input.Code == code);
            if (watchedInput != null)
            {
                long now = Stopwatch.GetTimestamp();
                long id = ++nextVisualId;
                RegisterPress(code, now);
                activePresses[code] = new ActivePress(id, watchedInput.Label, now);
                visualEvents.Enqueue(new InputVisualEvent(
                    InputVisualEventType.Started,
                    id,
                    code,
                    watchedInput.Label,
                    now,
                    now));
            }

            return true;
        }
    }

    private void HandleUp(int code)
    {
        lock (gate)
        {
            if (!downInputs.Remove(code))
            {
                return;
            }

            if (activePresses.Remove(code, out ActivePress? press))
            {
                long now = Stopwatch.GetTimestamp();
                visualEvents.Enqueue(new InputVisualEvent(
                    InputVisualEventType.Ended,
                    press.Id,
                    code,
                    press.Label,
                    press.StartedAt,
                    now));
            }
        }
    }

    private void ResetUnlocked()
    {
        counts.Clear();
        activePresses.Clear();
        recentPresses.Clear();
        totalPresses = 0;
        firstPressTick = null;
        maxKps = 0;
        visualEvents.Enqueue(new InputVisualEvent(InputVisualEventType.Reset, 0, 0, string.Empty, 0, 0));
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

    private sealed record ActivePress(long Id, string Label, long StartedAt);
}
