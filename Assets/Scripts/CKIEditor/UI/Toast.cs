using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI
{
    /// <summary>
    /// Minimal bottom-center toast, built in code (no prefab), CkiTheme styled.
    /// Toast.Show("Imported 24 CCs");
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private const float LIFETIME = 2.6f;
        private float _age;

        public static void Show(string message)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.Log($"Toast (no canvas): {message}");
                return;
            }

            var go = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(Toast));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 24);

            go.GetComponent<Image>().color = CkiTheme.Ink;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(go.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18, 8);
            textRect.offsetMax = new Vector2(-18, -8);

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = message;
            text.fontSize = 14;
            text.color = CkiTheme.Ground;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            //size the pill to its message
            var width = text.GetPreferredValues(message, 600, 40).x;
            rect.sizeDelta = new Vector2(Mathf.Min(width, 600) + 36, 36);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= LIFETIME)
                Destroy(gameObject);
        }
    }
}
