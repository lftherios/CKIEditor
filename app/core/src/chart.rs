//! "Paste from chart": MIDI implementation rows straight from a manual.
//!   19 Filter Cutoff 0-127
//!   74: Cutoff
//!   CC 21 | Filter Resonance | 0-127 | 64
//!   109 Filter Pole Select (0-3) 3
//! Lines without a leading CC number (headers, prose) are skipped.

use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct ChartEntry {
    pub cc: i32,
    pub name: String,
    pub min: Option<i32>,
    pub max: Option<i32>,
    pub start: Option<i32>,
}

pub fn parse(text: &str) -> Vec<ChartEntry> {
    text.lines()
        .filter_map(|line| {
            let line = line.trim();
            if line.is_empty() {
                return None;
            }
            let entry = if line.contains('|') || line.contains('\t') {
                parse_structured(line)
            } else {
                parse_plain(line)
            };
            entry.filter(|e| (0..=127).contains(&e.cc))
        })
        .collect()
}

/// Fields separated by pipes or tabs: cc | name | range | start.
fn parse_structured(line: &str) -> Option<ChartEntry> {
    let fields: Vec<&str> = line
        .split(['|', '\t'])
        .map(str::trim)
        .filter(|f| !f.is_empty())
        .collect();
    if fields.len() < 2 {
        return None;
    }

    let cc = extract_cc(fields[0])?;
    let mut entry = ChartEntry { cc, name: fields[1].to_string(), min: None, max: None, start: None };

    for field in &fields[2..] {
        if let Some((min, max)) = parse_range(field) {
            entry.min = Some(min);
            entry.max = Some(max);
        } else if let Ok(start) = field.parse::<i32>() {
            entry.start = Some(start);
        }
    }
    Some(entry)
}

/// Whitespace-separated: [CC] <num> <name words...> [range] [start].
fn parse_plain(line: &str) -> Option<ChartEntry> {
    let mut tokens: Vec<&str> = line.split_whitespace().collect();
    if tokens.is_empty() {
        return None;
    }

    // optional "CC" / "CC#" / "#" prefix before the number
    if tokens.len() > 1 {
        let first = tokens[0].to_lowercase();
        if first == "cc" || first == "cc#" || first == "#" {
            tokens.remove(0);
        }
    }

    let cc = extract_cc(tokens[0])?;
    tokens.remove(0);

    let mut entry = ChartEntry { cc, name: String::new(), min: None, max: None, start: None };

    // pull range (and a start value after it) off the end; a bare trailing
    // number is only a start value when a range precedes it, so names like
    // "Osc 2" survive intact
    let n = tokens.len();
    if n >= 2 && tokens[n - 1].parse::<i32>().is_ok() {
        if let Some((min, max)) = parse_range(tokens[n - 2]) {
            entry.start = tokens[n - 1].parse::<i32>().ok();
            entry.min = Some(min);
            entry.max = Some(max);
            tokens.truncate(n - 2);
        }
    }
    if entry.min.is_none() && !tokens.is_empty() {
        if let Some((min, max)) = parse_range(tokens[tokens.len() - 1]) {
            entry.min = Some(min);
            entry.max = Some(max);
            tokens.pop();
        }
    }

    entry.name = tokens.join(" ");
    Some(entry)
}

/// "0-127", "0–127", "0..127", "0 to 127" (single token forms), with optional parens.
fn parse_range(token: &str) -> Option<(i32, i32)> {
    let token = token.trim_start_matches('(').trim_end_matches(')');
    for sep in ["-", "\u{2013}", "\u{2014}", "..", "to", "TO", "To"] {
        if let Some((a, b)) = token.split_once(sep) {
            let (a, b) = (a.trim(), b.trim());
            if !a.is_empty() && !b.is_empty() {
                if let (Ok(min), Ok(max)) = (a.parse::<i32>(), b.parse::<i32>()) {
                    return Some((min, max));
                }
            }
        }
    }
    None
}

/// "19", "74:", "19.", "cc19", "CC 21" - leading CC number in a field/token.
fn extract_cc(token: &str) -> Option<i32> {
    let lower = token.to_lowercase();
    let rest = lower
        .strip_prefix("cc#")
        .or_else(|| lower.strip_prefix("cc"))
        .or_else(|| lower.strip_prefix('#'))
        .unwrap_or(&lower)
        .trim();
    let digits: String = rest.chars().take_while(|c| c.is_ascii_digit()).collect();
    let after = &rest[digits.len()..];
    if digits.is_empty() || !matches!(after, "" | ":" | "." | "-") {
        return None;
    }
    digits.parse::<i32>().ok()
}
