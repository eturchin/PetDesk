# Packaging and CI

The [Build and package workflow](https://github.com/eturchin/PetDesk/actions/workflows/build.yml) runs on pushes to `main`, pull requests and manual dispatches. It uses a Windows runner and read-only repository permissions.

The workflow checks repository contents and formatting, restores locked dependencies, compiles Release, runs tests, publishes a self-contained win-x64 app, compiles the Inno Setup installer, verifies ZIP contents/checksums and uploads the packages plus test reports. Actions are pinned to commit SHAs. No signing secrets or automatic release publication are configured.

Download the `PetDesk-win-x64` artifact from a successful run. It contains:

- `PetDesk-Setup-{version}-win-x64.exe`
- `PetDesk-Portable-{version}-win-x64.zip`
- `SHA256SUMS.txt`
- `build-manifest.json` with repository-relative paths

## Local packaging

1. Set the version in `Version.props`.
2. Install .NET SDK 10 and Inno Setup 6.7+.
3. Close any running development instance with `PetDesk.exe --exit`, including its `--data-dir` when applicable.
4. Run `./tools/Package-Release.ps1` from the checkout. For a nonstandard Inno installation, pass `-ISCCPath`.
5. Inspect `artifacts/Releases/{version}` and run the Windows checks in [validation](validation.md).

All paths are derived from the checkout or Windows environment. `-PackageCache` and `-Offline` support an existing offline cache. The staging directory is checked before replacement; user data is never used as publish output. Packages contain only the original demo pets, application/runtime files and public documentation.

## Install, update and uninstall

Installation uses the current user's LocalAppData Programs folder and creates a Start menu entry. Desktop shortcuts are optional; startup is opt-in from the app. A stable AppId preserves upgrade identity. The installer invokes `--exit` before replacing files, and uninstall also invokes `--unregister-startup` to remove an owned Run entry. User settings, pets, themes and logs are preserved.

The Windows installer minimum is Windows 11 x64. The .NET runtime is included. Builds are unsigned; SHA-256 verifies integrity, not publisher identity. Do not disable Windows protections to run an unknown build.

## Tagged releases

No release is published automatically. When ready, publish a GitHub Release from a reviewed commit using the verified workflow artifacts, include checksums and document remaining hardware/GUI limitations. CI success validates build/package behavior; it does not replace real tray, DPI and frame-pacing checks.
