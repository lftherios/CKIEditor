using System;
using System.Collections.Generic;
using System.Linq;
using CKIEditor.Model.Defs;
using CKIEditor.Validation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.Preflight
{
    /// <summary>
    /// The "Prepare for Cirklon" step: findings with one-click fixes, a
    /// hardware preview, and an export-scope choice. Errors block export -
    /// but only errors in what's actually being exported. Built entirely in
    /// code so it needs no prefab - styled from CkiTheme to match the design.
    /// </summary>
    public static class PreflightDialog
    {
        private const int PANEL_WIDTH = 620;
        private const int MAX_VISIBLE_FINDINGS = 8;

        /// <param name="onExport">receives true to export the whole library, false for just the edited instrument</param>
        public static void Show(List<ValidationFinding> findings, string summary,
            InstrumentDef editedInstrument, int instrumentCount,
            Action<bool> onExport, Action onCancel)
        {
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("PreflightDialog: no Canvas in scene - exporting without preflight UI.");
                onExport?.Invoke(true);
                return;
            }

            var overlay = CreateOverlay(canvas.transform);
            var panel = CreatePanel(overlay.transform);

            CreateText(panel.transform, "PREPARE FOR CIRKLON", 15, CkiTheme.Accent, FontStyles.Bold, 0.12f);
            CreateText(panel.transform,
                findings.Count > 0
                    ? "Preflight found things worth a look before this ships to hardware."
                    : "Preflight passed — nothing the hardware would trip over.",
                13, CkiTheme.Dim);

            var rows = new List<FindingRow>();
            var shown = findings.Take(MAX_VISIBLE_FINDINGS).ToList();
            foreach (var finding in shown)
                rows.Add(CreateFindingRow(panel.transform, finding));

            if (findings.Count > shown.Count)
                CreateText(panel.transform, $"…and {findings.Count - shown.Count} more.", 12, CkiTheme.Faint);

            if (findings.Count == 0)
                CreatePassRow(panel.transform);

            TextMeshProUGUI summaryText = null;
            if (!string.IsNullOrEmpty(summary))
                summaryText = CreateText(panel.transform, summary, 12, CkiTheme.Faint);

            //export scope - only a choice when there is more than one instrument.
            //statusText/exportButton are declared here (not at the footer) so the
            //scope-button lambdas referencing Refresh() see definitely-assigned locals
            var exportAll = true;
            Button allButton = null, currentButton = null;
            TextMeshProUGUI statusText = null;
            Button exportButton = null;
            var hasScopeChoice = instrumentCount > 1 && editedInstrument != null;

            if (hasScopeChoice)
            {
                var scopeRow = CreateRow(panel.transform, 8);
                var scopeLabel = CreateText(scopeRow.transform, "Export:", 12.5f, CkiTheme.Dim);
                scopeLabel.GetComponent<LayoutElement>().minWidth = 50;
                allButton = CreateButton(scopeRow.transform, $"All {instrumentCount} instruments", false,
                    () => { exportAll = true; Refresh(); });
                currentButton = CreateButton(scopeRow.transform, $"Only “{editedInstrument.Name}”", false,
                    () => { exportAll = false; Refresh(); });
            }

            //footer
            var footer = CreateRow(panel.transform, 10);
            statusText = CreateText(footer.transform, "", 12.5f, CkiTheme.Dim);
            statusText.GetComponent<LayoutElement>().flexibleWidth = 1;

            //errors gate export, but only for instruments inside the chosen scope
            int RelevantErrors()
            {
                return findings.Count(f => f.Severity == FindingSeverity.Error && !f.IsFixed
                                           && (exportAll || string.IsNullOrEmpty(f.InstrumentName)
                                               || f.InstrumentName == editedInstrument.Name));
            }

            void Refresh()
            {
                var errors = RelevantErrors();
                statusText.text = errors > 0
                    ? $"{errors} error{(errors > 1 ? "s" : "")} to fix before export"
                    : "Ready to export";
                statusText.color = errors > 0 ? CkiTheme.Error : CkiTheme.Ok;
                SetButtonEnabled(exportButton, errors == 0);

                if (hasScopeChoice)
                {
                    StyleScopeButton(allButton, exportAll);
                    StyleScopeButton(currentButton, !exportAll);

                    if (summaryText != null)
                        summaryText.text = exportAll
                            ? summary
                            : $"Exporting only “{editedInstrument.Name}” · {editedInstrument.CcDefs.Count} CC defs";
                }

                foreach (var row in rows)
                    row.Refresh();
            }

            void Close()
            {
                UnityEngine.Object.Destroy(overlay);
            }

            CreateButton(footer.transform, "Cancel", false, () =>
            {
                Close();
                onCancel?.Invoke();
            });

            if (editedInstrument != null)
            {
                CreateButton(footer.transform, "Preview", false,
                    () => HardwarePreviewDialog.Show(editedInstrument));
            }

            if (findings.Any(f => f.CanFix))
            {
                CreateButton(footer.transform, "Apply all fixes", false, () =>
                {
                    foreach (var finding in findings)
                        finding.ApplyFix();
                    Refresh();
                });
            }

            exportButton = CreateButton(footer.transform, "Export", true, () =>
            {
                Close();
                onExport?.Invoke(exportAll);
            });

            foreach (var row in rows)
                row.FixApplied = Refresh;

            Refresh();
        }

        // ------------------------------------------------------------ building blocks

        private class FindingRow
        {
            public ValidationFinding Finding;
            public Image Signal;
            public TextMeshProUGUI Detail;
            public GameObject FixButton;
            public Action FixApplied;

            public void Refresh()
            {
                if (!Finding.IsFixed)
                    return;

                Signal.color = CkiTheme.Ok;
                Detail.text = "Fixed.";
                if (FixButton != null)
                    FixButton.SetActive(false);
            }
        }

        private static void StyleScopeButton(Button button, bool selected)
        {
            if (button == null)
                return;

            button.GetComponent<Image>().color = selected ? CkiTheme.Accent : CkiTheme.Panel2;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.color = selected ? CkiTheme.AccentInk : CkiTheme.Ink;
        }

        private static void CreatePassRow(Transform parent)
        {
            var row = new GameObject("Pass", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = CkiTheme.Soft(CkiTheme.Ok, 0.10f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 9, 9);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var signal = new GameObject("Signal", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            signal.transform.SetParent(row.transform, false);
            signal.GetComponent<Image>().color = CkiTheme.Ok;
            var signalLayout = signal.GetComponent<LayoutElement>();
            signalLayout.preferredWidth = 8;
            signalLayout.preferredHeight = 8;
            signalLayout.flexibleWidth = 0;

            var text = CreateText(row.transform, "Everything passes.", 13.5f, CkiTheme.Ink, FontStyles.Bold);
            text.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        private static GameObject CreateOverlay(Transform parent)
        {
            var overlay = new GameObject("PreflightOverlay", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)overlay.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = CkiTheme.Overlay;
            return overlay;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("PreflightPanel", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PANEL_WIDTH, 0);

            panel.GetComponent<Image>().color = CkiTheme.Panel;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 18, 16);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panel;
        }

        private static GameObject CreateRow(Transform parent, int spacing)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return row;
        }

        private static FindingRow CreateFindingRow(Transform parent, ValidationFinding finding)
        {
            var row = new GameObject("Finding", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = CkiTheme.Soft(SeverityColor(finding.Severity), 0.10f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 9, 9);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            //severity signal
            var signal = new GameObject("Signal", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            signal.transform.SetParent(row.transform, false);
            var signalImage = signal.GetComponent<Image>();
            signalImage.color = SeverityColor(finding.Severity);
            var signalLayout = signal.GetComponent<LayoutElement>();
            signalLayout.preferredWidth = 8;
            signalLayout.preferredHeight = 8;
            signalLayout.flexibleWidth = 0;

            //texts
            var textCol = new GameObject("Texts", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textCol.transform.SetParent(row.transform, false);
            var textLayout = textCol.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            textCol.GetComponent<LayoutElement>().flexibleWidth = 1;

            var title = string.IsNullOrEmpty(finding.InstrumentName)
                ? finding.Title
                : $"{finding.InstrumentName} — {finding.Title}";
            CreateText(textCol.transform, title, 13.5f, CkiTheme.Ink, FontStyles.Bold);
            var detail = CreateText(textCol.transform, finding.Detail, 12, CkiTheme.Dim);

            var result = new FindingRow { Finding = finding, Signal = signalImage, Detail = detail };

            if (finding.CanFix)
            {
                var fix = CreateButton(row.transform, finding.FixLabel, false, () =>
                {
                    finding.ApplyFix();
                    result.Refresh();
                    result.FixApplied?.Invoke();
                });
                result.FixButton = fix.gameObject;
            }

            return result;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, float size, Color color,
            FontStyles style = FontStyles.Normal, float characterSpacing = 0)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.characterSpacing = characterSpacing * 100;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, bool primary, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = primary ? CkiTheme.Accent : CkiTheme.Panel2;

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 32;
            layoutElement.minWidth = 90;
            layoutElement.flexibleWidth = 0;

            var text = CreateText(go.transform, label, 13, primary ? CkiTheme.AccentInk : CkiTheme.Ink);
            text.alignment = TextAlignmentOptions.Center;
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14, 0);
            textRect.offsetMax = new Vector2(-14, 0);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            if (button == null)
                return;

            button.interactable = enabled;
            var image = button.GetComponent<Image>();
            var color = image.color;
            color.a = enabled ? 1f : 0.45f;
            image.color = color;
        }

        private static Color SeverityColor(FindingSeverity severity)
        {
            switch (severity)
            {
                case FindingSeverity.Error: return CkiTheme.Error;
                case FindingSeverity.Warning: return CkiTheme.Warning;
                default: return CkiTheme.Info;
            }
        }
    }
}
