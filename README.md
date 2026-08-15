<div align="center">

# Cirklon 2 Desktop App

**Instrument definitions for the Sequentix Cirklon — described like a synth manual, shipped like the hardware wants them.**

[![firmware](https://img.shields.io/badge/CirkOS-1.22-F0A63C)](https://www.sequentix.com/cirklon-downloads)
[![format](https://img.shields.io/badge/.CKI-Cirklon%201%20%2B%202-1C1F24)](Assets/Scripts/CKIEditor/Serialization/)
[![engine](https://img.shields.io/badge/Unity-2019.3-2E333A)](ProjectSettings/ProjectVersion.txt)
[![tests](https://img.shields.io/badge/format%20harness-87%20passing-2a7d4f)](tests/FormatHarness/)

</div>

## Why

The Cirklon stores **six characters per control**. Your synth's filter cutoff deserves more than `FltCut` — so this editor keeps the full name, range, and your notes for every CC, and ships the hardware exactly its six characters. Paste rows straight from a MIDI implementation chart (`19 Filter Cutoff 0-127`) and labels are abbreviated for you; the documentation lives in a `.ckix` sidecar next to each export and merges back by CC number on import, so a round-trip through the hardware never costs you what you wrote down.

## What you get

Track values are arranged in **rows of exactly six** — the same six slots above six encoders the Cirklon shows — with drag-to-move between slots and a character-true preview of the TRACK screens you'll see on stage. Before anything reaches an SD card, **preflight** catches what the hardware would silently mangle (duplicate CCs, over-long labels, poly-spread past channel 16, clashing routings), each finding with a one-click fix; errors block export, warnings ship knowingly. Supports every instrument-definition feature of CirkOS 1.22 on both Cirklon 1 and 2 — including `no_thru`, bank-select flags, `presend_pgm`, 180 track-value slots, and `default_note: off` — plus drum-grid note rows and Squarp Pyramid import.

## Run it

Grab the zip for your OS from **[Releases](../../releases)**, unzip, run — the builds are unsigned, so on first launch: macOS right-click → *Open*, Windows *More info → Run anyway*. Or open the project in **Unity 2019.3** and press play (first launch regenerates `.meta` files for newer scripts; releases are cut by [CI](docs/RELEASING.md) from a `v*` tag). Import any `.CKI` — a `.ckix` beside it is picked up automatically — edit, then *Export* walks preflight → hardware preview → scope (whole library or one instrument). On the Cirklon: `MENU → Card/Sysex → Card LOAD → LOAD Instrument(s)`. The format layer is pure C# with an 87-test harness that round-trips real firmware files (`cd tests/FormatHarness && dotnet run`) — the editor's output is verified against what the Cirklon actually writes.

## The Rust rewrite

The next generation lives in [`app/`](app/): a **Tauri** build of the same editor — Rust core (`cirklon-core`, 25 tests, verified byte-equivalent to the C# reference over real firmware files), the design-study UI as the actual frontend, single-digit-MB binaries, `cargo`-only CI. Try it with `cd app && cargo tauri dev`; run its tests with `cargo test -p cirklon-core`. The Unity app above remains the shipping reference until the Tauri app reaches parity.

---

A heavily extended fork of [dyskotron/CKIEditor](https://github.com/dyskotron/CKIEditor) — original editor and UI framework by dyskotron.
