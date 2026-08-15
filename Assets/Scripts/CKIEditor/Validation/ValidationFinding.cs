using System;

namespace CKIEditor.Validation
{
    public enum FindingSeverity
    {
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// One preflight finding. Speaks in consequences ("ships as 'Feedba'"),
    /// and carries its own repair when one is possible.
    /// </summary>
    public class ValidationFinding
    {
        public FindingSeverity Severity;
        public string InstrumentName;
        public string Title;
        public string Detail;

        //one-click repair - null when the finding needs a human decision
        public string FixLabel;
        public Action Fix;
        public bool IsFixed;

        public bool CanFix => Fix != null && !IsFixed;

        public void ApplyFix()
        {
            if (!CanFix)
                return;

            Fix();
            IsFixed = true;
        }
    }
}
