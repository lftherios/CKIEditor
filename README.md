# CKIEditor
**Instrument Editor for Sequentix Cirklon hardware midi sequencer.**

Simple One-screen desktop editor where you can tweak all aspects of instrument definition - global params, note rows, CCs and Track values - without need to manualy edit .CKI files.

Editor supports all instrument definition features of CirkOS v1.22 (Cirklon 1 v1.22e / Cirklon 2 v1.22d), including:

* Instrument flags added since v1.17: `no_thru` (No Edtrk Thru), `no_bankM` (CC0 = bankM), `no_bankL` (CC32 = bankL), `show_note_nums` (Note Nums) and `presend_pgm` (Pre-send Pgm)
* 30 rows of track values per instrument (180 slots)
* `poly_spread` in the firmware's format - `"off"` or the number of spread channels (2 - 16)
* `default_note: "off"` (default note follows the scene root note)
* Full note range C0 - G10, with octave 10 written as `X` (e.g. `G X`)

New in this version (following the [UI/UX design study](https://claude.ai/code/artifact/31ad5186-b122-4c25-9f47-ab0933ce70d4)):

* **Preflight on export** - duplicate CCs, over-long labels (with auto-abbreviations like "Feedback Level" → "FdbkLv"), illegal characters, poly-spread channel overflow, shared port/channel routing and more. Errors block export until fixed; every finding carries a one-click repair.
* **Sidecar documentation (.ckix)** - full control names, descriptions and notes saved as JSON next to the exported .CKI and merged back by CC number on import, so the six-character hardware labels never cost you your documentation.
* **Track values in rows of six**, mirroring the Cirklon's six slots above six encoders (toggle on TrackValueListView).

* **CC map editing** - "Paste chart" imports MIDI implementation rows straight from a manual (`19 Filter Cutoff 0-127`, piped/tabbed tables, `74: Cutoff`), auto-abbreviating six-character labels. The add-CC form takes a full name and notes (stored in the .ckix sidecar) and suggests the label as you type; list rows show the full name as the label's placeholder and follow CC renumbering.

It is currently work in progress. Remaining before v1.0:

* Track value drag-and-drop arranging
* More export options (export only selected instruments, choose target CirkOS version)
