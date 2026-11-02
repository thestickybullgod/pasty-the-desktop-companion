# Pasty the Desktop Companion 📎🎃

**Your desktop companion. Born on Halloween 2026.**

**Pasty the Desktop Companion** ("Pasty" for short) is a bottle of school glue with a pointy orange cap, very expressive eyebrows, and opinions. It's a spiritual
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
- 🎭 **Pick your companion**: Pasty (a school-glue bottle), Classic Pasty (the original tube), Gluey (a glue
  stick), Coppie (a little copier), Brick (a 90s brick phone), Sticky, Floppy, Mugsy, Hilite or Potato.
- 🎞️ **Animated**: idle, listening, thinking, talking, sleeping, plus reactions (pokes, hops, waves, smugness).
  Eyes follow your cursor, and the eyebrows do a lot of the acting.
- 🪟 **Windows 11-style settings**: Fluent design, light and dark mode, applies changes instantly.
- 📴 **Works completely offline.** Personality, jokes, tips and commands need no network.
- 🤖 **Optional AI brain** (Claude). Off by default; falls back to offline if anything goes wrong.
- 🔒 **Privacy-first.** App and clipboard awareness are opt-in, reduced to a category on the spot
  ("code editor", "a link"), and never stored or sent anywhere.
- 😴 **Built to run all day**: naps when you're away, hides during full-screen games and presentations,
  snooze from the tray, never repeats a joke within an hour, and throttles its animation frame rate.
- ⏰ **Reminders**: "remind me in 20 minutes to stretch", `/remind at 3pm call mom`. They survive restarts.
- 🧘 **Wellbeing**: optional break nudges after a long stretch at the keyboard, and quiet hours when Pasty
  keeps its thoughts to itself.
- ⌨️ **Ask from anywhere** with a global shortcut (Ctrl+Alt+P by default; Windows).
- 🐢 **Reduced motion**: follows your system's animation setting, or choose it in Settings.
- 🧩 **Plugins**: drop-in .NET DLLs can add slash commands (opt-in).
- 🪟 **Made for Windows 10 and 11**, with an [experimental Linux build](#linux-experimental).

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
Download `Pasty-1.3.0-win-x64.zip` (or `win-arm64`), extract anywhere, run `Pasty.exe`.
No installer and no admin rights needed. Turn on *Start with Windows* in Settings if you'd like.

### Linux (experimental)
Pasty is made for Windows. The Linux build is **experimental**: it's provided as-is, gets fixes when they're
simple, and isn't tested across distributions. Bug reports and pull requests are welcome.

Download `Pasty-1.3.0-linux-x64.tar.gz` (or `linux-arm64`), then:

```sh
tar -xzf Pasty-1.3.0-linux-x64.tar.gz
cd Pasty-1.3.0-linux-x64
sh install.sh           # per-user install, no root; sh install.sh --uninstall to remove
```

Known limits:

- **Transparency needs a compositor.** On X11, Pasty can only be see-through while a compositing window manager
  is running; without one it sits in a white square (Pasty tells you when this happens). On some desktops it
  shows the white square even with compositing on (seen on MATE); that's not solved yet.
- **X11 works best.** On Wayland, Pasty runs through XWayland: your compositor may ignore "always on top", and
  Pasty can only see the cursor and focused app for XWayland windows.
- **No global ask shortcut.** Bind a key to `pasty` in your desktop's keyboard settings instead (see below).
- **Tray icon** needs a panel that supports StatusNotifier/AppIndicator icons.
- **Optional packages:** `libxss1` / `libXScrnSaver` (napping when you're away) and `libsecret-tools`
  (store the API key in your keyring).

## Using Pasty

| Do this | Pasty does |
|---|---|
| Drag | Moves (and lands with a hop) |
| Click | Gets poked. Keep poking and see what happens. |
| Double-click, or Ctrl+Alt+P anywhere | Opens the ask box |
| Right-click / tray icon | Menu: ask, joke, snooze, hide, settings, exit |
| Launch Pasty again | The running Pasty comes forward to listen |
| `Pasty --settings` | Opens settings |

Type `/help` in the ask box for commands: `/joke`, `/tip`, `/roll 2d6`, `/flip`, `/time`, `/birthday`,
`/remind`, `/reminders`, `/forget`, `/boo`, `/trickortreat`, `/graveyard`, `/brain`, `/reset`, `/settings`.

On Linux there's no built-in global shortcut yet: bind a key to `pasty` in your desktop's keyboard
settings, and the running Pasty comes forward to listen.

## AI brain (optional)

Pasty's offline brain is always there. In **Settings → Brain** you can add a smarter one:

| Brain | What you need |
|---|---|
| **Ollama** (runs on your PC) | [Ollama](https://ollama.com) and a model (`ollama pull llama3.2`). No key; nothing leaves your computer. |
| **Claude** (Anthropic) | API key from console.anthropic.com, or `ANTHROPIC_API_KEY` |
| **Copilot** (GitHub Models) | GitHub token with `models:read`, or `GITHUB_TOKEN` |
| **ChatGPT** (OpenAI) | API key from platform.openai.com, or `OPENAI_API_KEY` |
| **Gemini** (Google) | API key from aistudio.google.com, or `GEMINI_API_KEY` |
| **Custom** | Any OpenAI-compatible endpoint: LM Studio, Azure OpenAI / Microsoft Foundry, OpenRouter, Mistral, Groq… |

Each brain keeps its own model, endpoint and key. **List models** shows what your account or server
actually offers. Keys are encrypted with DPAPI on Windows, or kept in your desktop keyring on Linux
when `secret-tool` is available. Only what you type into the ask box is sent; idle remarks, pokes,
easter eggs and context reactions stay offline. If a brain can't be reached, Pasty says so in
character and falls back to its offline brain.
## Building from source

Requires the .NET 10 SDK.

```sh
dotnet build                      # build everything
dotnet test                       # run the test suite
dotnet run --project src/Pasty.Desktop -f net10.0-windows10.0.19041.0   # Linux: -f net10.0
pwsh packaging/publish.ps1        # release packages for Windows + Linux into ./dist
pwsh packaging/windows/build-msix.ps1 -Test   # Microsoft Store (MSIX) test build; see the script for Store builds
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for how the pieces fit together.

## Privacy

Pasty collects nothing and has no telemetry. See [PRIVACY.md](PRIVACY.md) for exactly what stays on your computer and what an optional AI brain receives.

## License

Copyright (C) 2026 The Pasty Authors

Pasty is free software: you can redistribute it and/or modify it under the terms of the GNU General
Public License as published by the Free Software Foundation, either version 3 of the License, or (at
your option) any later version. See [LICENSE](LICENSE).

Pasty is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

`SPDX-License-Identifier: GPL-3.0-or-later`
