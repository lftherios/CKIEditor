using System.Collections.Generic;
using System.Linq;
using CKIEditor.Model;
using CKIEditor.Model.Defs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.Preflight
{
    /// <summary>
    /// Character-true preview of the Cirklon's TRACK-values page: six labels
    /// above six encoders, dashes where no value has been sent yet. Shows only
    /// populated rows, like the hardware does. Built entirely in code.
    /// </summary>
    public static class HardwarePreviewDialog
    {
        private const int PANEL_WIDTH = 640;
        private const int SLOTS_PER_ROW = 6;

        //display glass stays dark regardless of theme - hardware is hardware
        private static readonly Color Glass = new Color32(10, 12, 14, 255);
        private static readonly Color Phosphor = new Color32(255, 180, 84, 255);
        private static readonly Color PhosphorDim = new Color32(125, 99, 56, 255);
        private static readonly Color PhosphorFaint = new Color32(58, 50, 32, 255);

        public static void Show(InstrumentDef instrument)
        {
            if (instrument == null)
                return;

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            var rows = CollectPopulatedRows(instrument);

            var overlay = CreateOverlay(canvas.transform);
            var panel = CreatePanel(overlay.transform);

            var header = CreateText(panel.transform, "", 13, PhosphorDim);
            header.characterSpacing = 6;

            var cellTexts = new List<(TextMeshProUGUI label, TextMeshProUGUI value)>();
            var cellRow = CreateRow(panel.transform, 7);
            ((HorizontalLayoutGroup)cellRow.GetComponent<HorizontalLayoutGroup>()).childForceExpandWidth = true;
            for (var i = 0; i < SLOTS_PER_ROW; i++)
                cellTexts.Add(CreateCell(cellRow.transform));

            var rowIndex = 0;

            void Render()
            {
                if (rows.Count == 0)
                {
                    header.text = $"TRACK · {instrument.Name}  —  no track values yet";
                    for (var i = 0; i < SLOTS_PER_ROW; i++)
                    {
                        cellTexts[i].label.text = "······";
                        cellTexts[i].label.color = PhosphorFaint;
                        cellTexts[i].value.text = " ";
                    }
                    return;
                }

                var row = rows[rowIndex];
                header.text = $"TRACK · {instrument.Name}   row {rowIndex + 1} / {rows.Count}  (hw row {row.HardwareRow})";
                for (var i = 0; i < SLOTS_PER_ROW; i++)
                {
                    var slot = row.Slots[i];
                    if (slot == null)
                    {
                        cellTexts[i].label.text = "······";
                        cellTexts[i].label.color = PhosphorFaint;
                        cellTexts[i].value.text = " ";
                    }
                    else
                    {
                        cellTexts[i].label.text = slot;
                        cellTexts[i].label.color = Phosphor;
                        cellTexts[i].value.text = "— — —";
                    }
                }
            }

            //nav + close
            //plain ASCII arrows - the project's TMP font has no triangle glyphs
            var nav = CreateRow(panel.transform, 8);
            CreateButton(nav.transform, "< row", () =>
            {
                if (rows.Count > 1) { rowIndex = (rowIndex - 1 + rows.Count) % rows.Count; Render(); }
            });
            CreateButton(nav.transform, "row >", () =>
            {
                if (rows.Count > 1) { rowIndex = (rowIndex + 1) % rows.Count; Render(); }
            });
            var hint = CreateText(nav.transform, "turn the ROW encoder on hardware", 11, PhosphorDim);
            hint.GetComponent<LayoutElement>().flexibleWidth = 1;
            CreateButton(nav.transform, "Close", () => Object.Destroy(overlay));

            Render();
        }

        private class PreviewRow
        {
            public int HardwareRow;
            public string[] Slots = new string[SLOTS_PER_ROW];
        }

        private static List<PreviewRow> CollectPopulatedRows(InstrumentDef instrument)
        {
            var rows = new List<PreviewRow>();

            for (var row = 0; row < CkiConsts.TRACK_VALUE_ROWS; row++)
            {
                PreviewRow preview = null;
                for (var col = 0; col < SLOTS_PER_ROW; col++)
                {
                    var slotIndex = row * SLOTS_PER_ROW + col + 1;
                    if (!instrument.TrackValues.TryGetValue(slotIndex, out var tv)
                        || tv.Type == TrackValueType.Empty)
                        continue;

                    if (preview == null)
                        preview = new PreviewRow { HardwareRow = row + 1 };

                    preview.Slots[col] = Describe(tv);
                }

                if (preview != null)
                    rows.Add(preview);
            }

            return rows;
        }

        private static string Describe(TrackValueDef tv)
        {
            if (tv.Type == TrackValueType.TrackControl)
                return tv.TrackControl.ToDefString();

            return string.IsNullOrEmpty(tv.Label) ? $"cc{tv.MidiCC}" : tv.Label;
        }

        // ------------------------------------------------------------ building blocks

        private static GameObject CreateOverlay(Transform parent)
        {
            var overlay = new GameObject("HardwarePreviewOverlay", typeof(RectTransform), typeof(Image));
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
            var panel = new GameObject("HardwarePreviewPanel", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PANEL_WIDTH, 0);

            panel.GetComponent<Image>().color = Glass;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 14);
            layout.spacing = 12;
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

        private static (TextMeshProUGUI, TextMeshProUGUI) CreateCell(Transform parent)
        {
            var cell = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Outline),
                typeof(VerticalLayoutGroup), typeof(LayoutElement));
            cell.transform.SetParent(parent, false);
            cell.GetComponent<Image>().color = Glass;

            var outline = cell.GetComponent<Outline>();
            outline.effectColor = PhosphorFaint;
            outline.effectDistance = new Vector2(1, -1);

            var layout = cell.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 8, 8);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            cell.GetComponent<LayoutElement>().flexibleWidth = 1;

            var label = CreateText(cell.transform, "······", 14, Phosphor);
            label.alignment = TextAlignmentOptions.Center;
            var value = CreateText(cell.transform, "— — —", 11, PhosphorDim);
            value.alignment = TextAlignmentOptions.Center;
            return (label, value);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, float size, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = new Color32(28, 31, 36, 255);

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 28;
            layoutElement.minWidth = 74;
            layoutElement.flexibleWidth = 0;

            var text = CreateText(go.transform, label, 12, Phosphor);
            text.alignment = TextAlignmentOptions.Center;
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 0);
            textRect.offsetMax = new Vector2(-8, 0);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
        }
    }
}
