//! .ckix sidecar: the documentation the hardware can't hold - full control
//! names, descriptions, groups, instrument notes. Merged by CC number so a
//! round-trip through the Cirklon never destroys what you wrote.

use serde::{Deserialize, Serialize};
use serde_json::{json, Map, Value};
use std::collections::BTreeMap;

pub const VERSION: i64 = 1;

#[derive(Debug, Clone, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct CcMeta {
    #[serde(default, skip_serializing_if = "String::is_empty")]
    pub name: String,
    #[serde(default, skip_serializing_if = "String::is_empty")]
    pub desc: String,
    #[serde(default, skip_serializing_if = "String::is_empty")]
    pub group: String,
}

impl CcMeta {
    pub fn is_empty(&self) -> bool {
        self.name.is_empty() && self.desc.is_empty() && self.group.is_empty()
    }
}

#[derive(Debug, Clone, Default, PartialEq, Eq, Serialize, Deserialize)]
pub struct InstrumentMeta {
    #[serde(default, skip_serializing_if = "String::is_empty")]
    pub notes: String,
    #[serde(default, skip_serializing_if = "BTreeMap::is_empty")]
    pub cc_meta: BTreeMap<i32, CcMeta>,
}

impl InstrumentMeta {
    pub fn is_empty(&self) -> bool {
        self.notes.is_empty() && self.cc_meta.values().all(CcMeta::is_empty)
    }

    /// Field-level merge: incoming values fill in, never blank out what's here.
    pub fn merge(&mut self, incoming: &InstrumentMeta) {
        if !incoming.notes.is_empty() {
            self.notes = incoming.notes.clone();
        }
        for (cc, meta) in &incoming.cc_meta {
            let target = self.cc_meta.entry(*cc).or_default();
            if !meta.name.is_empty() {
                target.name = meta.name.clone();
            }
            if !meta.desc.is_empty() {
                target.desc = meta.desc.clone();
            }
            if !meta.group.is_empty() {
                target.group = meta.group.clone();
            }
        }
    }
}

pub type Sidecar = BTreeMap<String, InstrumentMeta>;

pub fn merge(target: &mut Sidecar, incoming: &Sidecar) {
    for (name, meta) in incoming {
        target.entry(name.clone()).or_default().merge(meta);
    }
}

pub fn rename(sidecar: &mut Sidecar, old_name: &str, new_name: &str) {
    if old_name == new_name {
        return;
    }
    if let Some(meta) = sidecar.remove(old_name) {
        sidecar.entry(new_name.to_string()).or_default().merge(&meta);
    }
}

pub fn sidecar_path(cki_path: &str) -> String {
    match cki_path.rfind('.') {
        Some(dot) if !cki_path[dot..].contains('/') => format!("{}.ckix", &cki_path[..dot]),
        _ => format!("{cki_path}.ckix"),
    }
}

pub fn serialize(sidecar: &Sidecar) -> String {
    let mut instruments = Map::new();
    for (name, meta) in sidecar {
        if meta.is_empty() {
            continue;
        }
        let mut obj = Map::new();
        if !meta.notes.is_empty() {
            obj.insert("notes".into(), json!(meta.notes));
        }
        let mut ccs = Map::new();
        for (cc, m) in &meta.cc_meta {
            if m.is_empty() {
                continue;
            }
            let mut entry = Map::new();
            if !m.name.is_empty() {
                entry.insert("name".into(), json!(m.name));
            }
            if !m.desc.is_empty() {
                entry.insert("desc".into(), json!(m.desc));
            }
            if !m.group.is_empty() {
                entry.insert("group".into(), json!(m.group));
            }
            ccs.insert(cc.to_string(), Value::Object(entry));
        }
        if !ccs.is_empty() {
            obj.insert("cc_meta".into(), Value::Object(ccs));
        }
        instruments.insert(name.clone(), Value::Object(obj));
    }
    let root = json!({ "ckix_version": VERSION, "instruments": Value::Object(instruments) });
    serde_json::to_string_pretty(&root).expect("serialization cannot fail")
}

pub fn parse(text: &str) -> Sidecar {
    let mut sidecar = Sidecar::new();
    let Ok(root) = serde_json::from_str::<Value>(text) else {
        return sidecar;
    };
    let Some(instruments) = root.get("instruments").and_then(Value::as_object) else {
        return sidecar;
    };
    for (name, body) in instruments {
        let mut meta = InstrumentMeta::default();
        if let Some(notes) = body.get("notes").and_then(Value::as_str) {
            meta.notes = notes.to_string();
        }
        if let Some(ccs) = body.get("cc_meta").and_then(Value::as_object) {
            for (key, m) in ccs {
                let Ok(cc) = key.parse::<i32>() else { continue };
                meta.cc_meta.insert(cc, CcMeta {
                    name: m.get("name").and_then(Value::as_str).unwrap_or("").to_string(),
                    desc: m.get("desc").and_then(Value::as_str).unwrap_or("").to_string(),
                    group: m.get("group").and_then(Value::as_str).unwrap_or("").to_string(),
                });
            }
        }
        sidecar.insert(name.clone(), meta);
    }
    sidecar
}
