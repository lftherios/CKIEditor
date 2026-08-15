using CKIEditor.Model;
using strange.extensions.command.impl;
using strange.extensions.signal.impl;

namespace CKIEditor.Controller
{
    public struct SlotMove
    {
        public int From;
        public int To;

        public SlotMove(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    public class MoveTrackValueSignal : Signal<SlotMove>
    {

    }

    public class TrackValuesChangedSignal : Signal
    {

    }

    public class MoveTrackValueCommand : Command
    {
        [Inject] public IInstrumentsModel InstrumentsModel { get; set; }
        [Inject] public SlotMove Move { get; set; }
        [Inject] public TrackValuesChangedSignal TrackValuesChangedSignal { get; set; }

        public override void Execute()
        {
            var instrument = InstrumentsModel.GetEditedInstrument();
            if (TrackValueArranger.Move(instrument, Move.From, Move.To))
                TrackValuesChangedSignal.Dispatch();
        }
    }
}
