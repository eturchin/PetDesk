# Architecture

PetDesk is a single Windows desktop process. It uses WPF for rendering and views, Win32 for monitor/window/input behavior, and WinForms for the tray icon and native color picker. It does not use a browser runtime or remote services.

| Project | Responsibility |
|---|---|
| `PetDesk.Core` | Manifest models/validation, atlas coordinates, animation timing, behavior, movement bounds, settings normalization, palette definitions, localization and popup placement. No WPF dependency. |
| `PetDesk.Rendering` | Image-header preflight, WIC decoding, independent frozen frames and alpha masks. |
| `PetDesk.Infrastructure` | Validated copied imports, pet/palette/settings storage, bounded logs, monitor geometry and startup registration. |
| `PetDesk.App` | Composition/lifetime, overlay, shared preview, page view models, themed resources, dialogs and tray commands. |
| Test projects | Pure core tests and Windows integration tests for decoding, storage, imports and presentation commands. |

`DesktopApplication` is the explicit composition root and owns service/window lifetime. There is no dependency-injection container or service locator. `SystemAppearance` contains the Windows theme preference lookup; the resource builder stays in `ThemeEngine`.

## Panel and presentation

`SettingsWindow` owns placement, activation, native dialogs and shutdown. Dashboard, settings, palette editor and About are individual controls in `Views/Pages`, with a shared `SettingsViewModel` DataContext. Navigation uses `DashboardSection`; page controls expose no storage logic. `ObservableObject` centralizes change notification, while `PetItem` and `PaletteChoice` represent selectable rows.

`PopupLifetime` tracks ComboBox/ContextMenu focus leases so temporary popups do not dismiss their owner. Modal operations use an idempotent scope. Outside activation loss and Escape hide the panel; an explicit Exit command terminates the process.

The panel is 512 DIP wide. `PopupPlacement` converts the anchor/work area/DPI/taskbar edge into a bounded physical rectangle. Production mode uses a tool window; the explicit `--ui-test` flag changes only automation/taskbar discoverability.

Themes share semantic resource keys across every page. `ThemeEngine` creates frozen brushes and gradients from the selected palette and light/dark mode. User palettes are five validated HEX colors plus a name. The editor previews a draft using local resources; the active palette is updated only after Save. RU/EN bindings refresh without a restart, with a Russian fallback for missing keys.

## Images, animation and input

The image header is checked before decoding. `ManifestValidator` verifies dimensions, frame ranges and memory limits. The loader uses `OnLoad`, closes the input stream, converts to BGRA32 and extracts independent frozen frames. Very low alpha noise is removed from the rendering cache; source files are untouched.

There is one animation player and adaptive timer in `PetWindow`. Movement advances at roughly 30 Hz while sprite frames advance according to their own timing. Sleep/resume deltas are bounded. The dashboard receives the same frozen frame through `FrameChanged`; it has no animation loop. If the desktop pet is hidden, an open preview uses idle; when both are hidden, the timer stops. Dropdown thumbnails use a reduced decode rather than retaining full atlases.

The overlay uses layered-window alpha with `AllowsTransparency`, a transparent background and `WS_EX_TOOLWINDOW`. `WM_NCHITTEST` consults the current frame's alpha mask. Mouse capture and `GetMessagePos` keep dragging stable under queued input. Positions are bounded to monitor work areas. Saved coordinates include monitor identity, DIP offsets and DPI; monitor/DPI changes recalculate placement and fall back to the primary monitor if necessary.

## Storage and lifecycle

The default data directory comes from Windows LocalApplicationData. `--data-dir` accepts an explicit alternative, useful for isolated tests. Schema-1 settings remain compatible: new palette/language/position flags have defaults. Saves flush a temporary file and replace the target with a backup; damaged files are retained for recovery.

Imports copy into a unique staging directory, validate and decode the copied bytes, then publish a library entry with a new ID. Paths cannot escape the library or traverse reparse points. Deleting an imported entry removes only its copied directory. Built-in pets have stable protected IDs. User artwork is never automatically bundled or imported from the author's machine.

A named mutex and show/exit events coordinate each data profile. Repeating a launch shows the existing panel. `--exit` requests shutdown and waits up to ten seconds for mutex release. Shutdown saves settings, stops timers, unsubscribes OS events, disposes the tray/atlases and closes windows. Logs are bounded and avoid user paths or manifest contents.

The per-user installer keeps the same AppId across upgrades, requests clean shutdown and removes only its own startup entry. User libraries/settings/themes survive uninstall. Packaging always uses a fresh checked staging directory and emits an installer, portable ZIP and SHA-256 checksums. See [packaging and CI](release.md).

## Windows references

- [Layered windows and hit testing](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows)
- [Per-monitor DPI awareness](https://learn.microsoft.com/en-us/windows/win32/hidpi/declaring-managed-apps-dpi-aware)
- [Monitor work areas](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfow)
- [Run registry keys](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)
