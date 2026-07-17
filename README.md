# Todo Studio

A sleek, modern, and cross-platform desktop task manager built using **.NET 10**, **Avalonia UI (MVVM)**, and **Entity Framework Core (SQLite)**. It is designed to work seamlessly across Windows, Linux, macOS, and Android without requiring any cloud registrations or active internet connection.

---
## Install the App directly without Compiling

 - [Click Here to see the different version](https://github.com/EJaydotpng/Todo/releases/tag/v1.0.0)
---

## Features

* **Category & Sub-Category Organization:** Group tasks case-insensitively and keep track of your task streams.
* **Hierarchical Subtasks:** Create and toggle multi-level nested checklists inside any main task.
* **Professional PDF Report Generation:** Generate beautifully designed PDF summary reports of your current tasks using **QuestPDF**.
* **Auto-Filtered Excel Export:** Export all task histories into a styled **Excel spreadsheet** (`.xlsx`) using **EPPlus 8**. Includes color-coded statuses, auto-fit columns, summary statistics, and built-in native Excel AutoFilters for quick sorting and filtering.
* **Local Data Backup & Restore:** Easily backup your task database or import backup databases to sync or move your tasks between different devices (e.g., Windows PC and Arch Linux) without any cloud server.
* **Modern Tab Interface:** Selected sidebar tabs (Active Tasks & Completed Tasks) highlight as flat, clean navigation bars **without any circular radio bullet points** across all systems.
* **True Standalone Distribution:** Builds into a **single, fully self-contained executable/package** with all .NET runtimes and native visual assets (Skia, SQLite) embedded.

---

## Development Requirements

To run, develop, or build the codebase, you need:

1. **.NET 10.0 SDK** (or higher) installed.
2. **IDE/Text Editor:** 
   - [Visual Studio 2022](https://visualstudio.microsoft.com/) (with .NET Desktop development workload)
   - [JetBrains Rider](https://www.jetbrains.com/rider/)
   - [Visual Studio Code](https://code.visualstudio.com/) with the C# Dev Kit extension.
3. **Desktop Environment:**
   - **Windows:** Windows 10 (1809+) or Windows 11.
   - **Linux:** X11 or Wayland-based window manager (e.g., GNOME, KDE, Arch i3/sway) with `fontconfig` and `xorg-server` installed.
   - **macOS:** macOS Sierra 10.12 or newer.

### Installing Linux System Dependencies
If you are developing or running the application on a minimal Linux environment (like Arch Linux, minimal Ubuntu, or Fedora), you need a few native graphics, windowing, and font libraries.

We have included a helper bash script to automatically detect your package manager and install all of them at once:
```bash
chmod +x install-deps.sh
./install-deps.sh
```
*(Supports **Arch Linux**, **Debian/Ubuntu**, and **Fedora** distributions).*

---

## Principal Dependencies

The project relies on these core NuGet dependencies:
* **Avalonia UI** (v11.1+) — Modern, cross-platform XAML GUI framework.
* **CommunityToolkit.Mvvm** (v8.4+) — MVVM architectural pattern bindings.
* **Microsoft.EntityFrameworkCore.Sqlite** (v10.0+) — Lightweight embedded database mapper.
* **QuestPDF** — Native PDF layout and graphics generator.
* **EPPlus** (v8.6+) — Professional Excel sheet builder.

---

## Running Locally

1. Clone the repository to your computer:
   ```bash
   git clone https://github.com/EJaydotpng/Todo.git
   cd Todo
   ```

2. **Restore NuGet dependencies (Installs all packages at once):**
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

---

## Publishing Standalone Single-File Executables (Desktop)

These commands package the application, the .NET 10.0 runtime, and native dependencies (such as SQLite, HarfBuzz, and Skia graphics) into **a single executable**. This allows anyone to run the program on their computer without installing .NET or copying adjacent `.dll` / `.so` files.

### Windows (64-bit)
Produces a single `Todo.exe` executable:
```bash
dotnet publish Todo.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
```
* **Output Path:** `bin/Release/net10.0/win-x64/publish/Todo.exe`

### Linux (64-bit / Arch / Debian / Fedora)
Produces a single `Todo` binary:
```bash
dotnet publish Todo.csproj -c Release -r linux-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
```
* **Output Path:** `bin/Release/net10.0/linux-x64/publish/Todo`
* **Execution:** Set the execution bit on Linux using `chmod +x Todo` before running.

### macOS (Intel & Apple Silicon)
Choose the runtime identifier (`osx-x64` for Intel Macs, or `osx-arm64` for Apple M1/M2/M3 chips):

* **Apple Silicon M-Series Macs:**
  ```bash
  dotnet publish Todo.csproj -c Release -r osx-arm64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
  ```
* **Intel-based Macs:**
  ```bash
  dotnet publish Todo.csproj -c Release -r osx-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
  ```
* **Output Path:** Located in `bin/Release/net10.0/osx-arm64/publish/` (or `osx-x64`).

---

## Mobile App Support (Android)

This repository includes a dedicated, touch-optimized **Android mobile project** inside the `Android/` folder!

### Key Architecture & UX Highlights:
* **Option 1 Architecture (Linked Shared Code):** The Android app directly compiles and shares your exact same `Models/`, `Services/`, `ViewModels/`, and `Data/` context directories from the main folder. Any task updates, backups, or reporting logic instantly syncs across both Desktop and Mobile!
* **Dedicated Mobile UI (`MobileView.axaml`):** Designed with portrait-first ergonomics, featuring large touch targets, vertical layout flows, and a fullscreen subtasks manager bottom-sheet modal.
* **Unified Dropdowns & Tabs:** Pushed the categories dropdown list vertically below the search bar to give it full width, leaving the Active and Completed tabs highly spacious.
* **Clean Bottom Actions Sheets:** Condensed the bottom toolbar into two buttons: **Data** (opens a popup with Backup/Restore options and simple descriptions) and **Report** (opens a popup with PDF/Excel choices and simple descriptions).
* **Safe-Area Insets:** Automatically offsets the top header by 35px to clear modern phone notches and notification status bars seamlessly.

### How to Compile & Run the Android App:

1. **Install the .NET Android Workload:**
   Open a terminal and run:
   ```bash
   dotnet workload install android
   ```

2. **Build the Android APK (Debug Mode):**
   ```bash
   dotnet build Android/Todo.Android.csproj -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"
   ```

3. **Compile & Publish Standalone APK (Production Mode):**
   Generates a standalone, fully signed, self-contained Android `.apk` package for distribution:
   ```bash
   dotnet publish Android/Todo.Android.csproj -c Release -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"
   ```
   * **Output Path:** `Android/bin/Release/net10.0-android/publish/com.ejaydotpng.todostudio-Signed.apk`

4. **Deploy & Launch:**
   - **Using an Emulator:** Open an Android Virtual Device (via Android Studio, Rider, or VS Code) and run `dotnet run --project Android/Todo.Android.csproj -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"`.
   - **Using a Physical Phone:** Connect your Android phone with "USB Debugging" enabled via a USB cable, and run the same command to compile, install, and launch it directly on your phone!

---

## Database Locations
The SQLite data file (`todo.db`) is automatically initialized and created locally at runtime:
* **Windows:** `%LOCALAPPDATA%\TodoStudio\todo.db`
* **Linux:** `~/.local/share/TodoStudio/todo.db`
* **macOS:** `~/Library/Application Support/TodoStudio/todo.db`

---

## Licensing
This project uses **EPPlus 8** which is licensed under the PolyForm Noncommercial License for personal and non-commercial purposes. For commercial deployment, a separate license must be acquired from EPPlus Software.
