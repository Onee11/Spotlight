# ⌨️ SpotlightWin — Controls & Keyboard Shortcuts Guide

SpotlightWin is engineered to be **100% keyboard-driven**. Perform desktop searches, execute system commands, point/click on UI elements, run AI queries, and search files across drives without leaving your keyboard.

---

## ⚡ Global & Navigation Controls

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Ctrl + Space` *(Default)* | **Toggle Spotlight** | Summon or hide the SpotlightWin window from anywhere. Captures active application handle before taking focus. |
| `Esc` | **Dismiss** | Instantly hide the search window. |
| `Down Arrow` | **Next Result** | Navigate down through the search results list. |
| `Up Arrow` | **Previous Result** | Navigate up through the search results list. |
| `Enter` | **Execute** | Run the selected app, execute system command, perform mouse action, copy AI output, or launch web search. |

---

## 📁 Selection Quick Actions

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Enter` *(on AI / Math)* | **Copy to Clipboard** | Copies formatted AI markdown, calculation result, or web report directly to your clipboard. |
| `Enter` *(on File)* | **Open Location / File** | Opens the selected file or folder in Windows Explorer. |

---

## 🔍 Command Triggers & Syntax

### 1. 🤖 AI Assistant (`ai <prompt>`)
Prefix any prompt with `ai ` to query the Groq AI engine:
* `ai explain async programming in C#` $\rightarrow$ Generates concise markdown answer.
* `ai clear` or `ai reset` $\rightarrow$ Clears conversation memory session.
* `ai flip a coin` $\rightarrow$ Flips a virtual coin and copies the result.

### 2. 👁️ Screen Vision AI (`ai <screen_query>`)
Include visual intent words (`screen`, `monitor`, `display`, `window`, `this`, `error`, `code`, `bug`) to trigger primary display screenshot analysis:
* `ai summarize this screen`
* `ai explain this error code`
* `ai solve this puzzle on screen`

### 3. 🎯 Native UI Automation (`ai point to <element>` / `ai click <element>`)
Directly target interface controls inside the active window you were working in before pressing `Ctrl + Space`:
* `ai point to Save` $\rightarrow$ Moves cursor to center of "Save" button.
* `ai click File` $\rightarrow$ Moves cursor to "File" menu item and executes a left-click.
* `ai locate Submit` $\rightarrow$ Highlights coordinates of "Submit" input element.

### 4. 🌐 Live Web Search (`ai search <query>` / `ai news <query>`)
Fetches real-time web context via DuckDuckGo and synthesizes a live report using Qwen:
* `ai search latest tech news`
* `ai web who won the latest F1 race`

### 5. 📁 Win32 Deep File Search & Drive Modifiers
Search across all connected physical drives using kernel-level file scanning. Use drive prefixes to restrict or exclude locations:
* `report.pdf` $\rightarrow$ Searches all connected drives (`C:\`, `D:\`, etc.).
* `c: contract.docx` $\rightarrow$ Search **only Drive C:**.
* `d:,e: project_backup` $\rightarrow$ Search **only Drives D: and E:**.
* `!c: photo.png` or `-c: photo.png` $\rightarrow$ Search **all drives except Drive C:**.
* `exclude:d notes.txt` $\rightarrow$ Search **all drives except Drive D:**.

### 6. ⚡ App Launcher & Power Commands
* Type any app name (e.g., `chrome`, `spotify`, `cmd`, `notepad`).
* System power commands:
  * `cmd:lock` $\rightarrow$ Locks PC.
  * `cmd:recycle` $\rightarrow$ Empties Recycle Bin.
  * `cmd:shutdown` / `cmd:restart` / `cmd:sleep`.

### 7. 🧮 Inline Calculator & Web Triggers
* **Math**: Type expressions directly into the search bar (`1024 / 8`, `(250 * 1.15) - 50`).
* **Google**: `g <query>` or `google <query>`.
* **YouTube**: `yt <query>` or `youtube <query>`.
* **URL**: Type full address (e.g., `https://console.groq.com`).
