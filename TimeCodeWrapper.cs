using System;
using System.Windows.Controls;

namespace VideoTrimmer
{
    public class TimeCodeWrapper
    {
        public bool IsStartMarker { get; private set; }
        public TextBox TextBox { get; private set; }
        public Button ButtonSet { get; private set; }
        public Button ButtonJumpTo { get; private set; }
        public TimeSpan? CurrentTime { get; private set; }

        public TimeCodeWrapper(TextBox textBox, Button buttonSet, Button buttonJumpTo, bool isStartMarker)
        {
            TextBox = textBox;
            ButtonSet = buttonSet;
            ButtonJumpTo = buttonJumpTo;
            IsStartMarker = isStartMarker;

            SetTimeSpan(null);
        }

        public void SetTimeSpan(TimeSpan? timeSpan)
        {
            CurrentTime = timeSpan;
            TextBox.Text = CurrentTime.HasValue ? CurrentTime.Value.ToString(Globals.timeFormat) : "N/A";
            ButtonSet.IsEnabled = CurrentTime.HasValue;
            ButtonJumpTo.IsEnabled = CurrentTime.HasValue;
        }

        public void SetLockStatus(bool newLockStatus)
        {
            TextBox.IsEnabled = newLockStatus;
            ButtonSet.IsEnabled = newLockStatus;
            ButtonJumpTo.IsEnabled = !newLockStatus;
            CurrentTime = null;
        }
    }
}
