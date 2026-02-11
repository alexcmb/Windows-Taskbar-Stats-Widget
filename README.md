# Windows Taskbar Stats Widget

A lightweight, minimalist, and highly customizable widget for Windows 10/11 that displays CPU and GPU temperatures directly above your taskbar.

## Features

### Core Functionality
*   **Real-time Monitoring**: Polls hardware sensors every second (CPU & GPU).
*   **Minimalist Overlay**: Designed to sit unobtrusively above the taskbar.
*   **Always on Top**: Detects full-screen applications (games) and forces itself to remain visible.
*   **Smart Positioning**:
    *   **Draggable**: Hold `Left Click` to move it anywhere.
    *   **Auto-Recovery**: Automatically resets position if it detects it's off-screen (e.g., during RDP sessions or resolution changes).

### Customization 
*   **Unified Color System**: A single "Text Color" setting controls all text labels and normal temperature values for a clean look.
*   **Dynamic Alerts**: Visual feedback when temperatures rise:
    *   **Normal**: Uses your custom text color.
    *   **Warning**: Turns **Orange** (Default > 75°C, customizable).
    *   **Critical**: Turns **Red** (Default > 85°C, customizable).
*   **Background Control**: Adjust the background color and transparency to match your desktop theme.
*   **Opacity**: Scroll `Mouse Wheel` over the widget to fade it in/out instantly.

### System Tray Integration
*   **Live Icon**: The tray icon dynamically updates to show the current temperature.
*   **Context Menu**: Right-click the tray icon to:
    *   Open **Settings** to configure colors and thresholds.
    *   **Reset Position** manually.
    *   **Exit** the application.

---

## Getting Started

### Prerequisites
*   **Windows 10 or 11**
*   **.NET 10.0 Runtime** (Required for the latest version).
*   **Administrator Privileges**: Essential for `LibreHardwareMonitor` to read hardware sensors.

### How to Run
1.  Download the latest release or build from source.
2.  Right-click `TaskbarStats.exe` and select **Run as Administrator**.
3.  The widget will appear in the bottom-right corner of your primary screen.

---

## Configuration

Right-click the **Tray Icon** and select **Settings** to customize:

*   **Colors**:
    *   **Background**: Pick any color and alpha (transparency).
    *   **Text Color**: Sets the color for "CPU:", "GPU:", and normal temperature values.
    *   **Warning/Critical**: colors for specific high-temperature states.
*   **Thresholds**:
    *   Set the specific temperatures (°C) that trigger Warning and Critical alerts.
*   **Tray Icon**:
    *   Customize the colors used by the dynamic tray icon itself.

---

## Troubleshooting

### Widget is not visible?
*   **Check the Tray**: Is the icon running? If yes, right-click and check settings.
*   **Remote Desktop (RDP)**: The widget includes a fix for RDP sessions. If it disappears, it should auto-recover to the bottom-right corner.
*   **Games**: If it's hidden behind a game, ensure the game is in "Borderless Window" mode if possible, although the widget tries to force "Topmost".

### Temperatures show "--"?
*   **Run as Admin**: This is the #1 cause. Hardware sensors require elevated permissions.
*   **Compatibility**: Some very new or specific hardware might not yet be supported by the underlying `LibreHardwareMonitor` library.

---

## How to Build

1.  Open a terminal in the project directory.
2.  Run the build command:
    ```powershell
    dotnet build
    ```
3.  To publish a single-file executable (Release mode):
    ```powershell
    dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
    ```
    *Output location: `bin/Release/net10.0-windows/win-x64/publish/`*
