using System.Collections.Generic;

namespace CKIEditor.Metadata
{
    /// <summary>
    /// What the editor remembers beyond the six characters the hardware keeps:
    /// full control names, descriptions and groupings. Lives in a .ckix sidecar
    /// next to the exported .CKI and merges back by CC number on import.
    /// </summary>
    public class CcMetadata
    {
        public string FullName;
        public string Description;
        public string Group;

        public bool IsEmpty =>
            string.IsNullOrEmpty(FullName) && string.IsNullOrEmpty(Description) && string.IsNullOrEmpty(Group);
    }

    public class InstrumentMetadata
    {
        public string Notes;
        public Dictionary<int, CcMetadata> CcMeta = new Dictionary<int, CcMetadata>();

        public bool IsEmpty
        {
            get
            {
                if (!string.IsNullOrEmpty(Notes))
                    return false;

                foreach (var meta in CcMeta.Values)
                {
                    if (!meta.IsEmpty)
                        return false;
                }

                return true;
            }
        }

        public CcMetadata GetOrCreateCc(int ccNum)
        {
            if (!CcMeta.TryGetValue(ccNum, out var meta))
            {
                meta = new CcMetadata();
                CcMeta[ccNum] = meta;
            }

            return meta;
        }

        /// <summary>Field-level merge: incoming values fill in, but never blank out what we have.</summary>
        public void Merge(InstrumentMetadata incoming)
        {
            if (incoming == null)
                return;

            if (!string.IsNullOrEmpty(incoming.Notes))
                Notes = incoming.Notes;

            foreach (var pair in incoming.CcMeta)
            {
                var target = GetOrCreateCc(pair.Key);
                if (!string.IsNullOrEmpty(pair.Value.FullName))
                    target.FullName = pair.Value.FullName;
                if (!string.IsNullOrEmpty(pair.Value.Description))
                    target.Description = pair.Value.Description;
                if (!string.IsNullOrEmpty(pair.Value.Group))
                    target.Group = pair.Value.Group;
            }
        }
    }
}
