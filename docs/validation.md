# Validation

## Automated checks

The public Windows workflow checks formatting, repository contents, locked dependency restore, Release compilation, tests, self-contained publishing, installer compilation, portable ZIP entries and SHA-256 checksums. Integration tests generate a deterministic Codex-v2 atlas in a disposable temporary directory. They do not depend on private artwork or the author's machine.

Tests cover manifest/grid/frame bounds, PNG header preflight, actual WIC decoding, alpha masks, animation timing, movement, saved positions, safe imports, source preservation, corrupt settings/palettes, protected built-ins, palette contrast/HEX validation, language catalogs/fallback, typed navigation and dashboard commands.

## Windows GUI checks

The 1.1 implementation was exercised in Windows through Computer Use: dashboard layout, page navigation, all five presets, light/dark mode, RU/EN, pet selection, import, live palette drafts, invalid HEX, save/cancel, visibility and sliders. The pet rendered on a blue QA surface without a rectangle; a transparent-corner click reached the underlying button, and dragging an opaque region moved the pet without clicking that button. Clicking outside hid the popup.

The self-contained installed app was inspected before and after a silent update. Install/update/uninstall returned success. Hash comparisons confirmed that settings, imported pets and a custom theme survived update and uninstall. Owned startup registration and installed shortcuts/files were removed by uninstall.

## Remaining manual coverage

- Actual Windows tray icon/context menu interaction and absence of a stale icon after exit.
- Cross-monitor dragging, mixed DPI, monitor disconnects and taskbar edge/auto-hide configurations.
- Sign-out/sign-in startup and a clean Windows machine without development tools.
- Continuous animation frame pacing and long-duration resource behavior. Snapshots establish changing frames, not sustained smoothness.
- Native color picker and installer wizard navigation. Lifecycle testing used silent installation commands.

The final separate portable-window capture in the original GUI pass was interrupted with Escape. The installed self-contained build was visually checked. CI/package success should not be presented as complete hardware/GUI certification.
