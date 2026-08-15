using System.Collections.Generic;

namespace CKIEditor.Metadata
{
    public interface IMetadataModel
    {
        InstrumentMetadata Get(string instrumentName);
        InstrumentMetadata GetOrCreate(string instrumentName);
        void Merge(Dictionary<string, InstrumentMetadata> incoming);
        Dictionary<string, InstrumentMetadata> GetAll();
        void Rename(string oldName, string newName);
    }

    public class MetadataModel : IMetadataModel
    {
        private readonly Dictionary<string, InstrumentMetadata> _byInstrument =
            new Dictionary<string, InstrumentMetadata>();

        public InstrumentMetadata Get(string instrumentName)
        {
            if (string.IsNullOrEmpty(instrumentName))
                return null;

            _byInstrument.TryGetValue(instrumentName, out var meta);
            return meta;
        }

        public InstrumentMetadata GetOrCreate(string instrumentName)
        {
            var meta = Get(instrumentName);
            if (meta == null)
            {
                meta = new InstrumentMetadata();
                _byInstrument[instrumentName] = meta;
            }

            return meta;
        }

        public void Merge(Dictionary<string, InstrumentMetadata> incoming)
        {
            if (incoming == null)
                return;

            foreach (var pair in incoming)
                GetOrCreate(pair.Key).Merge(pair.Value);
        }

        public Dictionary<string, InstrumentMetadata> GetAll()
        {
            return _byInstrument;
        }

        public void Rename(string oldName, string newName)
        {
            if (oldName == newName || string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                return;

            if (!_byInstrument.TryGetValue(oldName, out var meta))
                return;

            _byInstrument.Remove(oldName);

            //if the new name already has metadata, keep it and fold the old in
            if (_byInstrument.TryGetValue(newName, out var existing))
                existing.Merge(meta);
            else
                _byInstrument[newName] = meta;
        }
    }
}
