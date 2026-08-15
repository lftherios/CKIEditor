using Framewerk.UI.List;
using UnityEngine;
using UnityEngine.UI;

namespace CKIEditor.UI.TrackValues
{
    public class TrackValueListView : ListView
    {
        [Header("Rows of six (Cirklon screen layout)")]
        public bool UseSixColumnGrid = true;
        public float RowHeight = 44;

        protected override void Awake()
        {
            if (UseSixColumnGrid && ContentsParent != null)
                InstallGrid();

            base.Awake();
        }

        //convert whatever layout the prefab carries into rows of six, without prefab edits
        private void InstallGrid()
        {
            var existing = ContentsParent.GetComponent<LayoutGroup>();
            if (existing != null && !(existing is GridLayoutGroup))
                Destroy(existing);

            if (ContentsParent.GetComponent<GridLayoutGroup>() == null)
                ContentsParent.gameObject.AddComponent<GridLayoutGroup>();

            var grid = ContentsParent.GetComponent<SixColumnGrid>();
            if (grid == null)
                grid = ContentsParent.gameObject.AddComponent<SixColumnGrid>();

            grid.RowHeight = RowHeight;
        }
    }
}
