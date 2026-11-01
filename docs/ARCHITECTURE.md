# Architecture

```
src/
  Pasty.Core/           net10.0, no UI, no OS calls: Pasty's mind
    Personality/        lines, shuffle bags, PersonalityEngine (what to say, when)
    Seasons/            SeasonCalendar, Halloween Edition lines
    Animation/          PastyAnimator → PastyPose (procedural, framework-agnostic)
    Brains/             IPastyBrain, OfflineBrain, PastyConversation (routing + fallback)
    Commands/           slash-command registry
    Context/            IContextSource, ContextHub (consent chokepoint), classifiers
    Platform/           IPlatformServices: what the OS must provide
    Plugins/            IPastyPlugin, loader, built-in commands (written as a plugin)
    Settings/           PastySettings, SettingsStore (JSON)
  Pasty.Brains.Claude/  optional AI brain (official Anthropic SDK)
  Pasty.Desktop/        Avalonia UI for Windows + Linux
    Character/          PastyAvatar (vector art), BubbleWindow
    Platform/           WindowsPlatform (Win32), LinuxPlatform (X11/libXss, XDG, libsecret)
    Services/           context sources, presence monitor
    PastyCompanion.cs   composition root: Pasty's "will"
tests/Pasty.Core.Tests  xUnit
packaging/              publish script, Linux installer
assets/                 icons, generated from the artwork by `Pasty --export-icons assets`
```

## Principles

**Offline first.** Everything in Pasty.Core works with no network. An AI brain is an optional
`IPastyBrain`. `PastyConversation` runs slash commands and easter eggs before any brain, and falls back
to `OfflineBrain` if the brain throws.

**Privacy by construction.** All environment awareness goes through `ContextHub`, which starts an
`IContextSource` only while its consent flag is on and stops it when the flag goes off. Sources reduce
raw data to a category (`ContextSignal`) before raising it; nothing downstream sees window titles or
clipboard text. Context reaches an AI brain only if `ShareContextWithAi` is also on, and then only the
app category.

**Cheap enough to run all day.** `PastyAnimator` reports a `RecommendedFps` (8 asleep, 20 idle,
30–40 while active) and the avatar's frame timer follows it; animation stops entirely while hidden.
Presence and context sources poll cheap OS counters every few seconds. Shuffle bags stop repetition;
`PastyCompanion` enforces a minimum gap between unprompted remarks.

**Portable core, thin shell.** UI frameworks and OSes vary; Pasty's behavior doesn't. The animator
produces plain numbers (`PastyPose`), so a new front end only maps the pose onto its own drawing.
OS-specific needs sit behind `IPlatformServices` and degrade to "unknown" rather than failing.

**Spooky, not harmful (seasons).** Seasonal content is data (`HalloweenLines`) plus cosmetic toggles.
It never adds new capabilities, never flashes, and is covered by a test that rejects alarming wording.

## Extending

- **New lines:** add to `Lines` / `HalloweenLines`. Spice 0–2 is gated by the sarcasm slider.
- **New season:** add to `Season`, extend `SeasonCalendar`, add a lines class, and give
  `PastyAvatar.Season` a costume.
- **New brain:** implement `IPastyBrain` and throw `BrainException` with an in-character message for
  expected failures.
- **Plugin:** reference Pasty.Core, implement `IPastyPlugin`, register commands in `Initialize`, and drop
  the DLL into the plugins folder (`%APPDATA%\Pasty\plugins` or `~/.config/Pasty/plugins`).
- **New OS:** implement `IPlatformServices` and return it from `PlatformServices.Create()`.
