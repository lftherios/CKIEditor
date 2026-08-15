# Releasing

Builds are produced by GitHub Actions ([.github/workflows/release.yml](../.github/workflows/release.yml)) using [game-ci](https://game.ci) — no local Unity needed. Every `v*` tag builds macOS, Windows and Linux zips (format harness runs first as a gate) and publishes them as a GitHub Release.

## One-time setup: Unity license secret

CI needs a Unity Personal license file (free). Five minutes, once:

1. **Actions → "Acquire Unity activation file" → Run workflow.** Download the `.alf` artifact it produces.
2. Upload the `.alf` at [license.unity3d.com/manual](https://license.unity3d.com/manual) (sign in with any Unity account, choose *Personal*). You get a `.ulf` file back.
3. **Settings → Secrets and variables → Actions → New repository secret**: name `UNITY_LICENSE`, value = the entire contents of the `.ulf` file.

Optionally also add `UNITY_EMAIL` and `UNITY_PASSWORD` secrets (game-ci uses them as a fallback activation path).

## Cutting a release

```sh
git tag v0.9.0 && git push origin v0.9.0
```

That's the whole process. The release appears with `Cirklon2-Desktop-App-{macOS,Windows,Linux}.zip` attached and auto-generated notes. `workflow_dispatch` runs build the zips as artifacts without publishing a release — useful for testing the pipeline.

Version number shown to users comes from `bundleVersion` in `ProjectSettings/ProjectSettings.asset` — bump it when tagging.

## Local builds

With Unity 2019.3 installed: **Build** menu → macOS / Windows / Linux / All (output in `Builds/`, gitignored). Headless: `Unity -batchmode -quit -projectPath . -executeMethod CKIEditor.EditorTools.BuildScript.BuildAll`.

## Notes for users installing builds

The binaries are unsigned. macOS: right-click → Open the first time (or `xattr -cr "Cirklon2 Desktop App.app"`). Windows: SmartScreen → More info → Run anyway.
