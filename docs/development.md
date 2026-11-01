# Development

## Prerequisites

- Windows 11 x64 and PowerShell 7.
- .NET SDK 10; `global.json` selects the SDK feature band.
- Inno Setup 6.7+ for the installer. Building and testing do not require it.

From the repository root:

```powershell
dotnet restore PetDesk.slnx --locked-mode
dotnet format whitespace PetDesk.slnx --verify-no-changes --no-restore
dotnet build PetDesk.slnx -c Release --no-restore
dotnet test PetDesk.slnx -c Release --no-build --no-restore
./tools/Build.ps1
```

Package versions are centralized in `Directory.Packages.props`; lock files are committed. To intentionally update dependencies, edit the central versions and run `dotnet restore --force-evaluate`, then review the lock-file diff.

Scripts use the repository root, normal system folders or explicit command-line parameters. There are no author-specific absolute paths. `-PackageCache` and `-Offline` are optional for a pre-populated local NuGet cache; normally NuGet's standard cache is used.

## Code structure and conventions

The solution has four application layers and two test projects. Each top-level type has its own file. C# uses the built-in `dotnet format whitespace` rules from `.editorconfig`; XML/XAML uses two-space indentation. Keep domain logic in Core, Windows/storage operations in Infrastructure, decoding in Rendering and view composition in App. See [architecture](architecture.md).

The tray host is `Views/SettingsWindow`; its four page controls are in `Views/Pages`. Shared resources are grouped in `Themes/Foundation.xaml`, `Buttons.xaml`, `Inputs.xaml` and `Menus.xaml`. `SettingsViewModel` exposes commands and a typed `DashboardSection`; palette drafts use `PaletteEditorViewModel`.

Do not add a second animation player to a page. Frozen frames come from `PetWindow.FrameChanged`, and list thumbnails use a small independent decode. Add user-facing text to both `Localization/Strings.en.json` and `Strings.ru.json`.

## Isolated Windows QA

These commands derive all paths from the current checkout:

```powershell
$app = Join-Path $PWD 'src/PetDesk.App/bin/Release/net10.0-windows/win-x64/PetDesk.exe'
$profile = Join-Path $PWD 'artifacts/gui-profile'
& $app --data-dir $profile --ui-test --diagnostics
& $app --data-dir $profile
```

`--diagnostics` opens a blue test surface with a click counter and puts the pet on it. The surface takes focus and hides the popup; the second command reopens the same running instance. A transparent-corner click must increment the counter; dragging an opaque part must move the pet without incrementing it. `--ui-test` makes the normal popup targetable by window automation. Production mode hides it from the taskbar/Alt+Tab.

`--hit-test-target` opens only the counter window in a separate process. `--virtual-desktop` expands that surface across monitors; capture tools may still support only one screen. `--exit` requests a clean shutdown for the selected data profile. Diagnostic flags are opt-in and do not affect normal startup.

## Reproducing original assets

```powershell
dotnet run --project tools/PetDesk.AssetBuilder -- (Join-Path $PWD 'assets/builtin')
```

The built-in images are committed, so normal builds do not need to regenerate them. Tests create a synthetic Codex-v2 atlas in a disposable temporary directory; no personal pet image is needed.

## Performance

`tools/Measure-PetDesk.ps1 -ProcessId <id>` records whole-machine CPU, one-core equivalent, working set and private commit. Measure separately with the dashboard closed, during movement and with both windows hidden. Results depend on the atlas, DPI, startup history and GC; a strict 100 MiB working-set target is not guaranteed. Continuous frame pacing and mixed-DPI behavior require real Windows checks.
