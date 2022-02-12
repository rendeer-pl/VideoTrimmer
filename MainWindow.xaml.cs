using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Path = System.IO.Path;

namespace VideoTrimmer
{
    public class TimeCodeWrapper
    {
        public bool IsStartMarker { get; private set; }
        public System.Windows.Controls.TextBox TextBox { get; private set; }
        public System.Windows.Controls.Button ButtonSet { get; private set; }
        public System.Windows.Controls.Button ButtonJumpTo { get; private set; }
        public TimeSpan? CurrentTime { get; private set; }

        public TimeCodeWrapper(System.Windows.Controls.TextBox textBox, System.Windows.Controls.Button buttonSet, System.Windows.Controls.Button buttonJumpTo, bool isStartMarker)
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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MediaTimeline mediaPlayerTimeline = new MediaTimeline();
        public MediaClock mediaPlayerClock;
        public bool sliderUpdatesPossible = true;

        public VideoProcessing videoProcessing = new VideoProcessing();

        private TimeCodeWrapper timeMarkerStart;
        private TimeCodeWrapper timeMarkerEnd;

        private bool wasPlayingWhenSliderChanged;

        public MainWindow()
        {
            InitializeComponent();

            // Updating the "About" footer
            aboutFooter.Content = Globals.company + " " + Globals.customVersion;

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

            // Create wrapper classes for Start and End markers
            timeMarkerStart = new TimeCodeWrapper(timecodeStart, StartTimecodePickButton, jumpToStartMarkerButton, true);
            timeMarkerEnd = new TimeCodeWrapper(timecodeEnd, EndTimecodePickButton, jumpToEndMarkerButton, false);
        }

        // Used to enable or disable editable fields
        private void SetFieldsLockStatus(bool NewLockStatus)
        {
            trimVideoButton.IsEnabled = NewLockStatus;
            removeAudio.IsEnabled = NewLockStatus;
            recompressFile.IsEnabled = NewLockStatus;
            pauseAtEndMarker.IsEnabled = NewLockStatus;
            PlayPauseButton.IsEnabled = NewLockStatus;
            TimelineSlider.IsEnabled = NewLockStatus;
            timeMarkerStart.SetLockStatus(NewLockStatus);
            timeMarkerEnd.SetLockStatus(NewLockStatus);

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
            fileNameLabel.Content = "No video selected";
            fileNameLabel.ToolTip = "No video selected";
            timecodeStart.Text = GetStringFromTimeSpan(TimeSpan.FromSeconds(0.0));
            timecodeEnd.Text = GetStringFromTimeSpan(TimeSpan.FromSeconds(0.0));
            videoProcessing = new VideoProcessing();
            SetFieldsLockStatus(false);

            return;
        }

        private string GetStringFromTimeSpan(TimeSpan? timeSpan)
        {
            TimeSpan targetTimeSpan = timeSpan.HasValue ? timeSpan.Value : TimeSpan.FromSeconds(0.0);
            return targetTimeSpan.ToString(Globals.timeFormat);
        }

        // Analyzing a file selected via drag and dopping or using "Open File" dialog
        private void OpenSelectedFile(string FileToOpen)
        {
            Console.WriteLine("Opening file: " + FileToOpen);

            if (videoProcessing.LoadFile(FileToOpen) == true)
            {
                Console.WriteLine("File opened successfully");
            } else
            {
                Console.WriteLine("Couldn't open file");
            }

            // Unlock editable fields
            SetFieldsLockStatus(true);

            // Get video duration and paste it into "End" timecode TextBox
            timecodeStart.Text = GetStringFromTimeSpan(TimeSpan.FromSeconds(0.0));
            timecodeEnd.Text = GetStringFromTimeSpan(videoProcessing.GetDuration());

            if (mediaPlayerClock != null)
            {
                mediaPlayerClock.CurrentTimeInvalidated -= MediaPlayerOnTimeChanged;
                mediaPlayerClock.CurrentGlobalSpeedInvalidated -= MediaPlayerOnTimeChanged;
            }

            mediaPlayerTimeline.Source = new Uri(videoProcessing.GetFilePath());
            mediaPlayerClock = mediaPlayerTimeline.CreateClock();
            MediaPlayer.Clock = mediaPlayerClock;

            // If either of these callbacks are called, call the same function.
            // CurrentTimeInvalidate isn't always called when the video is paused, so we need the other callback as well.
            mediaPlayerClock.CurrentTimeInvalidated += MediaPlayerOnTimeChanged;
            mediaPlayerClock.CurrentGlobalSpeedInvalidated += MediaPlayerOnTimeChanged;

            TimelineSlider.AddHandler(MouseLeftButtonUpEvent,
                      new MouseButtonEventHandler(SliderValueManuallyChanged),
                      true);
            TimelineSlider.AddHandler(MouseLeftButtonDownEvent,
                      new MouseButtonEventHandler(SliderInteractionStarted),
                      true);

            // If file path is long, trim it
            String FileNameToDisplay = "";
            if (videoProcessing.GetFilePath().Length > 40) FileNameToDisplay = videoProcessing.GetFileRoot() + "...\\" + videoProcessing.GetFileName();
            else FileNameToDisplay = videoProcessing.GetFilePath();

            // Update display
            fileNameLabel.Content = "✔️ " + FileNameToDisplay;
            fileNameLabel.ToolTip = videoProcessing.GetFilePath();

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

        private void ValidateTimeMarker(TimeCodeWrapper timeMarker)
        {
            TimeSpan timeSpan;

            // check if a value is a valid TimeSpan
            if (TimeSpan.TryParseExact(timeMarker.TextBox.Text, Globals.timeFormat, null, out timeSpan))
            {
                // check if the value is shorter or equal to FileDuration
                if (TimeSpan.Parse(timeMarker.TextBox.Text) <= videoProcessing.GetDuration())
                {
                    // make sure that none of the TextBoxes is null (...wat)
                    if (timecodeStart == null | timecodeEnd == null)
                    {
                        // one of the fields has not been initialized, do nothing
                        return;
                    }
                    else
                    {
                        bool isStartTimecode = timeMarker == timeMarkerStart;

                        if (IsTimeSpanValid(timeSpan, isStartTimecode))
                        {
                            // accept the value, but parse it to make sure the leading zeros are there
                            timeMarker.SetTimeSpan(timeSpan);
                            return;
                        }
                    }
                }
            }

            // if everything failed -- undo editing field
            _ = Dispatcher.BeginInvoke(new Action(() => timeMarker.TextBox.Undo()));
            System.Media.SystemSounds.Asterisk.Play();
        }

        // handles value change in timecode text boxes
        private void Timecode_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox senderTextBox = (System.Windows.Controls.TextBox)sender;
            ValidateTimeMarker(senderTextBox == timeMarkerStart.TextBox ? timeMarkerStart : timeMarkerEnd);
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

            // establish Start and Duration for the needs of the console command
            // TODO: Make sure these are valid, or fallback to start/end if unset?
            TimeSpan Start = timeMarkerStart.CurrentTime.Value;
            TimeSpan End = timeMarkerEnd.CurrentTime.Value;

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
        // TODO: Add custom "About" window
        public void ButtonShowAbout_Click(object sender, RoutedEventArgs e)
        {
            /*
            // CURRENTLY UNUSED
            string Text;
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            Text = "Rendeer " + version.Major + "." + version.Minor + ".190512";
            System.Windows.Forms.MessageBox.Show(Text);
            */
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
            if (sliderUpdatesPossible) TimelineSlider.Value = mediaPlayerClock.CurrentTime.Value.TotalMilliseconds;

            timecodeCurrent.Text = GetStringFromTimeSpan(mediaPlayerClock.CurrentTime);

            bool videoEnd = mediaPlayerClock.NaturalDuration == mediaPlayerClock.CurrentTime;
            PlayPauseButton.Content = mediaPlayerClock.IsPaused || videoEnd ? "▶" : "❚❚";

            // Auto-pause video if checkbox is ticked.
            if (timeMarkerEnd.CurrentTime.HasValue)
            {
                bool isPauseAtEndMarkerChecked = pauseAtEndMarker.IsChecked.HasValue && pauseAtEndMarker.IsChecked.Value;
                if (isPauseAtEndMarkerChecked && mediaPlayerClock.CurrentTime.Value >= timeMarkerEnd.CurrentTime.Value)
                    mediaPlayerClock.Controller.Pause();
            }
        }

        // User interaction - button clicked
        private void OnJumpToStartMarkerButtonPressed(object sender, RoutedEventArgs e) => JumpToMarker(timeMarkerStart);
        private void OnJumpToEndMarkerButtonPressed(object sender, RoutedEventArgs e) => JumpToMarker(timeMarkerEnd);

        private void JumpToMarker(TimeCodeWrapper timeMarker)
        {
            if (mediaPlayerTimeline.Source == null) return;

            if (MediaPlayer.IsLoaded && timeMarker.CurrentTime.HasValue)
                mediaPlayerClock.Controller.Seek(timeMarker.CurrentTime.Value, TimeSeekOrigin.BeginTime);
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
                    mediaPlayerClock.Controller.Resume();
                }
                else
                {
                    mediaPlayerClock.Controller.Pause();
                }
            }
        }

        // User clicked on the slider, so let's disable automatic slider updates
        private void SliderInteractionStarted(object sender, MouseButtonEventArgs e)
        {
            sliderUpdatesPossible = false;

            if (!mediaPlayerClock.IsPaused)
            {
                wasPlayingWhenSliderChanged = true;
                mediaPlayerClock.Controller.Pause();
            }
        }

        // User finished interaction with the slider, let's update the video
        private void SliderValueManuallyChanged(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int sliderValue = (int)TimelineSlider.Value;
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, sliderValue);
            mediaPlayerClock.Controller.Seek(ts, TimeSeekOrigin.BeginTime);

            sliderUpdatesPossible = true;

            if (wasPlayingWhenSliderChanged)
            {
                mediaPlayerClock.Controller.Resume();
                wasPlayingWhenSliderChanged = false;
            }
        }


        // User wants to use current position of the video player as the Start or End timecode
        private void OnTimecodePickButtonClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button senderButton = (System.Windows.Controls.Button)sender;

            TimeCodeWrapper timeMarkerToUpdate = senderButton.Tag.ToString() == "Start" ? timeMarkerStart : timeMarkerEnd;

            TimeSpan newTimeSpan = mediaPlayerClock.CurrentTime.Value;

            // get current media position and set it
            if (IsTimeSpanValid(newTimeSpan, timeMarkerToUpdate.IsStartMarker))
                timeMarkerToUpdate.SetTimeSpan(newTimeSpan);
        }

        private bool IsTimeSpanValid(TimeSpan? timeSpan, bool isStartTimeSpan)
        {
            if (!timeSpan.HasValue || timeSpan > videoProcessing.GetDuration())
                return false;

            if (isStartTimeSpan)
                return !timeMarkerEnd.CurrentTime.HasValue || timeSpan < timeMarkerEnd.CurrentTime;

            return !timeMarkerStart.CurrentTime.HasValue || timeSpan > timeMarkerStart.CurrentTime;
        }

        private void ClearKeyboardFocus(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            System.Windows.Controls.Slider slider = (System.Windows.Controls.Slider)sender;

            // update current time
            if (mediaPlayerClock.NaturalDuration.HasTimeSpan)
            {
                float timePercentage = (float)(slider.Value / slider.Maximum);
                TimeSpan timeSpan = TimeSpan.FromSeconds(mediaPlayerClock.NaturalDuration.TimeSpan.TotalSeconds * timePercentage);
            }

            // update media to show current frame
            mediaPlayerClock.Controller.Seek(TimeSpan.FromMilliseconds(slider.Value), TimeSeekOrigin.BeginTime);
        }
    }
}
