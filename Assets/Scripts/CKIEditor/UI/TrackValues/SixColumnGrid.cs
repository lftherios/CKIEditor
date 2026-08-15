using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.TrackValues
{
    /// <summary>
    /// Lays track-value slots out in rows of exactly six - the same six slots the
    /// Cirklon shows above its six encoders. Cell width follows the container so
    /// a row always spans the panel; row height is fixed.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class SixColumnGrid : MonoBehaviour
    {
        public int Columns = 6;
        public float RowHeight = 44;
        public float Spacing = 6;

        private GridLayoutGroup _grid;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_grid != null && isActiveAndEnabled)
                Apply();
        }

        private void Apply()
        {
            var width = ((RectTransform)transform).rect.width;
            if (width <= 0)
                return;

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = Columns;
            _grid.spacing = new Vector2(Spacing, Spacing);

            var cellWidth = (width - _grid.padding.horizontal - Spacing * (Columns - 1)) / Columns;
            _grid.cellSize = new Vector2(Mathf.Max(cellWidth, 40), RowHeight);
        }
    }
}
