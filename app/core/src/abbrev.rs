//! Six-character label suggestions from full control names:
//! "Filter Cutoff" -> "FltCut", "Feedback Level" -> "FdbkLv".

use crate::model::MAX_LABEL_LEN;

const KNOWN: &[(&str, &str)] = &[
    ("filter", "Flt"), ("cutoff", "Cut"), ("resonance", "Res"), ("drive", "Drv"),
    ("envelope", "Env"), ("attack", "Atk"), ("decay", "Dec"), ("sustain", "Sus"),
    ("release", "Rel"), ("amount", "Amt"), ("oscillator", "Osc"), ("osc", "Osc"),
    ("level", "Lvl"), ("volume", "Vol"), ("feedback", "Fdbk"), ("frequency", "Frq"),
    ("wave", "Wav"), ("waveform", "Wav"), ("noise", "Noiz"), ("glide", "Gld"),
    ("portamento", "Port"), ("keyboard", "Kb"), ("tracking", "Trk"), ("track", "Trk"),
    ("modulation", "Mod"), ("mod", "Mod"), ("wheel", "Whl"), ("pitch", "Ptch"),
    ("bend", "Bnd"), ("delay", "Dly"), ("reverb", "Rev"), ("depth", "Dpth"),
    ("rate", "Rate"), ("pan", "Pan"), ("width", "Wdth"), ("detune", "Dtun"),
    ("sync", "Sync"), ("octave", "Oct"), ("velocity", "Velo"), ("gate", "Gate"),
    ("low", "Lo"), ("high", "Hi"), ("select", "Sel"), ("pole", "Pol"),
];

pub fn suggest(name: &str) -> String {
    let trimmed = name.trim();
    if trimmed.chars().count() <= MAX_LABEL_LEN {
        return trimmed.to_string();
    }

    let mut result = String::new();
    for word in trimmed.split(|c| c == ' ' || c == '_' || c == '-' || c == '/') {
        if word.is_empty() {
            continue;
        }
        let lower = word.to_lowercase();
        if let Some((_, mapped)) = KNOWN.iter().find(|(k, _)| *k == lower) {
            result.push_str(mapped);
        } else if word.chars().all(|c| c.is_ascii_digit()) {
            result.push_str(word);
        } else {
            result.push_str(&strip_vowels(word));
        }
    }

    result.chars().take(MAX_LABEL_LEN).collect()
}

/// First letter + following consonants, capped at 3 characters per word.
fn strip_vowels(word: &str) -> String {
    let mut chars = word.chars();
    let Some(first) = chars.next() else { return String::new() };
    let mut result = String::new();
    result.push(first.to_ascii_uppercase());
    for c in chars {
        if result.len() >= 3 {
            break;
        }
        if !"aeiouAEIOU".contains(c) {
            result.push(c);
        }
    }
    result
}
