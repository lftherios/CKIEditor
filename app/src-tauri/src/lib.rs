//! Thin Tauri shell over cirklon-core. All state lives in the frontend;
//! commands are pure functions plus file I/O, so everything stays testable
//! in the core crate.

use cirklon_core::chart::ChartEntry;
use cirklon_core::ckix::{self, Sidecar};
use cirklon_core::model::Library;
use cirklon_core::validate::{self, Finding, FixOp};
use cirklon_core::{abbrev, cki};
use serde::Serialize;

#[derive(Serialize)]
struct LoadResult {
    library: Library,
    sidecar: Sidecar,
    path: String,
}

#[tauri::command]
fn load_library(path: String) -> Result<LoadResult, String> {
    let text = std::fs::read_to_string(&path).map_err(|e| format!("can't read {path}: {e}"))?;
    let library = cki::parse(&text)?;

    let sidecar_path = ckix::sidecar_path(&path);
    let sidecar = std::fs::read_to_string(&sidecar_path)
        .map(|t| ckix::parse(&t))
        .unwrap_or_default();

    Ok(LoadResult { library, sidecar, path })
}

#[derive(Serialize)]
struct SaveResult {
    cki_path: String,
    ckix_path: Option<String>,
}

#[tauri::command]
fn save_library(path: String, library: Library, sidecar: Sidecar) -> Result<SaveResult, String> {
    std::fs::write(&path, cki::serialize(&library)).map_err(|e| format!("can't write {path}: {e}"))?;

    // only ship sidecar entries for instruments in this export
    let exported: Sidecar = sidecar
        .into_iter()
        .filter(|(name, meta)| !meta.is_empty() && library.instruments.iter().any(|i| &i.name == name))
        .collect();

    let ckix_path = if exported.is_empty() {
        None
    } else {
        let sp = ckix::sidecar_path(&path);
        std::fs::write(&sp, ckix::serialize(&exported)).map_err(|e| format!("can't write {sp}: {e}"))?;
        Some(sp)
    };

    Ok(SaveResult { cki_path: path, ckix_path })
}

#[tauri::command]
fn validate_library(library: Library) -> Vec<Finding> {
    validate::validate(&library)
}

#[tauri::command]
fn apply_fixes(mut library: Library, fixes: Vec<FixOp>) -> Library {
    for fix in &fixes {
        validate::apply_fix(&mut library, fix);
    }
    library
}

#[tauri::command]
fn parse_chart(text: String) -> Vec<ChartEntry> {
    cirklon_core::chart::parse(&text)
}

#[tauri::command]
fn suggest_label(name: String) -> String {
    abbrev::suggest(&name)
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .invoke_handler(tauri::generate_handler![
            load_library,
            save_library,
            validate_library,
            apply_fixes,
            parse_chart,
            suggest_label,
        ])
        .run(tauri::generate_context!())
        .expect("error while running application");
}
