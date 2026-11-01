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
