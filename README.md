# VNTextMiner

**VNTextMiner** is a streamlined, standalone overlay tool for Japanese learners. It monitors your clipboard to analyze Japanese text from **any source** (Visual Novels, Browsers, OCR tools) and provides a modern, Yomitan-style popup interface for "mining" vocabulary directly into Anki.

![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)

## ✨ Features

*   **Universal Clipboard Monitoring:** Works with Textractor, Game2Text, OCR, or just simple Copy+Paste.
*   **Smart Tokenization:** Built-in MeCab analysis to break sentences into words.
*   **Yomitan-Style Popup:** 
    *   Rich dictionary entries with pitch accent info.
    *   **Stacked Furigana:** Proper Ruby text rendering.
    *   **Color-Coded Tags:** Visual badges for parts of speech and commonality.
*   **One-Click Anki Mining:** 
    *   Connects via AnkiConnect.
    *   Auto-fills: Headword, Reading, Definition, and Context Sentence (with highlighted word).
*   **Overlay Features:**
    *   **Transparency Slider:** Blend the overlay seamlessly into your game.
    *   **Click-Through Toggle:** Hide the UI when reading, show it when mining.
    *   **Instant Lookup:** Click any word to pop the dictionary.

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
2.  Download `VNTextMiner_App.zip`, `jitendex.zip`, and `mecab-dic.zip`.
3.  Extract the App to a folder.
4.  Place `jitendex.zip` next to `OverlayApp.exe`.
5.  Extract `mecab-dic.zip` into a folder named `dic/` next to the exe.
6.  Run `OverlayApp.exe`.

## 🎮 Usage

1.  **Open VNTextMiner.**
2.  **Configure:** Open settings (⚙️) and set your Anki Deck/Model names.
3.  **Start your Game/VN:** Use [Textractor](https://github.com/Artikash/Textractor) to hook the game.
4.  VNTextMiner will automatically detect new text, analyze it, and display it.
5.  **Click a word** -> See definition -> **"Add to Anki"**.

## 🗺 Roadmap & Milestones

We are actively working to improve VNTextMiner. PRs are welcome!

- [ ] **📸 Auto-Screenshot:** Capture game context automatically on mine.
- [ ] **🎙️ Audio Replay:** Record the last 10s of game audio for Anki cards.
- [ ] **🧠 Smart Anki:** Visual indicators for words you already know.
- [ ] **Settings Persistence:** Save window position, opacity, and deck names between sessions.
- [ ] **Theme Support:** Dark/Light mode and custom CSS for the popup.
- [ ] **Audio Mining:** Support for capturing system audio when mining a card.
- [ ] **Multiple Dictionaries:** Support for JMdict, Kanjidic, and EPWING imports.
- [ ] **Auto-Updater:** Built-in check for new releases.
- [ ] **Add desktop icon:** So it doesn't look weird
- [ ] **fix the scroll-bar:** So the mouse doesn't need to be on the slider in order to move up or down
- [ ] **Add more Anki customization features**

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
