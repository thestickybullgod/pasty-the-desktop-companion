# Pasty 📎🎃

**Your desktop companion. Born on Halloween 2026.**

Pasty is a tube of paste with a screw-cap hat, very expressive eyebrows, and opinions. It's a spiritual
successor to the classic 90s desktop assistant: the one that might have evolved if that idea had
survived into the modern era. Helpful, nostalgic, slightly obnoxious, technically competent, and
occasionally sarcastic.

Pasty is an original character. All artwork and animation is original vector art; no third-party
character assets are used or redistributed.

```
// Pasty has entered the chat.
```

## Features

- 🖱️ **Lives on your desktop**: always on top, drag it anywhere, and it remembers where you left it.
- 💬 **Speech bubbles**, with a typewriter effect and an ask box (double-click Pasty).
- 🎞️ **Animated**: idle, listening, thinking, talking, sleeping, plus reactions (pokes, hops, waves, smugness).
  Eyes follow your cursor.
- 🪟 **Windows 11-style settings**: Fluent design, light and dark mode, applies changes instantly.
- 📴 **Works completely offline.** Personality, jokes, tips and commands need no network.
- 🤖 **Optional AI brain** (Claude). Off by default; falls back to offline if anything goes wrong.
- 🔒 **Privacy-first.** App and clipboard awareness are opt-in, reduced to a category on the spot
  ("code editor", "a link"), and never stored or sent anywhere.
- 😴 **Built to run all day**: naps when you're away, hides during full-screen games and presentations,
  snooze from the tray, never repeats a joke within an hour, and throttles its animation frame rate.
- 🧩 **Plugins**: drop-in .NET DLLs can add slash commands (opt-in).
- 🐧 **Windows and Linux.**

## 🎃 Halloween Edition

From **October 15 to November 1**, Pasty dresses up automatically (or set *Always Halloween* in
Settings → Personality → Seasonal costumes):

- 🧙 a tiny witch hat (it tips it when it waves)
- 🧛 tiny vampire fangs
- 🎃 an orange-and-black label and a candlelit speech bubble
- 🦇 a small bat friend that occasionally loops around its head
- 👻 ghost mode: Pasty goes see-through and floats for a moment (`/boo`)
- 💬 seasonal dialogue, `/trickortreat`, and `/graveyard` (respects paid to retired desktop assistants)
- 🪦 an easter egg if you say a certain paperclip's name three times
- 📎 at midnight on Halloween: *"Halloween is over. I refuse to remove the hat."*

**Spooky, never harmful:** no jump scares, no flashing, no fake warnings, nothing that touches your
files, cursor or screen. It respects Snooze, Quiet mode and full-screen hiding, and you can turn it off.

## Install

### Windows 10 / 11
Download `Pasty-1.0.0-win-x64.zip` (or `win-arm64`), extract anywhere, run `Pasty.exe`.
No installer and no admin rights needed. Turn on *Start with Windows* in Settings if you'd like.

### Linux
Download `Pasty-1.0.0-linux-x64.tar.gz` (or `linux-arm64`), then:

```sh
tar -xzf Pasty-1.0.0-linux-x64.tar.gz
cd Pasty-1.0.0-linux-x64
./install.sh            # per-user install, no root; ./install.sh --uninstall to remove
```

Works best on **X11**. On **Wayland**, Pasty runs through XWayland. Your compositor may ignore
"always on top", and Pasty can only see the cursor and focused app for XWayland windows.
Optional packages: `libxss1` / `libXScrnSaver` (napping when you're away) and `libsecret-tools`
(store the API key in your keyring).

## Using Pasty

| Do this | Pasty does |
|---|---|
| Drag | Moves (and lands with a hop) |
| Click | Gets poked. Keep poking and see what happens. |
| Double-click | Opens the ask box |
| Right-click / tray icon | Menu: ask, joke, snooze, hide, settings, exit |
| Launch Pasty again | The running Pasty comes forward to listen |
| `Pasty --settings` | Opens settings |

Type `/help` in the ask box for commands: `/joke`, `/tip`, `/roll 2d6`, `/flip`, `/time`, `/birthday`,
`/boo`, `/trickortreat`, `/graveyard`, `/brain`, `/reset`, `/settings`.

## AI brain (optional)

Settings → Brain → **Claude**, then paste an API key from console.anthropic.com (or set
`ANTHROPIC_API_KEY`). The key is encrypted with DPAPI on Windows, or kept in your desktop keyring on
Linux when `secret-tool` is available. Only what you type into the ask box is sent; idle remarks,
pokes, easter eggs and context reactions stay offline.

## Building from source

Requires the .NET 10 SDK.

```sh
dotnet build                      # build everything
dotnet test                       # run the test suite
dotnet run --project src/Pasty.Desktop
pwsh packaging/publish.ps1        # release packages for Windows + Linux into ./dist
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for how the pieces fit together.

## License

Copyright (C) 2026 The Pasty Authors

Pasty is free software: you can redistribute it and/or modify it under the terms of the GNU General
Public License as published by the Free Software Foundation, either version 3 of the License, or (at
your option) any later version. See [LICENSE](LICENSE).

Pasty is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

`SPDX-License-Identifier: GPL-3.0-or-later`
