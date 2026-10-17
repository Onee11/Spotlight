# 🚀 SpotlightWin

**SpotlightWin** is a high-performance, keyboard-first desktop assistant and launcher for Windows built with C# (.NET 9) and WPF. It combines instant application launching, kernel-level full-system file scanning, native OS UI automation, and free AI model processing into a single global interface.

---

## ✨ Key Features

### ⚡ Instant Application Launcher & Power Commands
- **Start Menu Indexer**: Caches installed software shortcuts (`.lnk` files) on startup for 0ms launcher response times.
- **System Commands**: Trigger native system power states directly:
  - `cmd:lock` $\rightarrow$ Lock Workstation
  - `cmd:recycle` $\rightarrow$ Empty Recycle Bin without confirmation prompts
  - `cmd:shutdown` / `cmd:restart` / `cmd:sleep`

### 🎯 Native OS UI Automation & Mouse Control
- **Precision Element Targeting**: Uses Windows UI Automation (`System.Windows.Automation`) to inspect the active application window before Spotlight takes focus.
- **Hardware Cursor Execution**: Locate or click buttons, input fields, menu items, and tabs via natural text queries (`ai point to [element]` or `ai click [element]`). Calculates exact relative bounding boxes and executes left-clicks via native Win32 `SetCursorPos` and `mouse_event` API calls.
- **Performance Capped**: Limits control scanning to 350 elements per query to prevent UI thread lockup on complex windows.

### 🤖 AI Engine & Screen Vision (100% Free Tier)
- **Groq Llama 3.3 70B Integration**: Natural language query processing, text synthesis, and automated action parsing.
- **Primary Screen Vision AI**: Automatically captures a primary monitor screenshot, scales it to 720p, and sends it to `llama-3.2-11b-vision-preview` to explain bugs, analyze code, or summarize screen content (`ai summarize this screen`, `ai solve this puzzle`).
- **Session Memory**: Tracks conversation history in-memory with quick clearing commands (`ai clear` or `ai reset`).
- **Instant Utilities**: Coin flip utility (`ai flip a coin`).

### 🌐 Live Web Search & Qwen Synthesis
- **Multi-Tier Scraper**: Queries starting with `ai search`, `ai web`, `ai news`, or `ai latest` trigger live web context retrieval using DuckDuckGo POST requests, DuckDuckGo Instant Answer API, and Wikipedia Search API fallbacks.
- **AI Synthesis**: Synthesizes live web context into markdown reports using `qwen/qwen3.6-27b` on Groq, automatically falling back to general model knowledge if web scraping is blocked.

### 📁 High-Speed Kernel File Search with Drive Modifiers
- **Win32 Kernel Traversal**: Bypasses Windows Search Indexer constraints by using multi-threaded C-level `FindFirstFileExW` and `FindNextFileW` Win32 API calls to search non-indexed directories across all connected drives (`C:\`, `D:\`, etc.).
- **Drive Modifiers**: Target or exclude specific drives directly in your search query:
  - `c: report.pdf` $\rightarrow$ Search C: drive only
  - `d:,e: project` $\rightarrow$ Search D: and E: drives only
  - `!c: image.png` or `-c: image.png` $\rightarrow$ Exclude C: drive from search
  - `exclude:d report` $\rightarrow$ Exclude D: drive from search

### 🧮 Inline Calculator & Web Triggers
- **Calculator**: Evaluates standard arithmetic expressions directly in the input bar (e.g., `250 * 1.15`) and copies the output on `Enter`.
- **Browser Shortcodes**:
  - `g <query>` or `google <query>` $\rightarrow$ Search Google
  - `yt <query>` or `youtube <query>` $\rightarrow$ Search YouTube
  - Direct URL execution (e.g., `https://github.com`)

---

## 🛠️ Tech Stack & Architecture

- **Language & Runtime**: C# / .NET 9.0 (WPF)
- **MVVM Pattern**: `CommunityToolkit.Mvvm` (`ObservableObject`, `RelayCommand`)
- **Native Interop**: Win32 `user32.dll` (`SetCursorPos`, `mouse_event`, `GetForegroundWindow`), `shell32.dll` (`SHEmptyRecycleBin`), `kernel32.dll` (`FindFirstFileExW`, `FindNextFileW`)
- **UI Automation**: `System.Windows.Automation` framework
- **AI API**: Groq Cloud REST API (`llama-3.3-70b-versatile`, `llama-3.2-11b-vision-preview`, `qwen/qwen3.6-27b`)

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 / 11 (64-bit)
- .NET 9.0 SDK
- Free API Key from [console.groq.com](https://console.groq.com/keys)

### Build & Run
1. Clone the repository:
   ```cmd
   git clone [https://github.com/YourUsername/SpotlightWin.git](https://github.com/YourUsername/SpotlightWin.git)
   cd SpotlightWin
   ```
2. Build the solution:
   ```cmd
   dotnet build
   ```
3. Run the application:
   ```cmd
   dotnet run --project SpotlightWin.UI
   ```
4. Press `Ctrl + Space` to summon SpotlightWin, open Settings, and paste your Groq API key.
