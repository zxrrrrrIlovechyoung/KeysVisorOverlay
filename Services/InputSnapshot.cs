namespace KeysVisorOverlay.Services;

public sealed record WatchedInput(int Code, string Label);

public sealed record InputTileSnapshot(int Code, string Label, long Count, bool IsDown);

public enum InputVisualEventType
{
    Started,
    Ended,
    Reset
}

public sealed record InputVisualEvent(
    InputVisualEventType Type,
    long Id,
    int Code,
    string Label,
    long StartedAt,
    long EndedAt);

public sealed record InputSnapshot(
    int CurrentKps,
    long TotalPresses,
    int MaxKps,
    double AverageKps,
    IReadOnlyList<InputTileSnapshot> Tiles);
