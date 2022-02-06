﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Path = System.IO.Path;

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

        public VideoProcessing videoProcessing = new VideoProcessing();

        private const string timeFormat = @"hh\:mm\:ss";

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
        }

        // Used to enable or disable editable fields
        private void SetFieldsLockStatus(bool NewLockStatus)
        {
            trimVideoButton.IsEnabled = NewLockStatus;
            timecodeStart.IsEnabled = NewLockStatus;
            timecodeEnd.IsEnabled = NewLockStatus;
            removeAudio.IsEnabled = NewLockStatus;
            recompressFile.IsEnabled = NewLockStatus;
            pauseAtEndMarker.IsEnabled = NewLockStatus;
            jumpToStartMarkerButton.IsEnabled = NewLockStatus == false;
            jumpToEndMarkerButton.IsEnabled = NewLockStatus == false;
            PlayPauseButton.IsEnabled = NewLockStatus;
            TimelineSlider.IsEnabled = NewLockStatus;
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
            fileNameLabel.Content = "No video selected";
            fileNameLabel.ToolTip = "No video selected";
            timecodeStart.Text = GetStringFromTimeSpan(TimeSpan.FromSeconds(0.0));
            timecodeEnd.Text = GetStringFromTimeSpan(TimeSpan.FromSeconds(0.0));
            videoProcessing = new VideoProcessing();
            SetFieldsLockStatus(false);

            return;
        }

        private void UpdateCurrentTimeText(TimeSpan? timeSpan)
        {
            timecodeCurrent.Text = GetStringFromTimeSpan(timeSpan);
        }

        private string GetStringFromTimeSpan(TimeSpan? timeSpan)
        {
            TimeSpan targetTimeSpan = timeSpan.HasValue ? timeSpan.Value : TimeSpan.FromSeconds(0.0);
            return targetTimeSpan.ToString(timeFormat);
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

            mediaPlayerTimeline.Source = new Uri(videoProcessing.GetFilePath());
            mediaPlayerClock = mediaPlayerTimeline.CreateClock();
            MediaPlayer.Clock = mediaPlayerClock;

            mediaPlayerClock.CurrentTimeInvalidated += delegate (object sender, EventArgs e)
            {
                MediaPlayerOnTimeChanged(sender, e);
            };

            mediaPlayerClock.Completed += delegate (object sender, EventArgs e)
            {
                PlayPauseButton.Content = "▶";
            };

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

        private bool ValidateTimecodeTextBox(System.Windows.Controls.TextBox TimecodeTextBox)
        {
            // check if a value is a valid TimeSpan
            if (TimeSpan.TryParseExact(TimecodeTextBox.Text, timeFormat, null, out _))
            {
                // check if the value is shorter or equal to FileDuration
                if (TimeSpan.Parse(TimecodeTextBox.Text) <= videoProcessing.GetDuration())
                {
                    // make sure that none of the TextBoxes is null
                    if (timecodeStart == null | timecodeEnd == null)
                    {
                        // one of the fields has not been initialized, do nothing
                        return false;
                    }
                    else
                    {
                        // make sure the starting timecode is smaller than the ending timecode
                        if (TimeSpan.Parse(timecodeStart.Text) < TimeSpan.Parse(timecodeEnd.Text))
                        {
                            // accept the value, but parse it to make sure the leading zeros are there
                            TimecodeTextBox.Text = TimeSpan.Parse(TimecodeTextBox.Text).ToString();
                            return true;
                        }
                    }
                }
            }

            // if everything failed -- undo editing field
            _ = Dispatcher.BeginInvoke(new Action(() => TimecodeTextBox.Undo()));
            System.Media.SystemSounds.Asterisk.Play();

            return false;
        }

        // handles value change in timecode text boxes
        private void Timecode_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox senderTextBox = (System.Windows.Controls.TextBox)sender;
            ValidateTimecodeTextBox(senderTextBox);
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
            if (SliderUpdatesPossible) TimelineSlider.Value = mediaPlayerClock.CurrentTime.Value.TotalMilliseconds;

            UpdateCurrentTimeText(mediaPlayerClock.CurrentTime);

            if (pauseAtEndMarker.IsChecked.HasValue && pauseAtEndMarker.IsChecked.Value && mediaPlayerClock.CurrentTime.Value >= TimeSpan.Parse(timecodeEnd.Text))
                    mediaPlayerClock.Controller.Pause();

            bool videoEnd = mediaPlayerClock.NaturalDuration == mediaPlayerClock.CurrentTime;

            PlayPauseButton.Content = mediaPlayerClock.IsPaused || videoEnd ? "▶" : "❚❚";
        }

        // User interaction - button clicked
        private void OnJumpToStartMarkerButtonPressed(object sender, RoutedEventArgs e)
        {
            if (mediaPlayerTimeline.Source == null) return;

            if (MediaPlayer.IsLoaded)
                mediaPlayerClock.Controller.Seek(TimeSpan.Parse(timecodeStart.Text), TimeSeekOrigin.BeginTime);
        }

        // User interaction - button clicked
        private void OnJumpToEndMarkerButtonPressed(object sender, RoutedEventArgs e)
        {
            if (mediaPlayerTimeline.Source == null) return;

            if (MediaPlayer.IsLoaded)
                mediaPlayerClock.Controller.Seek(TimeSpan.Parse(timecodeEnd.Text), TimeSeekOrigin.BeginTime);
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
            SliderUpdatesPossible = false;
        }

        private void SliderValueChanged(object sender, MouseButtonEventArgs e)
        {
            UpdateCurrentTimeText(mediaPlayerClock.CurrentTime);
        }

        // User finished interaction with the slider, let's update the video
        private void SliderValueManuallyChanged(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool wasPaused = mediaPlayerClock.IsPaused;
            if (wasPaused == false) mediaPlayerClock.Controller.Pause();
            int SliderValue = (int)TimelineSlider.Value;
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            mediaPlayerClock.Controller.Seek(ts, TimeSeekOrigin.BeginTime);
            if (wasPaused == false) mediaPlayerClock.Controller.Resume();
            SliderUpdatesPossible = true;
        }


        // User wants to use current position of the video player as the Start or End timecode
        private void OnTimecodePickButtonClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button senderButton = (System.Windows.Controls.Button)sender;

            System.Windows.Controls.TextBox TextBoxToUpdate;
            System.Windows.Controls.Button ButtonToUpdate;

            switch (senderButton.Tag.ToString())
            {
                case "Start":
                    TextBoxToUpdate = timecodeStart;
                    ButtonToUpdate = jumpToStartMarkerButton;
                    break;
                case "End":
                    TextBoxToUpdate = timecodeEnd;
                    ButtonToUpdate = jumpToEndMarkerButton;
                    break;
                default:
                    Console.WriteLine("Couldn't identify textbox to update!");
                    return;
            }

            // get current media position and set it 
            TextBoxToUpdate.Text = GetStringFromTimeSpan(mediaPlayerClock.CurrentTime);

            // attempt to set it as in timecode
            bool didValidateTimecode = ValidateTimecodeTextBox(TextBoxToUpdate);
            ButtonToUpdate.IsEnabled = didValidateTimecode;
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
                UpdateCurrentTimeText(timeSpan);
            }

            // update media to show current frame
            mediaPlayerClock.Controller.Seek(TimeSpan.FromMilliseconds(slider.Value), TimeSeekOrigin.BeginTime);
        }
    }
}
