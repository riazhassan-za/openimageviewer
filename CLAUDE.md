# OpenImageViewer

## Purpose
A simple, minimalist image viewer for Windows. It shows one image at a time and stays out of the way:
no sidebars, no panels, just one simple toolbar along the top.

## Core behavior (requirements)
- **Left / Up / Backspace / Page Up** go to the previous image and **Right / Down / Space / Page Down** go to the next image in the same folder (sorted by name, wrapping around at the ends). **Home** jumps to the first image and **End** to the last.
- **Mouse scroll wheel** zooms in and out, centered on the cursor.
- **Mouse wheel lock** (mouse-icon toggle in the toolbar, highlighted when on, **M** or m, or right-clicking the image area, toggles it): while it's on, scroll up goes to the previous image (like Left) and scroll down to the next (like Right), one image per wheel notch, instead of zooming. Zoom still works from the keys and buttons. It starts off each time the app opens.
- **+ / =** zoom in and **- / _** zoom out, centered on the window. Both characters on each key work, so a missed Shift doesn't matter, and so do numpad + and -.
- **F** (or f) fits the image to the window, the same as the Fit button.
- **Left-click drag** pans the image when it's zoomed in.
- **Toolbar (top only, dark to match the background)**: Open, Previous, Next, Zoom out, Zoom in, Fit, then the folder path filling the remaining space (the file name is only in the window title), with the status (index, dimensions, zoom %) on the right.
- Images open fit-to-window. Images smaller than the window are shown at their native pixel size, not stretched up.
- You can pass a file path on the command line (so "Open with…" works).
- **Fullscreen**: the toolbar button or **F11** toggles it, and **Esc** exits. The toolbar stays visible in fullscreen.
- **Clicking the folder path** in the toolbar opens the file picker in that folder, so you can pick another file.
- **About** button at the far right of the toolbar: a short description, the GPL notice, a homepage link (https://github.com/riazhassan-za/openimageviewer), and a donation notice with a Bitcoin address (`bc1qrelrzwd4rm6uxdjk9kkzynzl4ggtnl2c5f2k7f`, checksum verified). The address must stay exact, because a typo would send donations somewhere unrecoverable.
- **Drag and drop** an image file onto the window to open it. That folder then becomes the one you navigate through.

## Design principles
- Minimalist: add a feature only if someone asks for it. One toolbar, no extra chrome.
- No third-party dependencies. Image decoding uses WPF/WIC (jpg, png, bmp, gif, tiff, ico, plus webp/heic if Windows has the codecs).

## Tech stack
- C# / WPF, .NET 8 (`net8.0-windows`)
- `App.xaml(.cs)`: startup; passes the command-line file path to the main window
- `MainWindow.xaml(.cs)`: toolbar, viewport, zoom/pan transforms, folder navigation
- `AboutWindow.xaml(.cs)`: About / donation dialog
- Shared dark button and separator styles live in `App.xaml`

## License
GPL-3.0-or-later, the same as openvncviewer. See `LICENSE`. The About dialog shows the GPL copyright and no-warranty notice.

## Build & run
```powershell
dotnet build
dotnet run -- "C:\path\to\image.jpg"   # path is optional
```

## Releasing
The version is `<Version>` in `OpenImageViewer.csproj`, and the About box reads it from there. To release, bump it, commit and push, then build from that commit:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```
Rename the exe to `OpenImageViewer-x64.exe`, write `SHA256SUMS.txt` (`<sha256>  OpenImageViewer-x64.exe`), and attach both to a GitHub release tagged `vX.Y.Z`. The exe is unsigned, so the README's SmartScreen warning stays.
