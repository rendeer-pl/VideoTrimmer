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
            aboutFooter.Content = "Rendeer " + version.Major + "." + version.Minor + ".190510";
            
        }

        private void ButtonFileOpen_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.Filter = "videos (*.mp4)|*.mp4";
            fileDialog.RestoreDirectory = true;
            var result = fileDialog.ShowDialog();

            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    File = fileDialog.FileName;

                    var player = new WindowsMediaPlayer();
                    var clip = player.newMedia(File);
                    FileDuration = TimeSpan.FromSeconds(clip.duration);
                    timecodeEnd.Text = FileDuration.ToString();


                    trimVideoButton.IsEnabled = true;
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
                    fileNameLabel.Content = "No file selected";
                    fileNameLabel.ToolTip = "No file selected";
                    File = null;
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ButtonTrimVideo_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Validate file
            String File = fileNameLabel.ToolTip.ToString();


            // TODO: Validate timecodes
            String Start = timecodeStart.Text;
            TimeSpan Duration = TimeSpan.Parse(timecodeEnd.Text) - TimeSpan.Parse(timecodeStart.Text);

            // Put together a console command
            String FilePath = System.IO.Path.GetDirectoryName(File);
            String FileName = System.IO.Path.GetFileNameWithoutExtension(File);
            String FileExtension = System.IO.Path.GetExtension(File);

            String NewPartialPath = FilePath + "\\" + FileName + "_trim_";

            bool FoundUniqueName = false;
            int UniqueFilenameIndex = 0;

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
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = ConsoleCommand;
            process.StartInfo = startInfo;
            process.Start();
            MessageBox.Show(ConsoleCommand);
            Process.Start("explorer.exe", FilePath);
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        public void ButtonShowAbout_Click(object sender, RoutedEventArgs e)
        {
            string Text;
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            Text = "Rendeer " + version.Major + "." + version.Minor + ".190510";
            MessageBox.Show(Text);
        }
    }
}
