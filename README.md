# 🚀 OmniLaunch

**OmniLaunch** is a fast, lightweight, keyboard-first desktop launcher for Windows built with .NET 9 and WPF. It operates silently in the background, allowing you to summon applications, perform quick calculations, and execute web searches with a single global hotkey.

---

## ✨ Features

- ⚡ **Instant App Launcher**: Indexes Windows Start Menu shortcuts and extracts native high-res icons.
- ⌨️ **Global Hotkey**: Toggle anytime using `Ctrl + Space`.
- 🧮 **Inline Calculator**: Type math expressions (e.g., `250 * 1.15`) directly into the search bar and press `Enter` to copy the result to your clipboard.
- 🌐 **Web Search Triggers**:
  - `g <query>` or `google <query>` $\rightarrow$ Opens Google Search in your default browser.
  - `yt <query>` or `youtube <query>` $\rightarrow$ Searches YouTube directly.
  - Direct URL navigation (e.g., `https://github.com` or `www.google.com`).
- 🔔 **System Tray Integration**: Operates discreetly in the system tray with double-click toggle and exit controls.
- 🎨 **Minimalist UI**: Clean dark-mode interface with auto-hide behavior on focus loss.

---

## 🛠️ Built With

- **Framework**: .NET 9.0 (WPF)
- **Architecture**: Modular Provider-based System (`ISearchProvider`)
- **MVVM**: CommunityToolkit.Mvvm
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

---

## 🚀 Getting Started

1. Clone the repository:
   ```cmd
   git clone [https://github.com/Onee11/OmniLaunch.git] (https://github.com/Onee11/OmniLaunch.git)