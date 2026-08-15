//! Cross-implementation check helper: parse a .CKI and print the canonical
//! serialization to stdout. Compared semantically against the C# reference
//! (tests/FormatHarness --dump).

fn main() {
    let path = std::env::args().nth(1).expect("usage: dump <file.CKI>");
    let text = std::fs::read_to_string(&path).expect("readable file");
    let library = cirklon_core::cki::parse(&text).expect("parses");
    println!("{}", cirklon_core::cki::serialize(&library));
}
