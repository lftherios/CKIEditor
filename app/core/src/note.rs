//! Cirklon note names: C0..G10, three characters wide, octave 10 written "X".
//! Id is semitones from C0 (0..=127; G10 = 127).

const NAMES: [&str; 12] = [
    "C ", "C#", "D ", "D#", "E ", "F ", "F#", "G ", "G#", "A ", "A#", "B ",
];

pub const MAX_NOTE_ID: i32 = 127; // G10 ("G X")

pub fn note_name(id: i32) -> String {
    let index = (id.rem_euclid(12)) as usize;
    let octave = id.div_euclid(12);
    let octave_str = if octave == 10 { "X".to_string() } else { octave.to_string() };
    format!("{}{}", NAMES[index], octave_str)
}

pub fn note_id(name: &str) -> Option<i32> {
    if name.len() < 3 {
        return None;
    }
    let (note_part, octave_part) = name.split_at(2);
    let index = NAMES.iter().position(|n| *n == note_part)? as i32;
    let octave = if octave_part.trim() == "X" {
        10
    } else {
        octave_part.trim().parse::<i32>().ok()?
    };
    Some(index + octave * 12)
}

/// Tolerant variant matching the C# editor: unknown note letters read as C,
/// unparseable octaves as 0. Used for row_defs keys so odd files still load.
pub fn note_id_lenient(name: &str) -> i32 {
    if name.len() < 3 {
        return 0;
    }
    let (note_part, octave_part) = name.split_at(2);
    let index = NAMES.iter().position(|n| *n == note_part).unwrap_or(0) as i32;
    let octave = if octave_part.trim() == "X" {
        10
    } else {
        octave_part.trim().parse::<i32>().unwrap_or(0)
    };
    index + octave * 12
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn round_trips() {
        assert_eq!(note_name(61), "C#5");
        assert_eq!(note_id("C#5"), Some(61));
        assert_eq!(note_name(120), "C X");
        assert_eq!(note_id("C X"), Some(120));
        assert_eq!(note_name(0), "C 0");
        assert_eq!(note_id("G X"), Some(127));
    }
}
