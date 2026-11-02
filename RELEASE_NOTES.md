# Pasty the Desktop Companion 1.3.0

**Pasty, redesigned. And two new friends.**

- **Pasty is now a bottle of school glue** with a pointy orange twist cap that tips like a hat when he waves. His
  coral label and white swirl stay, and the label turns orange-and-black at Halloween.
- **Gluey** joins the cast: a glossy orange twist-up glue stick with a lightning-bolt label. The glue peeks out when
  his cap tips. He was Pasty's look in 1.2.0, and he's a little smug about never making a mess.
- **Coppie** joins too: a small desktop copier whose scanner lid flips up when he waves. He insists he never jams.
  He also repeats things for emphasis. He also repeats things for emphasis.
- **Classic Pasty** (the original tube) is still there. Ten companions in all.
- **Expressive eyebrows.** Bigger expressions (a one-brow-up "oh, really?", a worried arch, a proper scowl), a quick
  eyebrow flash when you open the ask box, brows that lift as he makes his point, and now and then, while idle, a
  single raised brow. On every character.
- Bolder arms to match the bolder outlines.
- New logo cap, app icons, tiles and Store screenshots.

---
# Pasty the Desktop Companion 1.2.0

**A new look, and pick your companion.**

- **Pasty is now an orange glue stick** with a teal snap-on cap and a twist-up base, and a black-and-orange label at
  Halloween. Prefer the original tube? Choose **Classic Pasty**.
- **Eight characters** in Settings → Personality → Character: **Pasty** (default), **Classic Pasty**, **Brick** (a 90s brick phone),
  **Sticky** (a sticky note), **Floppy** (a 3.5" floppy disk), **Mugsy** (a coffee mug whose steam shows its mood),
  **Hilite** (a highlighter) and **Potato**. Switching is instant.
- They all share Pasty's personality, animations and Halloween costume, with a few signature lines each, and the
  AI brain plays whichever character you pick.
- Under the hood: one shared animation rig, so every animation works for every character.

**Living together.**

- **Reminders.** Type "remind me in 20 minutes to stretch" or "remind me at 3pm to call mom" when you ask, or use
  `/remind 1h30m check the oven`, `/reminders` and `/forget 2`. Reminders are saved, so they survive a restart, and
  they come through snooze, Hide and quiet hours (they wait for full-screen apps to close).
- **Break nudges** (Settings → General, off by default): a gentle reminder to get up after 45, 60, 90 or 120
  minutes of continuous activity. Stepping away for 5 minutes resets the clock.
- **Quiet hours** (Settings → General): no idle chatter, reactions or break nudges in the window you choose.
  Answers and reminders still come through.
- **Ask from anywhere**: a global shortcut opens the ask box from any app. Ctrl+Alt+P by default, or
  Ctrl+Alt+Space or Ctrl+Shift+Alt+P. Windows only for now; on Linux, bind a key to `pasty`.
- **Reduced motion** (Settings → General → Motion): follows Windows' *Animation effects* or GNOME's animation setting
  by default. Pasty stays still (no bobbing, hopping or bat) and messages appear all at once.

**Lighter.**

- Pasty now draws on the CPU instead of loading the GPU stack, which suits its small windows. At idle that cut
  private memory from about 145 MB to about 39 MB, and CPU use to about a third. If a machine ever draws oddly,
  set `PASTY_RENDERING=gpu` to switch back.

**Linux is now experimental.**

- Pasty is made for Windows; the Linux build continues as **experimental**: provided as-is, fixed when fixes are
  simple, and not tested across distributions. Bug reports and pull requests are welcome.
- Known limits: transparency needs a running compositor (otherwise Pasty sits in a white square, and now says so),
  and some desktops show the white square even with compositing on (seen on MATE; not solved yet). X11 works best;
  Wayland runs through XWayland with limits. There's no global ask shortcut (bind a key to `pasty` instead), and
  the tray icon needs StatusNotifier/AppIndicator support.
- Install with `sh install.sh` (the old `./install.sh` instruction failed on tarballs built on Windows).

---
# Pasty the Desktop Companion 1.1.2

**First Microsoft Store release, published by Planet Archives.**

- Pasty is now published on the Microsoft Store by **Planet Archives**.
- The Halloween bat has a light outline, so it shows up on dark desktops.
- Store listing text and screenshots live in `packaging/store/` (screenshots are rendered from Pasty's own artwork with `--render-store-screenshots`).

---
# Pasty the Desktop Companion 1.1.1

**Getting ready for the Microsoft Store.**

- **Microsoft Store packaging (MSIX)** for x64 and ARM64, with Store tiles and logos rendered from Pasty's own artwork.
- **Start with Windows works in the Store version.** Store installs use Windows' startup task (also visible under Settings → Apps → Startup); the GitHub download keeps using the registry as before.
- **`pasty` command:** the Store version can be started from Run or a terminal with `pasty` (or `pasty --settings`).
- **Privacy policy** added: [PRIVACY.md](PRIVACY.md). Pasty collects nothing.
- Shutdown logging is quieter: it records only Pasty's own windows and shutdown reasons.

---
# Pasty the Desktop Companion 1.1.0

**More brains, and you can see what you type.**

- **New AI brains** in Settings → Brain, each with its own model, endpoint and key:
  - **Ollama**: runs on your PC, no key, nothing leaves your computer. Pasty warms the model up in the background so the first answer isn't slow.
  - **Copilot (GitHub Models)**, **ChatGPT (OpenAI)**, **Gemini (Google)**
  - **Custom**: any OpenAI-compatible endpoint (LM Studio, Azure OpenAI / Microsoft Foundry, OpenRouter, Mistral, Groq…)
- **List models**: asks the provider what's actually available to you.
- Roleplay actions from smaller models (`*waving*`) now become animations instead of text.
- **Fixed:** typed text in the ask box was invisible while focused in dark mode.
- **Fixed:** on Linux, saving a second provider's key no longer overwrites the first in the keyring.
- Pasty now records why it shut down in `pasty.log`, so it never disappears without a trace.
- Full product name: **Pasty the Desktop Companion**.

Existing Claude settings migrate automatically.

---

# Pasty 1.0.0: "Halloween Launch" 🎃

*Released October 31, 2026. Pasty has entered the chat.*

Pasty was born on Halloween, so it launches on Halloween: the standard release and the Halloween
Edition ship together in one download. The costume switches on automatically during Halloween season
and comes off on November 2 (under protest).

## In this release

**Pasty**
- Original character and animation: idle, listening, thinking, talking, sleeping, and reactions
- Speech bubbles, ask box, slash commands, drag anywhere, eye tracking
- Offline personality with tips, jokes and a sarcasm slider (Saintly / Cheeky / Insufferable)
- Optional Claude AI brain with automatic offline fallback
- Opt-in, privacy-first context awareness (app category, clipboard category)
- Built for all-day running: naps when you're away, hides in full-screen apps, snooze, low frame rates
- Windows 11-style settings, light and dark
- Start with Windows / start on login
- Plugin API for slash commands
- Windows (x64, ARM64) and Linux (x64, ARM64)

**Halloween Edition** 🦇
- Witch hat, fangs, orange-and-black theme, a bat friend, ghost mode
- Seasonal dialogue, `/boo`, `/trickortreat`, `/graveyard`, and an easter egg for the brave
- A very important announcement at midnight

## Known limitations
- Linux on Wayland: runs via XWayland; "always on top", global cursor and focused-app awareness
  depend on the compositor.
- macOS isn't officially supported yet (it may run, without OS awareness features).
- No speech or sound effects yet. Pasty is a silent menace.
