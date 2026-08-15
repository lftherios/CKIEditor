using System.Collections.Generic;
using System.IO;
using SimpleJSON;

namespace CKIEditor.Metadata
{
    /// <summary>
    /// Reads and writes the .ckix sidecar - plain JSON, human-readable,
    /// deliberately independent of the .CKI so the hardware never sees it.
    /// </summary>
    public static class MetadataSerializer
    {
        public const string EXTENSION = ".ckix";
        public const int VERSION = 1;

        private const string KEY_VERSION = "ckix_version";
        private const string KEY_INSTRUMENTS = "instruments";
        private const string KEY_NOTES = "notes";
        private const string KEY_CC_META = "cc_meta";
        private const string KEY_NAME = "name";
        private const string KEY_DESC = "desc";
        private const string KEY_GROUP = "group";

        public static string SidecarPathFor(string ckiPath)
        {
            return Path.ChangeExtension(ckiPath, EXTENSION);
        }

        public static string Serialize(Dictionary<string, InstrumentMetadata> library)
        {
            var root = new JSONObject();
            root.Add(KEY_VERSION, new JSONNumber(VERSION));

            var instrumentsJson = new JSONObject();
            foreach (var pair in library)
            {
                if (pair.Value == null || pair.Value.IsEmpty)
                    continue;

                var instJson = new JSONObject();
                if (!string.IsNullOrEmpty(pair.Value.Notes))
                    instJson.Add(KEY_NOTES, new JSONString(pair.Value.Notes));

                var ccJson = new JSONObject();
                foreach (var ccPair in pair.Value.CcMeta)
                {
                    if (ccPair.Value.IsEmpty)
                        continue;

                    var metaJson = new JSONObject();
                    if (!string.IsNullOrEmpty(ccPair.Value.FullName))
                        metaJson.Add(KEY_NAME, new JSONString(ccPair.Value.FullName));
                    if (!string.IsNullOrEmpty(ccPair.Value.Description))
                        metaJson.Add(KEY_DESC, new JSONString(ccPair.Value.Description));
                    if (!string.IsNullOrEmpty(ccPair.Value.Group))
                        metaJson.Add(KEY_GROUP, new JSONString(ccPair.Value.Group));

                    ccJson.Add(ccPair.Key.ToString(), metaJson);
                }

                if (ccJson.Count > 0)
                    instJson.Add(KEY_CC_META, ccJson);

                instrumentsJson.Add(pair.Key, instJson);
            }

            root.Add(KEY_INSTRUMENTS, instrumentsJson);
            return root.ToString(2);
        }

        public static Dictionary<string, InstrumentMetadata> Parse(string json)
        {
            var library = new Dictionary<string, InstrumentMetadata>();

            var root = JSON.Parse(json);
            if (root == null)
                return library;

            var instrumentsJson = root[KEY_INSTRUMENTS];
            if (instrumentsJson == null)
                return library;

            foreach (string instrumentName in instrumentsJson.Keys)
            {
                var instJson = instrumentsJson[instrumentName];
                var meta = new InstrumentMetadata();

                if (instJson[KEY_NOTES] != null)
                    meta.Notes = instJson[KEY_NOTES];

                var ccJson = instJson[KEY_CC_META];
                if (ccJson != null)
                {
                    foreach (string ccKey in ccJson.Keys)
                    {
                        if (!int.TryParse(ccKey, out var ccNum))
                            continue;

                        var metaJson = ccJson[ccKey];
                        meta.CcMeta[ccNum] = new CcMetadata
                        {
                            FullName = metaJson[KEY_NAME],
                            Description = metaJson[KEY_DESC],
                            Group = metaJson[KEY_GROUP],
                        };
                    }
                }

                library[instrumentName] = meta;
            }

            return library;
        }
    }
}
