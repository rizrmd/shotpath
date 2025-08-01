# ShotPath 📸

A lightweight Windows screenshot tool that runs in the system tray, providing quick screenshot capture with automatic path copying.

![App Icon](app.png)

## Features

- 🖼️ **System tray application** - Runs quietly in the background
- ⌨️ **Global hotkeys**:
  - `Alt+J` - Take screenshot and copy file path to clipboard
  - `Alt+Ctrl+J` - Take screenshot and copy image to clipboard
  - `Alt+Shift+J` - Take screenshot and upload to Imgur (copy URL)
- 🎯 **Selection tool** - Draw a box to capture specific screen areas
- 📁 **Organized storage** - Screenshots saved to `%TEMP%\shotpath\` folder
- 🚀 **Auto-start** - Runs at Windows startup by default
- 🎨 **Custom icons** - Uses custom app and tray icons

## Installation

1. Download `shotpath.exe` from the [Releases](https://github.com/rizky05/shotpath/releases) page
2. Place the executable anywhere on your system
3. (Optional) Add `app.png` and `tray.png` in the same directory for custom icons
4. Run `shotpath.exe`

## Usage

### Taking Screenshots

1. Press `Alt+J` to capture a screenshot
2. Draw a selection box around the area you want to capture
3. The file path is automatically copied to your clipboard
4. Press `ESC` to cancel the selection

### System Tray Menu

Right-click the tray icon to access:

- **Copy as Path (Alt+J)** - Take screenshot and copy file path
- **Copy as Image (Alt+Ctrl+J)** - Take screenshot and copy image data
- **Copy as Imgur URL (Alt+Shift+J)** - Take screenshot and upload to Imgur
- **Open Folder** - Open the screenshots folder in Windows Explorer
- **Clear Folder** - Delete all screenshots (with confirmation)
- **Run at Startup** - Toggle auto-start with Windows
- **Exit** - Close the application

## Building from Source

### Requirements

- Windows OS
- .NET Framework 4.0 or higher
- PowerShell

### Build Steps

```bash
# Clone the repository
git clone https://github.com/rizky05/shotpath.git
cd shotpath

# Build the executable
powershell -ExecutionPolicy Bypass -File build-tray.ps1
```

### Project Structure

```
shotpath/
├── shotpath.cs        # Main application source code
├── build-tray.ps1     # Build script
├── convert-icon.ps1   # Icon conversion utility
├── app.png           # Application icon (for Explorer)
├── tray.png          # System tray icon
└── README.md         # This file
```

## Configuration

### Custom Icons

- `app.png` - Used as the executable icon in Windows Explorer
- `tray.png` - Used as the system tray icon when running

Both files should be placed in the same directory as `shotpath.exe`.

### Screenshot Location

Screenshots are saved to: `%TEMP%\shotpath\`

Format: `screenshot_YYYYMMDD_HHMMSS.png`

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+J` | Take screenshot & copy path |
| `Alt+Ctrl+J` | Take screenshot & copy image |
| `Alt+Shift+J` | Take screenshot & upload to Imgur |
| `ESC` | Cancel screenshot selection |

## Troubleshooting

### Application doesn't start
- Ensure .NET Framework 4.0+ is installed
- Run as administrator if startup registration fails

### Hotkeys not working
- Check if another application is using the same hotkeys
- Restart the application
- Make sure no other screenshot tools are running

### Icons not showing
- Place `app.png` and `tray.png` in the same folder as `shotpath.exe`
- Ensure the PNG files are valid images

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Author

Created by Rizky (rizky05@gmail.com)

---

🤖 Generated with [Claude Code](https://claude.ai/code)