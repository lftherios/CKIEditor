using strange.extensions.mediation.impl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.EditSection.CcEditor
{
    public class CreateCcDefView : View
    {
        public TMP_InputField CcInput;
        public TMP_InputField NameInput;
        public TMP_InputField StartInput;
        public TMP_InputField MinInput;
        public TMP_InputField MaxInput;

        public Button SaveButton;

        //documentation row (sidecar .ckix) - built at runtime by cloning the
        //fields above, so the prefab needs no editing
        public TMP_InputField FullNameInput;
        public TMP_InputField NotesInput;
        public Button PasteChartButton;

        private const float ROW_HEIGHT = 40f;

        protected override void Awake()
        {
            if (FullNameInput == null && NameInput != null)
                BuildDocumentationRow();

            base.Awake();
        }

        private void BuildDocumentationRow()
        {
            var row = new GameObject("CcDocsRow", typeof(RectTransform), typeof(LayoutElement));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(transform.parent, false);
            row.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
            row.GetComponent<LayoutElement>().preferredHeight = ROW_HEIGHT;

            FullNameInput = CloneInput(NameInput, row.transform, 0f, 0.55f, "Full name (kept in .ckix)");
            NotesInput = CloneInput(NameInput, row.transform, 0.55f, 0.85f, "Notes");
            PasteChartButton = CloneButton(SaveButton, row.transform, "Paste chart");
        }

        private static TMP_InputField CloneInput(TMP_InputField source, Transform parent,
            float xMin, float xMax, string placeholder)
        {
            var go = Instantiate(source.gameObject, parent);
            go.name = placeholder;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(xMin, 0);
            rect.anchorMax = new Vector2(xMax, 1);
            rect.offsetMin = new Vector2(2.5f, 5);
            rect.offsetMax = new Vector2(-2.5f, -5);

            var input = go.GetComponent<TMP_InputField>();
            input.characterLimit = 0;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.text = "";
            input.onValueChanged.RemoveAllListeners();
            input.onEndEdit.RemoveAllListeners();

            if (input.placeholder is TMP_Text placeholderText)
                placeholderText.text = placeholder;

            return input;
        }

        private static Button CloneButton(Button source, Transform parent, string label)
        {
            var go = Instantiate(source.gameObject, parent);
            go.name = label;

            var button = go.GetComponent<Button>();
            button.onClick.RemoveAllListeners();

            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = label;

            return button;
        }
    }
}
