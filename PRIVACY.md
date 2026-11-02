# Privacy Policy: Pasty the Desktop Companion

*Effective November 2, 2026. Published by Planet Archives.*

Pasty is a desktop companion that runs on your computer. It was designed to work fully offline and to
observe nothing unless you ask it to. This policy explains exactly what Pasty does with information.

## The short version

- **Planet Archives collects nothing.** Pasty has no accounts, analytics, telemetry, ads or tracking,
  and it never sends anything to us.
- **Everything stays on your computer** unless you choose an online AI brain. Then, what you type into
  Pasty's ask box goes to the AI provider you picked.
- **Awareness features are off by default.** If you turn them on, Pasty reduces what it notices to a
  category on the spot and never stores or sends the underlying data.

## What Pasty stores on your computer

| What | Where | Why |
|---|---|---|
| Your settings (position, size, personality, chosen brain, model names) | `settings.json` in Pasty's app data folder | So Pasty remembers your preferences |
| AI API keys, if you add one | Encrypted with Windows DPAPI (Windows) or your desktop keyring (Linux), referenced from settings | So Pasty can use the AI provider you chose |
| Reminders you set | `reminders.json` in the same folder; each one is removed once it has been delivered | So reminders survive a restart |
| Error and shutdown logs | `pasty-errors.log`, `pasty.log` in the same folder | Troubleshooting; they contain technical details, not your conversations |

These files never leave your computer. Uninstalling Pasty and deleting its app data folder removes them.

## Optional awareness features (off by default)

- **App awareness:** when enabled, Pasty checks which program is in the foreground and reduces its
  process name to a category such as "code editor" or "browser". It never reads window titles or
  content. The category is used only for Pasty's local remarks, unless you also turn on
  *Share context with AI* (below).
- **Clipboard reactions:** when enabled, Pasty notices when you copy text and classifies it in memory
  as a category such as "a link" or "code". The text itself is discarded immediately. It is never
  stored, logged, or sent anywhere, including to AI providers.

## Optional AI brains

Pasty's built-in offline brain sends nothing anywhere. If you choose an AI brain in Settings → Brain:

- **What is sent:** the text you type into Pasty's ask box, the earlier messages of the current
  conversation (so the AI has context), and Pasty's personality instructions. If you turn on
  *Share context with AI*, the current app category (e.g. "spreadsheet") and the time are included too.
- **What is never sent:** clipboard contents, window titles, files, or anything Pasty says on its own
  (idle remarks, pokes, easter eggs, context reactions).
- **Who receives it:** only the provider you selected, under that provider's own privacy policy and terms:
  Anthropic (Claude), GitHub (Copilot / GitHub Models), OpenAI (ChatGPT), Google (Gemini), or the
  endpoint you configure (Custom). **Ollama** runs on your own computer, so nothing leaves it unless you
  point it at another machine.
- **Conversation history** is kept in memory only, for at most 30 minutes of inactivity, and is never
  written to disk.

## Children

Pasty does not knowingly collect information from anyone, including children. Online AI brains are
operated by third parties with their own age requirements; parents can keep Pasty on its offline brain.

## Changes

If this policy changes, the updated version will be published at
https://github.com/thestickybullgod/pasty-the-desktop-companion/blob/main/PRIVACY.md with a new
effective date.

## Contact

Questions about privacy: **support@planetarchives.org**
