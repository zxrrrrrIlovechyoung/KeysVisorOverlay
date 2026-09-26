# KeysVisorOverlay

KeysVisorOverlay is a lightweight key-per-second overlay for rhythm game players,
built with C#, .NET, and WPF. It tracks keyboard and mouse input globally, shows
KPS statistics in real time, and draws simple key trails that make tapping
patterns easier to read.

It was made with osu!mania in mind, but it can be used with any game or setup
where seeing your input rhythm matters.

## Demo

### Overlay

<video src="assets/demo-overlay.mp4" controls width="720"></video>

### Customization

<video src="assets/demo-customization.mp4" controls width="720"></video>

### Key Trails

<video src="assets/demo-trails.mp4" controls width="720"></video>

## A Small Goodbye To osu!mania

KeysVisorOverlay is more than a small utility to me. It is a final love letter to
osu!mania, a game that stayed with me through my adolescence and became part of
the way I remember that time of my life.

I do not play the same way anymore, but I still wanted to leave something behind
for the players who are still grinding, learning patterns, improving scores, or
just enjoying the music. This project is my way of giving something back: a small
open source tool that new and current players can use, modify, and make their
own.

Maybe it is only a tiny overlay. But for me, it is also a footprint in a game
that shaped me.

## Features

- Always-on-top transparent overlay.
- Native global keyboard and mouse capture.
- Current KPS, max KPS, average KPS, and total input count.
- Default osu!mania keys: `D`, `F`, `J`, and `K`.
- Customizable watched inputs, including keyboard keys and mouse buttons.
- Support for wider layouts such as 7K, 8K, 9K, or custom key setups.
- `Space` is slightly wider by default when added.
- Optional key trails that rise from each pressed key.
- Held notes are drawn longer, making long presses easier to see.
- Adjustable overlay scale, key width, key height, and trail height.
- Custom press color, also used by the trail.
- Transparent and blur background modes.
- Movable stats section: top or bottom.
- Right-click menu for customization, reset, hide/show, and exit.
- Global shortcuts:
  - `Ctrl + Shift + H`: hide or show the overlay.
  - `Ctrl + Shift + R`: reset counters.
  - `Ctrl + Shift + Q`: exit.

## How It Works

KeysVisorOverlay runs as a small Windows desktop app. It does not need to be
focused, and it does not need to attach itself to the game window.

Instead, it uses native Windows low-level input hooks to listen for key and mouse
press events across the system. When one of the configured inputs is pressed, the
overlay updates the counters, refreshes the KPS values, highlights the key, and
optionally draws a trail note above it.

For casual players, that means:

- You can keep osu!mania focused while the overlay keeps working.
- You can change the keys to match your layout.
- You can see whether your tapping is stable, rushed, uneven, or improving.
- You can use the trails to visually read short taps, bursts, streams, and long
  holds.

The app only counts input events. It does not store typed text, record gameplay,
send data anywhere, or interact with osu! directly.

## Tech Stack

- C#
- .NET 8
- WPF
- Native Windows input hooks
- Native Windows blur/acrylic-style window composition
- Local JSON settings stored in the user app data folder

## Build

Requirements:

- Windows
- .NET 8 SDK or newer

Build locally:

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

## Privacy

KeysVisorOverlay only counts configured key and mouse press events. It does not
store typed text, inspect game memory, or send input data anywhere.

## Disclaimer

This project is not affiliated with osu!, osu!mania, or ppy.
