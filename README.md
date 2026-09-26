# KeysVisorOverlay

KeysVisorOverlay is a lightweight key-per-second overlay for rhythm game players.
It is built from scratch with C#, .NET, and WPF, using native Windows input hooks
so the overlay can react globally without depending on the game window.

This project is a small open source farewell letter to osu!, a game that marked
my adolescence and stayed with me long after I stopped playing. The goal is to
share a clean, modern alternative for new and current players.

## Current Status

This is an early MVP.

- Transparent always-on-top overlay.
- Native global keyboard and mouse capture.
- Current KPS, max KPS, average KPS, and total input count.
- Default watched keys for osu!mania: `D`, `F`, `J`, and `K`.
- Customizable watched inputs, including keyboard keys and mouse buttons.
- Movable stats section: place KPS, max, average, and total above or below the keys.
- Optional key trails that rise from each pressed key.
- Adjustable overlay size.
- Right-click menu for customization, reset, hide/show, and exit.
- Global shortcuts:
  - `Ctrl + Shift + H`: hide or show the overlay.
  - `Ctrl + Shift + R`: reset counters.
  - `Ctrl + Shift + Q`: exit.

## Build

Requirements:

- Windows
- .NET 8 SDK or newer

```powershell
dotnet build
```

Run locally:

```powershell
dotnet run
```

Create a release build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## Notes

KeysVisorOverlay only counts key and mouse press events. It does not store typed
text or send input data anywhere.

This project is not affiliated with osu! or ppy.
