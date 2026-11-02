# Handoff: a Pasty page on planetarchives.org

You're picking this up in the **planetarchives** website repo. The task is to build a product page for
**Pasty the Desktop Companion**. Pasty itself lives in a separate repo on this machine:
`D:\Projects\Pasty` (read from it freely; don't modify it as part of the website work).

Start by inspecting this repo (framework, how existing pages are built and styled, how HashVektor's
page is done if there is one) and follow its conventions. Ask the user only about things the repo
can't tell you.

## The product in one paragraph

Pasty is an original desktop companion character: a tube of paste with a screw-cap hat, very
expressive eyebrows, and opinions. It's the spiritual successor to the classic 90s desktop
assistant: helpful, nostalgic, slightly obnoxious, technically competent, occasionally sarcastic.
It lives on top of your desktop, animates, tells jokes and Windows tips, and answers questions when
double-clicked. It works fully offline, collects nothing, and can optionally use an AI brain the user
chooses. **Born on Halloween 2026.** Published by **Planet Archives**. Free and open source (GPL-3.0-or-later).

## Facts and links

| | |
|---|---|
| Product name | Pasty the Desktop Companion ("Pasty" for short) |
| Publisher | Planet Archives |
| Latest version | 1.1.2 |
| Source | https://github.com/thestickybullgod/pasty-the-desktop-companion |
| Downloads (latest) | https://github.com/thestickybullgod/pasty-the-desktop-companion/releases/latest |
| Privacy policy | https://github.com/thestickybullgod/pasty-the-desktop-companion/blob/main/PRIVACY.md |
| Support | support@planetarchives.org |
| License | GNU GPL v3 or later |
| Microsoft Store | **Submitted, not yet live (as of 2026-11-01).** The Store URL is unknown until it's published (format `https://apps.microsoft.com/detail/<StoreId>`). Ask the user for it; until then, build the Store button so it's easy to switch on, or leave it out. |
| Platforms | Windows 10 (1809+) and 11, x64 and ARM64; Linux x64/ARM64 (X11; Wayland via XWayland) |

## Assets (all on this machine)

| What | Path | Notes |
|---|---|---|
| Poster art 9:16 | `D:\Projects\Pasty\packaging\store\art\poster-art-9x16-1440x2160.png` | Title + waving Pasty, good hero image on mobile |
| Box art 1:1 | `D:\Projects\Pasty\packaging\store\art\box-art-1x1-2160x2160.png` | Title + Pasty, good for social cards / hero |
| Screenshots 16:9 | `D:\Projects\Pasty\packaging\store\screenshots\*.png` | 1920×1080: meet Pasty, bring your own brain, private by design, Halloween Edition |
| Icons | `D:\Projects\Pasty\assets\pasty.png`, `pasty-halloween.png` (256 px), `pasty.ico` | Head portrait; use for favicon/small marks |
| Tiles | `D:\Projects\Pasty\packaging\store\art\app-tile-icon-1x1-{300,150,71}*.png` | Full-body Pasty, transparent background |

Copy assets into this repo (optimized: convert to WebP/AVIF where the site supports it, keep PNG
fallbacks); don't hotlink to GitHub. Need a different pose or size? Pasty's artwork is vector XAML, and
the Pasty repo can render new images: `Pasty.exe --render-store-screenshots <dir>` (see
`src/Pasty.Desktop/StoreScreenshots.cs`), `--export-icons <dir>`, `--export-store-assets <dir>`.

## Copy

Approved marketing text is in `D:\Projects\Pasty\packaging\store\listing.md` (short description,
long description, feature list, AI disclosure, Halloween Edition). Reuse it; keep the voice: friendly,
a little cheeky, never mean. Taglines in use: "A desktop buddy with opinions." and "Pasty has entered the chat."

## Brand colors (from the artwork)

| Use | Color |
|---|---|
| Page gradient (key art) | `#E3F4F1` → `#FFF6DD` |
| Headline text | `#123B37`; secondary `#1F5E57`, `#2E4A47` |
| Cap teal | `#14A99A` (gradient `#2CC5B5` → `#0E857A`) |
| Label coral | `#FF6B57` |
| Ink (eyes, brows) | `#2F3A48` |
| Speech bubble (nostalgic tooltip yellow) | fill `#FFFDE7`, border `#B9A867` |
| Halloween | orange `#FF9F1C` / `#FF7A1A`, hat purple `#3A2356`, dark bg `#170D22` → `#3B1D0B` |

The app's UI font is the system font (Segoe UI on Windows). Make sure the page meets contrast
requirements in both light and dark mode if the site supports dark mode.

## Suggested page outline

1. **Hero:** name, tagline, Pasty art, primary CTA (Microsoft Store when live; otherwise GitHub
   download), secondary CTA (source on GitHub).
2. **What Pasty does:** lives on your desktop, jokes and tips, double-click to ask, sarcasm slider,
   built to run all day (naps when you're away, hides in full-screen apps).
3. **Bring your own brain:** offline by default; optional Ollama (on your PC), Claude, ChatGPT,
   Gemini, GitHub Models, or any OpenAI-compatible endpoint.
4. **Private by design:** collects nothing; awareness features are opt-in and reduced to categories;
   link to the privacy policy.
5. **Halloween Edition:** witch hat, bat, ghost mode, "Halloween is over. I refuse to remove the hat."
6. **Download / install:** Store, Windows zip, Linux tarball + `./install.sh`, system requirements.
7. **FAQ** (see below), **license** (GPL, link to source), **support** email.

## Rules (please keep these)

- **No release announcement on the portal's main index.** The user normally posts one there for a
  release; for Pasty they've chosen not to. Don't add a news item, banner, or "new release" entry to
  the main index. The Pasty product page itself is fine as a normal page. If the main index has an
  ordinary product listing or navigation where Pasty would naturally appear, ask the user before adding
  it there. (User's decision, 2026-11-01.)

- **Don't use "Clippy"** in the page title, headings, metadata, keywords, or alt text; it's a
  Microsoft trademark. Describe Pasty as an original character / "spiritual successor to the classic
  90s desktop assistant". A light, clearly-nominative mention in body copy is the most you should do.
- Third-party names (Claude, ChatGPT, Gemini, GitHub Models, Ollama) only as optional, compatible
  brains, never in logos or as endorsements.
- **Be accurate about testing:** the Linux build hasn't been tested on real Linux hardware yet; the
  cloud AI brains are implemented and unit-tested but not verified with live keys (Ollama is
  verified end to end). Don't claim otherwise.
- The GitHub Windows zip isn't code-signed; SmartScreen may warn ("More info → Run anyway"). The
  Store version is signed by Microsoft.
- For the Store button, use Microsoft's official "Get it from Microsoft" badge under its usage
  guidelines (fetch the current badge/guidelines rather than drawing one).
- Privacy claims on the page must match `PRIVACY.md` exactly in substance.

## FAQ material

- **Is it free?** Yes, free and open source (GPL-3.0-or-later). No ads, no account.
- **Does it need the internet?** No. Only if you choose an online AI brain.
- **What does it send anywhere?** Nothing, unless you pick an online AI brain; then only what you
  type into its ask box goes to the provider you chose.
- **Can I turn the Halloween costume off?** Settings → Personality → Seasonal costumes.
- **How do I start it with Windows?** Settings → General → Start with Windows.
- **Linux?** Yes (X11 best; Wayland via XWayland with limits). Download the tarball, run `./install.sh`.

## Optional follow-up worth suggesting

Host the privacy policy on planetarchives.org too (e.g. `/pasty/privacy`), then the Store listing's
privacy URL can point to the user's own domain instead of GitHub. Keep it in sync with `PRIVACY.md`.
