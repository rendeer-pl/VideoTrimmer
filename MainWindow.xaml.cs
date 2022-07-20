using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Path = System.IO.Path;
using System.Windows.Controls;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MediaTimeline mediaPlayerTimeline = new MediaTimeline();
        public MediaClock mediaPlayerClock;
        public bool SliderUpdatesPossible = true;
        public double mouseHoldStartingX;
        public DateTime mouseHoldStartTime;
        public bool shouldPlaybackBeActive;

        public VideoProcessing videoProcessing = new VideoProcessing();

        public MainWindow()
        {
            InitializeComponent();

            // Checking if there is a file path passed through command line arguments (e.g. "Open with...")
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length>1) {
                // check if it has a compatible extension
                if (videoProcessing.CheckIfFileIsAccepted(arguments[1]))
                {
                    Console.WriteLine("File passed through arguments has been recognized!");
                    OpenSelectedFile(arguments[1]);
                }
                else
                {
                    Console.WriteLine("File passed through arguments has not been recognized.");
                }
            }
        }

        // Used to enable or disable editable fields
        private void SetFieldsLockStatus(bool NewLockStatus)
        {
            trimVideoButton.IsEnabled = NewLockStatus;
            timecodeStart.IsEnabled = NewLockStatus;
            timecodeEnd.IsEnabled = NewLockStatus;
            removeAudio.IsEnabled = NewLockStatus;
            recompressFile.IsEnabled = NewLockStatus;
            PlayPauseButton.IsEnabled = NewLockStatus;
            TimelineSlider.IsEnabled = NewLockStatus;
            TimelineStartButton.IsEnabled = NewLockStatus;
            TimelineEndButton.IsEnabled = NewLockStatus;
            StartTimecodePickButton.IsEnabled = NewLockStatus;
            EndTimecodePickButton.IsEnabled = NewLockStatus;

            // if the form is unlocking then enable the recompression options depending on the checkbox value. If the form is locking, then just disable everything.
            if (NewLockStatus == true) RecompressFile_ValueChanged(null, null);
            else
            {
                DesiredFileSize.IsEnabled = NewLockStatus;
                DesiredFileSizeLabel.IsEnabled = NewLockStatus;
            }

            return;
        }

        // Fired whenever user checks or unchecks the "Recompess file" checkbox
        private void RecompressFile_ValueChanged(object sender, RoutedEventArgs e)
        {
            bool NewLockStatus = (bool)recompressFile.IsChecked;
            DesiredFileSize.IsEnabled = NewLockStatus;
            DesiredFileSizeLabel.IsEnabled = NewLockStatus;
            DesiredFileSizeLabelSuffix.IsEnabled = NewLockStatus;
        }

        // Reset window to state without file selected
        private void ResetFilePicker()
        {
            MediaPlayer.Source = null;
            mediaPlayerTimeline = new MediaTimeline();
            mediaPlayerClock.Controller.Remove();
            SliderUpdatesPossible = false;
            MediaPlayer.Visibility = Visibility.Collapsed;
            FileNameLabel.Content = "No video selected";
            FileNameLabel.ToolTip = "No video selected";
            timecodeStart.Text = "00:00:00";
            timecodeEnd.Text = "00:00:00";
            UpdateRange();
            TimelineSlider.Value = 0;
            ButtonFilePicker.Visibility = Visibility.Visible;
            ButtonCloseFile.Visibility = Visibility.Collapsed;
            videoProcessing = new VideoProcessing();
            SetFieldsLockStatus(false);

            return;
        }

        // Analyzing a file selected via drag and dropping or using "Open File" dialog
        private void OpenSelectedFile(string FileToOpen)
        {
            Console.WriteLine("Opening file: " + FileToOpen);

            if (videoProcessing.LoadFile(FileToOpen) == true)
            {
                ButtonFilePicker.Visibility = Visibility.Collapsed;
                ButtonCloseFile.Visibility = Visibility.Visible;
                MediaPlayer.Visibility = Visibility.Visible;
                SliderUpdatesPossible = true;
                Console.WriteLine("File opened successfully");
            } else
            {
                Console.WriteLine("Couldn't open file");
            }

            // Unlock editable fields
            SetFieldsLockStatus(true);

            // Get video duration and paste it into "End" timecode TextBox
            timecodeStart.Text = "00:00:00";
            timecodeEnd.Text = videoProcessing.GetDuration().ToString(@"hh\:mm\:ss");
            mediaPlayerTimeline.Source = new Uri(videoProcessing.GetFilePath());
            mediaPlayerClock = mediaPlayerTimeline.CreateClock();
            MediaPlayer.Clock = mediaPlayerClock;

            mediaPlayerClock.CurrentTimeInvalidated += delegate (object sender, EventArgs e)
            {
                MediaPlayerOnTimeChanged(sender, e);
            };

            mediaPlayerClock.Completed += delegate (object sender, EventArgs e)
            {
                shouldPlaybackBeActive = false;
                PausePlayback();
            };

            TimelineSlider.AddHandler(MouseLeftButtonUpEvent,
                      new MouseButtonEventHandler(SliderValueManuallyChanged),
                      true);
            TimelineSlider.AddHandler(MouseLeftButtonDownEvent,
                      new MouseButtonEventHandler(SliderInteractionStarted),
                      true);

            UpdateRange();

            // If file path is long, trim it
            String FileNameToDisplay = "";
            if (videoProcessing.GetFilePath().Length > 40) FileNameToDisplay = videoProcessing.GetFileRoot() + "...\\" + videoProcessing.GetFileName();
            else FileNameToDisplay = videoProcessing.GetFilePath();

            // Update display
            FileNameLabel.Content = FileNameToDisplay;
            FileNameLabel.ToolTip = videoProcessing.GetFilePath();

            return;
        }

        // Called when clicked on the "Select File" button
        private void ButtonFileOpen_Click(object sender, RoutedEventArgs e)
        {
            string FilterParams = "Video files (" + videoProcessing.GetSupportedVideoFormats() + ")|" + videoProcessing.GetSupportedVideoFormats();
            FilterParams += "|Audio files (" + videoProcessing.GetSupportedAudioFormats() + ")|" + videoProcessing.GetSupportedAudioFormats();

            var fileDialog = new OpenFileDialog
            {
                Filter = FilterParams,
                RestoreDirectory = true
            };
            var result = fileDialog.ShowDialog();

            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    OpenSelectedFile(fileDialog.FileName);
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }
        }

        public void UpdateRange()
        {
            TimelineSlider.SelectionStart = TimeSpan.Parse(timecodeStart.Text).TotalMilliseconds;
            TimelineSlider.SelectionEnd = TimeSpan.Parse(timecodeEnd.Text).TotalMilliseconds;

            double Start = TimeSpan.Parse(timecodeStart.Text).TotalMilliseconds / videoProcessing.GetDuration().TotalMilliseconds;
            double End = TimeSpan.Parse(timecodeEnd.Text).TotalMilliseconds / videoProcessing.GetDuration().TotalMilliseconds;

            Canvas.SetLeft(TimelineStartButton, Start * (TimelineSlider.ActualWidth - 10));
            Canvas.SetLeft(TimelineEndButton, End * (TimelineSlider.ActualWidth - 10));

            return;
        }

        private void ValidateTimecodeTextBox(System.Windows.Controls.TextBox TimecodeTextBox)
        {
            // check if a value is a valid TimeSpan
            if (TimeSpan.TryParse(TimecodeTextBox.Text, out _))
            {
                // check if the value is shorter or equal to FileDuration
                if (TimeSpan.Parse(TimecodeTextBox.Text) <= videoProcessing.GetDuration())
                {
                    // make sure that none of the TextBoxes is null
                    if (timecodeStart == null | timecodeEnd == null)
                    {
                        // one of the fields has not been initialized, do nothing
                        return;
                    }
                    else
                    {
                        // make sure the starting timecode is smaller than the ending timecode
                        if (TimeSpan.Parse(timecodeStart.Text) < TimeSpan.Parse(timecodeEnd.Text))
                        {
                            // accept the value, but parse it to make sure the leading zeros are there
                            TimecodeTextBox.Text = TimeSpan.Parse(TimecodeTextBox.Text).ToString();
                            UpdateRange();

                            return;
                        }
                    }
                }
            }

            // if everything failed -- undo editing field
            _ = Dispatcher.BeginInvoke(new Action(() => TimecodeTextBox.Undo()));
            System.Media.SystemSounds.Asterisk.Play();

            return;
        }

        // handles value change in timecode text boxes
        private void Timecode_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox senderTextBox = (System.Windows.Controls.TextBox)sender;
            ValidateTimecodeTextBox(senderTextBox);
        }
        private void Timecode_KeyHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (videoProcessing.GetFilePath() == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                    Timecode_LostFocus(sender, e);
                    ClearFocus((UIElement)sender);
                    break;
            }
        }

        // validates values in the DesiredFileSize text box
        private void DesiredFileSize_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox senderTextBox = (System.Windows.Controls.TextBox)sender;

            int newIntValue;

            // check if a value is a valid int
            if (int.TryParse(senderTextBox.Text.Split(',')[0], out newIntValue))
            {
                // accept the value, but parse it to make sure the leading zeros are not there
                senderTextBox.Text = newIntValue.ToString();
                return;
            }
            if (int.TryParse(senderTextBox.Text.Split('.')[0], out newIntValue))
            {
                // accept the value, but parse it to make sure the leading zeros are not there
                senderTextBox.Text = newIntValue.ToString();
                return;
            }

            if (senderTextBox.Text == "")
            {
                senderTextBox.Text = "0";
                return;
            }

            // if everything failed -- undo editing field
            _ = Dispatcher.BeginInvoke(new Action(() => senderTextBox.Undo()));

            return;
        }

        private void DesiredFileSize_KeyHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (videoProcessing.GetFilePath() == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                    DesiredFileSize_LostFocus(sender, e);
                    ClearFocus((UIElement)sender);
                    break;
            }
        }

        private void ClearFocus(UIElement ElementToClear)
        {
            // Kill logical focus
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(ElementToClear), null);
            // Kill keyboard focus
            Keyboard.ClearFocus();
        }

        // main logic - "TRIM VIDEO" button click
        private void ButtonTrimVideo_Click(object sender, RoutedEventArgs e)
        {
            // Make sure the file exists
            if (!videoProcessing.DoesTheFileExist())
            {
                // TODO: Replace with a custom error window
                System.Windows.MessageBox.Show("Source video has been moved or deleted!");

                // Reset window and block inputs
                ResetFilePicker();

                return;
            }

            // pause the current playback
            shouldPlaybackBeActive = false;
            PausePlayback();

            // establish Start and Duration for the needs of the console command
            TimeSpan Start = TimeSpan.Parse(timecodeStart.Text);
            TimeSpan End = TimeSpan.Parse(timecodeEnd.Text);

            int DesiredFileSizeInt;
            Int32.TryParse(DesiredFileSize.Text, out DesiredFileSizeInt);

            // set process settings
            videoProcessing.SetParameters(Start, End, (bool)removeAudio.IsChecked, (bool)recompressFile.IsChecked, DesiredFileSizeInt);

            // open the progress window that executes the main process
            ProgressWindow progressWindow = new ProgressWindow();
            progressWindow.Owner = this;
            progressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            progressWindow.ShowDialog();

        }

        // Displays "About" window
        public void ButtonShowAbout_Click(object sender, RoutedEventArgs e)
        {
            shouldPlaybackBeActive = false;
            PausePlayback();
            About aboutWindow = new About();
            aboutWindow.Owner = this;
            aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            aboutWindow.ShowDialog();
        }

        // Fired whenever an object is dragged over the app window
        private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])(e.Data.GetData("FileDrop", false));
            if (files != null)
            {
                // get first file
                string draggedFile = files[0];

                // check if it has a compatible extension
                if (videoProcessing.CheckIfFileIsAccepted(draggedFile))
                {
                    Console.WriteLine("Dragged file recognized!");
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 0, 0x77, 0));
                    return;
                }
            }
            Console.WriteLine("Dragged file not recognized.");
            Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0, 0));
        }

        // Fired whenever an object is dropped over the app window
        private void Window_DragDrop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])(e.Data.GetData("FileDrop", false));
            
            // restore default background
            Background = new SolidColorBrush(Color.FromRgb(33,33,33));

            // in case the dropped object is not a file
            if (files == null) return;

            // get the first file, ignore the rest
            string droppedFile = files[0];
            Console.WriteLine(Path.GetExtension(droppedFile));
            
            // check if it has a compatible extension
            if (videoProcessing.CheckIfFileIsAccepted(droppedFile))
            {
                Console.WriteLine("Dropped file recognized!");
                OpenSelectedFile(droppedFile);
            }
            else
            {
                Console.WriteLine("Dropped file not recognized.");
            }
        }

        // Fired whenever an object that has been dragged over the app window has left the window
        private void Window_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            Background = new SolidColorBrush(Color.FromRgb(33, 33, 33));
        }

        // Methods related to the video player

        // When the media opens, initialize the "Seek To" slider maximum value
        // to the total number of miliseconds in the length of the media clip.
        private void MediaPlayerOnMediaOpened(object sender, EventArgs e)
        {
            TimelineSlider.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            TimelineSlider.Value = 0;
            mediaPlayerClock.Controller.Pause();
        }

        // This is the automatic update as video plays
        private void MediaPlayerOnTimeChanged(object sender, EventArgs e)
        {
            if (SliderUpdatesPossible) TimelineSlider.Value = mediaPlayerClock.CurrentTime.Value.TotalMilliseconds;
        }

        // User interaction - button clicked
        private void OnPlayPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (mediaPlayerTimeline.Source == null) return;

            if (MediaPlayer.IsLoaded)
            {
                bool videoEnd = mediaPlayerClock.NaturalDuration == mediaPlayerClock.CurrentTime;

                if (videoEnd) mediaPlayerClock.Controller.Seek(TimeSpan.Zero, TimeSeekOrigin.BeginTime);

                if (mediaPlayerClock.IsPaused || videoEnd)
                {
                    shouldPlaybackBeActive = true;
                    ResumePlayback();
                }
                else
                {
                    shouldPlaybackBeActive = false;
                    PausePlayback();
                }
            }
        }

        private void PausePlayback()
        {
            PlayPauseButton.Content = "▶";
            if (mediaPlayerTimeline.Source != null) mediaPlayerClock.Controller.Pause();
        }

        private void ResumePlayback()
        {
            PlayPauseButton.Content = "❚❚";
            mediaPlayerClock.Controller.Resume();
        }

        public void UpdatePlaybackTime(TimeSpan newTime)
        {
            if (videoProcessing.GetDuration() < newTime) newTime = videoProcessing.GetDuration();
            if (newTime < TimeSpan.Zero) newTime = TimeSpan.Zero;

            mediaPlayerClock.Controller.Seek(newTime, TimeSeekOrigin.BeginTime);
            PausePlayback();
        }

        // User clicked on the slider, so let's disable automatic slider updates
        private void SliderInteractionStarted(object sender, MouseButtonEventArgs e)
        {
            SliderUpdatesPossible = false;
            mediaPlayerClock.Controller.Pause();
        }

        // User finished interaction with the slider, let's update the video
        private void SliderValueManuallyChanged(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int SliderValue = (int)TimelineSlider.Value;
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            mediaPlayerClock.Controller.Seek(ts, TimeSeekOrigin.BeginTime);
            if (shouldPlaybackBeActive && e.LeftButton != MouseButtonState.Pressed) ResumePlayback();
            SliderUpdatesPossible = true;
        }

        // fired when dragging the thumb over the timeline and when clicking somewhere on the timeline
        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // if the difference between the media clock and the slider is bigger than a second and when the player is not paused and when slider updates are possible (which means: when not currently dragging the thumb)
            if (Math.Abs(mediaPlayerClock.CurrentTime.Value.TotalMilliseconds - e.NewValue) > 1000)
            {
                bool wasPaused = CheckIfPlaybackIsPaused();
                mediaPlayerClock.Controller.Pause();
                int SliderValue = (int)TimelineSlider.Value;
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                mediaPlayerClock.Controller.Seek(ts, TimeSeekOrigin.BeginTime);
                if (wasPaused==false) ResumePlayback();
            }
        }

        // User wants to use current position of the video player as the Start or End timecode
        private void OnTimecodePickButtonClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button senderButton = (System.Windows.Controls.Button)sender;

            System.Windows.Controls.TextBox TextBoxToUpdate;

            switch (senderButton.Tag.ToString())
            {
                case "Start":
                    TextBoxToUpdate = timecodeStart;
                    break;
                case "End":
                    TextBoxToUpdate = timecodeEnd;
                    break;
                default:
                    Console.WriteLine("Couldn't identify textbox to update!");
                    return;
            }

            UpdateStartOrEndTimecode(TextBoxToUpdate);
        }

        // update Start or End timecode
        public void UpdateStartOrEndTimecode(System.Windows.Controls.TextBox TextBoxToUpdate)
        {
            // get current media position and set it 
            TextBoxToUpdate.Text = mediaPlayerClock.CurrentTime.Value.ToString(@"hh\:mm\:ss");

            // attempt to set it as in timecode
            ValidateTimecodeTextBox(TextBoxToUpdate);
        }

        // User wants to jump to the position of the Start or End marker
        private void OnTimelineMarkerButtonClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button senderButton = (System.Windows.Controls.Button)sender;

            System.Windows.Controls.TextBox TextBoxToUse;

            switch (senderButton.Tag.ToString())
            {
                case "Start":
                    TextBoxToUse = timecodeStart;
                    break;
                case "End":
                    TextBoxToUse = timecodeEnd;
                    break;
                default:
                    Console.WriteLine("Couldn't identify Timeline marker button");
                    return;
            }

            // do the jump
            TimeSpan ts = TimeSpan.Parse(TextBoxToUse.Text);
            UpdatePlaybackTime(ts);
            if (shouldPlaybackBeActive) ResumePlayback();
        }

        private void ResetKeyboardFocus(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(TimelineSlider);
        }

        private void ButtonCloseFile_Click(object sender, RoutedEventArgs e)
        {
            shouldPlaybackBeActive = false;
            PausePlayback();
            ResetFilePicker();
        }

        // fired whenev the user clicks on a timeline marker (e.g. to start a drag)
        private void TimelineMarker_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            mouseHoldStartingX = e.MouseDevice.GetPosition(TimelineSlider).X;
            mouseHoldStartTime = DateTime.Now;
        }

        // fired every time the user moves the mouse after clicking on a timeline marker
        private void TimelineMarker_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // ignore the move if the user has only now started moving and the offset is small
                if (((DateTime.Now.Millisecond - mouseHoldStartTime.Millisecond) < 100) && (Math.Abs(mouseHoldStartingX - e.MouseDevice.GetPosition(TimelineSlider).X) < 10)) return;

                // get a usable position of the mouse (within the slider's width)
                double pixelOffset = Math.Min(Math.Max(e.MouseDevice.GetPosition(TimelineSlider).X, 0), TimelineSlider.ActualWidth - 10);

                // divide by total length of the slider
                double ratio = pixelOffset / (TimelineSlider.ActualWidth - 10);

                // get the desired time
                int newTime = (int)(videoProcessing.GetDuration().TotalMilliseconds * ratio);

                // attempt to set it
                System.Windows.Controls.Button senderButton = (System.Windows.Controls.Button)sender;
                System.Windows.Controls.TextBox TextBoxToUse;

                switch (senderButton.Tag.ToString())
                {
                    case "Start":
                        TextBoxToUse = timecodeStart;
                        break;
                    case "End":
                        TextBoxToUse = timecodeEnd;
                        break;
                    default:
                        Console.WriteLine("Couldn't identify Timeline marker button");
                        return;
                }

                TimeSpan newTimeSpan = new TimeSpan(0, 0, 0, 0, newTime);
                TextBoxToUse.Text = newTimeSpan.ToString(@"hh\:mm\:ss");
                ValidateTimecodeTextBox(TextBoxToUse);

                // update the position of the timeline thumb
                newTimeSpan = TimeSpan.Parse(TextBoxToUse.Text);
                mediaPlayerClock.Controller.Pause();
                mediaPlayerClock.Controller.Seek(newTimeSpan, TimeSeekOrigin.BeginTime);
                TimelineSlider.Value = newTime;
            }
        }

        // fired whenev the user releases a click on a timeline marker (e.g. after a drag)
        private void TimelineMarker_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (shouldPlaybackBeActive) ResumePlayback();
            else PausePlayback();
        }

        // keyboard bindings
        public void KeyPress_SetStart()
        {
            UpdateStartOrEndTimecode(timecodeStart);
        }

        public void KeyPress_SetEnd()
        {
            UpdateStartOrEndTimecode(timecodeEnd);
        }

        public void KeyPress_GoToStart()
        {
            mediaPlayerClock.Controller.Seek(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            PausePlayback();
        }

        public void KeyPress_GoToEnd()
        {
            mediaPlayerClock.Controller.Seek(MediaPlayer.NaturalDuration.TimeSpan, TimeSeekOrigin.BeginTime);
            PausePlayback();
        }

        public void KeyPress_GoToPreviousSecond()
        {
            UpdatePlaybackTime((TimeSpan)(mediaPlayerClock.CurrentTime - TimeSpan.FromSeconds(1)));
        }

        public void KeyPress_GoToNextSecond()
        {
            UpdatePlaybackTime((TimeSpan)(mediaPlayerClock.CurrentTime + TimeSpan.FromSeconds(1)));
        }

        public void KeyPress_TogglePause()
        {
            if (!shouldPlaybackBeActive)
            {
                shouldPlaybackBeActive = true;
                ResumePlayback();
            }
            else
            {
                shouldPlaybackBeActive = false;
                PausePlayback();
            }
        }

        public bool CheckIfPlaybackIsPaused()
        {
            return mediaPlayerClock.IsPaused || mediaPlayerClock.NaturalDuration == mediaPlayerClock.CurrentTime;
        }

        private void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // ignore if there's no file
            if (videoProcessing.GetFilePath() == null) return;

            // ignore if the user is currently editing a text field
            if (Keyboard.FocusedElement == timecodeStart || Keyboard.FocusedElement == timecodeEnd || Keyboard.FocusedElement == DesiredFileSize) return;

            switch (e.Key)
            {
                case Key.I:
                    KeyPress_SetStart();
                    break;
                case Key.OemOpenBrackets:
                    KeyPress_SetStart();
                    break;
                case Key.O:
                    KeyPress_SetEnd();
                    break;
                case Key.OemCloseBrackets:
                    KeyPress_SetEnd();
                    break;
                case Key.Up:
                    KeyPress_GoToStart();
                    break;
                case Key.Home:
                    KeyPress_GoToStart();
                    break;
                case Key.Down:
                    KeyPress_GoToEnd();
                    break;
                case Key.End:
                    KeyPress_GoToEnd();
                    break;
                case Key.Left:
                    KeyPress_GoToPreviousSecond();
                    e.Handled = true;
                    break;
                case Key.Right:
                    KeyPress_GoToNextSecond();
                    e.Handled = true;
                    break;
                case Key.Space:
                    if (Keyboard.FocusedElement != TimelineSlider) return;
                    KeyPress_TogglePause();
                    e.Handled = true;
                    break;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRange();
        }
    }
}
