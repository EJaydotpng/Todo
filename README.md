# TODO
## Custom Task Management System that cater your needs

A sleek, modern, and cross-platform desktop task manager built using **.NET 10**, **Avalonia UI (MVVM)**, and **Entity Framework Core (SQLite)**. It is designed to work seamlessly across Windows, Linux, and macOS without requiring any cloud registrations or active internet connection.

---

## Features

* **Category & Sub-Category Organization:** Group tasks case-insensitively and keep track of your task streams.
* **Hierarchical Subtasks:** Create and toggle multi-level nested checklists inside any main task.
* **Professional PDF Report Generation:** Generate beautifully designed PDF summary reports of your current tasks using **QuestPDF**.
* **Auto-Filtered Excel Export:** Export all task histories into a styled **Excel spreadsheet** (`.xlsx`) using **EPPlus 8**. Includes color-coded statuses, auto-fit columns, summary statistics, and built-in native Excel AutoFilters for quick sorting and filtering.
* **Local Data Backup & Restore:** Easily backup your task database or import backup databases to sync or move your tasks between different devices (e.g., Windows PC and Arch Linux) without any cloud server.
* **True Standalone Distribution:** Builds into a **single, fully self-contained executable** with all .NET runtimes and native visual assets (Skia, SQLite) embedded.

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

## Publishing Standalone Single-File Executables

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

## Database Locations
The SQLite data file (`todo.db`) is automatically initialized and created locally at runtime:
* **Windows:** `%LOCALAPPDATA%\TodoStudio\todo.db`
* **Linux:** `~/.local/share/TodoStudio/todo.db`
* **macOS:** `~/Library/Application Support/TodoStudio/todo.db`

---

## Licensing
This project uses **EPPlus 8** which is licensed under the PolyForm Noncommercial License for personal and non-commercial purposes. For commercial deployment, a separate license must be acquired from EPPlus Software.
