using System;
using System.Linq;
using CKIEditor;
using CKIEditor.Model;
using CKIEditor.Model.Defs;

public static class ArrangerTests
{
    public static void Run(Action<bool, string> check)
    {
        var inst = new InstrumentDef { Name = "Arrange" };
        inst.TrackValues[2].Type = TrackValueType.MidiCC;
        inst.TrackValues[2].MidiCC = 19;
        inst.TrackValues[2].Label = "FltCut";
        inst.TrackValues[7].Type = TrackValueType.TrackControl;
        inst.TrackValues[7].TrackControl = TrackControlType.Program;

        //move to an empty slot
        check(TrackValueArranger.Move(inst, 2, 5), "arranger: move to empty succeeds");
        check(inst.TrackValues[5].Type == TrackValueType.MidiCC && inst.TrackValues[5].Label == "FltCut",
            "arranger: value arrived at target");
        check(inst.TrackValues[2].Type == TrackValueType.Empty, "arranger: source now empty");
        check(inst.TrackValues[5].SlotIndex == 5 && inst.TrackValues[2].SlotIndex == 2,
            "arranger: SlotIndex fields follow the move");

        //swap two occupied slots
        check(TrackValueArranger.Move(inst, 5, 7), "arranger: swap succeeds");
        check(inst.TrackValues[7].Type == TrackValueType.MidiCC
              && inst.TrackValues[5].Type == TrackValueType.TrackControl,
            "arranger: swap exchanged both values");
        check(inst.TrackValues[7].SlotIndex == 7 && inst.TrackValues[5].SlotIndex == 5,
            "arranger: SlotIndex fields consistent after swap");

        //rejections
        check(!TrackValueArranger.Move(inst, 3, 4), "arranger: moving an empty slot is refused");
        check(!TrackValueArranger.Move(inst, 7, 7), "arranger: same-slot move is refused");
        check(!TrackValueArranger.Move(inst, 0, 5), "arranger: slot 0 out of range refused");
        check(!TrackValueArranger.Move(inst, 7, 181), "arranger: slot 181 out of range refused");
        check(!TrackValueArranger.Move(null, 1, 2), "arranger: null instrument refused");

        //dictionary integrity after all operations
        var slots = CkiConsts.TRACK_VALUES_PER_SCREEN * CkiConsts.TRACK_VALUE_ROWS;
        check(inst.TrackValues.Count == slots
              && inst.TrackValues.All(pair => pair.Value.SlotIndex == pair.Key),
            "arranger: all 180 slots intact, keys match SlotIndex");
    }
}
