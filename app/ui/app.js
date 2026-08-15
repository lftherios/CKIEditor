/* Cirklon2 Desktop App — frontend. All state lives here; the Rust core does
   parsing, validation, chart import and file I/O via IPC. */
'use strict';

const invoke = window.__TAURI__ ? window.__TAURI__.core.invoke : null;
const dlg = window.__TAURI__ ? window.__TAURI__.dialog : null;

// ---------------------------------------------------------------- state

const state = {
  library: { instruments: [] },
  sidecar: {},
  path: null,
  selected: 0,
  tab: 'setup',
  filter: '',
};

// ---------------------------------------------------------------- constants

const NOTES = ['C ', 'C#', 'D ', 'D#', 'E ', 'F ', 'F#', 'G ', 'G#', 'A ', 'A#', 'B '];
const CONTROLS = [
  ['pgm', 'pgm'], ['quant', 'quant%'], ['note-pct', 'note%'], ['note-c', 'noteC'],
  ['velo-pct', 'velo%'], ['velo-c', 'veloC'], ['leng-pct', 'leng%'], ['tbase', 'tbase'],
  ['xpos', 'xpos'], ['octave', 'octave'], ['knob1', 'knob1'], ['knob2', 'knob2'],
  ['fts-r', 'fts-R'], ['fts-s', 'fts-S'], ['reich', 'reich'],
];
const PORTS = ['MIDI 1', 'MIDI 2', 'MIDI 3', 'MIDI 4', 'MIDI 5',
  'USB 1', 'USB 2', 'USB 3', 'USB 4', 'USB 5', 'USB 6'];
const FLAGS = [
  ['multi', 'Multi-timbral', 'Show a channel picker per track instead of defining this synth once per channel.', 'multi'],
  ['no_xpose', 'Ignore transpose', "Scene transpose won't shift this instrument — right for drums and clips.", 'no_xpose'],
  ['no_fts', 'Ignore force-to-scale', "FTS never re-pitches this instrument's notes.", 'no_fts'],
  ['no_thru', 'No edit-track thru', "Don't soft-thru incoming MIDI when this sits on the edit track.", 'no_thru'],
  ['no_bank_m', 'Bank select MSB', 'Send CC 0 with program changes for synths with >128 patches.', 'no_bankM'],
  ['no_bank_l', 'Bank select LSB', 'Send CC 32 with program changes.', 'no_bankL'],
  ['show_note_nums', 'Show note numbers', 'Pattern editors display 36 instead of C 2 — kinder to samplers.', 'show_note_nums'],
  ['presend_pgm', 'Pre-send program changes', 'Fire stored programs ahead of the scene switch, for slow groove-boxes.', 'presend_pgm'],
];

// ---------------------------------------------------------------- helpers

function noteName(id) {
  const oct = Math.floor(id / 12);
  return NOTES[((id % 12) + 12) % 12] + (oct === 10 ? 'X' : oct);
}
function controlLabel(kebab) {
  const found = CONTROLS.find(c => c[0] === kebab);
  return found ? found[1] : kebab;
}
function cur() { return state.library.instruments[state.selected] || null; }
function meta(name) {
  if (!state.sidecar[name]) state.sidecar[name] = { notes: '', cc_meta: {} };
  if (!state.sidecar[name].cc_meta) state.sidecar[name].cc_meta = {};
  return state.sidecar[name];
}
function esc(s) {
  return String(s ?? '').replace(/[&<>"']/g, c =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}
function el(html) {
  const t = document.createElement('template');
  t.innerHTML = html.trim();
  return t.content.firstChild;
}
function toast(msg) {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.classList.add('show');
  clearTimeout(t._h);
  t._h = setTimeout(() => t.classList.remove('show'), 2600);
}
function slotDesc(tv) {
  if (tv.kind === 'MidiCc') return tv.label || ('cc' + tv.cc);
  return controlLabel(tv.control);
}

// ---------------------------------------------------------------- rendering

function renderAll() {
  renderRail();
  const has = state.library.instruments.length > 0;
  document.getElementById('welcome').style.display = has ? 'none' : '';
  document.getElementById('editor').style.display = has ? 'flex' : 'none';
  document.getElementById('crumb').textContent = state.path || 'unsaved library';
  if (has) { renderHead(); renderTabs(); renderPanel(); }
}

function renderRail() {
  const list = document.getElementById('railList');
  list.innerHTML = '';
  state.library.instruments.forEach((inst, i) => {
    if (state.filter && !inst.name.toLowerCase().includes(state.filter)) return;
    const btn = el(`<button class="inst${i === state.selected ? ' sel' : ''}">
      <span class="nm">${esc(inst.name)}</span>
      <span class="pc">${inst.midi_port}.${inst.midi_channel}</span></button>`);
    btn.onclick = () => { state.selected = i; renderAll(); };
    list.appendChild(btn);
  });
}

function renderHead() {
  const inst = cur();
  document.getElementById('instName').textContent = inst.name;
  const n = inst.name.length;
  document.getElementById('nameBudget').innerHTML =
    `name <b class="${n > 9 ? 'over' : ''}">${n} / 9</b>`;
  document.getElementById('instSub').textContent =
    `${PORTS[inst.midi_port - 1] || 'port ' + inst.midi_port} · ch ${inst.midi_channel} · ${inst.default_pattern} default`;
}

function renderTabs() {
  const inst = cur();
  const tabs = document.getElementById('tabs');
  tabs.innerHTML = '';
  const defs = [
    ['setup', 'Setup', ''],
    ['cc', 'CC Map', Object.keys(inst.cc_defs).length || ''],
    ['tv', 'Track Values', Object.keys(inst.track_values).length ? `${Object.keys(inst.track_values).length}/180` : ''],
    ['rows', 'Note Rows', Object.keys(inst.note_rows).length || ''],
  ];
  for (const [id, label, cnt] of defs) {
    const b = el(`<button class="tab${state.tab === id ? ' on' : ''}">${label}${cnt ? `<span class="cnt">${cnt}</span>` : ''}</button>`);
    b.onclick = () => { state.tab = id; renderPanel(); renderTabs(); };
    tabs.appendChild(b);
  }
}

function renderPanel() {
  const p = document.getElementById('panel');
  p.innerHTML = '';
  ({ setup: renderSetup, cc: renderCcMap, tv: renderTrackValues, rows: renderNoteRows })[state.tab](p);
}

// ---- setup

function renderSetup(p) {
  const inst = cur();
  const grid = el('<div class="fgrid"></div>');

  const identity = el(`<div class="fgroup"><h3>Identity</h3></div>`);
  const nameRow = el(`<div class="frow"><label>Name</label><input maxlength="9" value="${esc(inst.name)}"><span class="hint">9 characters max</span></div>`);
  nameRow.querySelector('input').onchange = e => {
    const oldName = inst.name;
    const newName = e.target.value.trim() || oldName;
    if (newName !== oldName) {
      if (state.library.instruments.some(o => o !== inst && o.name === newName)) {
        toast(`An instrument named “${newName}” already exists`);
        e.target.value = oldName;
        return;
      }
      inst.name = newName;
      if (state.sidecar[oldName]) {
        state.sidecar[newName] = state.sidecar[oldName];
        delete state.sidecar[oldName];
      }
    }
    renderAll();
  };
  identity.appendChild(nameRow);

  const noteOpts = ['<option value="">off — follow scene root</option>'];
  for (let id = 0; id <= 127; id++) noteOpts.push(`<option value="${id}">${noteName(id)}</option>`);
  const noteRow = el(`<div class="frow"><label>Default note</label><select>${noteOpts.join('')}</select></div>`);
  noteRow.querySelector('select').value = inst.default_note ?? '';
  noteRow.querySelector('select').onchange = e => { inst.default_note = e.target.value === '' ? null : +e.target.value; };
  identity.appendChild(noteRow);

  const patRow = el(`<div class="frow"><label>Default pattern</label><select>
    <option value="P3">P3 — step</option><option value="CK">CK — realtime</option>
    <option value="Sel">Sel — ask each time</option></select></div>`);
  patRow.querySelector('select').value = inst.default_pattern;
  patRow.querySelector('select').onchange = e => { inst.default_pattern = e.target.value; renderHead(); };
  identity.appendChild(patRow);
  grid.appendChild(identity);

  const routing = el(`<div class="fgroup"><h3>Routing</h3></div>`);
  const portRow = el(`<div class="frow"><label>Port</label><select>${PORTS.map((n, i) => `<option value="${i + 1}">${n}</option>`).join('')}</select></div>`);
  portRow.querySelector('select').value = inst.midi_port;
  portRow.querySelector('select').onchange = e => { inst.midi_port = +e.target.value; renderHead(); renderRail(); };
  routing.appendChild(portRow);

  const chanRow = el(`<div class="frow"><label>Channel</label><select>${Array.from({ length: 16 }, (_, i) => `<option>${i + 1}</option>`).join('')}</select></div>`);
  chanRow.querySelector('select').value = inst.midi_channel;
  chanRow.querySelector('select').onchange = e => { inst.midi_channel = +e.target.value; renderHead(); renderRail(); };
  routing.appendChild(chanRow);

  const spreadRow = el(`<div class="frow"><label>Poly spread</label><select>
    <option value="0">off</option>${Array.from({ length: 15 }, (_, i) => `<option value="${i + 2}">${i + 2} voices</option>`).join('')}
    </select><span class="hint">spreads up from the base channel</span></div>`);
  spreadRow.querySelector('select').value = inst.poly_spread;
  spreadRow.querySelector('select').onchange = e => { inst.poly_spread = +e.target.value; };
  routing.appendChild(spreadRow);
  grid.appendChild(routing);

  const behaviour = el(`<div class="fgroup wide"><h3>Behaviour</h3></div>`);
  for (const [field, label, why, key] of FLAGS) {
    const row = el(`<div class="flagrow"><input type="checkbox"${inst[field] ? ' checked' : ''}>
      <span class="ft"><b>${label}</b><span class="why">${why}</span></span>
      <span class="key">${key}</span></div>`);
    row.querySelector('input').onchange = e => { inst[field] = e.target.checked; };
    behaviour.appendChild(row);
  }
  grid.appendChild(behaviour);

  const notesGroup = el(`<div class="fgroup wide"><h3>Instrument notes <span style="text-transform:none;letter-spacing:0">· kept in the .ckix sidecar, never sent to hardware</span></h3></div>`);
  const notesArea = el(`<textarea style="width:100%;min-height:56px;font-family:var(--f-body)" placeholder="Patch sheet, wiring, anything worth remembering…"></textarea>`);
  notesArea.value = meta(inst.name).notes || '';
  notesArea.onchange = e => { meta(inst.name).notes = e.target.value; };
  notesGroup.appendChild(notesArea);
  grid.appendChild(notesGroup);

  p.appendChild(grid);
}

// ---- cc map

function renderCcMap(p) {
  const inst = cur();
  const bar = el(`<div class="toolbar">
    <input id="ccFilter" placeholder="Filter by name, number, label…" spellcheck="false">
    <button id="btnChart">Paste from chart…</button>
    <button id="btnAddCc">+ Add CC</button></div>`);
  p.appendChild(bar);
  bar.querySelector('#btnChart').onclick = openChartModal;
  bar.querySelector('#btnAddCc').onclick = () => {
    let cc = 0;
    while (cc <= 127 && inst.cc_defs[cc]) cc++;
    if (cc > 127) { toast('All 128 CCs are already defined.'); return; }
    inst.cc_defs[cc] = { cc, label: '', min: 0, max: 127, start: 0 };
    renderTabs(); renderPanel();
  };

  const table = el(`<table class="cc"><thead><tr>
    <th>CC</th><th>Full name</th><th>Label</th><th>Min</th><th>Max</th><th>Start</th><th>Notes</th><th></th>
    </tr></thead><tbody></tbody></table>`);
  const tbody = table.querySelector('tbody');

  const render = filter => {
    tbody.innerHTML = '';
    const ccMeta = meta(inst.name).cc_meta;
    const keys = Object.keys(inst.cc_defs).map(Number).sort((a, b) => a - b);
    for (const cc of keys) {
      const def = inst.cc_defs[cc];
      const m = ccMeta[cc] || {};
      const hay = `${cc} ${m.name || ''} ${def.label}`.toLowerCase();
      if (filter && !hay.includes(filter)) continue;

      const tr = el(`<tr>
        <td class="ccnum">${cc}</td>
        <td><input class="full" value="${esc(m.name || '')}" placeholder="full name" spellcheck="false"></td>
        <td><input class="lbl" maxlength="6" value="${esc(def.label)}" spellcheck="false"></td>
        <td><input class="num" value="${def.min}"></td>
        <td><input class="num" value="${def.max}"></td>
        <td><input class="num" value="${def.start}"></td>
        <td><input class="desc" value="${esc(m.desc || '')}" placeholder="notes" spellcheck="false"></td>
        <td><button class="quiet danger">×</button></td></tr>`);

      const [full, lbl, min, max, start, desc] = tr.querySelectorAll('input');
      full.onchange = async e => {
        const cm = ccMeta[cc] || (ccMeta[cc] = {});
        cm.name = e.target.value;
        if (!def.label && invoke && e.target.value) {
          def.label = await invoke('suggest_label', { name: e.target.value });
          lbl.value = def.label;
        }
      };
      lbl.onchange = e => { def.label = e.target.value; };
      const num = (input, apply) => { input.onchange = e => {
        const v = parseInt(e.target.value, 10);
        if (!Number.isNaN(v)) apply(Math.max(0, Math.min(127, v)));
        //cross-clamps can move any of the three - re-sync them all
        min.value = def.min; max.value = def.max; start.value = def.start;
      }; };
      num(min, v => { def.min = Math.min(v, def.max); def.start = Math.max(def.start, def.min); });
      num(max, v => { def.max = Math.max(v, def.min); def.start = Math.min(def.start, def.max); });
      num(start, v => { def.start = Math.max(def.min, Math.min(def.max, v)); });
      desc.onchange = e => { (ccMeta[cc] || (ccMeta[cc] = {})).desc = e.target.value; };
      tr.querySelector('button').onclick = () => { delete inst.cc_defs[cc]; renderTabs(); renderPanel(); };
      tbody.appendChild(tr);
    }
  };

  bar.querySelector('#ccFilter').oninput = e => render(e.target.value.toLowerCase());
  render('');
  p.appendChild(table);
  if (!Object.keys(inst.cc_defs).length) {
    p.appendChild(el(`<div class="empty-state"><h2>No CCs yet</h2>
      <p>Paste rows straight from the synth's MIDI implementation chart —
      labels are abbreviated to the hardware's six characters for you.</p></div>`));
  }
}

// ---- track values

function renderTrackValues(p) {
  const inst = cur();
  const slots = inst.track_values;
  const used = Object.keys(slots).map(Number);
  const maxRow = used.length ? Math.ceil(Math.max(...used) / 6) : 0;
  const showRows = Math.min(maxRow + 1, 30);

  p.appendChild(el(`<div class="tv-head"><span class="hint" style="color:var(--faint);font-size:12px">
    Rows of six — the same six slots above six encoders the Cirklon shows.
    Drag a slot onto another to move or swap; click an empty slot to fill it.</span></div>`));

  for (let row = 0; row < showRows; row++) {
    const rowEl = el(`<div class="tv-row"><div class="tv-row-head">Row ${row + 1}</div><div class="slotrow"></div></div>`);
    const slotRow = rowEl.querySelector('.slotrow');
    for (let col = 0; col < 6; col++) {
      const slot = row * 6 + col + 1;
      const tv = slots[slot];
      let cell;
      if (tv) {
        cell = el(`<div class="slot${tv.kind === 'Control' ? ' tc' : ''}" draggable="true">
          <div class="scc">${tv.kind === 'MidiCc' ? 'CC ' + tv.cc : 'track'}</div>
          <div class="slb">${esc(slotDesc(tv))}</div>
          <button class="x" title="Clear slot">×</button></div>`);
        cell.querySelector('.x').onclick = e => { e.stopPropagation(); delete slots[slot]; renderTabs(); renderPanel(); };
        cell.ondragstart = e => e.dataTransfer.setData('text/plain', String(slot));
      } else {
        cell = el(`<div class="slot empty">+</div>`);
        cell.onclick = () => openSlotPicker(slot);
      }
      cell.ondragover = e => { e.preventDefault(); cell.classList.add('dragover'); };
      cell.ondragleave = () => cell.classList.remove('dragover');
      cell.ondrop = e => {
        e.preventDefault();
        const from = +e.dataTransfer.getData('text/plain');
        if (!from || from === slot || !slots[from]) return;
        const tmp = slots[slot];
        slots[slot] = slots[from];
        if (tmp) slots[from] = tmp; else delete slots[from];
        renderPanel();
      };
      slotRow.appendChild(cell);
    }
    p.appendChild(rowEl);
  }
}

function openSlotPicker(slot) {
  const inst = cur();
  const ccOptions = Object.keys(inst.cc_defs).map(Number).sort((a, b) => a - b)
    .map(cc => `<option value="cc:${cc}">CC ${cc} — ${esc(inst.cc_defs[cc].label || 'unlabeled')}</option>`);
  const tcOptions = CONTROLS.map(([id, label]) => `<option value="tc:${id}">${label}</option>`);
  const m = openModal(`Fill slot ${slot}`, `
    <div class="frow"><label>Value</label><select id="pickVal">
      <optgroup label="Track controls">${tcOptions.join('')}</optgroup>
      ${ccOptions.length ? `<optgroup label="CC defs">${ccOptions.join('')}</optgroup>` : ''}
    </select></div>
    <p class="hint" style="color:var(--faint)">CCs come from the CC Map — define them there first, labels ride along.</p>`,
    [['Add', 'primary', () => {
      const v = m.querySelector('#pickVal').value;
      if (v.startsWith('tc:')) {
        inst.track_values[slot] = { kind: 'Control', control: v.slice(3) };
      } else {
        const cc = +v.slice(3);
        inst.track_values[slot] = { kind: 'MidiCc', cc, label: inst.cc_defs[cc]?.label || null };
      }
      closeModal(); renderTabs(); renderPanel();
    }]]);
}

// ---- note rows

function renderNoteRows(p) {
  const inst = cur();
  const keys = Object.keys(inst.note_rows).map(Number).sort((a, b) => a - b);

  if (keys.length) {
    const grid = el('<div class="nr-grid"></div>');
    for (const id of keys) {
      const row = inst.note_rows[id];
      const cell = el(`<div class="nr"><div class="dn">${noteName(id)}</div>
        <div class="dl">${esc(row.label || '—')}</div>
        <button class="pin${row.always_show ? ' on' : ''}" title="Always show in empty patterns">PIN</button>
        <button class="x" title="Remove">×</button></div>`);
      cell.querySelector('.pin').onclick = () => { row.always_show = !row.always_show; renderPanel(); };
      cell.querySelector('.x').onclick = () => { delete inst.note_rows[id]; renderTabs(); renderPanel(); };
      //inline editor - wry's WKWebView has no window.prompt()
      cell.querySelector('.dl').onclick = () => {
        const dl = cell.querySelector('.dl');
        if (!dl) return;
        const input = el(`<input maxlength="6" style="width:76px" value="${esc(row.label)}" spellcheck="false">`);
        dl.replaceWith(input);
        input.focus();
        input.select();
        let done = false;
        const commit = keep => {
          if (done) return;
          done = true;
          if (keep) row.label = input.value;
          renderPanel();
        };
        input.onkeydown = ev => {
          ev.stopPropagation();
          if (ev.key === 'Enter') commit(true);
          if (ev.key === 'Escape') commit(false);
        };
        input.onblur = () => commit(true);
      };
      grid.appendChild(cell);
    }
    p.appendChild(grid);
  } else {
    p.appendChild(el(`<div class="empty-state"><h2>No note rows</h2>
      <p>Note rows name the sounds a drum machine maps to keys and pin favourites
      to the drum grid. Melodic synths usually don't need them.</p></div>`));
  }

  const noteOpts = [];
  for (let id = 0; id <= 127; id++) noteOpts.push(`<option value="${id}">${noteName(id)}</option>`);
  const add = el(`<div class="nr-add"><select id="nrNote">${noteOpts.join('')}</select>
    <input id="nrLabel" maxlength="6" placeholder="label" style="width:90px" spellcheck="false">
    <button id="nrAdd">+ Add row</button></div>`);
  add.querySelector('#nrNote').value = keys.length ? Math.min(Math.max(...keys) + 1, 127) : 36;
  add.querySelector('#nrAdd').onclick = () => {
    const id = +add.querySelector('#nrNote').value;
    inst.note_rows[id] = { note_id: id, label: add.querySelector('#nrLabel').value, always_show: false };
    renderTabs(); renderPanel();
  };
  p.appendChild(add);
}

// ---------------------------------------------------------------- modals

function openModal(title, bodyHtml, buttons, wide) {
  const overlay = el(`<div class="overlay"><div class="modal"${wide ? ' style="width:760px"' : ''}>
    <div class="modal-head"><h2>${esc(title)}</h2><button class="x">×</button></div>
    <div class="modal-body">${bodyHtml}</div>
    <div class="modal-foot"><span class="status"></span><span class="spacer"></span></div>
  </div></div>`);
  overlay.querySelector('.x').onclick = closeModal;
  overlay.onclick = e => { if (e.target === overlay) closeModal(); };
  const foot = overlay.querySelector('.modal-foot');
  for (const [label, cls, fn] of buttons || []) {
    const b = el(`<button class="${cls || ''}">${esc(label)}</button>`);
    b.onclick = fn;
    foot.appendChild(b);
  }
  document.getElementById('modals').appendChild(overlay);
  return overlay;
}
//modals stack: closing removes only the topmost, so Preview can sit over Prepare
function closeModal() {
  const modals = document.getElementById('modals');
  if (modals.lastElementChild) modals.lastElementChild.remove();
}

// ---- chart import

function openChartModal() {
  const m = openModal('Paste from chart',
    `<p class="hint" style="color:var(--dim);margin-top:0">One CC per line:
     <span class="mono">&lt;cc&gt; &lt;full name&gt; [min-max] [start]</span> — or table rows
     <span class="mono">cc | name | range | start</span> (tabs from a spreadsheet work too).</p>
     <p class="hint" style="color:var(--faint);margin-top:0">
     The number leads: <span class="mono">19</span>, <span class="mono">74:</span>,
     <span class="mono">CC 5</span>, <span class="mono">cc# 9</span>.
     Ranges: <span class="mono">0-127</span>, <span class="mono">0..127</span>, <span class="mono">(0-3)</span>;
     a start value may follow a range: <span class="mono">(0-3) 3</span>.
     Lines without a leading CC number are skipped — paste the whole manual page if you like.
     Full names go to the sidecar; six-character labels are abbreviated for you.</p>
     <textarea class="chart" spellcheck="false" placeholder="19 Filter Cutoff 0-127&#10;109 Filter Pole Select (0-3) 3&#10;74: Cutoff&#10;CC 21 | Filter Resonance | 0-127 | 64"></textarea>`,
    [['Import', 'primary', async () => {
      const text = m.querySelector('textarea').value;
      const entries = await invoke('parse_chart', { text });
      if (!entries.length) { toast('No CC lines found — rows need a leading CC number.'); return; }
      const inst = cur();
      const ccMeta = meta(inst.name).cc_meta;
      let added = 0, updated = 0;
      const clamp = v => Math.max(0, Math.min(127, v));
      for (const e of entries) {
        const isNew = !inst.cc_defs[e.cc];
        const def = inst.cc_defs[e.cc] || (inst.cc_defs[e.cc] = { cc: e.cc, label: '', min: 0, max: 127, start: 0 });
        isNew ? added++ : updated++;
        if (e.name && (isNew || !def.label)) def.label = await invoke('suggest_label', { name: e.name });
        if (e.min != null && e.max != null) {
          def.min = clamp(Math.min(e.min, e.max));
          def.max = clamp(Math.max(e.min, e.max));
        } else if (e.min != null) {
          def.min = Math.min(clamp(e.min), def.max);
        } else if (e.max != null) {
          def.max = Math.max(clamp(e.max), def.min);
        }
        def.start = Math.max(def.min, Math.min(def.max, e.start != null ? e.start : def.start));
        if (e.name) (ccMeta[e.cc] || (ccMeta[e.cc] = {})).name = e.name;
      }
      closeModal();
      state.tab = 'cc';
      renderAll();
      toast(updated ? `Added ${added} CCs, updated ${updated} from chart` : `Added ${added} CC${added === 1 ? '' : 's'} from chart`);
    }]]);
}

// ---- prepare / preflight / export

async function openPrepare() {
  if (!state.library.instruments.length) return;
  let scopeAll = true;

  const m = openModal('Prepare for Cirklon', '<div id="prepBody"></div>', [
    ['Preview', '', () => openPreview()],
    ['Apply all fixes', '', async () => {
      const fixes = (m._findings || []).filter(f => f.fix).map(f => f.fix);
      if (fixes.length) {
        state.library = await invoke('apply_fixes', { library: state.library, fixes });
        renderAll(); await refresh();
      }
    }],
    ['Export', 'primary', () => doExport()],
  ], true);

  const relevantErrors = findings => findings.filter(f =>
    f.severity === 'error' && (scopeAll || !f.instrument || f.instrument === cur().name)).length;

  async function refresh() {
    const findings = await invoke('validate_library', { library: state.library });
    m._findings = findings;
    const body = m.querySelector('#prepBody');
    body.innerHTML = '';

    if (!findings.length) {
      body.appendChild(el(`<div class="check fixed"><span class="sig"></span>
        <span class="cb"><span class="h">Everything passes.</span><br>
        <span class="d">Nothing here the hardware would trip over.</span></span></div>`));
    }
    findings.forEach((f, i) => {
      const row = el(`<div class="check ${f.severity}"><span class="sig"></span>
        <span class="cb"><span class="h">${f.instrument ? esc(f.instrument) + ' — ' : ''}${esc(f.title)}</span><br>
        <span class="d">${esc(f.detail)}</span></span></div>`);
      if (f.fix) {
        const b = el(`<button>${esc(f.fix_label || 'Fix')}</button>`);
        b.onclick = async () => {
          state.library = await invoke('apply_fixes', { library: state.library, fixes: [f.fix] });
          renderAll(); await refresh();
        };
        row.appendChild(b);
      }
      body.appendChild(row);
    });

    if (state.library.instruments.length > 1) {
      const scope = el(`<div class="scope"><span class="lb">Export:</span>
        <button class="${scopeAll ? 'on' : ''}" id="scAll">All ${state.library.instruments.length} instruments</button>
        <button class="${scopeAll ? '' : 'on'}" id="scOne">Only “${esc(cur().name)}”</button></div>`);
      scope.querySelector('#scAll').onclick = () => { scopeAll = true; refresh(); };
      scope.querySelector('#scOne').onclick = () => { scopeAll = false; refresh(); };
      body.appendChild(scope);
    }

    const errs = relevantErrors(findings);
    const status = m.querySelector('.status');
    status.textContent = errs ? `${errs} error${errs > 1 ? 's' : ''} to fix before export` : 'Ready to export';
    status.className = 'status ' + (errs ? 'err' : 'ok');
    const exportBtn = [...m.querySelectorAll('.modal-foot button')].find(b => b.textContent === 'Export');
    exportBtn.disabled = errs > 0;
  }

  async function doExport() {
    const scoped = scopeAll ? state.library : { instruments: [cur()] };
    const defaultName = (scopeAll && scoped.instruments.length > 1 ? 'LIBRARY' : cur().name.replace(/\s+/g, '').toUpperCase()) + '.CKI';
    const path = await dlg.save({
      title: 'Export CKI',
      defaultPath: defaultName,
      filters: [{ name: 'Cirklon instruments', extensions: ['CKI', 'cki'] }],
    });
    if (!path) return;
    let result;
    try {
      result = await invoke('save_library', { path, library: scoped, sidecar: state.sidecar });
    } catch (err) {
      toast(String(err));
      return; //modal stays open so the user can retry another path
    }
    closeModal();
    if (scopeAll) state.path = result.cki_path;
    renderAll();
    toast(`Exported ${scoped.instruments.length} instrument${scoped.instruments.length === 1 ? '' : 's'}` +
      (result.ckix_path ? ' + sidecar notes' : ''));
  }

  await refresh();
}

// ---- hardware preview

function openPreview() {
  const inst = cur();
  const rows = [];
  for (let row = 0; row < 30; row++) {
    const cells = [];
    let any = false;
    for (let col = 0; col < 6; col++) {
      const tv = inst.track_values[row * 6 + col + 1];
      if (tv) { any = true; cells.push(slotDesc(tv).slice(0, 6)); } else cells.push(null);
    }
    if (any) rows.push({ hw: row + 1, cells });
  }
  let idx = 0;

  const m = openModal('On-hardware preview', `
    <div class="hw"><div class="hw-head"><span>TRACK · <b>${esc(inst.name)}</b></span><span id="hwInd"></span></div>
    <div class="hw-row" id="hwRow"></div>
    <div class="hw-nav"><button id="hwPrev">&lt; row</button><button id="hwNext">row &gt;</button>
    <span class="rowind">turn the ROW encoder on hardware</span></div></div>
    <p class="hint" style="color:var(--faint)">Character-true: six labels above six encoders, populated rows only — exactly what the Cirklon draws.</p>`,
    [], true);

  const render = () => {
    const rowEl = m.querySelector('#hwRow');
    rowEl.innerHTML = '';
    const row = rows[idx];
    m.querySelector('#hwInd').textContent = row ? `row ${idx + 1} / ${rows.length} (hw row ${row.hw})` : 'no track values yet';
    (row ? row.cells : Array(6).fill(null)).forEach(c => {
      rowEl.appendChild(el(c
        ? `<div class="hw-slot"><div class="l">${esc(c)}</div><div class="v">— — —</div></div>`
        : `<div class="hw-slot blank"><div class="l">······</div><div class="v">&nbsp;</div></div>`));
    });
  };
  m.querySelector('#hwPrev').onclick = () => { if (rows.length > 1) { idx = (idx - 1 + rows.length) % rows.length; render(); } };
  m.querySelector('#hwNext').onclick = () => { if (rows.length > 1) { idx = (idx + 1) % rows.length; render(); } };
  render();
}

// ---------------------------------------------------------------- actions

async function importFile() {
  const path = await dlg.open({
    title: 'Import CKI',
    multiple: false,
    filters: [{ name: 'Cirklon instruments', extensions: ['CKI', 'cki'] }],
  });
  if (!path) return;
  try {
    const result = await invoke('load_library', { path });
    if (!result.library.instruments.length) {
      toast('No instruments found in that file.');
      return;
    }
    const before = state.library.instruments.length;
    state.library.instruments.push(...result.library.instruments);
    //field-level sidecar merge, mirroring core ckix::merge - never blanks existing data
    for (const [name, incoming] of Object.entries(result.sidecar)) {
      const target = meta(name);
      if (incoming.notes) target.notes = incoming.notes;
      for (const [cc, cm] of Object.entries(incoming.cc_meta || {})) {
        const tc = target.cc_meta[cc] || (target.cc_meta[cc] = {});
        if (cm.name) tc.name = cm.name;
        if (cm.desc) tc.desc = cm.desc;
        if (cm.group) tc.group = cm.group;
      }
    }
    if (!state.path) state.path = result.path;
    state.selected = before;
    state.tab = 'setup';
    renderAll();
    toast(`Imported ${result.library.instruments.length} instrument${result.library.instruments.length === 1 ? '' : 's'}`);
  } catch (err) {
    toast(String(err));
  }
}

function newInstrument() {
  let name = 'New inst';
  let n = 2;
  while (state.library.instruments.some(i => i.name === name)) name = `New in ${n++}`;
  state.library.instruments.push({
    name, midi_port: 1, midi_channel: 1, default_note: 36, default_pattern: 'Sel',
    multi: false, poly_spread: 0, no_xpose: false, no_fts: false, no_thru: false,
    no_bank_m: false, no_bank_l: false, show_note_nums: false, presend_pgm: false,
    track_values: {}, cc_defs: {}, note_rows: {},
  });
  state.selected = state.library.instruments.length - 1;
  state.tab = 'setup';
  renderAll();
}

// ---------------------------------------------------------------- boot

document.getElementById('btnImport').onclick = importFile;
document.getElementById('btnImport2').onclick = importFile;
document.getElementById('btnNew').onclick = newInstrument;
document.getElementById('btnPrepare').onclick = openPrepare;
document.getElementById('railSearch').oninput = e => { state.filter = e.target.value.toLowerCase(); renderRail(); };
document.addEventListener('keydown', e => {
  if (e.key !== 'Escape') return;
  //never wipe typed chart text - a non-empty textarea needs the × button
  const modals = document.getElementById('modals');
  const top = modals.lastElementChild;
  if (!top) return;
  const textarea = top.querySelector('textarea');
  if (textarea && textarea.value.trim()) return;
  closeModal();
});

//surface any missed IPC rejection instead of dying silently in the console
window.addEventListener('unhandledrejection', e => toast(String(e.reason)));

renderAll();
