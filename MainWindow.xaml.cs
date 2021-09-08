using WMPLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        String File = null;
        TimeSpan FileDuration;


        public MainWindow()
        {
            InitializeComponent();

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            aboutFooter.Content = "Rendeer " + version.Major + "." + version.Minor + "." + version.Build + ".190512";
   
        }

        private void ChangeFieldsStatus(bool NewLockStatus)
        {
            trimVideoButton.IsEnabled = NewLockStatus;
            timecodeStart.IsEnabled = NewLockStatus;
            timecodeEnd.IsEnabled = NewLockStatus;

            return;
        }

        private void ButtonFileOpen_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "videos (*.mp4)|*.mp4",
                RestoreDirectory = true
            };
            var result = fileDialog.ShowDialog();

            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    File = fileDialog.FileName;

                    var player = new WindowsMediaPlayer();
                    var clip = player.newMedia(File);
                    FileDuration = TimeSpan.FromSeconds(clip.duration);
                    timecodeEnd.Text = FileDuration.ToString();

                    ChangeFieldsStatus(true);

                    String FileName = System.IO.Path.GetFileName(File);
                    String FileRoot = System.IO.Path.GetPathRoot(File);

                    String FileNameToDisplay = "";

                    if (File.Length > 40) FileNameToDisplay = FileRoot + "...\\" + FileName;
                    else FileNameToDisplay = File;

                    fileNameLabel.Content = "✔️ " + FileNameToDisplay;
                    fileNameLabel.ToolTip = File;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    ResetFilePicker();
                    break;
            }
        }

        private void ResetFilePicker()
        {
            fileNameLabel.Content = "No file selected";
            fileNameLabel.ToolTip = "No file selected";
            File = null;
            return;
        }


        // validates input into timecode text boxes
        private void Timecode_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox senderTextBox = (System.Windows.Controls.TextBox)sender;

            if (TimeSpan.TryParse(senderTextBox.Text, out _))
            {
                if (TimeSpan.Parse(senderTextBox.Text) <= FileDuration)
                {
                    if (timecodeStart == null | timecodeEnd == null)
                    {
                        // one of the fields has not been initialized, do nothing
                        return;
                    }
                    else
                    {
                        if (TimeSpan.Parse(timecodeStart.Text) < TimeSpan.Parse(timecodeEnd.Text))
                        {
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

        private void ButtonTrimVideo_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Validate file
            if (!System.IO.File.Exists(File))
            {
                System.Windows.MessageBox.Show("Source video has been moved or deleted!");
                ResetFilePicker();
                ChangeFieldsStatus(false);
                return;
            }

            // TODO: Validate timecodes
            String Start = timecodeStart.Text;
            TimeSpan Duration = TimeSpan.Parse(timecodeEnd.Text) - TimeSpan.Parse(timecodeStart.Text);

            // Put together a console command
            String FilePath = System.IO.Path.GetDirectoryName(File);
            String FileName = System.IO.Path.GetFileNameWithoutExtension(File);
            String FileExtension = System.IO.Path.GetExtension(File);

            String NewPartialPath = FilePath + "\\" + FileName + "_trim_";

            bool FoundUniqueName = false;
            int UniqueFilenameIndex = 1;

            while(FoundUniqueName == false)
            {
                if (System.IO.File.Exists(NewPartialPath + UniqueFilenameIndex + FileExtension))
                {
                    Console.WriteLine("File found: " + NewPartialPath + UniqueFilenameIndex + FileExtension);
                    UniqueFilenameIndex++;
                }
                else
                {
                    Console.WriteLine("File not found: " + NewPartialPath + UniqueFilenameIndex + FileExtension);
                    FoundUniqueName = true;
                }
            }

            String NewFileName = NewPartialPath + UniqueFilenameIndex + FileExtension;


            String ConsoleCommand = "/C ffmpeg -ss " + Start + " -i \"" + File + "\" -to " + Duration + " -c copy \"" + NewFileName + "\"";

            

            // Execute console command
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = ConsoleCommand
            };
            process.StartInfo = startInfo;
            process.Start();
            // MessageBox.Show(ConsoleCommand);

            // Initializes the variables to pass to the MessageBox.Show method.
            string caption = "Everything should be ok";
            string message = "Video trimmed using the following command:\n\r\n\r" + ConsoleCommand.Substring(3);
            MessageBoxButtons buttons = MessageBoxButtons.OK;

            // Displays the MessageBox.
            _ = System.Windows.Forms.MessageBox.Show(message, caption, buttons, System.Windows.Forms.MessageBoxIcon.Information);

            string argument = "/select, \"" + NewFileName + "\"";
            Process.Start("explorer.exe", argument);
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        public void ButtonShowAbout_Click(object sender, RoutedEventArgs e)
        {
            string Text;
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            Text = "Rendeer " + version.Major + "." + version.Minor + ".190512";
            System.Windows.Forms.MessageBox.Show(Text);
        }

    }
}
