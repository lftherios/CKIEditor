using CKIEditor.Model.Defs;

namespace CKIEditor.Model
{
    /// <summary>
    /// Re-arranges track values between slots. Dropping on an empty slot moves,
    /// dropping on an occupied slot swaps - either way no data is lost and the
    /// SlotIndex field inside each def stays consistent with its dictionary key.
    /// </summary>
    public static class TrackValueArranger
    {
        public static bool Move(InstrumentDef instrument, int fromSlot, int toSlot)
        {
            if (instrument == null || fromSlot == toSlot)
                return false;

            if (!instrument.TrackValues.TryGetValue(fromSlot, out var source)
                || !instrument.TrackValues.TryGetValue(toSlot, out var target))
                return false;

            if (source.Type == TrackValueType.Empty)
                return false;

            instrument.TrackValues[toSlot] = source;
            source.SlotIndex = toSlot;

            instrument.TrackValues[fromSlot] = target;
            target.SlotIndex = fromSlot;

            return true;
        }
    }
}
