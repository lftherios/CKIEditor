# Releasing

```sh
git tag v0.9.0 && git push origin v0.9.0
```

That's the whole process — no secrets, no accounts. [`release-app.yml`](../.github/workflows/release-app.yml) gates on `cargo test -p cirklon-core`, bundles on native runners, and publishes a GitHub Release with:

| artifact | platform |
| --- | --- |
| `Cirklon2-Desktop-App-macOS-universal.dmg` | macOS, Intel + Apple Silicon in one image |
| `Cirklon2-Desktop-App-Windows-setup.exe` (+ `.msi`) | Windows x64 |
| `Cirklon2-Desktop-App-Linux.AppImage` (+ `.deb`) | Linux x64 |

`workflow_dispatch` builds the bundles as run artifacts without publishing. Bump `version` in `app/src-tauri/tauri.conf.json` and the workspace version in `app/Cargo.toml` when tagging.

## Local builds

`cd app && cargo tauri build` — bundles land in `app/target/release/bundle/`. On a Mac outside CI, prefix with `CI=true` (the DMG styling step drives Finder via AppleScript, which needs UI-automation permission; `CI=true` skips it). `cargo tauri dev` runs the app.

## Notes for users installing builds

Binaries are unsigned. macOS: right-click → Open the first time (or `xattr -cr` the app). Windows: SmartScreen → More info → Run anyway. Linux: `chmod +x` the AppImage.
