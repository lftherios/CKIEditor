//! Port of the C# FormatHarness (tests/FormatHarness) - the executable spec
//! both implementations must satisfy.

use cirklon_core::model::*;
use cirklon_core::{abbrev, chart, cki, ckix, note, validate};

const MODERN: &str = r#"{
  "instrument_data": {
    "Templt": {
      "midi_port": 3,
      "midi_chan": 2,
      "multi": true,
      "presend_pgm": true,
      "default_note": "off",
      "default_patt": "P3",
      "poly_spread": 4,
      "no_bankL": true,
      "no_bankM": true,
      "no_xpose": true,
      "no_fts": true,
      "show_note_nums": true,
      "no_thru": true,
      "track_values": {
        "slot_1": { "track_control": "pgm" },
        "slot_2": { "MIDI_CC": 74, "label": "cutoff" },
        "slot_180": { "track_control": "reich" }
      },
      "CC_defs": {
        "CC_74": { "label": "cutoff", "min_val": 5, "max_val": 100, "start_val": 42 }
      },
      "row_defs": {
        "C 3": { "label": "kick", "always_show": true },
        "D X": { "label": "high", "always_show": false }
      }
    }
  }
}"#;

fn modern() -> Library {
    cki::parse(MODERN).expect("modern file parses")
}

#[test]
fn modern_file_globals() {
    let lib = modern();
    assert_eq!(lib.instruments.len(), 1);
    let inst = &lib.instruments[0];
    assert_eq!(inst.name, "Templt");
    assert_eq!((inst.midi_port, inst.midi_channel), (3, 2));
    assert!(inst.multi && inst.presend_pgm);
    assert_eq!(inst.default_note, None, "default_note off -> None");
    assert_eq!(inst.poly_spread, 4);
    assert!(inst.no_bank_l && inst.no_bank_m && inst.no_xpose && inst.no_fts);
    assert!(inst.show_note_nums && inst.no_thru);
}

#[test]
fn modern_file_sections() {
    let lib = modern();
    let inst = &lib.instruments[0];

    assert_eq!(inst.track_values.len(), 3);
    assert!(matches!(inst.track_values[&180], TrackValue::Control { control: TrackControl::Reich }));
    assert!(matches!(&inst.track_values[&2], TrackValue::MidiCc { cc: 74, label: Some(l) } if l == "cutoff"));

    let def = &inst.cc_defs[&74];
    assert_eq!((def.min, def.max, def.start), (5, 100, 42));

    let high = inst.note_rows.values().find(|r| r.label == "high").expect("D X row");
    assert_eq!(high.note_id, 122, "octave X parses to octave 10");
    assert_eq!(note::note_name(high.note_id), "D X");
    assert!(inst.note_rows[&note::note_id("C 3").unwrap()].always_show);
}

#[test]
fn round_trip_modern() {
    let lib = modern();
    let serialized = cki::serialize(&lib);

    assert!(serialized.contains("\"presend_pgm\""));
    assert!(serialized.contains("\"no_thru\""));
    assert!(serialized.contains("\"no_bankM\"") && serialized.contains("\"no_bankL\""));
    assert!(serialized.contains("\"show_note_nums\""));
    assert!(serialized.contains("\"default_note\": \"off\""));
    assert!(serialized.contains("\"poly_spread\": 4"));

    let reparsed = cki::parse(&serialized).expect("round-trip parses");
    assert_eq!(lib, reparsed, "round-trip is lossless");
}

#[test]
fn poly_spread_forms() {
    let off = r#"{"instrument_data":{"A":{"midi_port":1,"midi_chan":1,"default_note":"C 3","default_patt":"CK","poly_spread":"off"}}}"#;
    let lib = cki::parse(off).unwrap();
    assert_eq!(lib.instruments[0].poly_spread, 0);
    assert_eq!(lib.instruments[0].default_note, note::note_id("C 3"));
    assert!(cki::serialize(&lib).contains("\"poly_spread\": \"off\""));

    let legacy = r#"{"instrument_data":{"B":{"midi_port":1,"midi_chan":1,"default_note":"C 3","default_patt":"CK","poly_spread":true}}}"#;
    assert_eq!(cki::parse(legacy).unwrap().instruments[0].poly_spread, 2, "legacy bool true -> 2");

    let stringy_port = r#"{"instrument_data":{"C":{"midi_port":"1","midi_chan":1,"default_note":"C 3","default_patt":"P3"}}}"#;
    assert_eq!(cki::parse(stringy_port).unwrap().instruments[0].midi_port, 1, "string port tolerated");
}

#[test]
fn real_insts_cki_round_trip() {
    let text = std::fs::read_to_string(concat!(env!("CARGO_MANIFEST_DIR"), "/tests/fixtures/INSTS.CKI"))
        .expect("fixture present");
    let lib = cki::parse(&text).expect("INSTS.CKI parses");
    assert_eq!(lib.instruments.len(), 19, "19 instruments");

    let serialized = cki::serialize(&lib);
    let reparsed = cki::parse(&serialized).expect("reparses");
    assert_eq!(lib, reparsed, "full library round-trip is lossless");

    // instrument order is preserved
    assert_eq!(
        lib.instruments.iter().map(|i| &i.name).collect::<Vec<_>>(),
        reparsed.instruments.iter().map(|i| &i.name).collect::<Vec<_>>()
    );
}

#[test]
fn note_helper() {
    assert_eq!(note::note_name(61), "C#5");
    assert_eq!(note::note_id("C#5"), Some(61));
    assert_eq!(note::note_name(120), "C X");
    assert_eq!(note::note_id("C X"), Some(120));
}

// ---------------------------------------------------------------- abbreviator

#[test]
fn abbreviations() {
    assert_eq!(abbrev::suggest("Filter Cutoff"), "FltCut");
    assert_eq!(abbrev::suggest("Feedback"), "Fdbk");
    assert_eq!(abbrev::suggest("Feedback Level"), "FdbkLv");
    assert_eq!(abbrev::suggest("Glide"), "Glide");
    assert_eq!(abbrev::suggest("Osc 2 Frequency"), "Osc2Fr");
    assert_eq!(abbrev::suggest(""), "");
}

// ---------------------------------------------------------------- validator

fn one(inst: Instrument) -> Library {
    Library { instruments: vec![inst] }
}

#[test]
fn validator_clean() {
    let mut inst = Instrument::new("Sub 37");
    inst.midi_port = 3;
    inst.track_values.insert(1, TrackValue::MidiCc { cc: 19, label: Some("FltCut".into()) });
    assert!(validate::validate(&one(inst)).is_empty());
}

#[test]
fn validator_duplicate_cc_with_fix() {
    let mut inst = Instrument::new("DupCC");
    inst.track_values.insert(2, TrackValue::MidiCc { cc: 74, label: Some("OscOct".into()) });
    inst.track_values.insert(5, TrackValue::MidiCc { cc: 74, label: Some("CutMW".into()) });
    let mut lib = one(inst);

    let findings = validate::validate(&lib);
    let err = findings.iter().find(|f| f.severity == validate::Severity::Error).expect("error");
    assert!(err.title.contains("CC 74"));
    let fix = err.fix.clone().expect("has fix");

    validate::apply_fix(&mut lib, &fix);
    assert!(!lib.instruments[0].track_values.contains_key(&5), "later slot cleared");
    assert!(validate::validate(&lib).is_empty(), "clean after fix");
}

#[test]
fn validator_long_label_suggests() {
    let mut inst = Instrument::new("LongLbl");
    inst.track_values.insert(1, TrackValue::MidiCc { cc: 118, label: Some("Feedback".into()) });
    let mut lib = one(inst);

    let findings = validate::validate(&lib);
    let warn = findings.iter().find(|f| f.severity == validate::Severity::Warning).expect("warning");
    assert!(warn.fix_label.as_ref().unwrap().contains("Fdbk"));

    validate::apply_fix(&mut lib, warn.fix.as_ref().unwrap());
    assert!(matches!(&lib.instruments[0].track_values[&1],
        TrackValue::MidiCc { label: Some(l), .. } if l == "Fdbk"));
}

#[test]
fn validator_name_too_long() {
    let mut lib = one(Instrument::new("Grandmother!"));
    let findings = validate::validate(&lib);
    let err = findings.iter().find(|f| f.severity == validate::Severity::Error).expect("error");
    validate::apply_fix(&mut lib, err.fix.as_ref().unwrap());
    assert_eq!(lib.instruments[0].name, "Grandmoth");
}

#[test]
fn validator_spread_overflow() {
    let mut inst = Instrument::new("Spread");
    inst.midi_channel = 14;
    inst.poly_spread = 4;
    let mut lib = one(inst);

    let findings = validate::validate(&lib);
    let err = findings.iter().find(|f| f.title.contains("channel 16")).expect("overflow error");
    validate::apply_fix(&mut lib, err.fix.as_ref().unwrap());
    assert_eq!(lib.instruments[0].poly_spread, 3, "14+3-1=16");
}

#[test]
fn validator_library_level() {
    let a = Instrument::new("SameName");
    let mut b = Instrument::new("SameName");
    b.midi_port = 1;
    let lib = Library { instruments: vec![a, b] };
    let findings = validate::validate(&lib);
    assert!(findings.iter().any(|f| f.severity == validate::Severity::Info && f.title.contains("shared")));
    assert!(findings.iter().any(|f| f.severity == validate::Severity::Error && f.title.contains("named")));
}

#[test]
fn validator_illegal_chars() {
    let mut inst = Instrument::new("Weird");
    inst.track_values.insert(1, TrackValue::MidiCc { cc: 10, label: Some("Cut\"?*".into()) });
    let mut lib = one(inst);

    let findings = validate::validate(&lib);
    let warn = findings.iter().find(|f| f.severity == validate::Severity::Warning).expect("warning");
    validate::apply_fix(&mut lib, warn.fix.as_ref().unwrap());
    assert!(matches!(&lib.instruments[0].track_values[&1],
        TrackValue::MidiCc { label: Some(l), .. } if l == "Cut"));
}

#[test]
fn validator_summary() {
    let mut inst = Instrument::new("Sub 37");
    inst.track_values.insert(1, TrackValue::MidiCc { cc: 19, label: None });
    assert!(validate::summarize(&inst).contains("1 of 180 slots"));
}

// ---------------------------------------------------------------- sidecar

#[test]
fn sidecar_round_trip_merge_rename() {
    let mut sidecar = ckix::Sidecar::new();
    let meta = sidecar.entry("Sub 37".to_string()).or_default();
    meta.notes = "Live rig, channel 1.".into();
    meta.cc_meta.insert(19, ckix::CcMeta {
        name: "Filter Cutoff".into(),
        desc: "Main sweep, 20 Hz - 20 kHz.".into(),
        group: "Filter".into(),
    });

    let parsed = ckix::parse(&ckix::serialize(&sidecar));
    assert_eq!(parsed["Sub 37"].notes, "Live rig, channel 1.");
    assert_eq!(parsed["Sub 37"].cc_meta[&19].name, "Filter Cutoff");
    assert_eq!(parsed["Sub 37"].cc_meta[&19].group, "Filter");

    // merge updates non-empty fields, keeps the rest
    let mut incoming = ckix::Sidecar::new();
    incoming.entry("Sub 37".to_string()).or_default().cc_meta.insert(19, ckix::CcMeta {
        name: "Cutoff Frequency".into(),
        ..Default::default()
    });
    let mut target = sidecar.clone();
    ckix::merge(&mut target, &incoming);
    assert_eq!(target["Sub 37"].cc_meta[&19].name, "Cutoff Frequency");
    assert_eq!(target["Sub 37"].cc_meta[&19].desc, "Main sweep, 20 Hz - 20 kHz.");

    // rename carries documentation
    ckix::rename(&mut target, "Sub 37", "Sub37 v2");
    assert!(target.get("Sub 37").is_none());
    assert!(target["Sub37 v2"].cc_meta[&19].desc.contains("20 kHz"));
}

#[test]
fn sidecar_path() {
    assert_eq!(ckix::sidecar_path("/x/SUB37.CKI"), "/x/SUB37.ckix");
    assert_eq!(ckix::sidecar_path("noext"), "noext.ckix");
}

// ---------------------------------------------------------------- chart

#[test]
fn chart_shapes() {
    let one = chart::parse("19 Filter Cutoff 0-127");
    assert_eq!(one.len(), 1);
    assert_eq!((one[0].cc, one[0].name.as_str()), (19, "Filter Cutoff"));
    assert_eq!((one[0].min, one[0].max, one[0].start), (Some(0), Some(127), None));

    let colon = chart::parse("74: Cutoff");
    assert_eq!((colon[0].cc, colon[0].name.as_str()), (74, "Cutoff"));

    let piped = chart::parse("CC 21 | Filter Resonance | 0\u{2013}127 | 64");
    assert_eq!(piped[0].cc, 21);
    assert_eq!(piped[0].name, "Filter Resonance");
    assert_eq!((piped[0].min, piped[0].max, piped[0].start), (Some(0), Some(127), Some(64)));

    let tabbed = chart::parse("18\tFilter Drive\t0-127");
    assert_eq!((tabbed[0].cc, tabbed[0].max), (18, Some(127)));

    let parens = chart::parse("109 Filter Pole Select (0-3) 3");
    assert_eq!((parens[0].min, parens[0].max, parens[0].start), (Some(0), Some(3), Some(3)));
    assert_eq!(parens[0].name, "Filter Pole Select");
}

#[test]
fn chart_edge_cases() {
    assert!(chart::parse("CC Parameter Range").is_empty(), "header skipped");

    let osc2 = chart::parse("74 Osc 2");
    assert_eq!(osc2[0].name, "Osc 2");
    assert_eq!(osc2[0].start, None, "trailing digit stays in name without a range");

    assert!(chart::parse("999 Nope").is_empty(), "cc above 127 skipped");
    assert!(chart::parse("").is_empty());

    let dots = chart::parse("cc# 5 Glide Time 0..127");
    assert_eq!((dots[0].cc, dots[0].name.as_str()), (5, "Glide Time"));
    assert_eq!((dots[0].min, dots[0].max), (Some(0), Some(127)));

    let multi = chart::parse("19 Filter Cutoff 0-127\r\n\r\nsome prose here\n74: Cutoff\n");
    assert_eq!(multi.len(), 2);
    assert_eq!((multi[0].cc, multi[1].cc), (19, 74));
}
