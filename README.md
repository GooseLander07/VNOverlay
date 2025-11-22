# VNTextMiner

Ever get annoyed having to constantly switch between your game and a browser just to look up a word? You're deep in a Visual Novel, you hit a Kanji you don't know. You Alt-Tab out to find it, add it to anki, tab back in, and your immersion is gone.

**VNTextMiner** solves this. It is a streamlined, standalone overlay tool designed specifically for Japanese learners. It sits transparently over your game, monitors your clipboard (via Textractor, Game2Text, or OCR), and provides a modern, **Yomitan-style popup** right on top of the action.

![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)

## ✨ Features

*   **Universal Clipboard Monitoring:** Works with Textractor, Game2Text, OCR, or just simple Copy+Paste.
*   **🧠 Smart Anki Integration:**
    *   **Duplicate Detection:** Dictionary headwords turn **Green** if the word is already in your Anki deck.
    *   **Context Aware:** Prevents accidental duplicates while allowing heteronyms (same Kanji, different reading).
*   **📸 Auto-Screenshots:**
    *   Automatically captures the game window when you mine a card.
    *   **Smart Compression:** Resizes and compresses images to prevent Anki freezing or database bloat.
*   **Yomitan-Style Popup:** 
    *   **Lazy Loading:** Instant popup rendering with zero lag.
    *   **Rich Data:** Pitch accent, frequency data, and stacked Furigana.
    *   **Clean Formatting:** Exports HTML-styled lists and badges to Anki (looks just like Yomitan).
*   **Overlay Features:**
    *   **Transparency Slider:** Blend the overlay seamlessly into your game.
    *   **Click-Through Toggle:** Hide the UI when reading, show it when mining.
    *   **Modern UI:** KonoSuba-inspired aesthetic with dark mode support and custom scrollbars.

## 🛠 Prerequisites

1.  **[Anki](https://apps.ankiweb.net/):** With the **[AnkiConnect](https://ankiweb.net/shared/info/2055492159)** add-on installed.
2.  **.NET 8.0 Desktop Runtime:** [Download Here](https://dotnet.microsoft.com/download/dotnet/8.0).
3.  **Textractor:** [Necessary for game hooking](https://github.com/Artikash/Textractor)

## 🚀 Installation

1.  Open your **Start Menu**, type `PowerShell`, and press Enter.
2.  Paste the following command and press Enter:

```powershell
irm https://raw.githubusercontent.com/GooseLander07/VNTextMiner/main/setup.ps1 | iex
```

### Option 2: Manual Install
1.  Go to the [Releases Page](https://github.com/GooseLander/VNTextMiner/releases).
2.  Download `VNTextMiner_v1.1.zip`, `jitendex.zip`, and `mecab-dic.zip`.
3.  Extract the App to a folder.
4.  Place `jitendex.zip` next to `OverlayApp.exe`.
5.  Extract `mecab-dic.zip` into a folder named `dic/` next to the exe.
6.  Run `OverlayApp.exe`.

## 🎮 Usage

1.  **Open VNTextMiner.**
2.  **Configure:** Open settings (⚙️) and set your Anki Deck/Model names.
3.  **Start your Game/VN:** Use [Textractor](https://github.com/Artikash/Textractor) to hook the game.
4.  VNTextMiner will automatically detect new text, analyze it, and display it.
5.  **Click a word:**
    *   If the word turns **Green**, you already know it!
    *   If not, click **"Add +"** to mine it with screenshot and context.

## 🗺 Roadmap & Milestones

We are actively working to improve VNTextMiner. PRs are welcome!

- [x] **📸 Auto-Screenshot:** Capture game context automatically on mine.
- [x] **🧠 Smart Anki:** Visual indicators for words you already know.
- [x] **Settings Persistence:** Save window position, opacity, and deck names between sessions.
- [x] **Theme Support:** Dark/Light mode and custom CSS for the popup.
- [x] **Performance:** Instant lookups via lazy loading and threading fixes.
- [x] **UI Polish:** Custom app icon, responsive scrollbars, and clean typography.
- [ ] **🎙️ Audio Replay:** Record the last 10s of game audio for Anki cards.
- [ ] **Audio Mining:** Support for capturing system audio when mining a card.
- [ ] **Multiple Dictionaries:** Support for JMdict, Kanjidic, and EPWING imports.
- [ ] **Auto-Updater:** Built-in check for new releases.
- [ ] **Built-in OCR:** Mine text from games that can't be hooked.

## 🤝 Contributing

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/NewFeature`)
3.  Commit your Changes (`git commit -m 'Add NewFeature'`)
4.  Push to the Branch (`git push origin feature/NewFeature`)
5.  Open a Pull Request

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

## ⚖️ Legal & Attribution

**VNTextMiner** software is MIT Licensed.

**Dictionary Data:**
This software utilizes dictionary data from [Jitendex](https://jitendex.org/), which is based on **JMdict/EDICT**.
*   JMdict/EDICT files are the property of the [Electronic Dictionary Research and Development Group](http://www.edrdg.org/), and are used in conformance with the Group's licence (CC BY-SA 3.0).

**MeCab:**
Includes [MeCab](https://taku910.github.io/mecab/) and [UniDic](https://clrd.ninjal.ac.jp/unidic/) data.
