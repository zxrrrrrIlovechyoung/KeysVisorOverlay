namespace KeysVisorOverlay.Services;

public sealed record WatchedInput(int Code, string Label);

public sealed record InputTileSnapshot(int Code, string Label, long Count, bool IsDown);

public sealed record InputSnapshot(
    int CurrentKps,
    long TotalPresses,
    int MaxKps,
    double AverageKps,
    IReadOnlyList<InputTileSnapshot> Tiles);
