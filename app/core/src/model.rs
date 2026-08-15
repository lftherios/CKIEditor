//! Instrument-definition model, mirroring what CirkOS 1.22 stores.

use serde::{Deserialize, Serialize};
use std::collections::BTreeMap;

pub const TRACK_VALUE_SLOTS: u32 = 180; // 30 rows of 6 (CirkOS 1.22)
pub const SLOTS_PER_ROW: u32 = 6;
pub const MAX_NAME_LEN: usize = 9;
pub const MAX_LABEL_LEN: usize = 6;
pub const POLY_SPREAD_OFF: i32 = 0;
pub const POLY_SPREAD_MIN: i32 = 2;
pub const POLY_SPREAD_MAX: i32 = 16;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum PatternType {
    P3,
    CK,
    Sel,
}

impl PatternType {
    pub fn as_str(self) -> &'static str {
        match self {
            PatternType::P3 => "P3",
            PatternType::CK => "CK",
            PatternType::Sel => "Sel",
        }
    }

    pub fn from_str(s: &str) -> PatternType {
        match s {
            "CK" => PatternType::CK,
            "Sel" => PatternType::Sel,
            _ => PatternType::P3,
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "kebab-case")]
pub enum TrackControl {
    Pgm,
    Quant,
    NotePct,
    NoteC,
    VeloPct,
    VeloC,
    LengPct,
    Tbase,
    Xpos,
    Octave,
    Knob1,
    Knob2,
    FtsR,
    FtsS,
    Reich,
}

impl TrackControl {
    pub const ALL: [TrackControl; 15] = [
        TrackControl::Pgm, TrackControl::Quant, TrackControl::NotePct, TrackControl::NoteC,
        TrackControl::VeloPct, TrackControl::VeloC, TrackControl::LengPct, TrackControl::Tbase,
        TrackControl::Xpos, TrackControl::Octave, TrackControl::Knob1, TrackControl::Knob2,
        TrackControl::FtsR, TrackControl::FtsS, TrackControl::Reich,
    ];

    /// The exact string the firmware reads and writes.
    pub fn as_def_str(self) -> &'static str {
        match self {
            TrackControl::Pgm => "pgm",
            TrackControl::Quant => "quant%",
            TrackControl::NotePct => "note%",
            TrackControl::NoteC => "noteC",
            TrackControl::VeloPct => "velo%",
            TrackControl::VeloC => "veloC",
            TrackControl::LengPct => "leng%",
            TrackControl::Tbase => "tbase",
            TrackControl::Xpos => "xpos",
            TrackControl::Octave => "octave",
            TrackControl::Knob1 => "knob1",
            TrackControl::Knob2 => "knob2",
            TrackControl::FtsR => "fts-R",
            TrackControl::FtsS => "fts-S",
            TrackControl::Reich => "reich",
        }
    }

    pub fn from_def_str(s: &str) -> Option<TrackControl> {
        Self::ALL.iter().copied().find(|tc| tc.as_def_str() == s)
    }
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(tag = "kind")]
pub enum TrackValue {
    MidiCc { cc: i32, label: Option<String> },
    Control { control: TrackControl },
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct CcDef {
    pub cc: i32,
    pub label: String,
    pub min: i32,
    pub max: i32,
    pub start: i32,
}

impl CcDef {
    pub fn new(cc: i32) -> CcDef {
        CcDef { cc, label: String::new(), min: 0, max: 127, start: 0 }
    }

    /// Setters replicate the C# editor's clamp interactions exactly.
    pub fn set_label(&mut self, label: &str) {
        self.label = label.chars().take(MAX_LABEL_LEN).collect();
    }

    pub fn set_min(&mut self, value: i32) {
        self.min = value.clamp(0, self.max);
        self.max = self.max.clamp(self.min, 127);
        self.start = self.start.clamp(self.min, self.max);
    }

    pub fn set_max(&mut self, value: i32) {
        self.max = value.clamp(self.min, 127);
        self.min = self.min.clamp(0, self.max);
        self.start = self.start.clamp(self.min, self.max);
    }

    pub fn set_start(&mut self, value: i32) {
        self.start = value.clamp(self.min, self.max);
    }
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct NoteRow {
    pub note_id: i32,
    pub label: String,
    pub always_show: bool,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct Instrument {
    pub name: String,
    pub midi_port: i32,
    pub midi_channel: i32,
    /// None = "off": the default note follows the scene root note.
    pub default_note: Option<i32>,
    pub default_pattern: PatternType,
    pub multi: bool,
    /// 0 = off, otherwise number of spread channels (2-16).
    pub poly_spread: i32,
    pub no_xpose: bool,
    pub no_fts: bool,
    pub no_thru: bool,
    pub no_bank_m: bool,
    pub no_bank_l: bool,
    pub show_note_nums: bool,
    pub presend_pgm: bool,
    /// Sparse: only occupied slots (1..=180).
    pub track_values: BTreeMap<u32, TrackValue>,
    pub cc_defs: BTreeMap<i32, CcDef>,
    pub note_rows: BTreeMap<i32, NoteRow>,
}

impl Instrument {
    pub fn new(name: &str) -> Instrument {
        Instrument {
            name: name.to_string(),
            midi_port: 1,
            midi_channel: 1,
            default_note: crate::note::note_id("C 3"),
            default_pattern: PatternType::Sel,
            multi: false,
            poly_spread: POLY_SPREAD_OFF,
            no_xpose: false,
            no_fts: false,
            no_thru: false,
            no_bank_m: false,
            no_bank_l: false,
            show_note_nums: false,
            presend_pgm: false,
            track_values: BTreeMap::new(),
            cc_defs: BTreeMap::new(),
            note_rows: BTreeMap::new(),
        }
    }
}

/// An ordered .CKI file: instrument order is preserved through round-trips.
#[derive(Debug, Clone, PartialEq, Eq, Default, Serialize, Deserialize)]
pub struct Library {
    pub instruments: Vec<Instrument>,
}
