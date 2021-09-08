using WMPLib;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using Path = System.IO.Path;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string File = null;
        TimeSpan FileDuration;
        
        public MainWindow()
        {
            InitializeComponent();

            // Updating the "About" footer
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            aboutFooter.Content = "Rendeer " + version.Major + "." + version.Minor + "." + version.Build + ".190518";
        }

        // Used to enable or disable editable fields
        private void SetFieldsLockStatus(bool NewLockStatus)
        {
            trimVideoButton.IsEnabled = NewLockStatus;
            timecodeStart.IsEnabled = NewLockStatus;
            timecodeEnd.IsEnabled = NewLockStatus;
            removeAudio.IsEnabled = NewLockStatus;

            return;
        }


        // Reset window to state without file selected
        private void ResetFilePicker()
        {
            fileNameLabel.Content = "No file selected";
            fileNameLabel.ToolTip = "No file selected";
            timecodeStart.Text = "00:00:00";
            timecodeEnd.Text = "00:00:00";
            File = null;
            SetFieldsLockStatus(false);

            return;
        }

        // Analyzing a file selected via drag and dopping or using "Open File" dialog
        private void OpenSelectedFile(string FileToBeOpened)
        {
            Console.WriteLine("Opening file: " + FileToBeOpened);
            File = FileToBeOpened;
            String FileName = Path.GetFileName(File);
            String FileRoot = Path.GetPathRoot(File);

            // Unlock editable fields
            SetFieldsLockStatus(true);

            // Get video duration and paste it into "End" timecode TextBox
            var player = new WindowsMediaPlayer();
            var clip = player.newMedia(File);
            FileDuration = TimeSpan.FromSeconds(clip.duration);
            timecodeStart.Text = "00:00:00";
            timecodeEnd.Text = FileDuration.ToString();

            // If file path is long, trim it
            String FileNameToDisplay = "";
            if (File.Length > 40) FileNameToDisplay = FileRoot + "...\\" + FileName;
            else FileNameToDisplay = File;

            // Update display
            fileNameLabel.Content = "✔️ " + FileNameToDisplay;
            fileNameLabel.ToolTip = File;

            return;
        }

        // Called when clicked on the "Select File" button
        private void ButtonFileOpen_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "videos (*.mp4)|*.mp4", // TODO: introduce enum with supported file types
                RestoreDirectory = true
            };
            var result = fileDialog.ShowDialog();

            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    File = fileDialog.FileName;
                    OpenSelectedFile(File);
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    ResetFilePicker();
                    break;
            }
        }

        // validates values in timecode text boxes
        private void Timecode_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox senderTextBox = (System.Windows.Controls.TextBox)sender;

            // check if a value is a valid TimeSpan
            if (TimeSpan.TryParse(senderTextBox.Text, out _)) 
            {
                // check if the value is shorter or equal to FileDUration
                if (TimeSpan.Parse(senderTextBox.Text) <= FileDuration) 
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
                            senderTextBox.Text = TimeSpan.Parse(senderTextBox.Text).ToString();
                            return;
                        }
                    }
                }
            }

            // if everything failed -- undo editing field
            _ = Dispatcher.BeginInvoke(new Action(() => senderTextBox.Undo()));

            return;
        }


        // Handles output from the transcoding process
        public void ProcessEventHandler(object e, DataReceivedEventArgs outLine)
        {
            System.Console.WriteLine(DateTime.Now + " - " + outLine.Data);
        }


        // main logic - "TRIM VIDEO" button click
        private void ButtonTrimVideo_Click(object sender, RoutedEventArgs e)
        {
            // Make sure the file exists
            if (!System.IO.File.Exists(File))
            {
                // TODO: Replace with a custom error window
                System.Windows.MessageBox.Show("Source video has been moved or deleted!");

                // Reset window and block inputs
                ResetFilePicker();

                return;
            }

            // establish Start and Duration for the needs of the console command
            string Start = timecodeStart.Text;
            TimeSpan Duration = TimeSpan.Parse(timecodeEnd.Text) - TimeSpan.Parse(timecodeStart.Text);

            // CONSOLE COMMAND START
            String FilePath = System.IO.Path.GetDirectoryName(File);
            String FileName = System.IO.Path.GetFileNameWithoutExtension(File);
            String FileExtension = System.IO.Path.GetExtension(File);

            // find a unique filename
            String NewPartialPath = FilePath + "\\" + FileName + "_trim_";
            int UniqueFilenameIndex = 1;
            bool FoundUniqueName = false;
            while (FoundUniqueName == false)
            {
                if (System.IO.File.Exists(NewPartialPath + UniqueFilenameIndex + FileExtension))
                {
                    Console.WriteLine("Name already taken: " + NewPartialPath + UniqueFilenameIndex + FileExtension);
                    UniqueFilenameIndex++;
                }
                else
                {
                    Console.WriteLine("Name available: " + NewPartialPath + UniqueFilenameIndex + FileExtension);
                    FoundUniqueName = true;
                }
            }
            String NewFileName = NewPartialPath + UniqueFilenameIndex + FileExtension;


            // forge the command
            String ConsoleCommand = "/C ffmpeg -ss " + Start + " -i \"" + File + "\" -to " + Duration + " -c copy ";

            // remove audio
            if (removeAudio.IsChecked == true) ConsoleCommand = ConsoleCommand + "-an ";

            ConsoleCommand = ConsoleCommand+"\"" + NewFileName + "\"";
            // CONSOLE COMMAND END


            // Prepare the process to execute the command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                Arguments = ConsoleCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // bind methods to receiving output data from pricess
            process.OutputDataReceived += new DataReceivedEventHandler(ProcessEventHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ProcessEventHandler);

            // Launch the process and start listening for output
            process.StartInfo = startInfo;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();


            // initialize vars that will be used in results window
            string caption;
            string message;

            // Command executed successfully
            if (process.ExitCode == 0) 
            {
                caption = "Everything should be ok";
                message = "Video trimmed using the following command:\n\r\n\r" + ConsoleCommand.Substring(3);
            }
            // Command failed
            else
            {
                caption = "Something went wrong";
                message = "Command that was used:\n\r\n\r" + ConsoleCommand.Substring(3);
            }

            // Displays the MessageBox
            // TODO: Replace with a custom window
            _ = System.Windows.Forms.MessageBox.Show(message, caption, MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

            // Open the folder
            string argument = "/select, \"" + NewFileName + "\"";
            Process.Start("explorer.exe", argument);
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
                // TODO: compare against an enum of supported extensions
                if (Path.GetExtension(draggedFile) == ".mp4")
                {
                    Console.WriteLine("Dragged file recognized!");
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0, 0x77, 0));
                }
                else
                {
                    Console.WriteLine("Dragged file not recognized.");
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x88, 0, 0));
                }
            }
        }


        // Fired whenever an object is dropped over the app window
        private void Window_DragDrop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])(e.Data.GetData("FileDrop", false));
            
            // restore default background
            Background = System.Windows.Media.Brushes.Black;
                        
            // get the first file, ignore the rest
            string droppedFile = files[0];
            Console.WriteLine(Path.GetExtension(droppedFile));
            
            // check if it has a compatible extension
            // TODO: compare against an enum of supported extensions
            if (Path.GetExtension(droppedFile)==".mp4")
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
            Background = System.Windows.Media.Brushes.Black;
        }
    }
}
