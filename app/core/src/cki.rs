//! .CKI parse and serialize, faithful to what CirkOS 1.22 reads and writes.
//! Tolerant on input (string ports, bool poly_spread from older editors),
//! canonical on output (firmware key order, "off" forms, 3-char note names).

use crate::model::*;
use crate::note;
use serde_json::{json, Map, Value};

pub fn parse(text: &str) -> Result<Library, String> {
    let root: Value = serde_json::from_str(text).map_err(|e| format!("not valid JSON: {e}"))?;
    let data = root
        .get("instrument_data")
        .and_then(Value::as_object)
        .ok_or("no instrument_data object")?;

    let mut library = Library::default();
    for (name, body) in data {
        library.instruments.push(parse_instrument(name, body));
    }
    Ok(library)
}

fn parse_instrument(name: &str, json: &Value) -> Instrument {
    let mut inst = Instrument::new(name);

    inst.midi_port = as_i32(json.get("midi_port")).unwrap_or(1);
    inst.midi_channel = as_i32(json.get("midi_chan")).unwrap_or(1);

    inst.default_note = match json.get("default_note").and_then(Value::as_str) {
        None => None,
        Some("off") | Some("") => None,
        Some(s) => note::note_id(s),
    };

    inst.default_pattern = json
        .get("default_patt")
        .and_then(Value::as_str)
        .map(PatternType::from_str)
        .unwrap_or(PatternType::P3);

    inst.multi = as_bool(json.get("multi"));
    inst.poly_spread = parse_poly_spread(json.get("poly_spread"));
    inst.no_xpose = as_bool(json.get("no_xpose"));
    inst.no_fts = as_bool(json.get("no_fts"));
    inst.no_thru = as_bool(json.get("no_thru"));
    inst.no_bank_m = as_bool(json.get("no_bankM"));
    inst.no_bank_l = as_bool(json.get("no_bankL"));
    inst.show_note_nums = as_bool(json.get("show_note_nums"));
    inst.presend_pgm = as_bool(json.get("presend_pgm"));

    parse_track_values(&mut inst, json);
    parse_cc_defs(&mut inst, json);
    parse_note_rows(&mut inst, json);

    inst
}

fn parse_track_values(inst: &mut Instrument, json: &Value) {
    let Some(track_values) = json.get("track_values").and_then(Value::as_object) else {
        return;
    };
    for (key, tv) in track_values {
        let Some(slot) = key.strip_prefix("slot_").and_then(|s| s.parse::<u32>().ok()) else {
            continue;
        };
        if let Some(value) = parse_track_value(tv) {
            inst.track_values.insert(slot, value);
        }
    }
}

fn parse_cc_defs(inst: &mut Instrument, json: &Value) {
    let Some(cc_defs) = json.get("CC_defs").and_then(Value::as_object) else {
        return;
    };
    for (key, def) in cc_defs {
        let Some(cc) = key.strip_prefix("CC_").and_then(|s| s.parse::<i32>().ok()) else {
            continue;
        };
        inst.cc_defs.insert(cc, parse_cc_def(cc, def));
    }
}

fn parse_cc_def(cc: i32, def: &Value) -> CcDef {
    let mut cc_def = CcDef::new(cc);
    if let Some(label) = def.get("label").and_then(Value::as_str) {
        cc_def.set_label(label);
    }
    if let Some(min) = as_i32(def.get("min_val")) {
        cc_def.set_min(min);
    }
    if let Some(max) = as_i32(def.get("max_val")) {
        cc_def.set_max(max);
    }
    if let Some(start) = as_i32(def.get("start_val")) {
        cc_def.set_start(start);
    }
    cc_def
}

fn parse_note_rows(inst: &mut Instrument, json: &Value) {
    let Some(rows) = json.get("row_defs").and_then(Value::as_object) else {
        return;
    };
    for (note_name, row) in rows {
        let note_id = note::note_id_lenient(note_name);
        inst.note_rows.insert(note_id, NoteRow {
            note_id,
            label: row.get("label").and_then(Value::as_str).unwrap_or("").to_string(),
            always_show: as_bool(row.get("always_show")),
        });
    }
}

fn parse_track_value(json: &Value) -> Option<TrackValue> {
    if let Some(cc) = as_i32(json.get("MIDI_CC")) {
        let label = json
            .get("label")
            .and_then(Value::as_str)
            .map(str::to_string)
            .filter(|l| !l.is_empty());
        return Some(TrackValue::MidiCc { cc, label });
    }
    if let Some(control) = json.get("track_control").and_then(Value::as_str) {
        return TrackControl::from_def_str(control).map(|c| TrackValue::Control { control: c });
    }
    None
}

/// Firmware writes "off" or a channel count 2-16; older editors wrote a bool.
fn parse_poly_spread(value: Option<&Value>) -> i32 {
    match value {
        None => POLY_SPREAD_OFF,
        Some(Value::Bool(true)) => POLY_SPREAD_MIN,
        Some(Value::Bool(false)) => POLY_SPREAD_OFF,
        Some(Value::String(s)) if s.eq_ignore_ascii_case("off") || s.is_empty() => POLY_SPREAD_OFF,
        Some(Value::String(s)) => s.parse::<i32>().unwrap_or(0).clamp(POLY_SPREAD_OFF, POLY_SPREAD_MAX),
        Some(v) => as_i32(Some(v)).unwrap_or(0).clamp(POLY_SPREAD_OFF, POLY_SPREAD_MAX),
    }
}

fn as_i32(value: Option<&Value>) -> Option<i32> {
    match value? {
        Value::Number(n) => n.as_i64().map(|v| v as i32),
        Value::String(s) => s.trim().parse::<i32>().ok(),
        Value::Bool(b) => Some(*b as i32),
        _ => None,
    }
}

fn as_bool(value: Option<&Value>) -> bool {
    match value {
        Some(Value::Bool(b)) => *b,
        Some(Value::String(s)) => s.eq_ignore_ascii_case("true") || s == "1" || s.eq_ignore_ascii_case("on"),
        Some(Value::Number(n)) => n.as_i64().unwrap_or(0) != 0,
        _ => false,
    }
}

// ---------------------------------------------------------------- serialize

pub fn serialize(library: &Library) -> String {
    let mut data = Map::new();
    for inst in &library.instruments {
        data.insert(inst.name.clone(), serialize_instrument(inst));
    }
    let root = json!({ "instrument_data": Value::Object(data) });
    serde_json::to_string_pretty(&root).expect("serialization cannot fail")
}

fn serialize_instrument(inst: &Instrument) -> Value {
    let mut obj = Map::new();
    obj.insert("midi_port".into(), json!(inst.midi_port));
    obj.insert("midi_chan".into(), json!(inst.midi_channel));
    obj.insert("multi".into(), json!(inst.multi));
    obj.insert("presend_pgm".into(), json!(inst.presend_pgm));
    obj.insert("default_note".into(), match inst.default_note {
        Some(id) => json!(note::note_name(id)),
        None => json!("off"),
    });
    obj.insert("default_patt".into(), json!(inst.default_pattern.as_str()));
    obj.insert("poly_spread".into(), if inst.poly_spread >= POLY_SPREAD_MIN {
        json!(inst.poly_spread)
    } else {
        json!("off")
    });
    obj.insert("no_bankL".into(), json!(inst.no_bank_l));
    obj.insert("no_bankM".into(), json!(inst.no_bank_m));
    obj.insert("no_xpose".into(), json!(inst.no_xpose));
    obj.insert("no_fts".into(), json!(inst.no_fts));
    obj.insert("show_note_nums".into(), json!(inst.show_note_nums));
    obj.insert("no_thru".into(), json!(inst.no_thru));

    let mut track_values = Map::new();
    for (slot, tv) in &inst.track_values {
        let mut entry = Map::new();
        match tv {
            TrackValue::MidiCc { cc, label } => {
                entry.insert("MIDI_CC".into(), json!(cc));
                if let Some(label) = label {
                    entry.insert("label".into(), json!(label));
                }
            }
            TrackValue::Control { control } => {
                entry.insert("track_control".into(), json!(control.as_def_str()));
            }
        }
        track_values.insert(format!("slot_{slot}"), Value::Object(entry));
    }
    obj.insert("track_values".into(), Value::Object(track_values));

    let mut cc_defs = Map::new();
    for (cc, def) in &inst.cc_defs {
        cc_defs.insert(format!("CC_{cc}"), json!({
            "label": def.label,
            "min_val": def.min,
            "max_val": def.max,
            "start_val": def.start,
        }));
    }
    obj.insert("CC_defs".into(), Value::Object(cc_defs));

    let mut row_defs = Map::new();
    for row in inst.note_rows.values() {
        row_defs.insert(note::note_name(row.note_id), json!({
            "label": row.label,
            "always_show": row.always_show,
        }));
    }
    obj.insert("row_defs".into(), Value::Object(row_defs));

    Value::Object(obj)
}
